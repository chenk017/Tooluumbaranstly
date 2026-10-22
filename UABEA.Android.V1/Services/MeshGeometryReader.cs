using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using AssetsTools.NET;

namespace UABEA.Android.V1.Services
{
    public sealed class MeshGeometry
    {
        public float[] Positions { get; }
        public int[] Indices { get; }

        public int VertexCount => Positions.Length / 3;
        public int TriangleCount => Indices.Length / 3;

        public MeshGeometry(float[] positions, int[] indices)
        {
            if (positions == null)
                throw new ArgumentNullException(nameof(positions));

            if (indices == null)
                throw new ArgumentNullException(nameof(indices));

            if (positions.Length == 0 || positions.Length % 3 != 0)
                throw new ArgumentException(
                    "Jumlah koordinat vertex harus kelipatan tiga.",
                    nameof(positions));

            if (indices.Length == 0 || indices.Length % 3 != 0)
                throw new ArgumentException(
                    "Jumlah indeks harus kelipatan tiga.",
                    nameof(indices));

            int vertexCount = positions.Length / 3;

            foreach (float value in positions)
            {
                if (float.IsNaN(value) || float.IsInfinity(value))
                    throw new ArgumentException(
                        "Koordinat vertex harus berupa angka finite.",
                        nameof(positions));
            }

            foreach (int index in indices)
            {
                if (index < 0 || index >= vertexCount)
                    throw new ArgumentException(
                        "Indeks segitiga berada di luar rentang vertex.",
                        nameof(indices));
            }

            Positions = positions;
            Indices = indices;
        }
    }

    public static class MeshGeometryReader
    {
        public static MeshGeometry Read(
            AssetTypeValueField root,
            string temporaryObjPath)
        {
            if (root == null)
                throw new ArgumentNullException(nameof(root));

            if (string.IsNullOrWhiteSpace(temporaryObjPath))
                throw new ArgumentException(
                    "Path OBJ sementara tidak valid.",
                    nameof(temporaryObjPath));

            try
            {
                MeshObjExporter.Export(root, temporaryObjPath);
                return ReadObj(temporaryObjPath);
            }
            finally
            {
                if (File.Exists(temporaryObjPath))
                    File.Delete(temporaryObjPath);
            }
        }

        private static MeshGeometry ReadObj(string path)
        {
            var positions = new List<float>();
            var indices = new List<int>();

            foreach (string rawLine in File.ReadLines(path))
            {
                string line = rawLine.Trim();

                if (line.Length == 0 || line[0] == '#')
                    continue;

                if (line.StartsWith("v ", StringComparison.Ordinal))
                {
                    string[] parts = line.Split(
                        (char[]?)null,
                        StringSplitOptions.RemoveEmptyEntries);

                    if (parts.Length < 4)
                        throw new InvalidDataException(
                            "Baris vertex OBJ tidak lengkap.");

                    positions.Add(ParseFloat(parts[1]));
                    positions.Add(ParseFloat(parts[2]));
                    positions.Add(ParseFloat(parts[3]));
                }
                else if (line.StartsWith("f ", StringComparison.Ordinal))
                {
                    string[] parts = line.Split(
                        (char[]?)null,
                        StringSplitOptions.RemoveEmptyEntries);

                    if (parts.Length < 4)
                        throw new InvalidDataException(
                            "Face OBJ memiliki kurang dari tiga vertex.");

                    var face = new int[parts.Length - 1];
                    int vertexCount = positions.Count / 3;

                    for (int i = 1; i < parts.Length; i++)
                    {
                        string[] references = parts[i].Split('/');

                        if (references.Length == 0 ||
                            references[0].Length == 0)
                        {
                            throw new InvalidDataException(
                                "Referensi vertex OBJ tidak valid.");
                        }

                        if (!int.TryParse(
                            references[0],
                            NumberStyles.Integer,
                            CultureInfo.InvariantCulture,
                            out int index) || index == 0)
                        {
                            throw new InvalidDataException(
                                "Indeks vertex OBJ tidak valid.");
                        }

                        int resolved = index > 0
                            ? index - 1
                            : vertexCount + index;

                        if (resolved < 0 || resolved >= vertexCount)
                            throw new InvalidDataException(
                                "Indeks face berada di luar rentang vertex.");

                        face[i - 1] = resolved;
                    }

                    // Triangulasi polygon menggunakan triangle fan.
                    for (int i = 1; i < face.Length - 1; i++)
                    {
                        indices.Add(face[0]);
                        indices.Add(face[i]);
                        indices.Add(face[i + 1]);
                    }
                }
            }

            if (positions.Count < 9)
                throw new InvalidDataException(
                    "OBJ tidak memiliki cukup data vertex.");

            if (indices.Count < 3 || indices.Count % 3 != 0)
                throw new InvalidDataException(
                    "OBJ tidak memiliki data segitiga yang valid.");

            return new MeshGeometry(
                positions.ToArray(),
                indices.ToArray());
        }

        private static float ParseFloat(string value)
        {
            if (!float.TryParse(
                value,
                NumberStyles.Float,
                CultureInfo.InvariantCulture,
                out float result) ||
                float.IsNaN(result) ||
                float.IsInfinity(result))
            {
                throw new InvalidDataException(
                    "Koordinat vertex OBJ tidak valid.");
            }

            return result;
        }
    }
}
