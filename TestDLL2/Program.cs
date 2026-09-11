using System;
using System.Reflection;

class TestDLL
{
    static void Main()
    {
        var asm = Assembly.LoadFrom("Libs/AssetsTools.NET.dll");

        foreach (var t in asm.GetTypes())
        {
            if (t.FullName != null && t.FullName.Contains("AssetsManager"))
            {
                Console.WriteLine(t.FullName);
                Console.WriteLine(t.IsPublic);
            }
        }
    }
}
