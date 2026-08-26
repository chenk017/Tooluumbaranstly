using System;
using AssetsTools.NET.Texture;
using AssetsTools.NET.Extra;


public static class TextureEngine
{
    public static byte[] DecodeToPNG(
        TextureFile texture,
        AssetsFileInstance assetsFile)
    {

        Console.WriteLine(
            "Texture Format : "
            + texture.m_TextureFormat
        );


        byte[] rawData =
            texture.GetTextureData(
                assetsFile
            );


        Console.WriteLine(
            "Raw Size : "
            + rawData.Length
        );


        byte[] rgbaData;


        try
        {
            rgbaData =
                TextureFile.DecodeManaged(
                    rawData,
                    (TextureFormat)texture.m_TextureFormat,
                    texture.m_Width,
                    texture.m_Height,
                    true
                );
        }
        catch(Exception ex)
        {
            throw new Exception(
                "Decode texture gagal : "
                + ex.Message
            );
        }


        Console.WriteLine(
            "RGBA Size : "
            + rgbaData.Length
        );


        byte[] pngData;


        try
        {
            pngData =
                TextureFile.Encode(
                    rgbaData,
                    TextureFormat.RGBA32,
                    texture.m_Width,
                    texture.m_Height
                );
        }
        catch(Exception ex)
        {
            throw new Exception(
                "Encode PNG gagal : "
                + ex.Message
            );
        }


        return pngData;
    }
}
