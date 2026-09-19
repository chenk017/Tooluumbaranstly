using AssetsTools.NET;
using AssetsTools.NET.Extra;
using AssetsTools.NET.Texture;
using System;
using System.IO;
using System.Linq;
using UABEAvalonia;

namespace TexturePlugin
{
    public sealed class Texture2DMetadata
    {
        public int Width { get; set; }
        public int Height { get; set; }
        public string TextureFormat { get; set; } = "";
        public int FormatId { get; set; }
        public int MipCount { get; set; }
        public string StreamPath { get; set; } = "";
        public ulong StreamOffset { get; set; }
        public ulong StreamSize { get; set; }
    }

    public static class TextureInspector
    {
        public static Texture2DMetadata ReadTexture2D(
            AssetWorkspace workspace,
            AssetContainer container)
        {
            if (workspace == null)
                throw new ArgumentNullException(nameof(workspace));

            if (container == null)
                throw new ArgumentNullException(nameof(container));

            AssetTypeValueField texBaseField =
                GetByteArrayTexture(workspace, container);

            if (texBaseField == null)
                throw new Exception(
                    "Texture base field is null.");

            TextureFile texFile =
                TextureFile.ReadTextureFile(texBaseField);

            TextureFormat format =
                (TextureFormat)texFile.m_TextureFormat;

            TextureFile.StreamingInfo streamInfo =
                texFile.m_StreamData;

            return new Texture2DMetadata
            {
                Width = texFile.m_Width,
                Height = texFile.m_Height,
                TextureFormat = format.ToString(),
                FormatId = texFile.m_TextureFormat,
                MipCount = texFile.m_MipCount,
                StreamPath =
                    string.IsNullOrEmpty(streamInfo.path)
                        ? "(none)"
                        : streamInfo.path,
                StreamOffset = streamInfo.offset,
                StreamSize = streamInfo.size
            };
        }

public static byte[] ReadTextureData(
    AssetWorkspace workspace,
    AssetContainer container,
    AssetBundleFile bundle)
{
    if (workspace == null)
        throw new ArgumentNullException(
            nameof(workspace));

    if (container == null)
        throw new ArgumentNullException(
            nameof(container));

    if (bundle == null)
        throw new ArgumentNullException(
            nameof(bundle));

    AssetTypeValueField texBaseField =
        GetByteArrayTexture(
            workspace,
            container);

    if (texBaseField == null)
        throw new Exception(
            "Texture base field is null.");

    TextureFile texFile =
        TextureFile.ReadTextureFile(
            texBaseField);

    if (texFile.pictureData != null &&
        texFile.pictureData.Length > 0)
    {
        return texFile.pictureData;
    }

    TextureFile.StreamingInfo stream =
        texFile.m_StreamData;

    if (stream.size == 0)
        throw new Exception(
            "Texture has no inline data and stream size is 0.");

    if (string.IsNullOrEmpty(stream.path))
        throw new Exception(
            "Texture has stream data but stream path is empty.");

    string searchPath =
        stream.path;

    if (searchPath.StartsWith(
            "archive:/",
            StringComparison.OrdinalIgnoreCase))
    {
        searchPath =
            searchPath.Substring(9);
    }

    searchPath =
        Path.GetFileName(
            searchPath);

    AssetBundleDirectoryInfo? streamInfo =
        null;

    foreach (
        AssetBundleDirectoryInfo dirInfo
        in bundle
            .BlockAndDirInfo
            .DirectoryInfos)
    {
        if (string.Equals(
                dirInfo.Name,
                searchPath,
                StringComparison.OrdinalIgnoreCase))
        {
            streamInfo =
                dirInfo;

            break;
        }
    }

    if (streamInfo == null)
        throw new Exception(
            ".resS file was not found in bundle DirectoryInfos: " +
            searchPath);

    ulong endPosition =
        checked(
            stream.offset +
            stream.size);

    if (stream.offset >
        (ulong)long.MaxValue)
    {
        throw new Exception(
            "Texture stream offset is too large.");
    }

    if (streamInfo.DecompressedSize < 0)
    {
        throw new Exception(
            ".resS decompressed size is negative.");
    }

    if (endPosition >
        (ulong)streamInfo.DecompressedSize)
    {
        throw new Exception(
            "Texture range is outside the .resS data.");
    }

    if (stream.size >
        (ulong)int.MaxValue)
    {
        throw new Exception(
            "Texture stream size is too large.");
    }

    long absolutePosition =
        checked(
            streamInfo.Offset +
            checked(
                (long)stream.offset));

    AssetsFileReader reader =
        bundle.DataReader;

    reader.Position =
        absolutePosition;

    byte[] resolvedData =
        reader.ReadBytes(
            checked(
                (int)stream.size));

    if (resolvedData.Length == 0)
        throw new Exception(
            ".resS returned 0 bytes.");

    if ((ulong)resolvedData.Length !=
        stream.size)
    {
        throw new Exception(
            ".resS byte count mismatch. Expected " +
            stream.size +
            ", received " +
            resolvedData.Length +
            ".");
    }

    return resolvedData;
}

        private static AssetTypeValueField GetByteArrayTexture(
            AssetWorkspace workspace,
            AssetContainer tex)
        {
            AssetTypeTemplateField textureTemp =
                workspace.GetTemplateField(tex);

            AssetTypeTemplateField imageData =
                textureTemp.Children
                    .FirstOrDefault(
                        f => f.Name == "image data");

            if (imageData == null)
                throw new Exception(
                    "Texture2D 'image data' field not found.");

            imageData.ValueType =
                AssetValueType.ByteArray;

            AssetTypeTemplateField platformBlob =
                textureTemp.Children
                    .FirstOrDefault(
                        f => f.Name == "m_PlatformBlob");

            if (platformBlob != null &&
                platformBlob.Children.Count > 0)
            {
                AssetTypeTemplateField platformBlobArray =
                    platformBlob.Children[0];

                platformBlobArray.ValueType =
                    AssetValueType.ByteArray;
            }

            AssetTypeValueField baseField =
                textureTemp.MakeValue(
                    tex.FileReader,
                    tex.FilePosition);

            return baseField;
        }
    }
}
