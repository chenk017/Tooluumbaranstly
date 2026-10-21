
using System;                                                               
using System.Globalization;
using System.IO;
using System.Text;
using AssetsTools.NET;

namespace UABEA.Android.V1.Services
{
    public static class MeshObjExporter
    {
        private static AssetTypeValueField Child(
            AssetTypeValueField parent, string name)
        {
            if (parent == null || parent.Children == null)
                return null;

            foreach (var child in parent.Children)
                if (child.FieldName == name)
                    return child;

            return null;
        }

        private static byte[] PackedBytes(
            AssetTypeValueField root, string name,
            out int count, out int bits,
            out float range, out float start)
        {
            var packed = Child(Child(root, "m_CompressedMesh"), name);
            var array = Child(Child(packed, "m_Data"), "Array");

            if (packed == null || array == null)
                throw new Exception("Data " + name + " tidak ditemukan.");

            count = Child(packed, "m_NumItems").AsInt;
            bits = Child(packed, "m_BitSize").AsInt;

            var rangeField = Child(packed, "m_Range");
            var startField = Child(packed, "m_Start");

            range = rangeField == null ? 0f : rangeField.AsFloat;
            start = startField == null ? 0f : startField.AsFloat;

            if (count < 0 || bits < 1 || bits > 24)
                throw new Exception("Jumlah item atau bit size tidak valid.");

            return array.AsByteArray;
        }

        private static uint ReadPacked(
            byte[] data, int index, int bits)
        {
            long bitOffset = (long)index * bits;
            uint value = 0;

            for (int bit = 0; bit < bits; bit++)
            {
                long position = bitOffset + bit;
                int byteIndex = (int)(position / 8);
                int bitIndex = (int)(position % 8);

                if (byteIndex >= data.Length)
                    throw new Exception("Data packed terpotong.");

                if ((data[byteIndex] & (1 << bitIndex)) != 0)
                    value |= 1u << bit;
            }

            return value;
        }

        private static float[] DecodeFloats(
            byte[] bytes, int count, int bits,
            float range, float start)
        {
            double max = (1UL << bits) - 1UL;
            float[] values = new float[count];

            for (int i = 0; i < count; i++)
            {
                uint raw = ReadPacked(bytes, i, bits);
                values[i] = (float)(raw / max) * range + start;
            }

            return values;
        }

private static int ChannelFormatSize(int format)
{
    switch (format)
    {
        case 0: return 4; // Float32
        case 1: return 2; // Float16
        case 2: return 1; // UNorm8
        case 3: return 1; // SNorm8
        case 4: return 2; // UNorm16
        case 5: return 2; // SNorm16
        case 6: return 1; // UInt8
        case 7: return 1; // SInt8
        case 8: return 2; // UInt16
        case 9: return 2; // SInt16
        case 10: return 4; // UInt32
        case 11: return 4; // SInt32
        default:
            throw new Exception(
                "Format channel belum didukung: " + format);
    }
}

private static float ReadFloat32(
    byte[] data, int offset)
{
    if (offset < 0 || offset + 4 > data.Length)
        throw new Exception("Posisi Float32 di luar buffer.");

    return BitConverter.ToSingle(data, offset);
}

private static float ReadFloat16(
    byte[] data, int offset)
{
    if (offset < 0 || offset + 2 > data.Length)
        throw new Exception("Posisi Float16 di luar buffer.");

    ushort raw = BitConverter.ToUInt16(data, offset);
    return (float)BitConverter.UInt16BitsToHalf(raw);
}

private static void ExportVertexData(
    AssetTypeValueField root, string outputPath)
{
    var vertexData = Child(root, "m_VertexData");
    var vertexCountField = Child(vertexData, "m_VertexCount");
    var channelsField = Child(vertexData, "m_Channels");
    var vertexBytesField = Child(vertexData, "m_DataSize");

    var indexFormatField = Child(root, "m_IndexFormat");
    var indexBufferField = Child(root, "m_IndexBuffer");
    var indexArrayField = Child(indexBufferField, "Array");

    if (vertexCountField == null ||
        channelsField == null ||
        vertexBytesField == null ||
        indexFormatField == null ||
        indexArrayField == null)
    {
        throw new Exception(
            "VertexData atau IndexBuffer tidak lengkap.");
    }

    int vertexCount = vertexCountField.AsInt;
    int indexFormat = indexFormatField.AsInt;

    if (vertexCount <= 0)
        throw new Exception("VertexCount tidak valid.");

    byte[] vertexBytes = vertexBytesField.AsByteArray;
    byte[] indexBytes = indexArrayField.AsByteArray;

    if (vertexBytes == null || indexBytes == null)
        throw new Exception("Buffer geometri kosong.");

    var channelArray = Child(channelsField, "Array");

    if (channelArray == null)
        throw new Exception("Array channel tidak ditemukan.");

    int[] strides = new int[3];

    AssetTypeValueField positionChannel = null;
    AssetTypeValueField normalChannel = null;
    AssetTypeValueField uvChannel = null;

    int positionStream = -1;
    int positionOffset = -1;
    int normalStream = -1;
    int normalOffset = -1;
    int uvStream = -1;
    int uvOffset = -1;

    for (int channelIndex = 0;
         channelIndex < channelArray.Children.Count;
         channelIndex++)
    {
        var channel = channelArray.Children[channelIndex];

        var streamField = Child(channel, "stream");
        var offsetField = Child(channel, "offset");
        var formatField = Child(channel, "format");
        var dimensionField = Child(channel, "dimension");

        if (streamField == null ||
            offsetField == null ||
            formatField == null ||
            dimensionField == null)
            continue;

        int stream = streamField.AsInt;
        int offset = offsetField.AsInt;
        int format = formatField.AsInt;
        int dimension = dimensionField.AsInt;

        if (dimension <= 0)
            continue;

        if (stream < 0 || stream >= strides.Length)
            throw new Exception(
                "Nomor vertex stream tidak didukung.");

        if (offset < 0)
            throw new Exception(
                "Offset vertex channel tidak valid.");

        int channelSize =
            ChannelFormatSize(format) * dimension;

        strides[stream] = Math.Max(
            strides[stream], checked(offset + channelSize));

        // Unity channel 0: Position
        if (channelIndex == 0 &&
            format == 0 && dimension == 3)
        {
            positionChannel = channel;
            positionStream = stream;
            positionOffset = offset;
        }

        // Unity channel 1: Normal
        if (channelIndex == 1 &&
            format == 0 && dimension == 3)
        {
            normalChannel = channel;
            normalStream = stream;
            normalOffset = offset;
        }

        // Unity channel 4: UV0
        if (channelIndex == 4 &&
            format == 1 && dimension == 2)
        {
            uvChannel = channel;
            uvStream = stream;
            uvOffset = offset;
        }
    }

    if (positionChannel == null)
        throw new Exception(
            "Channel posisi Float32x3 tidak ditemukan.");

    if (normalChannel == null)
        throw new Exception(
            "Channel normal Float32x3 tidak ditemukan.");

    if (uvChannel == null)
        throw new Exception(
            "Channel UV0 Float16x2 tidak ditemukan.");

    foreach (var channel in channelArray.Children)
    {
        var streamField = Child(channel, "stream");
        var offsetField = Child(channel, "offset");
        var formatField = Child(channel, "format");
        var dimensionField = Child(channel, "dimension");

        if (streamField == null ||
            offsetField == null ||
            formatField == null ||
            dimensionField == null)
            continue;

        int stream = streamField.AsInt;
        int offset = offsetField.AsInt;
        int format = formatField.AsInt;
        int dimension = dimensionField.AsInt;

        if (dimension <= 0)
            continue;

        if (stream < 0 || stream >= strides.Length)
            throw new Exception("Nomor vertex stream tidak didukung.");

        int channelSize =
            ChannelFormatSize(format) * dimension;

        strides[stream] = Math.Max(
            strides[stream], offset + channelSize);

        if (stream == 0 && offset == 0 &&
            format == 0 && dimension == 3)
        {
            positionChannel = channel;
        }

        if (stream == 1 && offset == 4 &&
            format == 1 && dimension == 2)
        {
            uvChannel = channel;
        }
        if (stream == 0 && offset == 12 &&
            format == 0 && dimension == 3)
        {
            normalChannel = channel;
        }
    }

    if (positionChannel == null)
        throw new Exception("Channel posisi Float32x3 tidak ditemukan.");

    if (normalChannel == null)
        throw new Exception("Channel normal Float32x3 tidak ditemukan.");

    if (uvChannel == null)
        throw new Exception("Channel UV0 Float16x2 tidak ditemukan.");

    int positionStride = strides[0];
    int uvStride = strides[1];

    if (positionStride < 12 || uvStride < 8)
        throw new Exception("Stride vertex tidak valid.");

    long stream0Size = (long)vertexCount * strides[0];
    long stream1Size = (long)vertexCount * strides[1];

    long[] streamBases = new long[3];

streamBases[0] = 0;
streamBases[1] = streamBases[0] + stream0Size;
streamBases[2] = streamBases[1] + stream1Size;

long expectedSize = stream0Size + stream1Size +
    (long)vertexCount * strides[2];

    if (expectedSize != vertexBytes.Length)
    {
        throw new Exception(
            "Ukuran buffer tidak cocok. Expected=" +
            expectedSize + ", actual=" + vertexBytes.Length);
    }

    if (indexFormat != 0 && indexFormat != 1)
        throw new Exception("IndexFormat tidak dikenal.");

    int indexSize = indexFormat == 0 ? 2 : 4;

    if (indexBytes.Length % indexSize != 0)
        throw new Exception("Ukuran IndexBuffer tidak valid.");

    var subMeshes = Child(root, "m_SubMeshes");
    var subArray = Child(subMeshes, "Array");

    if (subArray == null || subArray.Children.Count == 0)
        throw new Exception("Submesh tidak ditemukan.");

    var sb = new StringBuilder();

    sb.AppendLine("# OBJ diagnostic export from VertexData");
    sb.AppendLine("# Vertices: " + vertexCount);

    for (int i = 0; i < vertexCount; i++)
    {
        int offset = checked((int)(
    streamBases[positionStream] +
    (long)i * strides[positionStream] +
    positionOffset));

        float x = ReadFloat32(vertexBytes, offset);
        float y = ReadFloat32(vertexBytes, offset + 4);
        float z = ReadFloat32(vertexBytes, offset + 8);

        sb.Append("v ");
        sb.Append(x.ToString("R", CultureInfo.InvariantCulture));
        sb.Append(' ');
        sb.Append(y.ToString("R", CultureInfo.InvariantCulture));
        sb.Append(' ');
        sb.AppendLine(z.ToString("R", CultureInfo.InvariantCulture));
    }

    for (int i = 0; i < vertexCount; i++)
    {
        int offset = checked((int)(
    streamBases[normalStream] +
    (long)i * strides[normalStream] +
    normalOffset));

        float nx = ReadFloat32(vertexBytes, offset);
        float ny = ReadFloat32(vertexBytes, offset + 4);
        float nz = ReadFloat32(vertexBytes, offset + 8);

        sb.Append("vn ");
        sb.Append(nx.ToString("R", CultureInfo.InvariantCulture));
        sb.Append(' ');
        sb.Append(ny.ToString("R", CultureInfo.InvariantCulture));
        sb.Append(' ');
        sb.AppendLine(nz.ToString("R", CultureInfo.InvariantCulture));
    }

    for (int i = 0; i < vertexCount; i++)
    {
        int offset = checked((int)(
    streamBases[uvStream] +
    (long)i * strides[uvStream] +
    uvOffset));

        float u = ReadFloat16(vertexBytes, offset);
        float v = ReadFloat16(vertexBytes, offset + 2);

        sb.Append("vt ");
        sb.Append(u.ToString("R", CultureInfo.InvariantCulture));
        sb.Append(' ');
        sb.AppendLine(v.ToString("R", CultureInfo.InvariantCulture));
    }

    int faceCount = 0;

    for (int sm = 0; sm < subArray.Children.Count; sm++)
    {
        var sub = subArray.Children[sm];

        int topology = Child(sub, "topology").AsInt;
        int indexCount = Child(sub, "indexCount").AsInt;
        int baseVertex = Child(sub, "baseVertex").AsInt;
        long firstByte = Child(sub, "firstByte").AsLong;

        if (topology != 0)
            throw new Exception(
                "Submesh bukan triangle-list.");

        if (indexCount < 0 || indexCount % 3 != 0)
            throw new Exception(
                "Jumlah indeks submesh tidak valid.");

        if (firstByte < 0 || firstByte % indexSize != 0)
            throw new Exception(
                "firstByte tidak selaras dengan ukuran indeks.");

        long firstIndex = firstByte / indexSize;
        long totalIndices = indexBytes.Length / indexSize;

        if (firstIndex + indexCount > totalIndices)
            throw new Exception(
                "Rentang indeks submesh di luar buffer.");

        sb.AppendLine("g SubMesh_" + sm);

        for (int i = 0; i < indexCount; i += 3)
        {
            int[] face = new int[3];

            for (int j = 0; j < 3; j++)
            {
                long indexPosition =
                    (firstIndex + i + j) * indexSize;

                int byteOffset = checked((int)indexPosition);

                uint rawIndex;

                if (indexFormat == 0)
                {
                    rawIndex = BitConverter.ToUInt16(
                        indexBytes, byteOffset);
                }
                else
                {
                    rawIndex = BitConverter.ToUInt32(
                        indexBytes, byteOffset);
                }

                long adjusted = (long)rawIndex + baseVertex;

                if (adjusted < 0 || adjusted >= vertexCount)
                    throw new Exception(
                        "Indeks face di luar rentang vertex.");

                face[j] = checked((int)adjusted + 1);
            }

            sb.AppendLine(
                $"f {face[0]}/{face[0]}/{face[0]} " +
                $"{face[1]}/{face[1]}/{face[1]} " +
                $"{face[2]}/{face[2]}/{face[2]}");

            faceCount++;
        }
    }

    string fullPath = Path.GetFullPath(outputPath);
    string directory = Path.GetDirectoryName(fullPath);

    if (!string.IsNullOrEmpty(directory))
        Directory.CreateDirectory(directory);

    File.WriteAllText(fullPath, sb.ToString(), Encoding.UTF8);

    Console.WriteLine();
    Console.WriteLine("=== OBJ EXPORT: VERTEX DATA ===");
    Console.WriteLine("File     : " + fullPath);
    Console.WriteLine("Vertices : " + vertexCount);
    Console.WriteLine("UV       : " + vertexCount);
    Console.WriteLine("Faces    : " + faceCount);
    Console.WriteLine("Strides  : " +
        strides[0] + ", " + strides[1] + ", " + strides[2]);
}

        public static void Export(
            AssetTypeValueField root, string outputPath)
        {
        var compressed = Child(root, "m_CompressedMesh");
var compressedVertices = Child(compressed, "m_Vertices");
var compressedCount = Child(compressedVertices, "m_NumItems");

if (compressedCount == null || compressedCount.AsInt == 0)
{
    ExportVertexData(root, outputPath);
    return;
}

            int vertexItems, vertexBits;
            float vertexRange, vertexStart;

            byte[] vertexBytes = PackedBytes(
                root, "m_Vertices",
                out vertexItems, out vertexBits,
                out vertexRange, out vertexStart);

            int uvItems, uvBits;
            float uvRange, uvStart;

            byte[] uvBytes = PackedBytes(
                root, "m_UV",
                out uvItems, out uvBits,
                out uvRange, out uvStart);

            int indexItems, indexBits;
            float unusedRange, unusedStart;

            byte[] indexBytes = PackedBytes(
                root, "m_Triangles",
                out indexItems, out indexBits,
                out unusedRange, out unusedStart);

            if (vertexItems == 0 || vertexItems % 3 != 0)
                throw new Exception("Jumlah komponen vertex tidak valid.");

            if (uvItems != 0 && uvItems % 2 != 0)
                throw new Exception("Jumlah komponen UV tidak valid.");

            float[] vertices = DecodeFloats(
                vertexBytes, vertexItems, vertexBits,
                vertexRange, vertexStart);

            float[] uvs = DecodeFloats(
                uvBytes, uvItems, uvBits, uvRange, uvStart);

            int vertexCount = vertexItems / 3;
            int uvCount = uvItems / 2;

            var subMeshes = Child(root, "m_SubMeshes");
            var subArray = Child(subMeshes, "Array");

            if (subArray == null || subArray.Children.Count == 0)
                throw new Exception("Submesh tidak ditemukan.");

            if (indexItems == 0)
                throw new Exception("Data triangle kosong.");

            uint[] indices = new uint[indexItems];

            for (int i = 0; i < indexItems; i++)
                indices[i] = ReadPacked(indexBytes, i, indexBits);

            var sb = new StringBuilder();

            sb.AppendLine("# Mesh OBJ diagnostic export");
            sb.AppendLine("# Vertex count: " + vertexCount);
            sb.AppendLine("# UV count: " + uvCount);

            for (int i = 0; i < vertexCount; i++)
            {
                sb.Append("v ");
                sb.Append(vertices[i * 3].ToString("R", CultureInfo.InvariantCulture));
                sb.Append(' ');
                sb.Append(vertices[i * 3 + 1].ToString("R", CultureInfo.InvariantCulture));
                sb.Append(' ');
                sb.AppendLine(vertices[i * 3 + 2].ToString("R", CultureInfo.InvariantCulture));
            }

            for (int i = 0; i < uvCount; i++)
            {
                sb.Append("vt ");
                sb.Append(uvs[i * 2].ToString("R", CultureInfo.InvariantCulture));
                sb.Append(' ');
                sb.AppendLine(uvs[i * 2 + 1].ToString("R", CultureInfo.InvariantCulture));
            }

            int faceCount = 0;

            for (int sm = 0; sm < subArray.Children.Count; sm++)
            {
                var sub = subArray.Children[sm];

                int topology = Child(sub, "topology").AsInt;
                int indexCount = Child(sub, "indexCount").AsInt;
                int baseVertex = Child(sub, "baseVertex").AsInt;
                long firstByte = Child(sub, "firstByte").AsLong;

                if (topology != 0)
                    throw new Exception(
                        "Submesh " + sm +
                        " bukan triangle-list. Ekspor dihentikan.");

                if (indexCount < 0 || indexCount % 3 != 0)
                    throw new Exception(
                        "Jumlah indeks submesh tidak valid.");

                // Mesh ini memiliki 444 vertex, sehingga indeksnya
                // menggunakan offset index 16-bit.
                long firstIndex = firstByte / 2;

                if (firstIndex < 0 ||
                    firstIndex + indexCount > indices.Length)
                    throw new Exception(
                        "Rentang indeks submesh di luar data triangle.");

                sb.AppendLine("g SubMesh_" + sm);

                for (int i = 0; i < indexCount; i += 3)
                {
                    int a = checked((int)indices[firstIndex + i] + baseVertex);
                    int b = checked((int)indices[firstIndex + i + 1] + baseVertex);
                    int c = checked((int)indices[firstIndex + i + 2] + baseVertex);

                    if (a < 0 || b < 0 || c < 0 ||
                        a >= vertexCount || b >= vertexCount ||
                        c >= vertexCount)
                        throw new Exception(
                            "Indeks face di luar rentang vertex.");

                    a++; b++; c++;

                    if (uvCount == vertexCount)
                        sb.AppendLine(
                            $"f {a}/{a} {b}/{b} {c}/{c}");
                    else
                        sb.AppendLine($"f {a} {b} {c}");

                    faceCount++;
                }
            }

            string fullPath = Path.GetFullPath(outputPath);
            string directory = Path.GetDirectoryName(fullPath);

            if (!string.IsNullOrEmpty(directory))
                Directory.CreateDirectory(directory);

            File.WriteAllText(fullPath, sb.ToString(), Encoding.UTF8);

            Console.WriteLine();
            Console.WriteLine("=== OBJ EXPORT ===");
            Console.WriteLine("File     : " + fullPath);
            Console.WriteLine("Vertices : " + vertexCount);
            Console.WriteLine("UV       : " + uvCount);
            Console.WriteLine("Faces    : " + faceCount);
            Console.WriteLine("Catatan  : Validasi visual masih diperlukan.");
        }
    }
}

