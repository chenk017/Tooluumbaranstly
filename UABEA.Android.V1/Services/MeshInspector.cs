
using System;
using System.Linq;
using System.IO;
using AssetsTools.NET;
using AssetsTools.NET.Extra;
using UABEAvalonia;

namespace UABEA.Android.V1.Services
{
    public static class MeshInspector
    {
        public static void Dump(
            AssetWorkspace workspace,
            AssetContainer cont)
        {
            Console.WriteLine();
            Console.WriteLine("========================================");
            Console.WriteLine(" Mesh Inspector - Structure Probe");
            Console.WriteLine("========================================");
            Console.WriteLine("TypeID : " + cont.ClassId);
            Console.WriteLine("PathID : " + cont.PathId);
            Console.WriteLine(
               "Name   : " + GetName(workspace, cont));
            Console.WriteLine(
               "Unity  : " +
               cont.FileInstance.file.Metadata.UnityVersion);

            Console.WriteLine();

            try
            {
                AssetTypeValueField root =
                    GetByteArrayMesh(workspace, cont);

                if (root == null)
                {
                    Console.WriteLine(
                        "BaseField tidak tersedia.");
                    return;
                }

                DumpField(root, 0);
                DumpMeshData(root);
                  ExportInventory(
                      root,
                      "/storage/emulated/0/PROJECT_MOBOX/mesh_inventory.txt");

        Console.WriteLine();
        Console.Write("Ekspor OBJ untuk uji geometri? (y/N): ");
        string? exportChoice = Console.ReadLine();

        if (string.Equals(
            exportChoice?.Trim(), "y",
            StringComparison.OrdinalIgnoreCase))
        {
            Console.Write("Path file OBJ: ");
            string? outputPath = Console.ReadLine();

            if (!string.IsNullOrWhiteSpace(outputPath))
            {
                try
                {
                    MeshObjExporter.Export(root, outputPath.Trim());
                }
                catch (Exception exportError)
                {
                    Console.WriteLine(
                        "Ekspor OBJ gagal: " + exportError.Message);
                }
            }
        }

                Console.WriteLine();
                Console.WriteLine(
                    "Selesai memeriksa struktur Mesh.");
            }
            catch (Exception ex)
            {
                Console.WriteLine(
                    "Gagal membaca Mesh: " + ex.Message);
            }

            Console.WriteLine(
                "========================================");
            Console.WriteLine();
        }

        private static void DumpField(
            AssetTypeValueField field,
            int depth)
        {
            if (field == null)
                return;

            string indent = new string(' ', depth * 2);

            Console.Write(
                indent + field.FieldName +
                " [" + field.TypeName + "]");

            if (field.Children == null ||
                field.Children.Count == 0)
            {
                Console.WriteLine();
                return;
            }

            Console.WriteLine(
                " - children: " + field.Children.Count);

            if (depth >= 8)
            {
                Console.WriteLine(
                    indent + "  ... batas kedalaman");
                return;
            }

            int limit = Math.Min(
                field.Children.Count, 24);

            for (int i = 0; i < limit; i++)
            {
                DumpField(field.Children[i], depth + 1);
            }

            if (field.Children.Count > limit)
            {
                Console.WriteLine(
                    indent + "  ... " +
                    (field.Children.Count - limit) +
                    " field lain tidak ditampilkan");
            }
        }


private static void DumpMeshData(
    AssetTypeValueField root)
{
    Console.WriteLine();
    Console.WriteLine("----------------------------------------");
    Console.WriteLine(" Mesh Data Diagnostic - Compressed");
    Console.WriteLine("----------------------------------------");

    Console.WriteLine("[SubMeshes]");
    DumpSelectedChildren(root, "m_SubMeshes", 4);

    Console.WriteLine();
    Console.WriteLine("[Compressed Vertices]");
    DumpSelectedChildren(
        root, "m_CompressedMesh.m_Vertices", 5);

    Console.WriteLine();
    Console.WriteLine("[Compressed UV]");
    DumpSelectedChildren(
        root, "m_CompressedMesh.m_UV", 5);

    Console.WriteLine();
    Console.WriteLine("[Compressed Triangles]");
    DumpSelectedChildren(
        root, "m_CompressedMesh.m_Triangles", 5);

    Console.WriteLine();
    Console.WriteLine("[Compressed Normals]");
    DumpSelectedChildren(
        root, "m_CompressedMesh.m_Normals", 4);

        PrintPackedData(root, "m_Vertices");
        PrintPackedData(root, "m_UV");
        PrintPackedData(root, "m_Triangles");

        ProbeTriangleRange(root);

PrintVertexStorageData(root);
PrintRawBufferInfo(root);
DumpSelectedChildren(root, "m_VertexData.m_Streams", 4);
DumpSelectedChildren(root, "m_SubMeshes", 8);
}

private static void PrintVertexStorageData(
    AssetTypeValueField root)
{
    Console.WriteLine();
    Console.WriteLine("----------------------------------------");
    Console.WriteLine(" Mesh Data Diagnostic - Vertex Storage");
    Console.WriteLine("----------------------------------------");

    DumpSelectedChildren(
        root, "m_VertexData.m_VertexCount", 1);

    DumpSelectedChildren(
        root, "m_VertexData.m_Channels", 3);

    DumpSelectedChildren(
        root, "m_VertexData.m_DataSize", 1);

    DumpSelectedChildren(
        root, "m_IndexFormat", 1);

    DumpSelectedChildren(
        root, "m_IndexBuffer", 1);

    DumpSelectedChildren(
        root, "m_StreamData", 2);
}

private static void PrintRawBufferInfo(
    AssetTypeValueField root)
{
    Console.WriteLine();
    Console.WriteLine("----------------------------------------");
    Console.WriteLine(" Raw Geometry Buffer Diagnostic");
    Console.WriteLine("----------------------------------------");

    var vertexData = FindChild(root, "m_VertexData");
    var vertexBytesField = FindChild(vertexData, "m_DataSize");

    var indexBuffer = FindChild(root, "m_IndexBuffer");
    var indexArray = FindChild(indexBuffer, "Array");

    try
    {
        byte[] vertexBytes = vertexBytesField == null
            ? null
            : vertexBytesField.AsByteArray;

        Console.WriteLine(
            "Vertex buffer bytes: " +
            (vertexBytes == null ? -1 : vertexBytes.Length));

        if (vertexBytes != null && vertexBytes.Length > 0)
        {
            Console.Write("Vertex buffer sample: ");

            for (int i = 0; i < Math.Min(16, vertexBytes.Length); i++)
                Console.Write(vertexBytes[i] + " ");

            Console.WriteLine();
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine(
            "Vertex buffer read error: " + ex.Message);
    }

    try
    {
        byte[] indexBytes = indexArray == null
            ? null
            : indexArray.AsByteArray;

        Console.WriteLine(
            "Index buffer bytes: " +
            (indexBytes == null ? -1 : indexBytes.Length));

        if (indexBytes != null && indexBytes.Length > 0)
        {
            Console.Write("Index buffer sample: ");

            for (int i = 0; i < Math.Min(16, indexBytes.Length); i++)
                Console.Write(indexBytes[i] + " ");

            Console.WriteLine();
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine(
            "Index buffer read error: " + ex.Message);
    }
}

private static void PrintPackedData(
    AssetTypeValueField root,
    string packedName)
{
    AssetTypeValueField compressed =
        FindChild(root, "m_CompressedMesh");

    AssetTypeValueField packed =
        FindChild(compressed, packedName);

    AssetTypeValueField dataField =
        FindChild(packed, "m_Data");

    AssetTypeValueField array =
        FindChild(dataField, "Array");

    Console.WriteLine();
    Console.WriteLine("[" + packedName + " decoded probe]");

    if (array == null)
    {
        Console.WriteLine("Array tidak ditemukan.");
        return;
    }

    try
    {
        byte[] bytes = array.AsByteArray;

        if (bytes == null)
        {
            Console.WriteLine("Data byte null.");
            return;
        }

        AssetTypeValueField countField =
            FindChild(packed, "m_NumItems");

        AssetTypeValueField bitsField =
            FindChild(packed, "m_BitSize");

        AssetTypeValueField rangeField =
            FindChild(packed, "m_Range");

        AssetTypeValueField startField =
            FindChild(packed, "m_Start");

        uint.TryParse(
            GetSimpleValue(countField),
            out uint itemCount);

        byte.TryParse(
            GetSimpleValue(bitsField),
            out byte bitSize);

        float range = rangeField == null
            ? 0f
            : rangeField.AsFloat;

        float start = startField == null
            ? 0f
            : startField.AsFloat;

        Console.WriteLine("Bytes: " + bytes.Length);
        Console.WriteLine("Items: " + itemCount);
        Console.WriteLine("BitSize: " + bitSize);

        if (bitSize == 0 || bitSize > 24)
        {
            Console.WriteLine("BitSize tidak valid.");
            return;
        }

        int sampleCount = (int)Math.Min(itemCount, 12u);

        for (int i = 0; i < sampleCount; i++)
        {
            uint raw = ReadPackedValue(
                bytes, i, bitSize);

            if (packedName == "m_Vertices" ||
                packedName == "m_UV")
            {
                double maxValue =
                    (1UL << bitSize) - 1UL;

                float decoded =
                    (float)(raw / maxValue) * range + start;

                Console.WriteLine(
                    "[" + i + "] raw=" + raw +
                    " decoded=" + decoded.ToString("F6"));
            }
            else
            {
                Console.WriteLine(
                    "[" + i + "] index=" + raw);
            }
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine(
            "Gagal mendekode: " + ex.Message);
    }
}

private static void DumpSelectedChildren(
    AssetTypeValueField root,
    string fieldName,
    int maxDepth)
{
    try
    {
        AssetTypeValueField field = root[fieldName];

        if (field == null)
        {
            Console.WriteLine(fieldName + " = <null>");
            return;
        }

        DumpValuesRecursive(field, 0, maxDepth);
    }
    catch (Exception ex)
    {
        Console.WriteLine(
            fieldName + " = <gagal: " + ex.Message + ">");
    }
}

private static void DumpValuesRecursive(
    AssetTypeValueField field,
    int depth,
    int maxDepth)
{
    if (field == null)
        return;

    string indent = new string(' ', depth * 2);
    string value = GetSimpleValue(field);

    Console.WriteLine(
        indent + field.FieldName +
        " [" + field.TypeName + "]" +
        (value == null ? "" : " = " + value));

    if (depth >= maxDepth || field.Children == null)
        return;

    int count = Math.Min(field.Children.Count, 32);

    for (int i = 0; i < count; i++)
    {
        DumpValuesRecursive(
            field.Children[i],
            depth + 1,
            maxDepth);
    }

    if (field.Children.Count > count)
    {
        Console.WriteLine(
            indent + "  ... " +
            (field.Children.Count - count) +
            " children lainnya");
    }
}

private static string GetSimpleValue(
    AssetTypeValueField field)
{
    try
    {
        switch (field.TypeName)
        {
            case "bool":
                return field.AsBool.ToString();

            case "int":
                return field.AsInt.ToString();

            case "UInt32":
            case "unsigned int":
                return field.AsUInt.ToString();

            case "UInt64":
                return field.AsULong.ToString();

            case "SInt64":
                return field.AsLong.ToString();

            case "float":
                return field.AsFloat.ToString();

            case "string":
                return field.AsString;

            case "UInt8":
            case "unsigned char":
                return field.AsByte.ToString();

            default:
                return null;
        }
    }
    catch
    {
        return "<tidak tersedia>";
    }
}

        private static void PrintValue(
            AssetTypeValueField root,
            string path)
        {
            try
            {
                AssetTypeValueField field = root[path];

                if (field == null)
                {
                    Console.WriteLine(
                        path + " = <null>");
                    return;
                }

                string value;

                switch (field.TypeName)
                {
                    case "bool":
                        value = field.AsBool.ToString();
                        break;

                    case "int":
                        value = field.AsInt.ToString();
                        break;

                    case "UInt32":
                    case "unsigned int":
                        value = field.AsUInt.ToString();
                        break;

                    case "UInt64":
                        value = field.AsULong.ToString();
                        break;

                    case "SInt64":
                        value = field.AsLong.ToString();
                        break;

                    case "float":
                        value = field.AsFloat.ToString();
                        break;

                    case "string":
                        value = field.AsString;
                        break;

                    default:
                        value = "<" + field.TypeName + ">";
                        break;
                }

                Console.WriteLine(path + " = " + value);
            }
            catch (Exception ex)
            {
                Console.WriteLine(
                    path + " = <gagal: " + ex.Message + ">");
            }
        }

        private static void PrintBytes(
            AssetTypeValueField root,
            string path)
        {
            try
            {
                AssetTypeValueField field = root[path];

                if (field == null)
                {
                    Console.WriteLine(
                        path + " = <null>");
                    return;
                }

                byte[] data = field.AsByteArray;

                Console.WriteLine(
                    path + " = " +
                    (data == null
                        ? "null"
                        : data.Length + " bytes"));
            }
            catch (Exception ex)
            {
                Console.WriteLine(
                    path + " = <gagal: " + ex.Message + ">");
            }
        }

private static AssetTypeValueField FindChild(
    AssetTypeValueField parent,
    string name)
{
    if (parent == null || parent.Children == null)
        return null;

    foreach (AssetTypeValueField child in parent.Children)
    {
        if (child.FieldName == name)
            return child;
    }

    return null;
}

private static uint ReadPackedValue(
    byte[] data,
    int index,
    int bitSize)
{
    long bitOffset = (long)index * bitSize;
    uint value = 0;

    for (int bit = 0; bit < bitSize; bit++)
    {
        long position = bitOffset + bit;
        int byteIndex = (int)(position / 8);
        int bitIndex = (int)(position % 8);

        if (byteIndex >= data.Length)
            break;

        if ((data[byteIndex] & (1 << bitIndex)) != 0)
            value |= 1u << bit;
    }

    return value;
}

private static void ProbeTriangleRange(
    AssetTypeValueField root)
{
    AssetTypeValueField compressed =
        FindChild(root, "m_CompressedMesh");

    AssetTypeValueField packed =
        FindChild(compressed, "m_Triangles");

    AssetTypeValueField dataField =
        FindChild(packed, "m_Data");

    AssetTypeValueField array =
        FindChild(dataField, "Array");

    AssetTypeValueField countField =
        FindChild(packed, "m_NumItems");

    AssetTypeValueField bitsField =
        FindChild(packed, "m_BitSize");

    AssetTypeValueField subMeshes =
        FindChild(root, "m_SubMeshes");

    if (array == null || countField == null ||
        bitsField == null || subMeshes == null)
    {
        Console.WriteLine(
            "Data triangle tidak lengkap.");
        return;
    }

    byte[] bytes = array.AsByteArray;

    uint.TryParse(
        GetSimpleValue(countField),
        out uint count);

    byte.TryParse(
        GetSimpleValue(bitsField),
        out byte bitSize);

    AssetTypeValueField subArray =
        FindChild(subMeshes, "Array");

    AssetTypeValueField firstSubMesh =
        subArray == null || subArray.Children.Count == 0
            ? null
            : subArray.Children[0];

    AssetTypeValueField vertexCountField =
        FindChild(firstSubMesh, "vertexCount");

    uint.TryParse(
        GetSimpleValue(vertexCountField),
        out uint vertexCount);

    uint min = uint.MaxValue;
    uint max = 0;
    uint invalid = 0;

    Console.WriteLine();
    Console.WriteLine("[Triangle index range]");

    for (int i = 0; i < (int)count; i++)
    {
        uint value = ReadPackedValue(
            bytes, i, bitSize);

        if (value < min) min = value;
        if (value > max) max = value;

        if (value >= vertexCount)
            invalid++;
    }

    Console.WriteLine("Index count : " + count);
    Console.WriteLine("Vertex count: " + vertexCount);
    Console.WriteLine("Minimum raw : " + min);
    Console.WriteLine("Maximum raw : " + max);
    Console.WriteLine("Out of range: " + invalid);
}

        
          private static void ExportInventory(
              AssetTypeValueField root,
              string outputPath)
          {
              try
              {
                  using (StreamWriter writer =
                      new StreamWriter(outputPath, false))
                  {
                      writer.WriteLine("MESH STRUCTURE INVENTORY");
                      writer.WriteLine("Root: " + root.FieldName);
                      writer.WriteLine("Unity Mesh field tree");
                      writer.WriteLine();

                      DumpInventoryField(
                          writer, root, root.FieldName);
                  }

                  Console.WriteLine();
                  Console.WriteLine(
                      "Inventaris tersimpan: " + outputPath);
              }
              catch (Exception ex)
              {
                  Console.WriteLine(
                      "Gagal menulis inventaris: " + ex.Message);
              }
          }

          private static void DumpInventoryField(
              StreamWriter writer,
              AssetTypeValueField field,
              string path)
          {
              if (field == null)
                  return;

              int childCount =
                  field.Children == null ? 0 : field.Children.Count;

              string value = GetSimpleValue(field);

              writer.Write(
                  path + " [" + field.TypeName + "]" +
                  " children=" + childCount);

              if (value != null)
                  writer.Write(" value=" + value);

              if (childCount == 0)
              {
                  try
                  {
                      byte[] bytes = field.AsByteArray;
                      if (bytes != null)
                          writer.Write(" byteLength=" + bytes.Length);
                  }
                  catch
                  {
                      // Bukan byte array yang dapat dibaca.
                  }

                  writer.WriteLine();
                  return;
              }

              writer.WriteLine();

              // Catat seluruh channel vertex, tetapi batasi array
              // lainnya ke elemen pertama agar inventaris tetap ringkas.
              if (field.FieldName == "Array")
              {
                  if (path.EndsWith(
                      "m_VertexData.m_Channels.Array",
                      StringComparison.Ordinal))
                  {
                      for (int i = 0; i < childCount; i++)
                      {
                          DumpInventoryField(
                              writer,
                              field.Children[i],
                              path + "[" + i + "]");
                      }

                      return;
                  }

                  if (childCount > 0)
                  {
                      DumpInventoryField(
                          writer,
                          field.Children[0],
                          path + "[0]");
                  }

                  if (childCount > 1)
                  {
                      writer.WriteLine(
                          path + " ... " +
                          (childCount - 1) +
                          " elemen lain tidak diperluas");
                  }

                  return;
              }

              foreach (AssetTypeValueField child in field.Children)
              {
                  string childPath =
                      string.IsNullOrEmpty(path)
                          ? child.FieldName
                          : path + "." + child.FieldName;

                  DumpInventoryField(
                      writer, child, childPath);
              }
          }

private static AssetTypeValueField GetByteArrayMesh(
            AssetWorkspace workspace,
            AssetContainer cont)
        {
            AssetTypeTemplateField meshTemplate =
                workspace.GetTemplateField(cont);

            if (meshTemplate == null)
            {
                throw new Exception(
                    "Mesh template tidak tersedia.");
            }

            AssetTypeTemplateField indexBuffer =
                meshTemplate.Children.FirstOrDefault(
                    f => f.Name == "m_IndexBuffer");

            if (indexBuffer != null &&
                indexBuffer.Children.Count > 0)
            {
                indexBuffer.Children[0].ValueType =
                    AssetValueType.ByteArray;
            }

            AssetTypeTemplateField vertexData =
                meshTemplate.Children.FirstOrDefault(
                    f => f.Name == "m_VertexData");

            if (vertexData != null)
            {
                AssetTypeTemplateField dataSize =
                    vertexData.Children.FirstOrDefault(
                        f => f.Name == "m_DataSize");

                if (dataSize != null)
                {
                    dataSize.ValueType =
                        AssetValueType.ByteArray;
                }
            }

            // Baca data bone weights sebagai byte array
            // untuk membantu pemeriksaan ukuran data mentah.
            AssetTypeTemplateField variableBoneCountWeights =
                meshTemplate.Children.FirstOrDefault(
                    f => f.Name == "m_VariableBoneCountWeights");

            if (variableBoneCountWeights != null)
            {
                AssetTypeTemplateField weightData =
                    variableBoneCountWeights.Children.FirstOrDefault(
                        f => f.Name == "m_Data");

                if (weightData != null &&
                    weightData.Children.Count > 0)
                {
                    weightData.Children[0].ValueType =
                        AssetValueType.ByteArray;
                }
            }

            AssetTypeTemplateField compressedMesh =
                meshTemplate.Children.FirstOrDefault(
                    f => f.Name == "m_CompressedMesh");

            if (compressedMesh != null)
            {
                foreach (AssetTypeTemplateField packedField
                    in compressedMesh.Children)
                {
                    AssetTypeTemplateField dataField =
                        packedField.Children.FirstOrDefault(
                            f => f.Name == "m_Data");

                    if (dataField != null &&
                        dataField.Children.Count > 0)
                    {
                        dataField.Children[0].ValueType =
                            AssetValueType.ByteArray;
                    }
                }
            }

            return meshTemplate.MakeValue(
                cont.FileReader,
                cont.FilePosition);
        }

        private static string GetName(
            AssetWorkspace workspace,
            AssetContainer cont)
        {
            try
            {
                AssetTypeValueField root =
                    workspace.GetBaseField(cont);

                if (root != null)
                {
                    AssetTypeValueField nameField =
                        root["m_Name"];

                    if (nameField != null &&
                        !string.IsNullOrEmpty(
                            nameField.AsString))
                    {
                        return nameField.AsString;
                    }
                }
            }
            catch
            {
            }

            return "-";
        }
    }
}

