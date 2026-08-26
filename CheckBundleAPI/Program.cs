using System;
using System.Reflection;
using AssetsTools.NET;


class Program
{
    static void Main()
    {

        Assembly asm =
            typeof(AssetsFile).Assembly;


        Console.WriteLine(
            "DLL : "
            + asm.FullName
        );


        foreach(
            Type t in asm.GetTypes()
        )
        {

            foreach(
                MethodInfo m in t.GetMethods(
                    BindingFlags.Public |
                    BindingFlags.NonPublic |
                    BindingFlags.Instance |
                    BindingFlags.Static
                )
            )
            {

                if(
                    m.Name.Contains("GetBaseField") ||
                    m.Name.Contains("ReadAsset")
                )
                {

                    Console.WriteLine();

                    Console.WriteLine(
                        "CLASS : "
                        + t.FullName
                    );


                    Console.WriteLine(
                        "METHOD : "
                        + m.Name
                    );


                    Console.WriteLine(
                        "RETURN : "
                        + m.ReturnType.FullName
                    );


                    foreach(
                        var p in m.GetParameters()
                    )
                    {
                        Console.WriteLine(
                            "PARAM : "
                            + p.ParameterType.FullName
                            + " "
                            + p.Name
                        );
                    }

                }

            }

        }

    }
}
