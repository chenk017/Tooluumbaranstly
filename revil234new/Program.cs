using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AssetsTools.NET;
using AssetsTools.NET.Extra;
using AssetsTools.NET.Texture;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using UABEAvalonia;

class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("==============================================");
        Console.WriteLine(" revil234 / Texture2D PNG Roundtrip Oracle");
        Console.WriteLine("==============================================");

        if (args.Length == 0)
        {
            Console.WriteLine();
            Console.WriteLine("Bundle path belum diberikan.");
            Console.WriteLine();
            Console.WriteLine("Usage:");
            Console.WriteLine(
                "dotnet run -- /storage/emulated/0/PROJECT_MOBOX/nama_file.unity3d");
            return;
        }

        string path = args[0];

        Console.WriteLine();
        Console.WriteLine("Input bundle:");
        Console.WriteLine(path);

        if (!File.Exists(path))
        {
            Console.WriteLine();
            Console.WriteLine("Bundle tidak ditemukan:");
            Console.WriteLine(path);
            return;
        }

        AssetsManager am = new AssetsManager();

        Console.WriteLine();
        Console.WriteLine("[1] Loading bundle...");

        BundleFileInstance bundleInst;

        try
        {
            bundleInst = am.LoadBundleFile(path, false);
        }
        catch (Exception ex)
        {
            Console.WriteLine();
            Console.WriteLine("Gagal LoadBundleFile.");
            Console.WriteLine(ex);
            return;
        }

        if (bundleInst == null)
        {
            Console.WriteLine("Gagal LoadBundleFile.");
            return;
        }

        AssetBundleFile bundle = bundleInst.file;

        /*
         * ==========================================================
         * PIPELINE LAMA DIPERTAHANKAN
         * ==========================================================
         *
         * LoadBundleFile
         * -> Unpack
         * -> Read unpacked AssetBundleFile
         * -> cari CAB utama
         * -> manual CAB load
         *
         * Bagian ini sengaja tidak diganti.
         */

        Console.WriteLine("[2] Unpacking bundle...");

        using MemoryStream bundleStream = new MemoryStream();

        try
        {
            bundle.Unpack(new AssetsFileWriter(bundleStream));
        }
        catch (Exception ex)
        {
            Console.WriteLine();
            Console.WriteLine("Gagal Unpack bundle.");
            Console.WriteLine(ex);
            return;
        }

        bundleStream.Position = 0;

        AssetBundleFile unpackedBundle = new AssetBundleFile();

        try
        {
            unpackedBundle.Read(
                new AssetsFileReader(bundleStream));
        }
        catch (Exception ex)
        {
            Console.WriteLine();
            Console.WriteLine("Gagal membaca unpacked bundle.");
            Console.WriteLine(ex);
            return;
        }

        AssetBundleDirectoryInfo[] dirs =
            unpackedBundle.BlockAndDirInfo.DirectoryInfos;

        AssetBundleDirectoryInfo? cabInfo = null;

        foreach (AssetBundleDirectoryInfo dirInfo in dirs)
        {
            Console.WriteLine(
                $"  {dirInfo.Name} | offset={dirInfo.Offset} | size={dirInfo.DecompressedSize}");

            if (dirInfo.Name.StartsWith(
                    "CAB-",
                    StringComparison.OrdinalIgnoreCase)
                &&
                !dirInfo.Name.EndsWith(
                    ".resS",
                    StringComparison.OrdinalIgnoreCase))
            {
                cabInfo = dirInfo;
            }
        }

        if (cabInfo == null)
        {
            Console.WriteLine();
            Console.WriteLine("CAB utama tidak ditemukan.");
            return;
        }

        Console.WriteLine();
        Console.WriteLine("Main CAB:");
        Console.WriteLine($"Name   : {cabInfo.Name}");
        Console.WriteLine($"Offset : {cabInfo.Offset}");
        Console.WriteLine($"Size   : {cabInfo.DecompressedSize}");

        Console.WriteLine();
        Console.WriteLine("[3] Loading AssetsFile...");
        Console.WriteLine("Menggunakan MANUAL CAB LOAD...");

        AssetsFileInstance? assetsFile = null;

        try
        {
            AssetsFileReader cabReader =
                unpackedBundle.DataReader;

            cabReader.Position = cabInfo.Offset;

            byte[] cabBytes =
                cabReader.ReadBytes(
                    checked((int)cabInfo.DecompressedSize));

            Console.WriteLine(
                $"CAB bytes read : {cabBytes.Length}");

            MemoryStream cabStream =
                new MemoryStream(cabBytes);

            assetsFile =
                am.LoadAssetsFile(
                    cabStream,
                    cabInfo.Name,
                    false);
        }
        catch (Exception ex)
        {
            Console.WriteLine();
            Console.WriteLine("MANUAL CAB LOAD ERROR:");
            Console.WriteLine(ex);
            return;
        }

        if (assetsFile == null)
        {
            Console.WriteLine();
            Console.WriteLine("Gagal LoadAssetsFile: result NULL.");
            return;
        }

        Console.WriteLine();
        Console.WriteLine("AssetsFile berhasil dimuat.");
        Console.WriteLine($"AssetsFile path : {assetsFile.path}");
        Console.WriteLine(
            $"ParentBundle    : {(assetsFile.parentBundle != null ? "AVAILABLE" : "NULL")}");
        Console.WriteLine(
            $"Asset count     : {assetsFile.file.AssetInfos.Count}");
        Console.WriteLine(
            $"External count  : {assetsFile.file.Metadata.Externals.Count}");

        Console.WriteLine();
        Console.WriteLine("[4] Creating AssetWorkspace...");

        AssetWorkspace workspace =
            new AssetWorkspace(am, false);

        workspace.LoadAssetsFile(
            assetsFile,
            false);

        workspace.GenerateAssetsFileLookup();

        Console.WriteLine();
        Console.WriteLine("==============================================");
        Console.WriteLine(" ASSET LIST");
        Console.WriteLine("==============================================");

        List<AssetContainer> assets =
            workspace.LoadedAssets.Values.ToList();

        for (int i = 0; i < assets.Count; i++)
        {
            AssetContainer cont = assets[i];

            Console.WriteLine(
                $"[{i,2}] {cont.ClassId,3}  {GetTypeName(cont.ClassId),-25} " +
                $"{cont.PathId,22}  {GetAssetName(workspace, cont)}");
        }

        Console.WriteLine();
        Console.WriteLine("Ketik index Texture2D untuk Inspector.");
        Console.WriteLine("Ketik q untuk keluar.");

        while (true)
        {
            Console.WriteLine();
            Console.Write("> ");

            string? input = Console.ReadLine();

            if (input == null)
                continue;

            input = input.Trim();

            if (input.Equals(
                "q",
                StringComparison.OrdinalIgnoreCase))
            {
                break;
            }

            if (!int.TryParse(
                input,
                out int index))
            {
                Console.WriteLine("Input harus berupa index.");
                continue;
            }

            if (index < 0 || index >= assets.Count)
            {
                Console.WriteLine("Index tidak valid.");
                continue;
            }

            AssetContainer cont = assets[index];

            if (cont.ClassId ==
                (int)AssetClassID.Texture2D)
            {
                DumpTextureInspector(
                    workspace,
                    cont,
                    unpackedBundle);
            }
            else
            {
                Console.WriteLine();
                Console.WriteLine("----------------------------------------------");
                Console.WriteLine("Asset Inspector");
                Console.WriteLine("----------------------------------------------");
                Console.WriteLine($"TypeID : {cont.ClassId}");
                Console.WriteLine($"Type   : {GetTypeName(cont.ClassId)}");
                Console.WriteLine($"PathID : {cont.PathId}");
                Console.WriteLine(
                    $"Name   : {GetAssetName(workspace, cont)}");
                Console.WriteLine();
                Console.WriteLine("Asset ini bukan Texture2D.");
            }
        }

        Console.WriteLine();
        Console.WriteLine("Selesai.");
    }

    static void DumpTextureInspector(
        AssetWorkspace workspace,
        AssetContainer cont,
        AssetBundleFile unpackedBundle)
    {
        Console.WriteLine();
        Console.WriteLine("================================================");
        Console.WriteLine(" Texture2D Inspector + PNG Export");
        Console.WriteLine("================================================");

        Console.WriteLine($"PathID       : {cont.PathId}");

        string name =
            GetAssetName(workspace, cont);

        Console.WriteLine($"Name         : {name}");

        try
        {
            /*
             * ======================================================
             * NORMALISASI KE TextureCore
             * ======================================================
             *
             * Recovery lama membuat TextureFile/BaseField sendiri.
             * Sekarang pembacaan metadata/data dipusatkan di
             * TextureInspector agar resolver .resS tidak diduplikasi.
             *
             * Pipeline data TIDAK berubah:
             *
             * Texture2D
             * -> raw compressed bytes
             * -> managed decoder
             * -> RGBA
             * -> PNG vertical flip
             * -> PNG import
             * -> vertical flip kembali
             * -> byte compare
             */

            TexturePlugin.Texture2DMetadata metadata =
                TexturePlugin.TextureInspector.ReadTexture2D(
                    workspace,
                    cont);

            Console.WriteLine();
            Console.WriteLine("----------------------------------------------");
            Console.WriteLine(" BASIC INFO");
            Console.WriteLine("----------------------------------------------");

            Console.WriteLine($"Width        : {metadata.Width}");
            Console.WriteLine($"Height       : {metadata.Height}");
            Console.WriteLine($"TextureFormat: {metadata.TextureFormat}");
            Console.WriteLine($"Format ID    : {metadata.FormatId}");
            Console.WriteLine($"Mip Count    : {metadata.MipCount}");

            Console.WriteLine();
            Console.WriteLine("----------------------------------------------");
            Console.WriteLine(" STREAM DATA");
            Console.WriteLine("----------------------------------------------");

            Console.WriteLine($"Path         : {metadata.StreamPath}");
            Console.WriteLine($"Offset       : {metadata.StreamOffset}");
            Console.WriteLine($"Size         : {metadata.StreamSize}");

            Console.WriteLine();
            Console.WriteLine("----------------------------------------------");
            Console.WriteLine(" TEXTURE DATA");
            Console.WriteLine("----------------------------------------------");

            byte[]? textureData =
                TexturePlugin.TextureInspector.ReadTextureData(
                    workspace,
                    cont,
                    unpackedBundle);

            if (textureData == null ||
                textureData.Length == 0)
            {
                Console.WriteLine("Texture data kosong.");
                Console.WriteLine("PNG export dihentikan.");
                return;
            }

            Console.WriteLine(
                $"Resolved texture bytes : {textureData.Length}");

            TextureFormat format =
                (TextureFormat)metadata.FormatId;

            Console.WriteLine();
            Console.WriteLine("----------------------------------------------");
            Console.WriteLine(" DECODER");
            Console.WriteLine("----------------------------------------------");

            Console.WriteLine($"Decoder Format: {format}");
            Console.WriteLine($"Input Bytes   : {textureData.Length}");

            bool managedSupported =
                TexturePlugin.TextureEncoderDecoder
                    .IsManagedDecodeSupported(format);

            Console.WriteLine(
                $"Managed decoder: {(managedSupported ? "SUPPORTED" : "NOT SUPPORTED")}");

            if (!managedSupported)
            {
                Console.WriteLine();
                Console.WriteLine(
                    "Format ini belum didukung managed decoder.");
                Console.WriteLine("PNG export dilewati.");
                return;
            }

            Console.WriteLine();
            Console.WriteLine("Decoding...");

            byte[] rgbaData =
                TexturePlugin.TextureEncoderDecoder.Decode(
                    textureData,
                    metadata.Width,
                    metadata.Height,
                    format);

            Console.WriteLine(
                $"RGBA Bytes    : {rgbaData.Length}");

            long expectedRgba =
                (long)metadata.Width *
                (long)metadata.Height *
                4L;

            Console.WriteLine(
                $"Expected RGBA : {expectedRgba}");

            if (rgbaData.Length != expectedRgba)
            {
                Console.WriteLine();
                Console.WriteLine(
                    "ERROR: ukuran RGBA tidak sesuai.");
                Console.WriteLine(
                    "PNG export dihentikan.");
                return;
            }

            /*
             * ======================================================
             * PNG EXPORT
             * ======================================================
             *
             * INI BAGIAN ORACLE.
             *
             * Vertical flip sengaja dipertahankan persis seperti
             * pipeline revil234 yang sebelumnya PASS.
             */

            Console.WriteLine();
            Console.WriteLine("----------------------------------------------");
            Console.WriteLine(" PNG EXPORT");
            Console.WriteLine("----------------------------------------------");

            string outputDirectory =
                "/storage/emulated/0/PROJECT_MOBOX/export_test";

            Directory.CreateDirectory(outputDirectory);

            string safeName = name;

            if (string.IsNullOrWhiteSpace(safeName) ||
                safeName == "-")
            {
                safeName =
                    $"Texture2D_{cont.PathId}";
            }

            foreach (char c in Path.GetInvalidFileNameChars())
            {
                safeName =
                    safeName.Replace(c, '_');
            }

            string outputPath =
                Path.Combine(
                    outputDirectory,
                    safeName + ".png");

            using (Image<Rgba32> image =
                Image.LoadPixelData<Rgba32>(
                    rgbaData,
                    metadata.Width,
                    metadata.Height))
            {
                /*
                 * JANGAN DIUBAH.
                 *
                 * Pipeline lama:
                 * decoded RGBA -> vertical flip -> PNG
                 */
                image.Mutate(
                    x => x.Flip(
                        FlipMode.Vertical));

                using FileStream outputStream =
                    File.Create(outputPath);

                image.Save(
                    outputStream,
                    new SixLabors.ImageSharp.Formats.Png.PngEncoder());
            }

            Console.WriteLine();
            Console.WriteLine("PNG EXPORT SUCCESS");
            Console.WriteLine($"Output: {outputPath}");

            /*
             * ======================================================
             * PNG -> RGBA ROUNDTRIP
             * ======================================================
             *
             * Tetap sama secara algoritmik dengan recovery.
             */

            RunPngRgbaRoundtripTest(
                rgbaData,
                metadata.Width,
                metadata.Height,
                outputPath,
                outputDirectory,
                safeName);
        }
        catch (Exception ex)
        {
            Console.WriteLine();
            Console.WriteLine("Texture2D Inspector ERROR:");
            Console.WriteLine(ex.GetType().Name);
            Console.WriteLine(ex.Message);
            Console.WriteLine();
            Console.WriteLine(ex);
        }

        Console.WriteLine();
        Console.WriteLine("================================================");
        Console.WriteLine(" END Texture2D Inspector");
        Console.WriteLine("================================================");
    }

    static void RunPngRgbaRoundtripTest(
        byte[] originalRgba,
        int width,
        int height,
        string pngPath,
        string outputDirectory,
        string safeName)
    {
        Console.WriteLine();
        Console.WriteLine("================================================");
        Console.WriteLine(" PNG -> RGBA ROUNDTRIP TEST");
        Console.WriteLine("================================================");

        try
        {
            if (originalRgba == null)
                throw new ArgumentNullException(
                    nameof(originalRgba));

            int expectedLength =
                checked(width * height * 4);

            if (originalRgba.Length != expectedLength)
            {
                throw new InvalidOperationException(
                    "Original RGBA size mismatch. Expected " +
                    expectedLength +
                    ", received " +
                    originalRgba.Length +
                    ".");
            }

            string originalRgbaPath =
                Path.Combine(
                    outputDirectory,
                    safeName + "_original_rgba.bin");

            File.WriteAllBytes(
                originalRgbaPath,
                originalRgba);

            Console.WriteLine();
            Console.WriteLine("ORIGINAL RGBA");
            Console.WriteLine(
                $"Size   : {originalRgba.Length} bytes");
            Console.WriteLine(
                $"SHA256 : {GetSha256(originalRgba)}");
            Console.WriteLine(
                $"Output : {originalRgbaPath}");

            if (!File.Exists(pngPath))
            {
                throw new FileNotFoundException(
                    "PNG export file tidak ditemukan.",
                    pngPath);
            }

            byte[] pngData =
                File.ReadAllBytes(pngPath);

            Console.WriteLine();
            Console.WriteLine("PNG INPUT");
            Console.WriteLine($"Size : {pngData.Length} bytes");
            Console.WriteLine($"Path : {pngPath}");

            TexturePlugin.TexturePngImage imported =
                TexturePlugin.TexturePngImporter.Load(
                    pngData);

            Console.WriteLine();
            Console.WriteLine("IMPORTED RGBA");
            Console.WriteLine($"Width  : {imported.Width}");
            Console.WriteLine($"Height : {imported.Height}");
            Console.WriteLine($"Bytes  : {imported.Rgba.Length}");
            Console.WriteLine(
                $"SHA256 : {GetSha256(imported.Rgba)}");

            if (imported.Width != width ||
                imported.Height != height)
            {
                throw new InvalidOperationException(
                    "Imported PNG dimensions berbeda dari texture asli.");
            }

            if (imported.Rgba.Length != expectedLength)
            {
                throw new InvalidOperationException(
                    "Imported RGBA size mismatch. Expected " +
                    expectedLength +
                    ", received " +
                    imported.Rgba.Length +
                    ".");
            }

            string importedRawPath =
                Path.Combine(
                    outputDirectory,
                    safeName + "_imported_rgba_raw.bin");

            File.WriteAllBytes(
                importedRawPath,
                imported.Rgba);

            Console.WriteLine(
                $"Output : {importedRawPath}");

            /*
             * ======================================================
             * NORMALISASI ORIENTASI
             * ======================================================
             *
             * Karena export menggunakan vertical flip,
             * import harus di-flip kembali sebelum comparison.
             *
             * Tidak ada perubahan channel.
             */

            byte[] normalizedRgba =
                FlipRgbaVertical(
                    imported.Rgba,
                    width,
                    height);

            string normalizedPath =
                Path.Combine(
                    outputDirectory,
                    safeName + "_imported_rgba_normalized.bin");

            File.WriteAllBytes(
                normalizedPath,
                normalizedRgba);

            Console.WriteLine();
            Console.WriteLine("NORMALIZED IMPORTED RGBA");
            Console.WriteLine(
                $"Size   : {normalizedRgba.Length} bytes");
            Console.WriteLine(
                $"SHA256 : {GetSha256(normalizedRgba)}");
            Console.WriteLine(
                $"Output : {normalizedPath}");

            RgbaCompareResult result =
                CompareRgba(
                    originalRgba,
                    normalizedRgba);

            Console.WriteLine();
            Console.WriteLine("COMPARE");
            Console.WriteLine(
                $"Original bytes   : {originalRgba.Length}");
            Console.WriteLine(
                $"Imported bytes   : {normalizedRgba.Length}");
            Console.WriteLine(
                $"Different bytes  : {result.DifferentBytes}");
            Console.WriteLine(
                $"Max channel error: {result.MaxChannelError}");
            Console.WriteLine(
                $"Average error    : {result.AverageError:F6}");

            Console.WriteLine();
            Console.WriteLine("CHANNEL DIFFERENCES");
            Console.WriteLine(
                $"R different      : {result.RedDifferent}");
            Console.WriteLine(
                $"G different      : {result.GreenDifferent}");
            Console.WriteLine(
                $"B different      : {result.BlueDifferent}");
            Console.WriteLine(
                $"A different      : {result.AlphaDifferent}");

            Console.WriteLine();

            if (result.DifferentBytes == 0)
            {
                Console.WriteLine(
                    "RGBA ROUNDTRIP TEST: PASS");

                Console.WriteLine(
                    "PNG -> RGBA menghasilkan byte yang identik.");
            }
            else
            {
                Console.WriteLine(
                    "RGBA ROUNDTRIP TEST: DIFFERENT");

                Console.WriteLine(
                    "PNG -> RGBA tidak identik setelah normalisasi Y.");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine();
            Console.WriteLine(
                "RGBA ROUNDTRIP TEST ERROR:");
            Console.WriteLine(ex.GetType().Name);
            Console.WriteLine(ex.Message);
            Console.WriteLine();
            Console.WriteLine(ex);
        }

        Console.WriteLine();
        Console.WriteLine("================================================");
    }

    static string GetSha256(byte[] data)
    {
        byte[] hash =
            System.Security.Cryptography.SHA256.HashData(data);

        return Convert.ToHexString(hash);
    }

    static byte[] FlipRgbaVertical(
        byte[] rgba,
        int width,
        int height)
    {
        if (rgba == null)
            throw new ArgumentNullException(
                nameof(rgba));

        int expectedLength =
            checked(width * height * 4);

        if (rgba.Length != expectedLength)
        {
            throw new ArgumentException(
                "RGBA buffer size tidak sesuai.",
                nameof(rgba));
        }

        byte[] result =
            new byte[rgba.Length];

        int rowBytes =
            checked(width * 4);

        for (int y = 0; y < height; y++)
        {
            int sourceY =
                height - 1 - y;

            Buffer.BlockCopy(
                rgba,
                sourceY * rowBytes,
                result,
                y * rowBytes,
                rowBytes);
        }

        return result;
    }

    sealed class RgbaCompareResult
    {
        public int DifferentBytes;

        public int RedDifferent;
        public int GreenDifferent;
        public int BlueDifferent;
        public int AlphaDifferent;

        public int MaxChannelError;

        public long TotalChannelError;
        public long ComparedChannels;

        public double AverageError
        {
            get
            {
                if (ComparedChannels == 0)
                    return 0.0;

                return
                    (double)TotalChannelError /
                    ComparedChannels;
            }
        }
    }

    static RgbaCompareResult CompareRgba(
        byte[] original,
        byte[] imported)
    {
        if (original == null)
            throw new ArgumentNullException(
                nameof(original));

        if (imported == null)
            throw new ArgumentNullException(
                nameof(imported));

        if (original.Length != imported.Length)
        {
            throw new InvalidOperationException(
                "RGBA buffer lengths berbeda. Original=" +
                original.Length +
                ", Imported=" +
                imported.Length +
                ".");
        }

        if ((original.Length % 4) != 0)
        {
            throw new InvalidOperationException(
                "RGBA buffer bukan kelipatan 4.");
        }

        RgbaCompareResult result =
            new RgbaCompareResult();

        for (int i = 0; i < original.Length; i++)
        {
            int difference =
                Math.Abs(
                    original[i] -
                    imported[i]);

            if (difference != 0)
                result.DifferentBytes++;

            result.TotalChannelError +=
                difference;

            result.ComparedChannels++;

            if (difference >
                result.MaxChannelError)
            {
                result.MaxChannelError =
                    difference;
            }

            switch (i % 4)
            {
                case 0:
                    if (difference != 0)
                        result.RedDifferent++;
                    break;

                case 1:
                    if (difference != 0)
                        result.GreenDifferent++;
                    break;

                case 2:
                    if (difference != 0)
                        result.BlueDifferent++;
                    break;

                case 3:
                    if (difference != 0)
                        result.AlphaDifferent++;
                    break;
            }
        }

        return result;
    }

    static string GetAssetName(
        AssetWorkspace workspace,
        AssetContainer cont)
    {
        try
        {
            AssetTypeValueField? baseField =
                workspace.GetBaseField(cont);

            if (baseField != null)
            {
                AssetTypeValueField nameField =
                    baseField["m_Name"];

                if (nameField != null)
                {
                    string name =
                        nameField.AsString;

                    if (!string.IsNullOrEmpty(name))
                        return name;
                }
            }
        }
        catch
        {
        }

        return "-";
    }

    static string GetTypeName(int classId)
    {
        switch ((AssetClassID)classId)
        {
            case AssetClassID.GameObject:
                return "GameObject";

            case AssetClassID.Transform:
                return "Transform";

            case AssetClassID.Texture2D:
                return "Texture2D";

            case AssetClassID.Mesh:
                return "Mesh";

            case AssetClassID.Material:
                return "Material";

            case AssetClassID.MeshRenderer:
                return "MeshRenderer";

            case AssetClassID.ParticleSystem:
                return "ParticleSystem";

            case AssetClassID.ParticleSystemRenderer:
                return "ParticleSystemRenderer";

            case AssetClassID.MeshFilter:
                return "MeshFilter";

            case AssetClassID.Animation:
                return "Animation";

            case AssetClassID.AnimationClip:
                return "AnimationClip";

            case AssetClassID.MonoBehaviour:
                return "MonoBehaviour";

            case AssetClassID.MonoScript:
                return "MonoScript";

            case AssetClassID.AssetBundle:
                return "AssetBundle";

            default:
                return $"ClassID {classId}";
        }
    }
}

