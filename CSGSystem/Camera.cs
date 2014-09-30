using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using Microsoft.DirectX;

namespace CSGSystem
{
    class TCamera
    {
        const float offset_mul = 500.0f;
        const float rot_mul = 100.0f;
        const float scroll_mul = 0.1f;

        Vector3 up, dir, target;
        float offset_x, offset_y;
        float dist;

        Point curr_pos;
        Matrix view;
        public void UpdateView()
        {
            Vector3 pos = dir * (-dist);
            Vector3 right = Vector3.Cross(dir, up);
            Vector3 off_global = right * offset_x + up * offset_y;
            view=Matrix.LookAtLH(pos + off_global, target + off_global, up);
        }
        public Matrix GetView()
        {
            return view;
        }
        public Vector3 GetPos()
        {
            Vector3 right = Vector3.Cross(dir, up);
            return dir * (-dist)+right * offset_x + up * offset_y;
        }
        public Vector3 GetDir()
        {
            return dir;
        }
        public Vector3 GetTarget()
        {
            return target;
        }

        bool panning = false;
        bool rotating = false;

        public TCamera()
        {
            target = new Vector3(0.0f, 0.0f, 0.0f);
            dir = new Vector3(-0.5f, -0.5f, -0.5f);
            dist = 5.0f;
            up = new Vector3(0.0f, 0.0f, 1.0f);
            offset_x = 0;
            offset_y = 0;
            UpdateView();
        }
        public float GetDist()
        {
            return dist;
        }
        public void SetDist(float use_dist)
        {
            dist = use_dist;
            UpdateView();
        }
        public void OnMouseMove(Point new_pos)
        {
            Vector3 right = new Vector3(0, 0, 0);
            if (panning || rotating) right = Vector3.Cross(dir, up);
            if (rotating)
            {
                float x = (new_pos.X - curr_pos.X) / rot_mul;
                float y = (curr_pos.Y - new_pos.Y) / rot_mul;

                if (x != 0)
                {
                    dir.TransformNormal(Matrix.RotationAxis(up, x));
                    dir.Normalize();
                }
                if (y != 0)
                {
                    dir.TransformNormal(Matrix.RotationAxis(right, y));
                    dir.Normalize();
                    up = Vector3.Cross(right, dir);
                    up.Normalize();
                }
            }
            if (panning)
            {
                offset_x += dist * (new_pos.X - curr_pos.X) / offset_mul;
                offset_y += dist * (new_pos.Y - curr_pos.Y) / offset_mul;
            }
            curr_pos = new_pos;
            if (panning || rotating) UpdateView();
        }
        public void OnMouseWheel(float scroll)
        {
            dist += dist * scroll * scroll_mul;
            UpdateView();
        }
        public void StartRotating(Point mouse_pos)
        {
            curr_pos = mouse_pos;
            rotating = true;
        }
        public void StartPanning(Point mouse_pos)
        {
            curr_pos = mouse_pos;
            panning = true;
        }
        public void EndRotating()
        {
            rotating = false;
        }
        public void EndPanning()
        {
            panning = false;
        }
    }
}
