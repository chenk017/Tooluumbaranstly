using System;
using System.IO;
using Android.App;
using Android.Content;
using Android.Graphics;
using Android.OS;
using Android.Views;
using Android.Widget;
using UABEA.Android.V1.Services;

namespace UABEA.Android.V1;

[Activity(
    Label = "UABEA Android V1",
    MainLauncher = true,
    Exported = true)]
public class MainActivity : Activity
{
    private const int PickFileRequest = 1001;

    private TextView status = null!;
    private LinearLayout assetList = null!;

    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);

        BuildUi();
    }

    private void BuildUi()
    {
        LinearLayout root =
            new LinearLayout(this);

        root.Orientation =
            Orientation.Vertical;

        root.SetPadding(
            30,
            40,
            30,
            30);

        root.SetBackgroundColor(
            Color.Black);

        TextView title =
            new TextView(this);

        title.Text =
            "UABEA Android V1";

        title.TextSize =
            24;

        title.SetTextColor(
            Color.White);

        Button openButton =
            new Button(this);

        openButton.Text =
            "OPEN UNITY3D";

        openButton.Click +=
            delegate
            {
                OpenUnityFile();
            };

        status =
            new TextView(this);

        status.Text =
            "\nNo file selected.";

        status.TextSize =
            16;

        status.SetTextColor(
            Color.White);

        ScrollView scroll =
            new ScrollView(this);

        assetList =
            new LinearLayout(this);

        assetList.Orientation =
            Orientation.Vertical;

        scroll.AddView(assetList);

        root.AddView(title);
        root.AddView(openButton);
        root.AddView(status);
        root.AddView(scroll);

        SetContentView(root);
    }

    private void OpenUnityFile()
    {
        try
        {
            Intent intent =
                new Intent(Intent.ActionOpenDocument);

            intent.AddCategory(
                Intent.CategoryOpenable);

            intent.SetType(
                "*/*");

            StartActivityForResult(
                intent,
                PickFileRequest);
        }
        catch (Exception ex)
        {
            status.Text =
                "\nOPEN ERROR:\n" +
                ex.Message;
        }
    }

    protected override void OnActivityResult(
        int requestCode,
        Result resultCode,
        Intent? data)
    {
        base.OnActivityResult(
            requestCode,
            resultCode,
            data);

        if (requestCode != PickFileRequest)
            return;

        string debug =
            "\n===== PICKER DEBUG =====";

        debug +=
            "\nResultCode = " +
            resultCode;

        debug +=
            "\nData = " +
            (data == null
                ? "NULL"
                : "NOT NULL");

        if (data == null)
        {
            status.Text =
                debug +
                "\n\nNo Intent data returned.";

            return;
        }

        global::Android.Net.Uri? uri =
            data.Data;

        debug +=
            "\nURI = " +
            (uri == null
                ? "NULL"
                : uri.ToString());

        if (uri == null)
        {
            status.Text =
                debug +
                "\n\nNo URI returned.";

            return;
        }

        debug +=
            "\nScheme = " +
            (uri.Scheme ?? "NULL");

        debug +=
            "\nPath = " +
            (uri.Path ?? "NULL");

        try
        {
            string? mime =
                ContentResolver?.GetType(uri);

            debug +=
                "\nMIME = " +
                (mime ?? "NULL");
        }
        catch (Exception ex)
        {
            debug +=
                "\nMIME ERROR = " +
                ex.Message;
        }

        string? displayName = null;

        try
        {
            displayName =
                GetDisplayName(uri);

            debug +=
                "\nDisplayName = " +
                (displayName ?? "NULL");
        }
        catch (Exception ex)
        {
            debug +=
                "\nDisplayName ERROR = " +
                ex.Message;
        }

        bool directPathExists =
            false;

        try
        {
            string? path =
                uri.Path;

            if (!string.IsNullOrEmpty(path))
            {
                directPathExists =
                    File.Exists(path);
            }

            debug +=
                "\nDirectFile.Exists = " +
                directPathExists;
        }
        catch (Exception ex)
        {
            debug +=
                "\nFile.Exists ERROR = " +
                ex.Message;
        }

        if (resultCode != Result.Ok)
        {
            status.Text =
                debug +
                "\n\nRESULT WAS NOT OK.";

            return;
        }

        assetList.RemoveAllViews();

        /*
         * If Android returned a real filesystem path,
         * use it directly.
         */
        if (directPathExists)
        {
            try
            {
                string path =
                    uri.Path!;

                status.Text =
                    debug +
                    "\n\nDirect filesystem path found." +
                    "\nLoading Unity bundle...";

                LoadUnityBundle(path);
            }
            catch (Exception ex)
            {
                status.Text =
                    debug +
                    "\n\nLOAD ERROR:\n" +
                    ex;
            }

            return;
        }

        /*
         * Android normally returns content:// URIs.
         *
         * Copy the selected document into the app cache,
         * then pass the normal filesystem path to the
         * proven UnityBundleLoader.
         */
        try
        {
            status.Text =
                debug +
                "\n\nContent URI detected." +
                "\nCopying file to app cache...";

            string cachePath =
                CopyUriToCache(
                    uri,
                    displayName);

            status.Text =
                debug +
                "\n\nCached file:" +
                "\n" +
                cachePath +
                "\n\nLoading Unity bundle...";

            LoadUnityBundle(cachePath);
        }
        catch (Exception ex)
        {
            status.Text =
                debug +
                "\n\nCACHE / LOAD ERROR:\n" +
                ex;
        }
    }

    private string? GetDisplayName(
        global::Android.Net.Uri uri)
    {
        using global::Android.Database.ICursor? cursor =
            ContentResolver?.Query(
                uri,
                new string[]
                {
                    global::Android.Provider.OpenableColumns.DisplayName
                },
                null,
                null,
                null);

        if (cursor == null)
            return null;

        int nameIndex =
            cursor.GetColumnIndex(
                global::Android.Provider.OpenableColumns.DisplayName);

        if (nameIndex < 0)
            return null;

        if (!cursor.MoveToFirst())
            return null;

        return cursor.GetString(nameIndex);
    }

    private string CopyUriToCache(
        global::Android.Net.Uri uri,
        string? displayName)
    {
        if (ContentResolver == null)
            throw new InvalidOperationException(
                "ContentResolver is null.");

        Stream? input =
            ContentResolver.OpenInputStream(uri);

        if (input == null)
            throw new IOException(
                "Could not open input stream for URI.");

        string safeName =
            string.IsNullOrWhiteSpace(displayName)
                ? "selected.unity3d"
                : displayName;

        foreach (char invalidChar
            in System.IO.Path.GetInvalidFileNameChars())
        {
            safeName =
                safeName.Replace(
                    invalidChar,
                    '_');
        }

        string cacheDirectory =
            CacheDir?.AbsolutePath
            ?? throw new IOException(
                "App cache directory is unavailable.");

        string cachePath =
            System.IO.Path.Combine(
                cacheDirectory,
                safeName);

        using (input)
        using (FileStream output =
            new FileStream(
                cachePath,
                FileMode.Create,
                FileAccess.Write,
                FileShare.None))
        {
            input.CopyTo(output);
        }

        FileInfo info =
            new FileInfo(cachePath);

        if (!info.Exists)
            throw new IOException(
                "Cached file was not created.");

        if (info.Length == 0)
            throw new IOException(
                "Cached file is empty.");

        return cachePath;
    }

    private void LoadUnityBundle(
        string path)
    {
        try
        {
            UnityBundleLoadResult result =
                UnityBundleLoader.Load(path);

            status.Text =
                "\n===== UNITY BUNDLE =====" +
                "\nLoaded: " +
                result.CabName +
                "\nAssets: " +
                result.Assets.Count;

            foreach (UnityAssetInfo asset
                in result.Assets)
            {
                TextView item =
                    new TextView(this);

                item.Text =
                    asset.ToString();

                item.TextSize =
                    14;

                item.SetTextColor(
                    Color.White);

                item.SetPadding(
                    10,
                    10,
                    10,
                    10);

                /*
                 * Make each asset row clickable.
                 */
                item.Clickable = true;

                item.Click +=
                    delegate
                    {
                        status.Text =
                            "\n===== SELECTED ASSET =====" +
                            "\nIndex: " +
                            asset.Index +
                            "\nType: " +
                            asset.TypeName +
                            "\nClassID: " +
                            asset.ClassId +
                            "\nPathID: " +
                            asset.PathId +
                            "\nName: " +
                            (string.IsNullOrEmpty(asset.Name)
                                ? "(unnamed)"
                                : asset.Name);
                    };

                assetList.AddView(item);
            }
        }
        catch (Exception ex)
        {
            status.Text =
                "\nUNITY LOAD ERROR:\n" +
                ex;
        }
    }
}
