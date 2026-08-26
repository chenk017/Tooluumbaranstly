using System;
using AssetsTools.NET;
using AssetsTools.NET.Extra;


public class Inspector
{

    private AssetsManager am;


    public Inspector(
        AssetsManager manager
    )
    {
        am = manager;
    }




    public void DumpAsset(
        AssetsFileInstance assetsFile,
        long pathID
    )
    {

        try
        {

            Console.WriteLine();

            Console.WriteLine("==============================");
            Console.WriteLine(" INSPECTOR ");
            Console.WriteLine("==============================");


            Console.WriteLine(
                "PathID : "
                + pathID
            );



            AssetTypeValueField baseField =
                am.GetBaseField(
                    assetsFile,
                    pathID,
                    AssetReadFlags.None
                );



            if(baseField == null)
            {

                Console.WriteLine(
                    "BaseField kosong"
                );

                return;

            }



            InspectorTree tree =
                new InspectorTree();



            tree.Show(
                baseField
            );


        }
        catch(Exception ex)
        {

            Console.WriteLine(
                "Inspector ERROR : "
                + ex.Message
            );

        }

    }


}
