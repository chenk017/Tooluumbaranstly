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
