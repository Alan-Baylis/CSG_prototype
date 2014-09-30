using System;
using Microsoft.DirectX;
using Microsoft.DirectX.Direct3D;
using System.Diagnostics;
using System.Windows.Forms;

namespace CSGSystem.CSGObjects
{
    class TCSGCylinder : TCSGObject
    {
        float radius;
        int segments;
        float height;
        float min_rad;
        void CalcMinRad()
        {
            min_rad = (float)Math.Cos(Math.PI / segments) * radius;
        }
        public TCSGCylinder(float use_radius, float use_height, int use_segments)
        {
            height = use_height;
            segments = use_segments;
            radius = use_radius;
            CalcMinRad();
            mesh = new TTriMesh();
            RebuildMesh();
        }
        public override bool Update()
        {
            if (need_update)
            {
                need_update = false;
                return true;
            }
            else return false;
        }
        public override void BuildTreeView(TreeNode node)
        {
            node.Tag = this;
            node.Text = "Цилиндр";
        }
        public override void Serialize(System.IO.Stream stream, bool read)
        {
            if (read)
            {
                System.IO.BinaryReader r = new System.IO.BinaryReader(stream);
                SerializeOrient(stream, read);
                radius = r.ReadSingle();
                segments = r.ReadInt32();
                height = r.ReadSingle();
                RebuildMesh();
            }
            else
            {
                System.IO.BinaryWriter r = new System.IO.BinaryWriter(stream);
                r.Write((UInt32)TCSGObjectType.CYLINDER);
                SerializeOrient(stream, read);
                r.Write(radius);
                r.Write(segments);
                r.Write(height);
            }
        }
        bool PreciseContain(float angle,Vector3 local_pos)
        {
            int id = (int)Math.Floor(angle / (2 * Math.PI / segments));
            if (id >= segments) id = segments-1;
            float a = (float)(id * (2 * Math.PI / segments) + (Math.PI / segments));
            Vector3 n = new Vector3((float)Math.Cos(a), (float)Math.Sin(a), 0); 
            //n.Normalize();
            float dd = Vector3.Dot(n, local_pos);
            return dd < min_rad;
        }
        public override bool Contain(Vector3 v)
        {
            float t = 0;
            //TODO оптимизировать
            Vector3 pos = GetPos();
            Vector3 dir=new Vector3(GetOrient().M31, GetOrient().M32, GetOrient().M33);
            t = Vector3.Dot(dir, v - pos);
            if (t>=0&&t<=height)
            {
                Vector3 local_pos =(v-pos)- dir*t;
                float sqr_len = local_pos.LengthSq();
                if (sqr_len > radius * radius) return false;
                else if (sqr_len > min_rad * min_rad)
                {
                    float proj_x = Vector3.Dot(new Vector3(GetOrient().M11, GetOrient().M12, GetOrient().M13), local_pos);
                    float proj_y = Vector3.Dot(new Vector3(GetOrient().M21, GetOrient().M22, GetOrient().M23), local_pos);
                    float l =  (float)Math.Sqrt(proj_x * proj_x + proj_y * proj_y);
                    return PreciseContain((float)(proj_y < 0 ? (2 * Math.PI - Math.Acos(proj_x/l)) : Math.Acos(proj_x/l)), local_pos);
                }
                else return true;
            }
            return false;
        }
        public float Radius
        {
            set { radius = value; RebuildMesh(); }
            get { return radius; }
        }
        public float Height
        {
            set { height = value; RebuildMesh(); }
            get { return height; }
        }
        public int Segments
        {
            set
            {
                segments = Math.Abs(value);
                if (segments < 3) segments = 3;
                RebuildMesh();
            }
            get { return segments; }
        }
        void RebuildMesh()
        {
            CalcMinRad();
            mesh.pos.Pop(mesh.pos.Length);
            mesh.normal.Pop(mesh.normal.Length);
            mesh.triangle.Pop(mesh.triangle.Length);

            mesh.pos.Add(new Vector3(0, 0, 0));
            mesh.normal.Add(new Vector3(0, 0, -1));
            mesh.pos.Add(new Vector3(0, 0, height));
            mesh.normal.Add(new Vector3(0, 0, 1));
            for (int i = 0; i < segments; i++)
            {
                double a = i * Math.PI * 2 / segments;
                mesh.pos.Add(new Vector3((float)Math.Cos(a) * radius, (float)Math.Sin(a) * radius, 0));
            }
            for (int i = 0; i < segments; i++)
            {
                mesh.pos.Add(new Vector3(mesh.pos[i + 2].X, mesh.pos[i + 2].Y, height));
            }
            //низ
            for (int i = 0; i < segments; i++)
            {
                mesh.triangle.Add(new TTriMesh.TMeshTri(i + 2, 0, (i + 1) % segments + 2,
                    0, 0, 0,
                    0, 0, 0));
            }
            //верх 
            for (int i = 0; i < segments; i++)
            {
                mesh.triangle.Add(new TTriMesh.TMeshTri((i + 1) % segments + 2 + segments, 1, i + 2 + segments,
                    1, 1, 1,
                    0, 0, 0));
            }
            //нормали боковины
            for (int i = 0; i < segments; i++)
            {
                Vector3 n = new Vector3(mesh.pos[2 + i].X, mesh.pos[2 + i].Y,0);
                n.Normalize();
                mesh.normal.Add(n);
            }
            //боковина
            for (int i = 0; i < segments; i++)
            {
                mesh.triangle.Add(new TTriMesh.TMeshTri(
                    i + 2 + segments,
                    i + 2, 
                    (i + 1) % segments + 2, 
                    i + 2, 
                    i + 2,
                    (i + 1) % segments + 2,
                    0, 0, 0));
                mesh.triangle.Add(new TTriMesh.TMeshTri(
                    i + 2 + segments,
                    (i + 1) % segments + 2, 
                    (i + 1) % segments + 2 + segments, 
                    i + 2,
                    (i + 1) % segments + 2,
                    (i + 1) % segments + 2, 
                    0, 0, 0));
            }
            need_update = true;
        }
    }
}
