using System;
using AssetsTools.NET;
using AssetsTools.NET.Extra;


class Program
{

    static void Main(string[] args)
    {

        Console.WriteLine("==============================");
        Console.WriteLine(" Umbaran Asset Browser");
        Console.WriteLine("==============================");
        Console.WriteLine();



        string bundlePath =
            args.Length > 0
            ?
            args[0]
            :
            "/storage/emulated/0/PROJECT_MOBOX/nextkagura_skill03_effect_1_skin06_add.unity3d";



        Console.WriteLine(
            "Bundle : "
            + bundlePath
        );

        Console.WriteLine();



        AssetsManager am =
            new AssetsManager();



        try
        {

            BundleFileInstance bundle =
                am.LoadBundleFile(
                    bundlePath,
                    true
                );


            Console.WriteLine(
                "Bundle berhasil dibuka"
            );

            Console.WriteLine();



            int fileCount =
                bundle.file.BlockAndDirInfo.DirectoryInfos.Length;



            Console.WriteLine(
                "Jumlah AssetsFile : "
                + fileCount
            );



            for(
                int i = 0;
                i < fileCount;
                i++
            )
            {

                Console.WriteLine();

                Console.WriteLine(
                    "=============================="
                );

                Console.WriteLine(
                    "FILE INDEX : "
                    + i
                );

                Console.WriteLine(
                    "=============================="
                );



                AssetsFileInstance assetsFile =
                    am.LoadAssetsFileFromBundle(
                        bundle,
                        i,
                        false
                    );



                if(assetsFile == null)
                {

                    Console.WriteLine(
                        "Bukan AssetsFile Unity, dilewati."
                    );

                    continue;

                }



                AssetBrowser browser =
                    new AssetBrowser(am);



                var assets =
                    browser.ReadAssets(
                        assetsFile
                    );



                Console.WriteLine();

                Console.WriteLine(
                    "PathID | Type | Name"
                );

                Console.WriteLine(
                    "--------------------------------"
                );



                foreach(
                    var asset in assets
                )
                {

                    Console.WriteLine(
                        asset.PathID
                        + " | "
                        + asset.TypeName
                        + " | "
                        + asset.Name
                    );

                }



                // ==============================
                // TEST INSPECTOR
                // ==============================


                if(
                    assets.Count > 0
                )
                {

                    Console.WriteLine();

                    Console.WriteLine(
                        "Inspect asset pertama..."
                    );



                    Inspector inspector =
                        new Inspector(am);



                    inspector.DumpAsset(
                        assetsFile,
                        assets[0].PathID
                    );

                }



            }



            Console.WriteLine();

            Console.WriteLine(
                "=============================="
            );

            Console.WriteLine(
                "Selesai membaca bundle."
            );


        }
        catch(Exception ex)
        {

            Console.WriteLine();

            Console.WriteLine(
                "ERROR : "
                + ex.Message
            );

        }



        Console.WriteLine();

        Console.WriteLine(
            "Tekan ENTER untuk keluar..."
        );

        Console.ReadLine();

    }

}
