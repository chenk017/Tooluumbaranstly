using AssetsTools.NET.Texture;
using System;

namespace TexturePlugin
{
    public static class TextureEncoderDecoder
    {
        // ============================================================
        // MANAGED DECODER
        // ============================================================

        private static byte[] DecodeAssetRipperTex(
            byte[] data,
            int width,
            int height,
            TextureFormat format)
        {
            byte[] dest = null;

            try
            {
                dest = TextureFile.DecodeManaged(
                    data,
                    format,
                    width,
                    height);
            }
            catch
            {
                dest = null;
            }

            // RGB565 fallback
            if (dest == null)
            {
                switch (format)
                {
                    case TextureFormat.RGB565:
                        dest = RGBADecoders.ReadRGB565(
                            data,
                            width,
                            height);
                        break;

                    default:
                        return null;
                }
            }

            if (dest == null)
                return null;

            // Decoder harus menghasilkan RGBA32
            if (dest.Length != width * height * 4)
                return null;

            // Behavior asli UABEA:
            // swap Red <-> Blue
            for (int i = 0; i < dest.Length; i += 4)
            {
                byte temp = dest[i];
                dest[i] = dest[i + 2];
                dest[i + 2] = temp;
            }

            return dest;
        }


        // ============================================================
        // DECODE
        // ============================================================

        public static byte[] Decode(
            byte[] data,
            int width,
            int height,
            TextureFormat format)
        {
            if (data == null)
                return null;

            if (width <= 0 || height <= 0)
                return null;

            switch (format)
            {
                // ====================================================
                // ASSETRIPPER / MANAGED
                // ====================================================

                case TextureFormat.DXT1:
                case TextureFormat.DXT3:
                case TextureFormat.DXT5:

                case TextureFormat.BC4:
                case TextureFormat.BC5:
                case TextureFormat.BC6H:
                case TextureFormat.BC7:

                case TextureFormat.RGB9e5Float:

                case TextureFormat.RGBA64:

                case TextureFormat.ETC_RGB4:
                case TextureFormat.ETC2_RGB4:
                case TextureFormat.ETC2_RGBA1:
                case TextureFormat.ETC2_RGBA8:

                case TextureFormat.ETC_RGB4_3DS:
                case TextureFormat.ETC_RGBA8_3DS:

                case TextureFormat.EAC_R:
                case TextureFormat.EAC_R_SIGNED:
                case TextureFormat.EAC_RG:
                case TextureFormat.EAC_RG_SIGNED:

                case TextureFormat.ASTC_RGB_4x4:
                case TextureFormat.ASTC_RGB_5x5:
                case TextureFormat.ASTC_RGB_6x6:
                case TextureFormat.ASTC_RGB_8x8:
                case TextureFormat.ASTC_RGB_10x10:
                case TextureFormat.ASTC_RGB_12x12:

                case TextureFormat.ASTC_RGBA_4x4:
                case TextureFormat.ASTC_RGBA_5x5:
                case TextureFormat.ASTC_RGBA_6x6:
                case TextureFormat.ASTC_RGBA_8x8:
                case TextureFormat.ASTC_RGBA_10x10:
                case TextureFormat.ASTC_RGBA_12x12:

                // RGB565 fallback
                case TextureFormat.RGB565:
                {
                    return DecodeAssetRipperTex(
                        data,
                        width,
                        height,
                        format);
                }

                default:
                    return null;
            }
        }


        // ============================================================
        // INFORMATION
        // ============================================================

        public static bool IsManagedDecodeSupported(
            TextureFormat format)
        {
            switch (format)
            {
                case TextureFormat.DXT1:
                case TextureFormat.DXT3:
                case TextureFormat.DXT5:

                case TextureFormat.BC4:
                case TextureFormat.BC5:
                case TextureFormat.BC6H:
                case TextureFormat.BC7:

                case TextureFormat.RGB9e5Float:
                case TextureFormat.RGBA64:

                case TextureFormat.ETC_RGB4:
                case TextureFormat.ETC2_RGB4:
                case TextureFormat.ETC2_RGBA1:
                case TextureFormat.ETC2_RGBA8:

                case TextureFormat.ETC_RGB4_3DS:
                case TextureFormat.ETC_RGBA8_3DS:

                case TextureFormat.EAC_R:
                case TextureFormat.EAC_R_SIGNED:
                case TextureFormat.EAC_RG:
                case TextureFormat.EAC_RG_SIGNED:

                case TextureFormat.ASTC_RGB_4x4:
                case TextureFormat.ASTC_RGB_5x5:
                case TextureFormat.ASTC_RGB_6x6:
                case TextureFormat.ASTC_RGB_8x8:
                case TextureFormat.ASTC_RGB_10x10:
                case TextureFormat.ASTC_RGB_12x12:

                case TextureFormat.ASTC_RGBA_4x4:
                case TextureFormat.ASTC_RGBA_5x5:
                case TextureFormat.ASTC_RGBA_6x6:
                case TextureFormat.ASTC_RGBA_8x8:
                case TextureFormat.ASTC_RGBA_10x10:
                case TextureFormat.ASTC_RGBA_12x12:

                case TextureFormat.RGB565:
                    return true;

                default:
                    return false;
            }
        }
    }
}
