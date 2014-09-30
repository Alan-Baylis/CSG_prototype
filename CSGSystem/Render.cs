using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using System.Windows.Forms;
using Microsoft.DirectX;
using Microsoft.DirectX.Direct3D;

namespace CSGSystem
{
    class TRender
    {
        Device _device;
        Microsoft.DirectX.Direct3D.Font font;
        Control canvas;
        public class TState
        {
            Device _device;
            public TState(Device use_device)
            {
                _device = use_device;
            }
            public void CullFace(bool cull)
            {
                if (cull) _device.RenderState.CullMode = Cull.CounterClockwise;
                else _device.RenderState.CullMode = Cull.None;
            }
            public void FillModeWireFrame()
            {
                _device.RenderState.FillMode = FillMode.WireFrame;
            }
            public void FillModePoint()
            {
                _device.RenderState.FillMode = FillMode.Point;
            }
            public void FillModeSolid()
            {
                _device.RenderState.FillMode = FillMode.Solid;
            }
            public void DepthBias(bool use)
            {
                _device.RenderState.DepthBias = use ? -0.0001f : 0.0f;
            }
            public void DepthTest(bool use)
            {
                _device.RenderState.ZBufferEnable = use;
            }
            public void PointSize(float size)
            {
                _device.RenderState.PointSize = size;
            }
        }
        public TState State;

        public class TTransform
        {
            Device _device;
            Control canvas;
            public TTransform(Device use_device, Control use_canvas)
            {
                _device = use_device;
                canvas = use_canvas;
            }
            public void Ortho()
            {
                _device.Transform.Projection = Matrix.OrthoOffCenterLH(-1, 1, -1, 1, -1, 1);
                _device.Transform.View = Matrix.Identity;
            }
            public void Projection()
            {
                _device.Transform.Projection = Matrix.PerspectiveLH(
                    canvas.Width / 5000.0f, canvas.Height / 5000.0f, 0.1f, 100.0f);
            }
            public void ViewByCam(TCamera cam)
            {
                _device.Transform.View = cam.GetView();
            }
        }
        public TTransform Transform;

        public class TDraw
        {
            Device _device;
            Microsoft.DirectX.Direct3D.Font font;
            public TDraw(Device use_device, Microsoft.DirectX.Direct3D.Font use_font)
            {
                _device = use_device;
                font = use_font;
            }
            public void Text(string text, Point pos)
            {
                font.DrawText(null, text, pos, System.Drawing.Color.Beige);
            }
            public void Triangles(CustomVertex.PositionNormal[] triangles)
            {
                if (triangles.Length == 0) return;
                _device.VertexFormat = CustomVertex.PositionNormal.Format;
                _device.DrawUserPrimitives(PrimitiveType.TriangleList, triangles.Length / 3, triangles);
            }
            public void Triangles(CustomVertex.PositionNormalColored[] triangles)
            {
                if (triangles.Length == 0) return;
                _device.VertexFormat = CustomVertex.PositionNormalColored.Format;
                _device.DrawUserPrimitives(PrimitiveType.TriangleList, triangles.Length / 3, triangles);
            }
            public void Triangles(CustomVertex.PositionColored[] triangles)
            {
                if (triangles.Length == 0) return;
                _device.VertexFormat = CustomVertex.PositionColored.Format;
                _device.DrawUserPrimitives(PrimitiveType.TriangleList, triangles.Length / 3, triangles);
            }
            public void Triangles(TVector<CustomVertex.PositionNormalColored> triangles)
            {
                if (triangles.Length == 0) return;
                _device.VertexFormat = CustomVertex.PositionNormalColored.Format;
                _device.DrawUserPrimitives(PrimitiveType.TriangleList, triangles.Length / 3, triangles.GetData());
            }
            public void Points(CustomVertex.PositionColored[] points)
            {
                _device.VertexFormat = CustomVertex.PositionColored.Format;
                _device.DrawUserPrimitives(PrimitiveType.PointList, points.Length, points);
            }
            public void Lines(CustomVertex.PositionColored[] lines)
            {
                _device.VertexFormat = CustomVertex.PositionColored.Format;
                _device.DrawUserPrimitives(PrimitiveType.LineList, lines.Length / 2, lines);
            }
        }
        public TDraw Draw;

        public class TLighting
        {
            Device _device;
            public TLighting(Device use_device)
            {
                _device = use_device;
            }
            public void Enable(bool enable)
            {
                if (enable)
                {
                    _device.RenderState.Lighting = true;

                    Material m = new Material();
                    m.Diffuse = Color.White;
                    _device.Material = m;

                    _device.Lights[0].Type = LightType.Directional;
                    _device.Lights[0].Diffuse = Color.White;
                    _device.Lights[0].Enabled = true;
                    _device.Lights[0].Update();
                }
                else
                {
                    _device.RenderState.Lighting = false;
                }
            }
            public void LightDir(Vector3 dir)
            {
                _device.Lights[0].Direction = Vector3.Normalize(dir);
            }
            public void Material(Color diffuse, Color spec, float Sharpness)
            {
                Material boxMaterial = new Material();
                boxMaterial.Diffuse = diffuse;
                _device.Material = boxMaterial;
            }
        }
        public TLighting Lighting;

        public TRender(Control use_canvas)
        {

            PresentParameters presentParams = new PresentParameters();
            presentParams.Windowed = true;
            presentParams.SwapEffect = SwapEffect.Discard;
            presentParams.AutoDepthStencilFormat = DepthFormat.D24S8;
            presentParams.EnableAutoDepthStencil = true;
            presentParams.PresentationInterval = PresentInterval.One;

            canvas = use_canvas;
            _device = new Device(
                0,
                DeviceType.Hardware,
                use_canvas,
                CreateFlags.HardwareVertexProcessing,
                presentParams);

            System.Drawing.Font local_font = new System.Drawing.Font("Arial", 14, System.Drawing.FontStyle.Regular);

            font = new Microsoft.DirectX.Direct3D.Font(_device, local_font);
            _device.RenderState.Lighting = false;
            _device.RenderState.CullMode = Cull.None;
            _device.RenderState.ZBufferEnable = true;
            _device.DeviceResizing += new System.ComponentModel.CancelEventHandler(this.DeviceResizing);
            
            State = new TState(_device);
            Transform = new TTransform(_device, canvas);
            Draw = new TDraw(_device, font);
            Lighting = new TLighting(_device);
        }
        private void DeviceResizing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            _device.RenderState.Lighting = false;
            _device.RenderState.CullMode = Cull.None;
            _device.RenderState.ZBufferEnable = true;
        }

        public void BeginScene()
        {
            _device.Clear(ClearFlags.Target | ClearFlags.ZBuffer, System.Drawing.Color.DarkBlue, 1.0f, 0);
            _device.BeginScene();
        }
        public void ClearDepth()
        {
            _device.Clear(ClearFlags.ZBuffer, System.Drawing.Color.DarkBlue, 1.0f, 0);
        }
        public void EndScene()
        {
            _device.EndScene();
            _device.Present();
        }
        public TRay FromScreenToWorld(Point screen_pos)
        {
            return ToWorld(canvas.PointToClient(screen_pos));
        }
        public TRay ToWorld(Point canvas_pos)
        {
            Point local = canvas_pos;
            Vector3 pos = (canvas.Width == 0 || canvas.Height == 0) ? new Vector3(0, 0, 0) :
                    new Vector3(local.X / (float)canvas.Width,
                    (canvas.Height - local.Y) / (float)canvas.Height,
                    0.0f);
            pos = pos * 2.0f - new Vector3(1.0f, 1.0f, 1.0f);

            Matrix inv = Matrix.Invert(_device.Transform.View * _device.Transform.Projection);
            Vector3 world_pos = Vector3.TransformCoordinate(pos, inv);
            Vector3 world_dir = Vector3.Normalize(Vector3.TransformCoordinate(new Vector3(pos.X, pos.Y, 0.5f), inv) - world_pos);

            return new TRay(world_pos, world_dir);
        }
    }
}
