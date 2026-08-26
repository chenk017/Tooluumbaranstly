using System;
using System.IO;
using AssetsTools.NET;
using AssetsTools.NET.Extra;
using AssetsTools.NET.Texture;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;


public class TextureExporter
{

    private AssetsManager am;
    private string exportFolder;


    public TextureExporter(
        AssetsManager manager,
        string folder
    )
    {
        am = manager;
        exportFolder = folder;


        if(!Directory.Exists(exportFolder))
        {
            Directory.CreateDirectory(
                exportFolder
            );
        }

    }



    public void ExportTexture(
        AssetsFileInstance assetsFile,
        AssetFileInfo asset
    )
    {

        try
        {

            Console.WriteLine();
            Console.WriteLine(
                ">>> TEXTURE2D"
            );


            AssetTypeValueField baseField =
                am.GetBaseField(
                    assetsFile,
                    asset
                );



            TextureFile texture =
                TextureFile.ReadTextureFile(
                    baseField
                );



            Console.WriteLine(
                "PathID : "
                + asset.PathId
            );


            Console.WriteLine(
                "Name : "
                + texture.m_Name
            );


            Console.WriteLine(
                "Size : "
                + texture.m_Width
                + "x"
                + texture.m_Height
            );


            Console.WriteLine(
                "Texture Format : "
                + texture.m_TextureFormat
            );



            byte[] textureData =
                texture.GetTextureData(
                    assetsFile
                );



            if(textureData == null)
            {

                Console.WriteLine(
                    "Texture data kosong"
                );

                return;

            }



            string name =
                string.IsNullOrEmpty(
                    texture.m_Name
                )
                ?
                asset.PathId.ToString()
                :
                texture.m_Name;



            string output =
                Path.Combine(
                    exportFolder,
                    name
                    + "_"
                    + asset.PathId
                    + ".png"
                );



            SavePNG(
                textureData,
                texture.m_Width,
                texture.m_Height,
                output
            );



            Console.WriteLine(
                "Export : "
                + output
            );


        }
        catch(Exception ex)
        {

            Console.WriteLine(
                "Texture gagal : "
                + ex.Message
            );

        }

    }




    private void SavePNG(
        byte[] rgba,
        int width,
        int height,
        string path
    )
    {

        using(
            Image<Rgba32> image =
            Image.LoadPixelData<Rgba32>(
                rgba,
                width,
                height
            )
        )
        {

            image.SaveAsPng(
                path
            );

        }

    }

}
