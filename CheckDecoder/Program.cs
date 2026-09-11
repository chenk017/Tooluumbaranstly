using System;
using System.Reflection;

class Program
{
    static void Main()
    {
        var asm =
            Assembly.LoadFrom(
                "Libs/AssetRipper.TextureDecoder.dll"
            );


        var type =
            asm.GetType(
            "AssetRipper.TextureDecoder.Dxt.DxtDecoder"
            );


        foreach(var m in type.GetMethods())
        {
            if(m.Name.Contains("Decompress"))
            {
                Console.WriteLine(
                    "\nMETHOD: " + m.Name
                );


                foreach(var p in m.GetParameters())
                {
                    Console.WriteLine(
                        " PARAM: " +
                        p.ParameterType +
                        " " +
                        p.Name
                    );
                }


                Console.WriteLine(
                    " RETURN: " +
                    m.ReturnType
                );
            }
        }
    }
}
