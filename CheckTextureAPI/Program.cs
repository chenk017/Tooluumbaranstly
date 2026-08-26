using System;
using System.Reflection;

class Program
{
    static void Main()
    {
        string dllPath =
            "../../Libs/AssetsTools.NET.Texture.dll";


        Console.WriteLine(
            "Loading: " + dllPath
        );


        Assembly dll =
            Assembly.LoadFrom(dllPath);


        Console.WriteLine(
            "Berhasil load DLL"
        );


        foreach(Type type in dll.GetTypes())
        {
            Console.WriteLine(
                "CLASS: " + type.FullName
            );


            foreach(MethodInfo method in type.GetMethods())
            {
                Console.WriteLine(
                    "  " + method.ToString()
                );
            }
        }
    }
}
