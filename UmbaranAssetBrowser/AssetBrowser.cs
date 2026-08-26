using System;
using System.Collections.Generic;
using AssetsTools.NET;
using AssetsTools.NET.Extra;


public class AssetBrowser
{

    private AssetsManager am;


    public AssetBrowser(
        AssetsManager manager
    )
    {
        am = manager;
    }



    public List<AssetInfoModel> ReadAssets(
        AssetsFileInstance assetsFile
    )
    {

        List<AssetInfoModel> list =
            new List<AssetInfoModel>();


        foreach(
            AssetFileInfo asset 
            in assetsFile.file.AssetInfos
        )
        {

            string typeName =
                GetTypeName(
                    asset.TypeId
                );


            string name =
                NameResolver.GetName(
                    am,
                    assetsFile,
                    asset
                );


            list.Add(
                new AssetInfoModel
                {

                    PathID = asset.PathId,

                    TypeID = asset.TypeId,

                    TypeName = typeName,

                    Name = name

                }
            );


        }


        return list;

    }





    private string GetTypeName(
        int typeId
    )
    {

        switch(typeId)
        {

            case 1:
                return "GameObject";


            case 4:
                return "Transform";


            case 21:
                return "Material";


            case 28:
                return "Texture2D";


            case 43:
                return "Mesh";


            case 48:
                return "Shader";


            case 83:
                return "AudioClip";


            case 114:
                return "MonoBehaviour";


            case 128:
                return "Font";


            case 142:
                return "AssetBundle";


            case 198:
                return "ParticleSystem";


            case 199:
                return "ParticleSystemRenderer";


            default:

                return "TypeID_" + typeId;

        }

    }

}
