using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AssetsTools.NET;
using AssetsTools.NET.Extra;
using UABEAvalonia;

namespace UABEA.Android.V1.Services;

public sealed class UnityAssetInfo
{
    public int Index { get; }
    public int ClassId { get; }
    public long PathId { get; }
    public string TypeName { get; }
    public string Name { get; }

    public UnityAssetInfo(
        int index,
        int classId,
        long pathId,
        string typeName,
        string name)
    {
        Index = index;
        ClassId = classId;
        PathId = pathId;
        TypeName = typeName;
        Name = name;
    }

    public override string ToString()
    {
        return $"[{Index}] {TypeName} | PathID={PathId} | {Name}";
    }
}

public sealed class UnityBundleLoadResult
{
    public string FilePath { get; }
    public string CabName { get; }
    public AssetWorkspace Workspace { get; }
    public AssetBundleFile Bundle { get; }
    public IReadOnlyList<UnityAssetInfo> Assets { get; }

    public UnityBundleLoadResult(
        string filePath,
        string cabName,
        AssetWorkspace workspace,
        AssetBundleFile bundle,
        IReadOnlyList<UnityAssetInfo> assets)
    {
        FilePath = filePath;
        CabName = cabName;
        Workspace = workspace;
        Bundle = bundle;
        Assets = assets;
    }
}

public static class UnityBundleLoader
{
    public static UnityBundleLoadResult Load(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException(
                "File path is empty.",
                nameof(path));

        if (!File.Exists(path))
            throw new FileNotFoundException(
                "Unity bundle file was not found.",
                path);

        AssetsManager am = new AssetsManager();

        BundleFileInstance bundleInst =
            am.LoadBundleFile(path, false);

        if (bundleInst == null)
            throw new InvalidOperationException(
                "AssetsTools.NET failed to load the bundle.");

        AssetBundleFile bundle =
            bundleInst.file;

        if (bundle == null)
            throw new InvalidOperationException(
                "Bundle file is null.");

        MemoryStream bundleStream =
            new MemoryStream();

        bundle.Unpack(
            new AssetsFileWriter(bundleStream));

        bundleStream.Position = 0;

        AssetBundleFile unpackedBundle =
            new AssetBundleFile();

        unpackedBundle.Read(
            new AssetsFileReader(bundleStream));

        AssetBundleDirectoryInfo[] dirs =
            unpackedBundle
                .BlockAndDirInfo
                .DirectoryInfos;

        AssetBundleDirectoryInfo? cabInfo = null;

        foreach (AssetBundleDirectoryInfo dirInfo in dirs)
        {
            if (dirInfo.Name.StartsWith(
                    "CAB-",
                    StringComparison.OrdinalIgnoreCase)
                &&
                !dirInfo.Name.EndsWith(
                    ".resS",
                    StringComparison.OrdinalIgnoreCase))
            {
                cabInfo = dirInfo;
                break;
            }
        }

        if (cabInfo == null)
            throw new InvalidDataException(
                "Main CAB asset file was not found.");

        AssetsFileReader cabReader =
            unpackedBundle.DataReader;

        cabReader.Position =
            cabInfo.Offset;

        byte[] cabBytes =
            cabReader.ReadBytes(
                checked((int)cabInfo.DecompressedSize));

        MemoryStream cabStream =
            new MemoryStream(cabBytes);

        AssetsFileInstance assetsFile =
            am.LoadAssetsFile(
                cabStream,
                cabInfo.Name,
                false);

        if (assetsFile == null)
            throw new InvalidDataException(
                "Failed to load the CAB AssetsFile.");

        AssetWorkspace workspace =
            new AssetWorkspace(
                am,
                false);

        workspace.LoadAssetsFile(
            assetsFile,
            false);

        workspace.GenerateAssetsFileLookup();

        List<UnityAssetInfo> assets =
            new List<UnityAssetInfo>();

        List<AssetContainer> loadedAssets =
            workspace.LoadedAssets.Values.ToList();

        for (int i = 0; i < loadedAssets.Count; i++)
        {
            AssetContainer cont =
                loadedAssets[i];

            string typeName =
                GetTypeName(cont.ClassId);

            string assetName =
                GetAssetName(
                    workspace,
                    cont);

            assets.Add(
                new UnityAssetInfo(
                    i,
                    cont.ClassId,
                    cont.PathId,
                    typeName,
                    assetName));
        }

        return new UnityBundleLoadResult(
            path,
            cabInfo.Name,
            workspace,
            unpackedBundle,
            assets);
    }

    private static string GetTypeName(int classId)
    {
        if (Enum.IsDefined(
                typeof(AssetClassID),
                classId))
        {
            return ((AssetClassID)classId).ToString();
        }

        return $"ClassID_{classId}";
    }

    private static string GetAssetName(
        AssetWorkspace workspace,
        AssetContainer cont)
    {
        try
        {
            AssetTypeTemplateField tempField =
                workspace.GetTemplateField(
                    cont);

            AssetTypeValueField baseField =
                tempField.MakeValue(
                    cont.FileReader,
                    cont.FilePosition);

            if (!baseField.IsDummy)
            {
                AssetTypeValueField? nameField =
                    FindNameField(baseField);

                if (nameField != null &&
                    !nameField.IsDummy)
                {
                    try
                    {
                        string name =
                            nameField.AsString;

                        if (!string.IsNullOrEmpty(name))
                            return name;
                    }
                    catch
                    {
                    }
                }
            }
        }
        catch
        {
        }

        return "(unnamed)";
    }

    private static AssetTypeValueField? FindNameField(
        AssetTypeValueField field)
    {
        try
        {
            AssetTypeValueField name =
                field["m_Name"];

            if (!name.IsDummy)
                return name;
        }
        catch
        {
        }

        return null;
    }
}
