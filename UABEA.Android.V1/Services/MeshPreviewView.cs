using System;
using Android.Content;
using Android.Opengl;
using Android.Views;

namespace UABEA.Android.V1.Services
{
    public sealed class MeshPreviewView : GLSurfaceView
    {
        private readonly MeshPreviewRenderer meshRenderer;

        private float lastX;
        private float lastY;
        private float lastTouchDistance;
        private bool isDragging;

        public event Action<string>? PreviewError;

        public MeshPreviewView(Context context) : base(context)
        {
            meshRenderer = new MeshPreviewRenderer();

            SetEGLContextClientVersion(1);
            SetRenderer(meshRenderer);
            RenderMode = Rendermode.WhenDirty;

            SetFocusable(true);
            SetFocusableInTouchMode(true);
        }

        public void SetGeometry(MeshGeometry geometry)
        {
            if (geometry == null)
            {
                ReportError("Data Mesh kosong.");
                return;
            }

            try
            {
                QueueEvent(() =>
                {
                    try
                    {
                        meshRenderer.SetGeometry(geometry);
                        RequestRender();
                    }
                    catch (Exception ex)
                    {
                        ReportError("Gagal menampilkan Mesh: " + ex.Message);
                    }
                });
            }
            catch (Exception ex)
            {
                ReportError("Gagal menyiapkan preview Mesh: " + ex.Message);
            }
        }

        public void ResetView()
        {
            try
            {
                QueueEvent(() =>
                {
                    try
                    {
                        meshRenderer.ResetView();
                        RequestRender();
                    }
                    catch (Exception ex)
                    {
                        ReportError("Gagal mereset tampilan: " + ex.Message);
                    }
                });
            }
            catch (Exception ex)
            {
                ReportError("Gagal menjalankan reset: " + ex.Message);
            }
        }

        private void ReportError(string message)
        {
            Post(() => PreviewError?.Invoke(message));
        }

        public override bool OnTouchEvent(MotionEvent? e)
        {
            if (e == null)
                return false;

            switch (e.ActionMasked)
            {
                case MotionEventActions.Down:
                    lastX = e.GetX();
                    lastY = e.GetY();
                    isDragging = true;
                    return true;

                case MotionEventActions.PointerDown:
                    lastTouchDistance = GetTouchDistance(e);
                    isDragging = false;
                    return true;

                case MotionEventActions.Move:
                    if (e.PointerCount >= 2)
                    {
                        float newDistance = GetTouchDistance(e);

                        if (lastTouchDistance > 0f && newDistance > 0f)
                        {
                            float factor = newDistance / lastTouchDistance;

                            QueueEvent(() =>
                            {
                                try
                                {
                                    meshRenderer.Zoom(factor);
                                }
                                catch (Exception ex)
                                {
                                    ReportError("Zoom Mesh gagal: " + ex.Message);
                                }
                            });

                            RequestRender();
                        }

                        lastTouchDistance = newDistance;
                        return true;
                    }

                    if (isDragging && e.PointerCount == 1)
                    {
                        float x = e.GetX();
                        float y = e.GetY();

                        float deltaX = x - lastX;
                        float deltaY = y - lastY;

                        lastX = x;
                        lastY = y;

                        QueueEvent(() =>
                        {
                            try
                            {
                                meshRenderer.Rotate(deltaX, deltaY);
                            }
                            catch (Exception ex)
                            {
                                ReportError("Rotasi Mesh gagal: " + ex.Message);
                            }
                        });

                        RequestRender();
                    }

                    return true;

                case MotionEventActions.PointerUp:
                    lastTouchDistance = 0f;
                    isDragging = false;
                    return true;

                case MotionEventActions.Up:
                case MotionEventActions.Cancel:
                    isDragging = false;
                    lastTouchDistance = 0f;
                    return true;

                default:
                    return true;
            }
        }

        private static float GetTouchDistance(MotionEvent e)
        {
            if (e.PointerCount < 2)
                return 0f;

            float dx = e.GetX(0) - e.GetX(1);
            float dy = e.GetY(0) - e.GetY(1);

            return (float)Math.Sqrt(dx * dx + dy * dy);
        }
    }
}
