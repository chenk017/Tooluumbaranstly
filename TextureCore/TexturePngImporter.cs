using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System;
using System.IO;

namespace TexturePlugin
{
    public sealed class TexturePngImage
    {
        public int Width { get; }
        public int Height { get; }
        public byte[] Rgba { get; }

        public TexturePngImage(int width, int height, byte[] rgba)
        {
            Width = width;
            Height = height;
            Rgba = rgba;
        }
    }

    public static class TexturePngImporter
    {
        public static TexturePngImage Load(byte[] pngData)
        {
            if (pngData == null)
                throw new ArgumentNullException(nameof(pngData));

            if (pngData.Length == 0)
                throw new InvalidDataException("PNG data is empty.");

            using Image<Rgba32> image =
                Image.Load<Rgba32>(pngData);

            int width = image.Width;
            int height = image.Height;

            if (width <= 0 || height <= 0)
                throw new InvalidDataException(
                    "PNG has invalid dimensions.");

            int pixelCount =
                checked(width * height);

            byte[] rgba =
                new byte[checked(pixelCount * 4)];

            image.CopyPixelDataTo(rgba);

            return new TexturePngImage(
                width,
                height,
                rgba);
        }
    }
}
