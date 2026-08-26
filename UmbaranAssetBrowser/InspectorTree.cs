using System;
using AssetsTools.NET;


public class InspectorTree
{

    private int nodeID = 0;



    public void Show(
        AssetTypeValueField root
    )
    {

        nodeID = 0;


        Console.WriteLine();

        Console.WriteLine("==============================");
        Console.WriteLine(" INSPECTOR TREE ");
        Console.WriteLine("==============================");


        Dump(
            root,
            0,
            ""
        );

    }





    private void Dump(
        AssetTypeValueField field,
        int depth,
        string parentPath
    )
    {

        int currentID =
            nodeID++;



        string path;


        if(
            string.IsNullOrEmpty(parentPath)
        )
        {

            path =
                field.FieldName;

        }
        else
        {

            path =
                parentPath
                + "."
                + field.FieldName;

        }



        string value =
            GetValue(field);



        string indent =
            new string(
                ' ',
                depth * 3
            );



        if(
            string.IsNullOrEmpty(value)
        )
        {

            Console.WriteLine(
                indent
                + "["
                + currentID
                + "] "
                + field.FieldName
            );

        }
        else
        {

            Console.WriteLine(
                indent
                + "["
                + currentID
                + "] "
                + field.FieldName
                + " : "
                + value
            );

        }



        Console.WriteLine(
            indent
            + "    Path : "
            + path
        );



        if(field.Children != null)
        {

            foreach(
                AssetTypeValueField child
                in field.Children
            )
            {

                Dump(
                    child,
                    depth + 1,
                    path
                );

            }

        }


    }





    private string GetValue(
        AssetTypeValueField field
    )
    {

        try
        {

            if(field.Value == null)
                return "";



            return field.Value.AsString;

        }
        catch
        {

            return "";

        }

    }


}
