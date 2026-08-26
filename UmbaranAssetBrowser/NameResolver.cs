using AssetsTools.NET;
using AssetsTools.NET.Extra;


public static class NameResolver
{

    public static string GetName(
        AssetsManager am,
        AssetsFileInstance file,
        AssetFileInfo asset
    )
    {

        try
        {

            AssetTypeValueField field =
                am.GetBaseField(
                    file,
                    asset
                );


            var nameField =
                field["m_Name"];


            if(nameField != null)
            {
                return nameField.AsString;
            }


        }
        catch
        {

        }


        return "-";

    }

}
