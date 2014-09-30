using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using Microsoft.DirectX;
using Microsoft.DirectX.Direct3D;

namespace CSGSystem
{
    class TEditor
    {
        const float size_mul = 0.2f;//относительная длинна осей координат
        const float aabb_size_mul = 0.2f;//размер OBB при выделении

        TCamera cam;
        TRender render;
        TCSGObject selected;

        bool world_movement=false;
        bool rotating = false;

        bool moving = false;
        bool x_selected = false;
        bool y_selected = false;
        bool z_selected = false;

        Vector3 bx;
        Vector3 by;
        Vector3 bz;
        Matrix start_orient;
        Vector3 start_pos;

        Vector3 moving_dir;
        float start_proj;
          
        public TEditor(TCamera use_cam, TRender use_render)
        {
            cam = use_cam;
            render = use_render;
        }
        public void Unselect()
        {
            selected = null;
        }
        public void Select(TCSGObject use_selected)
        {
            selected = use_selected;
        }
        void TestAxises(Point new_pos)
        {
            float size = size_mul * Vector3.Length(selected.GetPos() - cam.GetPos());
            Vector3 pos = selected.GetPos();
            Matrix m;
            Vector3 bx, by, bz;
            if (world_movement)
            {
                m = Matrix.Translation(selected.GetPos());
                bx = new Vector3(1, 0, 0);
                by = new Vector3(0, 1, 0);
                bz = new Vector3(0, 0, 1);
            }
            else
            {
                m = selected.GetOrient();
                bx = new Vector3(m.M11, m.M12, m.M13);
                by = new Vector3(m.M21, m.M22, m.M23);
                bz = new Vector3(m.M31, m.M32, m.M33);
            }
            TOBB x, y, z;
            TOBB xy, yz, zx;//TODO перемещение в плоскости
            //создаем OBB's на месте направляющих осей для определения пересечения
            x = new TOBB(new Vector3(1, 0, 0) * size * 0.5f, new Vector3(size, aabb_size_mul * size, aabb_size_mul * size));
            x.SetOrient(m);
            y = new TOBB(new Vector3(0, 1, 0) * size * 0.5f, new Vector3(aabb_size_mul * size, size, aabb_size_mul * size));
            y.SetOrient(m);
            z = new TOBB(new Vector3(0, 0, 1) * size * 0.5f, new Vector3(aabb_size_mul * size, aabb_size_mul * size, size));
            z.SetOrient(m);
            TRay world_ray = render.ToWorld(new_pos);
            x_selected = false;
            y_selected = false;
            z_selected = false;
            x_selected = x.Overlaps(world_ray);
            if (x_selected) return;
            y_selected = y.Overlaps(world_ray);
            if (x_selected||y_selected) return;
            z_selected = z.Overlaps(world_ray);
        }
        public void OnMouseMove(Point new_pos)
        {
            if (selected == null) return;
            if (moving)
            {
                TRay world_ray = render.ToWorld(new_pos);
                if (rotating)
                {
                    Vector3 temp = Vector3.Cross(world_ray.dir, moving_dir);
                    float new_proj = Vector3.Dot(world_ray.pos, temp);
                    Matrix new_orient = start_orient * Matrix.RotationAxis(moving_dir, new_proj);
                    selected.SetRotation(new_orient);
                }
                else
                {
                    float curr_proj = 0; ;
                    (new TRay(start_pos, moving_dir)).PointNearestToOtherRay(world_ray, ref curr_proj);
                    selected.SetPos(start_pos + moving_dir * (curr_proj - start_proj));
                }
            }
            else
                TestAxises(new_pos);
        }
        public void OnMouseDown(Point new_pos)
        {
            if (selected == null) return;
            x_selected = false;
            y_selected = false;
            z_selected = false;
            TestAxises(new_pos);
            if (x_selected) moving_dir = bx;
            else if (y_selected) moving_dir = by;
            else if (z_selected) moving_dir = bz;
            if (x_selected || y_selected || z_selected)
            {
                TRay world_ray = render.ToWorld(new_pos);
                moving = true;
                if (rotating)
                {
                    start_orient = selected.GetOrient();
                    Vector3 temp = Vector3.Cross(world_ray.dir, moving_dir);
                    start_proj = Vector3.Dot(world_ray.pos, temp);
                }
                else
                {
                    start_pos = selected.GetPos();
                    (new TRay(start_pos, moving_dir)).PointNearestToOtherRay(world_ray, ref start_proj);
                }
            }
        }
        public void OnMouseUp(Point new_pos)
        {
            if (selected == null) return;
            moving = false;
        }
        public void DrawAxis(bool use_world_movement,bool use_rotating)
        {
            world_movement = use_world_movement;
            rotating = use_rotating;
            if (selected == null) return;
            Matrix m;
            if (world_movement)
            {
                bx = new Vector3(1, 0, 0);
                by = new Vector3(0, 1, 0);
                bz = new Vector3(0, 0, 1);
            }
            else
            {
                m = selected.GetOrient();
                bx = new Vector3(m.M11, m.M12, m.M13);
                by = new Vector3(m.M21, m.M22, m.M23);
                bz = new Vector3(m.M31, m.M32, m.M33);
            }
            //рисуем координатные оси
            float size = size_mul * Vector3.Length(selected.GetPos() - cam.GetPos());
            CustomVertex.PositionColored[] axises = new CustomVertex.PositionColored[6];

            axises[0].Color = x_selected ?System.Drawing.Color.White.ToArgb():System.Drawing.Color.Red.ToArgb() ;
            axises[0].Position = selected.GetPos();
            axises[1].Color = axises[0].Color;
            axises[1].Position = axises[0].Position + bx * size;

            axises[2].Color = y_selected ? System.Drawing.Color.White.ToArgb() : System.Drawing.Color.Red.ToArgb();
            axises[2].Position = selected.GetPos();
            axises[3].Color = axises[2].Color;
            axises[3].Position = axises[2].Position + by * size;

            axises[4].Color = z_selected ? System.Drawing.Color.White.ToArgb() : System.Drawing.Color.Red.ToArgb();
            axises[4].Position = selected.GetPos();
            axises[5].Color = axises[4].Color;
            axises[5].Position = axises[4].Position + bz * size;
            render.Draw.Lines(axises);
            //
        }
    }
}
