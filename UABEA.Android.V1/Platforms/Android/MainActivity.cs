
using System;
using System.IO;
using Android.App;
using Android.Content;
using Android.Graphics;
using Android.OS;
using Android.Views;
using Android.Widget;
using AssetsTools.NET;
using AssetsTools.NET.Texture;
using UABEA.Android.V1.Services;
using UABEAvalonia;

namespace UABEA.Android.V1
{
    [Activity(
        Label = "UABEA Android V1",
        MainLauncher = true,
        Exported = true)]
    public class MainActivity : Activity
    {
        private const int PickFileRequest = 1001;
        private const int ExportPngRequest = 1002;
        private const int ExportObjRequest = 1003;

        private TextView status = null!;
        private LinearLayout assetList = null!;

        /*
         * Keep the loaded bundle result in memory.
         * This allows us to leave the Asset Browser,
         * open the Inspector, and then return to the
         * same asset list without loading the bundle again.
         */
        private UnityBundleLoadResult? loadedBundle;

        /*
         * Current screen state.
         * false = Asset Browser
         * true  = Inspector
         */
        private bool inspectorVisible;

        /*
         * Currently selected asset.
         */
        private UnityAssetInfo? selectedAsset;

        /*
         * Texture selected for the Android save-document operation.
         */
        private UnityAssetInfo? pendingExportAsset;

        /*
         * Mesh selected for the Android save-document operation.
         */
        private UnityAssetInfo? pendingExportMeshAsset;

        protected override void OnCreate(Bundle? savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            inspectorVisible = false;

            BuildUi();
        }

        private void BuildUi()
        {
            inspectorVisible = false;

            LinearLayout root = new LinearLayout(this);
            root.Orientation = Orientation.Vertical;
            root.SetPadding(30, 40, 30, 30);
            root.SetBackgroundColor(Color.Black);

            TextView title = new TextView(this);
            title.Text = "UABEA Android V1";
            title.TextSize = 24;
            title.SetTextColor(Color.White);

            Button openButton = new Button(this);
            openButton.Text = "OPEN UNITY3D";
            openButton.Click += delegate { OpenUnityFile(); };

            status = new TextView(this);
            status.Text = "\nNo file selected.";
            status.TextSize = 16;
            status.SetTextColor(Color.White);

            ScrollView scroll = new ScrollView(this);
            assetList = new LinearLayout(this);
            assetList.Orientation = Orientation.Vertical;

            scroll.AddView(assetList);

            root.AddView(title);
            root.AddView(openButton);
            root.AddView(status);
            root.AddView(scroll);

            SetContentView(root);

            if (loadedBundle != null)
            {
                ShowAssetBrowser();
            }
        }

        private void OpenUnityFile()
        {
            try
            {
                Intent intent = new Intent(Intent.ActionOpenDocument);
                intent.AddCategory(Intent.CategoryOpenable);
                intent.SetType("*/*");

                StartActivityForResult(intent, PickFileRequest);
            }
            catch (Exception ex)
            {
                status.Text = "\nOPEN ERROR:\n" + ex.Message;
            }
        }

        protected override void OnActivityResult(
            int requestCode,
            Result resultCode,
            Intent? data)
        {
            base.OnActivityResult(requestCode, resultCode, data);

            if (requestCode == ExportPngRequest)
            {
                HandleExportPngResult(resultCode, data);
                return;
            }

            if (requestCode == ExportObjRequest)
            {
                HandleExportObjResult(resultCode, data);
                return;
            }

            if (requestCode != PickFileRequest)
                return;

            string debug = "\n===== PICKER DEBUG =====";
            debug += "\nResultCode = " + resultCode;
            debug += "\nData = " + (data == null ? "NULL" : "NOT NULL");

            if (data == null)
            {
                status.Text = debug + "\n\nNo Intent data returned.";
                return;
            }

            global::Android.Net.Uri? uri = data.Data;
            debug += "\nURI = " + (uri == null ? "NULL" : uri.ToString());

            if (uri == null)
            {
                status.Text = debug + "\n\nNo URI returned.";
                return;
            }

            debug += "\nScheme = " + (uri.Scheme ?? "NULL");
            debug += "\nPath = " + (uri.Path ?? "NULL");

            try
            {
                string? mime = ContentResolver?.GetType(uri);
                debug += "\nMIME = " + (mime ?? "NULL");
            }
            catch (Exception ex)
            {
                debug += "\nMIME ERROR = " + ex.Message;
            }

            string? displayName = null;
            try
            {
                displayName = GetDisplayName(uri);
                debug += "\nDisplayName = " + (displayName ?? "NULL");
            }
            catch (Exception ex)
            {
                debug += "\nDisplayName ERROR = " + ex.Message;
            }

            bool directPathExists = false;
            try
            {
                string? path = uri.Path;
                if (!string.IsNullOrEmpty(path))
                {
                    directPathExists = File.Exists(path);
                }

                debug += "\nDirectFile.Exists = " + directPathExists;
            }
            catch (Exception ex)
            {
                debug += "\nFile.Exists ERROR = " + ex.Message;
            }

            if (resultCode != Result.Ok)
            {
                status.Text = debug + "\n\nRESULT WAS NOT OK.";
                return;
            }

            assetList.RemoveAllViews();

            loadedBundle = null;
            selectedAsset = null;

            if (directPathExists)
            {
                try
                {
                    string path = uri.Path!;
                    status.Text = debug + "\n\nDirect filesystem path found.\nLoading Unity bundle...";
                    LoadUnityBundle(path);
                }
                catch (Exception ex)
                {
                    status.Text = debug + "\n\nLOAD ERROR:\n" + ex;
                }

                return;
            }

            try
            {
                status.Text = debug + "\n\nContent URI detected.\nCopying file to app cache...";
                string cachePath = CopyUriToCache(uri, displayName);
                status.Text = debug + "\n\nCached file:\n" + cachePath + "\n\nLoading Unity bundle...";

                LoadUnityBundle(cachePath);
            }
            catch (Exception ex)
            {
                status.Text = debug + "\n\nCACHE / LOAD ERROR:\n" + ex;
            }
        }

        private string? GetDisplayName(global::Android.Net.Uri uri)
        {
            using global::Android.Database.ICursor? cursor =
                ContentResolver?.Query(
                    uri,
                    new string[] { global::Android.Provider.OpenableColumns.DisplayName },
                    null,
                    null,
                    null);

            if (cursor == null) return null;

            int nameIndex = cursor.GetColumnIndex(global::Android.Provider.OpenableColumns.DisplayName);
            if (nameIndex < 0) return null;
            if (!cursor.MoveToFirst()) return null;

            return cursor.GetString(nameIndex);
        }

        private string CopyUriToCache(global::Android.Net.Uri uri, string? displayName)
        {
            if (ContentResolver == null)
                throw new InvalidOperationException("ContentResolver is null.");

            Stream? input = ContentResolver.OpenInputStream(uri);
            if (input == null)
                throw new IOException("Could not open input stream for URI.");

            string safeName = string.IsNullOrWhiteSpace(displayName) ? "selected.unity3d" : displayName;

            foreach (char invalidChar in System.IO.Path.GetInvalidFileNameChars())
            {
                safeName = safeName.Replace(invalidChar, '_');
            }

            string cacheDirectory = CacheDir?.AbsolutePath
                ?? throw new IOException("App cache directory is unavailable.");

            string cachePath = System.IO.Path.Combine(cacheDirectory, safeName);

            using (input)
            using (FileStream output = new FileStream(cachePath, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                input.CopyTo(output);
            }

            FileInfo info = new FileInfo(cachePath);
            if (!info.Exists) throw new IOException("Cached file was not created.");
            if (info.Length == 0) throw new IOException("Cached file is empty.");

            return cachePath;
        }

        private void LoadUnityBundle(string path)
        {
            try
            {
                UnityBundleLoadResult result = UnityBundleLoader.Load(path);
                loadedBundle = result;
                selectedAsset = null;

                ShowAssetBrowser();
            }
            catch (Exception ex)
            {
                status.Text = "\nUNITY LOAD ERROR:\n" + ex;
            }
        }

        private void ShowAssetBrowser()
        {
            inspectorVisible = false;

            if (loadedBundle == null)
            {
                BuildUi();
                return;
            }

            LinearLayout root = new LinearLayout(this);
            root.Orientation = Orientation.Vertical;
            root.SetPadding(30, 40, 30, 30);
            root.SetBackgroundColor(Color.Black);

            TextView title = new TextView(this);
            title.Text = "UABEA Android V1";
            title.TextSize = 24;
            title.SetTextColor(Color.White);

            Button openButton = new Button(this);
            openButton.Text = "OPEN UNITY3D";
            openButton.Click += delegate { OpenUnityFile(); };

            status = new TextView(this);
            status.Text = "\n===== UNITY BUNDLE =====" +
                          "\nLoaded: " + loadedBundle.CabName +
                          "\nAssets: " + loadedBundle.Assets.Count;
            status.TextSize = 16;
            status.SetTextColor(Color.White);

            ScrollView scroll = new ScrollView(this);
            assetList = new LinearLayout(this);
            assetList.Orientation = Orientation.Vertical;

            scroll.AddView(assetList);

            root.AddView(title);
            root.AddView(openButton);
            root.AddView(status);
            root.AddView(scroll);

            SetContentView(root);

            foreach (UnityAssetInfo asset in loadedBundle.Assets)
            {
                TextView item = new TextView(this);
                item.Text = asset.ToString();
                item.TextSize = 14;
                item.SetTextColor(Color.White);
                item.SetPadding(10, 10, 10, 10);
                item.Clickable = true;

                item.Click += delegate { ShowInspector(asset); };

                assetList.AddView(item);
            }
        }

        private void ShowInspector(UnityAssetInfo asset)
        {
            selectedAsset = asset;
            inspectorVisible = true;

            LinearLayout root = new LinearLayout(this);
            root.Orientation = Orientation.Vertical;
            root.SetPadding(30, 40, 30, 30);
            root.SetBackgroundColor(Color.Black);

            Button backButton = new Button(this);
            backButton.Text = "← BACK";
            backButton.Click += delegate { NavigateBack(); };

            // ==========================================
            // CONFIGURASI TOMBOL EXPORT DINAMIS
            // ==========================================
            Button exportButton = new Button(this);
            exportButton.TextSize = 16;

            bool hasExportButton = false;

            if (asset.ClassId == 28) // Texture2D
            {
                exportButton.Text = "EXPORT PNG";
                exportButton.Click += delegate { StartTexturePngExport(asset); };
                hasExportButton = true;
            }
            else if (asset.ClassId == 43) // Mesh
            {
                exportButton.Text = "EXPORT OBJ";
                exportButton.Click += delegate { StartMeshObjExport(asset); };
                hasExportButton = true;
            }

            TextView title = new TextView(this);
            title.Text = "INSPECTOR";
            title.TextSize = 24;
            title.SetTextColor(Color.White);

            TextView info = new TextView(this);

            string extraInfo = "";
            if (asset.ClassId == 28)
            {
                extraInfo = GetTexture2DInfo(asset) + GetTexture2DDataInfo(asset);
            }
            else if (asset.ClassId == 43)
            {
                extraInfo = "\n\n===== MESH INFO =====" +
                            "\nType: 3D Geometry Data" +
                            "\nStatus: Ready for .obj export";
            }
            else
            {
                extraInfo = GetAssetDumpText(asset);
            }

            info.Text =
                "\n===== SELECTED ASSET =====" +
                "\n\nIndex: " + asset.Index +
                "\nType: " + asset.TypeName +
                "\nClassID: " + asset.ClassId +
                "\nPathID: " + asset.PathId +
                "\nName: " + (string.IsNullOrEmpty(asset.Name) ? "(unnamed)" : asset.Name) +
                extraInfo;

            info.TextSize = 16;
            info.SetTextColor(Color.White);

            ImageView preview = new ImageView(this);
            preview.SetBackgroundColor(Color.DarkGray);
            preview.SetScaleType(ImageView.ScaleType.FitCenter);
            preview.SetAdjustViewBounds(true);

            LinearLayout.LayoutParams previewParams =
                new LinearLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, 500);
            preview.LayoutParameters = previewParams;

            TextView previewStatus = new TextView(this);
            previewStatus.TextSize = 14;
            previewStatus.SetTextColor(Color.LightGray);

            if (asset.ClassId == 28)
            {
                try
                {
                    Bitmap? bitmap = BuildTexturePreview(asset);
                    if (bitmap != null)
                    {
                        preview.SetImageBitmap(bitmap);
                        previewStatus.Text = "\n===== TEXTURE PREVIEW =====\nManaged decode: OK";
                    }
                    else
                    {
                        previewStatus.Text = "\n===== TEXTURE PREVIEW =====\nDecode returned no image.";
                    }
                }
                catch (Exception ex)
                {
                    previewStatus.Text = "\n===== TEXTURE PREVIEW =====\nDECODE ERROR:\n" + ex.Message;
                }
            }

            ScrollView scroll = new ScrollView(this);
            scroll.AddView(info);

            // ==========================================
            // RENDER TAMPILAN KE ROOT LAYOUT
            // ==========================================
            root.AddView(backButton);

            if (hasExportButton)
            {
                root.AddView(exportButton);
            }

            root.AddView(title);

            if (asset.ClassId == 28)
            {
                root.AddView(previewStatus);
                root.AddView(preview);
            }

            root.AddView(scroll);

            SetContentView(root);
        }

        private void StartTexturePngExport(UnityAssetInfo asset)
        {
            if (asset.ClassId != 28) return;

            pendingExportAsset = asset;
            string fileName = string.IsNullOrWhiteSpace(asset.Name) ? "texture.png" : asset.Name + ".png";

            foreach (char invalidChar in System.IO.Path.GetInvalidFileNameChars())
            {
                fileName = fileName.Replace(invalidChar, '_');
            }

            try
            {
                Intent intent = new Intent(Intent.ActionCreateDocument);
                intent.AddCategory(Intent.CategoryOpenable);
                intent.SetType("image/png");
                intent.PutExtra(Intent.ExtraTitle, fileName);

                StartActivityForResult(intent, ExportPngRequest);
            }
            catch (Exception ex)
            {
                pendingExportAsset = null;
                status.Text = "\nPNG EXPORT ERROR:\n" + ex;
            }
        }

        private void HandleExportPngResult(Result resultCode, Intent? data)
        {
            UnityAssetInfo? asset = pendingExportAsset;
            pendingExportAsset = null;

            if (resultCode != Result.Ok || data == null || data.Data == null || asset == null) return;

            global::Android.Net.Uri uri = data.Data;

            try
            {
                ExportTexturePng(asset, uri);
                status.Text = "\n===== PNG EXPORT =====" +
                              "\nExport: OK" +
                              "\nAsset: " + (string.IsNullOrEmpty(asset.Name) ? "(unnamed)" : asset.Name);
            }
            catch (Exception ex)
            {
                status.Text = "\n===== PNG EXPORT =====" +
                              "\nEXPORT ERROR:\n" + ex;
            }
        }

        private void ExportTexturePng(UnityAssetInfo asset, global::Android.Net.Uri uri)
        {
            if (asset.ClassId != 28)
                throw new InvalidOperationException("Selected asset is not Texture2D.");

            if (ContentResolver == null)
                throw new InvalidOperationException("ContentResolver is null.");

            Bitmap? bitmap = BuildTexturePreview(asset);
            if (bitmap == null)
                throw new InvalidOperationException("Could not build texture bitmap.");

            try
            {
                Stream? output = ContentResolver.OpenOutputStream(uri);
                if (output == null)
                    throw new IOException("Could not open destination output stream.");

                using (output)
                {
                    bool success = bitmap.Compress(Bitmap.CompressFormat.Png, 100, output);
                    if (!success)
                        throw new IOException("Android Bitmap PNG compression failed.");
                }
            }
            finally
            {
                bitmap.Dispose();
            }
        }

        private void StartMeshObjExport(UnityAssetInfo asset)
        {
            if (asset.ClassId != 43) return;

            pendingExportMeshAsset = asset;
            string fileName = string.IsNullOrWhiteSpace(asset.Name) ? "mesh.obj" : asset.Name + ".obj";

            foreach (char invalidChar in System.IO.Path.GetInvalidFileNameChars())
            {
                fileName = fileName.Replace(invalidChar, '_');
            }

            try
            {
                Intent intent = new Intent(Intent.ActionCreateDocument);
                intent.AddCategory(Intent.CategoryOpenable);
                intent.SetType("text/plain");
                intent.PutExtra(Intent.ExtraTitle, fileName);

                StartActivityForResult(intent, ExportObjRequest);
            }
            catch (Exception ex)
            {
                pendingExportMeshAsset = null;
                status.Text = "\nOBJ EXPORT ERROR:\n" + ex.Message;
            }
        }

        private void HandleExportObjResult(Result resultCode, Intent? data)
        {
            UnityAssetInfo? asset = pendingExportMeshAsset;
            pendingExportMeshAsset = null;

            if (resultCode != Result.Ok || data == null || data.Data == null || asset == null) return;

            global::Android.Net.Uri uri = data.Data;

            try
            {
                ExportMeshToUri(asset, uri);
                status.Text = "\n===== OBJ EXPORT =====" +
                              "\nExport: OK" +
                              "\nAsset: " + (string.IsNullOrEmpty(asset.Name) ? "(unnamed)" : asset.Name);
            }
            catch (Exception ex)
            {
                status.Text = "\n===== OBJ EXPORT =====" +
                              "\nEXPORT ERROR:\n" + ex;
            }
        }

        private void ExportMeshToUri(UnityAssetInfo asset, global::Android.Net.Uri uri)
        {
            if (loadedBundle == null)
                throw new InvalidOperationException("Bundle is not loaded.");

            AssetContainer? cont = null;
            foreach (AssetContainer candidate in loadedBundle.Workspace.LoadedAssets.Values)
            {
                if (candidate.ClassId == asset.ClassId && candidate.PathId == asset.PathId)
                {
                    cont = candidate;
                    break;
                }
            }

            if (cont == null)
                throw new InvalidOperationException("AssetContainer not found.");

            string tempPath = Path.Combine(CacheDir!.AbsolutePath, "temp_mesh.obj");

            try
            {
                AssetTypeTemplateField tempField = loadedBundle.Workspace.GetTemplateField(cont);
                AssetTypeValueField baseField = tempField.MakeValue(cont.FileReader, cont.FilePosition);

                // Memanggil MeshObjExporter dari folder Services
                MeshObjExporter.Export(baseField, tempPath);

                using (Stream? output = ContentResolver?.OpenOutputStream(uri))
                {
                    if (output == null)
                        throw new IOException("Could not open destination output stream.");

                    using (FileStream input = new FileStream(tempPath, FileMode.Open, FileAccess.Read))
                    {
                        input.CopyTo(output);
                    }
                }
            }
            finally
            {
                if (File.Exists(tempPath))
                    File.Delete(tempPath);
            }
        }

        private string GetAssetDumpText(UnityAssetInfo asset)
        {
            if (loadedBundle == null) return "\n\n===== TEXT DUMP =====\nBundle is not loaded.";

            AssetContainer? cont = null;
            foreach (AssetContainer candidate in loadedBundle.Workspace.LoadedAssets.Values)
            {
                if (candidate.ClassId == asset.ClassId && candidate.PathId == asset.PathId)
                {
                    cont = candidate;
                    break;
                }
            }

            if (cont == null) return "\n\n===== TEXT DUMP =====\nAssetContainer not found.";

            try
            {
                AssetTypeTemplateField tempField = loadedBundle.Workspace.GetTemplateField(cont);
                AssetTypeValueField baseField = tempField.MakeValue(cont.FileReader, cont.FilePosition);

                if (baseField.IsDummy) return "\n\n===== TEXT DUMP =====\nBaseField is dummy.";

                string dumpContent = FormatValueField(baseField, 0);

                if (dumpContent.Length > 3000)
                {
                    dumpContent = dumpContent.Substring(0, 3000) + "\n\n... (truncated)";
                }

                return "\n\n===== TEXT DUMP =====\n" + dumpContent;
            }
            catch (Exception ex)
            {
                return "\n\n===== TEXT DUMP =====\nREAD ERROR:\n" + ex.Message;
            }
        }

        private string FormatValueField(AssetTypeValueField field, int indent)
        {
            string indentation = new string(' ', indent * 2);
            string result = $"{indentation}{field.FieldName} ({field.TypeName}): ";

            try
            {
                if (!field.HasChildren)
                {
                    result += field.AsString + "\n";
                }
                else
                {
                    result += "\n";
                    foreach (var child in field.Children)
                    {
                        result += FormatValueField(child, indent + 1);
                    }
                }
            }
            catch
            {
                result += "(value error)\n";
            }

            return result;
        }

        private string GetTexture2DInfo(UnityAssetInfo asset)
        {
            if (asset.ClassId != 28) return "";
            if (loadedBundle == null) return "\n\n===== TEXTURE2D =====\nBundle is not loaded.";

            AssetContainer? cont = null;
            foreach (AssetContainer candidate in loadedBundle.Workspace.LoadedAssets.Values)
            {
                if (candidate.ClassId == asset.ClassId && candidate.PathId == asset.PathId)
                {
                    cont = candidate;
                    break;
                }
            }

            if (cont == null) return "\n\n===== TEXTURE2D =====\nAssetContainer not found.";

            try
            {
                TexturePlugin.Texture2DMetadata metadata =
                    TexturePlugin.TextureInspector.ReadTexture2D(loadedBundle.Workspace, cont);

                return "\n\n===== TEXTURE2D =====" +
                       "\nWidth: " + metadata.Width +
                       "\nHeight: " + metadata.Height +
                       "\nTextureFormat: " + metadata.TextureFormat +
                       "\nFormat ID: " + metadata.FormatId +
                       "\nMipCount: " + metadata.MipCount +
                       "\nStream Path: " + metadata.StreamPath +
                       "\nStream Offset: " + metadata.StreamOffset +
                       "\nStream Size: " + metadata.StreamSize;
            }
            catch (Exception ex)
            {
                return "\n\n===== TEXTURE2D =====\nREAD ERROR:\n" + ex.Message;
            }
        }

        private string GetTexture2DDataInfo(UnityAssetInfo asset)
        {
            if (asset.ClassId != 28) return "";
            if (loadedBundle == null) return "\n\n===== TEXTURE DATA =====\nBundle is not loaded.";

            AssetContainer? cont = null;
            foreach (AssetContainer candidate in loadedBundle.Workspace.LoadedAssets.Values)
            {
                if (candidate.ClassId == asset.ClassId && candidate.PathId == asset.PathId)
                {
                    cont = candidate;
                    break;
                }
            }

            if (cont == null) return "\n\n===== TEXTURE DATA =====\nAssetContainer not found.";

            try
            {
                byte[] textureData =
                    TexturePlugin.TextureInspector.ReadTextureData(
                        loadedBundle.Workspace, cont, loadedBundle.Bundle);

                return "\n\n===== TEXTURE DATA =====\nResolved Bytes: " + textureData.Length;
            }
            catch (Exception ex)
            {
                return "\n\n===== TEXTURE DATA =====\nREAD ERROR:\n" + ex.Message;
            }
        }

        private Bitmap? BuildTexturePreview(UnityAssetInfo asset)
        {
            if (asset.ClassId != 28) return null;
            if (loadedBundle == null) throw new InvalidOperationException("Bundle is not loaded.");

            AssetContainer? cont = null;
            foreach (AssetContainer candidate in loadedBundle.Workspace.LoadedAssets.Values)
            {
                if (candidate.ClassId == asset.ClassId && candidate.PathId == asset.PathId)
                {
                    cont = candidate;
                    break;
                }
            }

            if (cont == null) throw new InvalidOperationException("AssetContainer not found.");

            TexturePlugin.Texture2DMetadata metadata =
                TexturePlugin.TextureInspector.ReadTexture2D(loadedBundle.Workspace, cont);

            byte[] textureData =
                TexturePlugin.TextureInspector.ReadTextureData(
                    loadedBundle.Workspace, cont, loadedBundle.Bundle);

            TextureFormat format = (TextureFormat)metadata.FormatId;

            if (!TexturePlugin.TextureEncoderDecoder.IsManagedDecodeSupported(format))
            {
                throw new InvalidOperationException(
                    "Managed decoder does not support TextureFormat " + metadata.TextureFormat +
                    " (" + metadata.FormatId + ").");
            }

            byte[]? rgba = TexturePlugin.TextureEncoderDecoder.Decode(
                textureData, metadata.Width, metadata.Height, format);

            if (rgba == null) throw new InvalidOperationException("Managed texture decoder returned null.");

            int expectedLength = checked(metadata.Width * metadata.Height * 4);
            if (rgba.Length != expectedLength)
            {
                throw new InvalidOperationException(
                    "Decoded RGBA size mismatch. Expected " + expectedLength +
                    " bytes, received " + rgba.Length + ".");
            }

            int[] pixels = new int[metadata.Width * metadata.Height];
            int pixelIndex = 0;

            for (int i = 0; i < rgba.Length; i += 4)
            {
                int r = rgba[i];
                int g = rgba[i + 1];
                int b = rgba[i + 2];
                int a = rgba[i + 3];

                pixels[pixelIndex] = (a << 24) | (r << 16) | (g << 8) | b;
                pixelIndex++;
            }

            Bitmap bitmap = Bitmap.CreateBitmap(
                metadata.Width, metadata.Height, Bitmap.Config.Argb8888);

            bitmap.SetPixels(pixels, 0, metadata.Width, 0, 0, metadata.Width, metadata.Height);

            return bitmap;
        }

        private void NavigateBack()
        {
            if (inspectorVisible)
            {
                ShowAssetBrowser();
                return;
            }

            Finish();
        }

        public override void OnBackPressed()
        {
            NavigateBack();
        }
    }
}

