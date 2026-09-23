using System;
using AssetsTools.NET;
using AssetsTools.NET.Extra;
using UABEAvalonia;

namespace CoreTest.Inspectors
{
    public static class ParticleSystemRendererInspector
    {
        public static void Dump(
            AssetWorkspace workspace,
            AssetContainer cont)
        {
            Console.WriteLine();
            Console.WriteLine("========================================");
            Console.WriteLine(" ParticleSystemRenderer Inspector");
            Console.WriteLine("========================================");

            Console.WriteLine($"TypeID : {cont.ClassId}");
            Console.WriteLine($"Type   : ParticleSystemRenderer");
            Console.WriteLine($"PathID : {cont.PathId}");

            AssetTypeValueField? baseField =
                workspace.GetBaseField(cont);

            if (baseField == null)
            {
                Console.WriteLine();
                Console.WriteLine(
                    "Gagal membaca BaseField ParticleSystemRenderer.");
                Console.WriteLine();
                return;
            }

            Console.WriteLine();
            Console.WriteLine("Structure:");
            Console.WriteLine("----------------------------------------");

            DumpField(baseField, 0);

            Console.WriteLine("----------------------------------------");
            Console.WriteLine();
        }

        private static void DumpField(
            AssetTypeValueField field,
            int depth)
        {
            string indent =
                new string(' ', depth * 2);

            string fieldName =
                field.FieldName ?? "";

            string typeName =
                field.TypeName ?? "";

            Console.WriteLine(
                $"{indent}{fieldName} [{typeName}]");

            if (field.IsDummy)
            {
                Console.WriteLine(
                    $"{indent}  <dummy>");

                return;
            }

            if (field.Value != null)
            {
                switch (field.Value.ValueType)
                {
                    case AssetValueType.Bool:
                        Console.WriteLine(
                            $"{indent}  Value : {field.AsBool}");
                        break;

                    case AssetValueType.Int8:
                    case AssetValueType.Int16:
                    case AssetValueType.Int32:
                        Console.WriteLine(
                            $"{indent}  Value : {field.AsInt}");
                        break;

                    case AssetValueType.Int64:
                        Console.WriteLine(
                            $"{indent}  Value : {field.AsLong}");
                        break;

                    case AssetValueType.UInt8:
                    case AssetValueType.UInt16:
                    case AssetValueType.UInt32:
                        Console.WriteLine(
                            $"{indent}  Value : {field.AsUInt}");
                        break;

                    case AssetValueType.UInt64:
                        Console.WriteLine(
                            $"{indent}  Value : {field.AsULong}");
                        break;

                    case AssetValueType.Float:
                        Console.WriteLine(
                            $"{indent}  Value : {field.AsFloat}");
                        break;

                    case AssetValueType.Double:
                        Console.WriteLine(
                            $"{indent}  Value : {field.AsDouble}");
                        break;

                    case AssetValueType.String:
                        Console.WriteLine(
                            $"{indent}  Value : \"{field.AsString}\"");
                        break;
                }
            }

            if (field.Children == null ||
                field.Children.Count == 0)
            {
                return;
            }

            foreach (AssetTypeValueField child in field.Children)
            {
                if (child == null)
                    continue;

                DumpField(
                    child,
                    depth + 1);
            }
        }
    }
}
