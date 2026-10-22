using System;
using Android.Opengl;
using Java.Nio;
using Javax.Microedition.Khronos.Egl;
using Javax.Microedition.Khronos.Opengles;

namespace UABEA.Android.V1.Services
{
    public sealed class MeshPreviewRenderer : GLSurfaceView.IRenderer
    {
        private readonly object geometryLock = new object();

        private FloatBuffer? vertexBuffer;
        private ShortBuffer? indexBuffer;
        private int indexCount;

        private float rotationX = -15f;
        private float rotationY = 25f;
        private float zoom = 1f;

        public void SetGeometry(MeshGeometry geometry)
        {
            if (geometry == null)
                throw new ArgumentNullException(nameof(geometry));

            if (geometry.Positions == null || geometry.Indices == null)
                throw new ArgumentException("Data geometri kosong.");

            float[] positions = (float[])geometry.Positions.Clone();
            int[] indices = geometry.Indices;

            if (positions.Length < 9 || positions.Length % 3 != 0)
                throw new ArgumentException(
                    "Mesh harus memiliki minimal tiga vertex.");

            int vertexCount = positions.Length / 3;

            if (vertexCount > 65535)
                throw new ArgumentException(
                    "Preview versi awal mendukung maksimal 65535 vertex.");

            if (geometry.VertexCount != vertexCount ||
                indices.Length == 0 ||
                indices.Length % 3 != 0)
            {
                throw new ArgumentException(
                    "Jumlah vertex atau indeks geometri tidak valid.");
            }

            for (int i = 0; i < indices.Length; i++)
            {
                if (indices[i] < 0 || indices[i] >= vertexCount)
                    throw new ArgumentException(
                        "Indeks segitiga berada di luar rentang vertex.");
            }

            float minX = float.MaxValue;
            float minY = float.MaxValue;
            float minZ = float.MaxValue;
            float maxX = float.MinValue;
            float maxY = float.MinValue;
            float maxZ = float.MinValue;

            for (int i = 0; i < positions.Length; i += 3)
            {
                float x = positions[i];
                float y = positions[i + 1];
                float z = positions[i + 2];

                if (float.IsNaN(x) || float.IsInfinity(x) ||
                    float.IsNaN(y) || float.IsInfinity(y) ||
                    float.IsNaN(z) || float.IsInfinity(z))
                {
                    throw new ArgumentException(
                        "Mesh memiliki koordinat yang tidak valid.");
                }

                minX = Math.Min(minX, x);
                minY = Math.Min(minY, y);
                minZ = Math.Min(minZ, z);
                maxX = Math.Max(maxX, x);
                maxY = Math.Max(maxY, y);
                maxZ = Math.Max(maxZ, z);
            }

            float centerX = (minX + maxX) * 0.5f;
            float centerY = (minY + maxY) * 0.5f;
            float centerZ = (minZ + maxZ) * 0.5f;

            float extentX = maxX - minX;
            float extentY = maxY - minY;
            float extentZ = maxZ - minZ;

            float largestExtent = Math.Max(
                extentX, Math.Max(extentY, extentZ));

            if (largestExtent <= 0.000001f)
                throw new ArgumentException("Ukuran Mesh terlalu kecil.");

            float normalization = 2f / largestExtent;

            for (int i = 0; i < positions.Length; i += 3)
            {
                positions[i] = (positions[i] - centerX) * normalization;
                positions[i + 1] = (positions[i + 1] - centerY) * normalization;
                positions[i + 2] = (positions[i + 2] - centerZ) * normalization;
            }

            short[] shortIndices = new short[indices.Length];

            for (int i = 0; i < indices.Length; i++)
            {
                if (indices[i] < 0 || indices[i] >= geometry.VertexCount)
                    throw new ArgumentException(
                        "Indeks segitiga berada di luar rentang vertex.");

                shortIndices[i] = unchecked((short)indices[i]);
            }

            ByteBuffer vertexBytes = ByteBuffer
                .AllocateDirect(positions.Length * 4)
                .Order(ByteOrder.NativeOrder());

            FloatBuffer newVertexBuffer = vertexBytes.AsFloatBuffer();
            newVertexBuffer.Put(positions);
            newVertexBuffer.Position(0);

            ByteBuffer indexBytes = ByteBuffer
                .AllocateDirect(shortIndices.Length * 2)
                .Order(ByteOrder.NativeOrder());

            ShortBuffer newIndexBuffer = indexBytes.AsShortBuffer();
            newIndexBuffer.Put(shortIndices);
            newIndexBuffer.Position(0);

            lock (geometryLock)
            {
                vertexBuffer = newVertexBuffer;
                indexBuffer = newIndexBuffer;
                indexCount = shortIndices.Length;
            }
        }

        public void Rotate(float deltaX, float deltaY)
        {
            lock (geometryLock)
            {
                rotationY += deltaX * 0.7f;
                rotationX += deltaY * 0.7f;

                rotationX = Math.Max(-89f, Math.Min(89f, rotationX));
            }
        }

        public void Zoom(float factor)
        {
            lock (geometryLock)
            {
                zoom *= factor;
                zoom = Math.Max(0.25f, Math.Min(5f, zoom));
            }
        }

        public void ResetView()
        {
            lock (geometryLock)
            {
                rotationX = -15f;
                rotationY = 25f;
                zoom = 1f;
            }
        }

        public void OnSurfaceCreated(IGL10? gl, EGLConfig? config)
        {
            if (gl == null)
                return;

            gl.GlClearColor(0.12f, 0.12f, 0.14f, 1f);
            gl.GlEnable(GL10.GlDepthTest);
            gl.GlDisable(GL10.GlCullFace);
            gl.GlShadeModel(GL10.GlSmooth);
        }

        public void OnSurfaceChanged(IGL10? gl, int width, int height)
        {
            if (gl == null)
                return;

            int safeWidth = Math.Max(1, width);
            int safeHeight = Math.Max(1, height);

            gl.GlViewport(0, 0, safeWidth, safeHeight);
            gl.GlMatrixMode(GL10.GlProjection);
            gl.GlLoadIdentity();

            float aspect = (float)safeWidth / safeHeight;

            if (aspect >= 1f)
                gl.GlFrustumf(-aspect, aspect, -1f, 1f, 1.5f, 20f);
            else
                gl.GlFrustumf(-1f, 1f, -1f / aspect, 1f / aspect, 1.5f, 20f);

            gl.GlMatrixMode(GL10.GlModelview);
            gl.GlLoadIdentity();
        }

        public void OnDrawFrame(IGL10? gl)
        {
            if (gl == null)
                return;

            FloatBuffer? vertices;
            ShortBuffer? indices;
            int count;
            float rx;
            float ry;
            float currentZoom;

            lock (geometryLock)
            {
                vertices = vertexBuffer;
                indices = indexBuffer;
                count = indexCount;
                rx = rotationX;
                ry = rotationY;
                currentZoom = zoom;
            }

            gl.GlClear(GL10.GlColorBufferBit | GL10.GlDepthBufferBit);

            if (vertices == null || indices == null || count == 0)
                return;

            gl.GlMatrixMode(GL10.GlModelview);
            gl.GlLoadIdentity();
            gl.GlTranslatef(0f, 0f, -4.5f);
            gl.GlScalef(currentZoom, currentZoom, currentZoom);
            gl.GlRotatef(rx, 1f, 0f, 0f);
            gl.GlRotatef(ry, 0f, 1f, 0f);

            gl.GlEnableClientState(GL10.GlVertexArray);
            gl.GlVertexPointer(3, GL10.GlFloat, 0, vertices);

            gl.GlColor4f(0.35f, 0.78f, 0.95f, 1f);
            indices.Position(0);

            gl.GlDrawElements(
                GL10.GlTriangles,
                count,
                GL10.GlUnsignedShort,
                indices);

            gl.GlDisableClientState(GL10.GlVertexArray);
        }
    }
}
