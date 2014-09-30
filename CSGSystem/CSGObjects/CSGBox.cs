using System;
using Microsoft.DirectX;
using Microsoft.DirectX.Direct3D;
using System.Diagnostics;
using System.Windows.Forms;

namespace CSGSystem.CSGObjects
{
    class TCSGBox : TCSGObject
    {
        float height, width, length;
        public float Height
        {
            set { height = value; RebuildMesh(); }
            get { return height; }
        }
        public float Width
        {
            set { width = value; RebuildMesh(); }
            get { return width; }
        }
        public float Length
        {
            set { length = value; RebuildMesh(); }
            get { return length; }
        }
        public override bool Contain(Vector3 v)
        {
            TOBB b = new TOBB(new Vector3(0, 0, 0), new Vector3(height, width, length));//TODO оптимизировать
            b.SetOrient(GetOrient());
            return b.Contain(v);
        }
        public override void Serialize(System.IO.Stream stream, bool read)
        {
            if (read)
            {
                System.IO.BinaryReader r = new System.IO.BinaryReader(stream);
                SerializeOrient(stream, read);
                width = r.ReadSingle();
                height = r.ReadSingle();
                length = r.ReadSingle();
                RebuildMesh();
            }
            else
            {
                System.IO.BinaryWriter r = new System.IO.BinaryWriter(stream);
                r.Write((UInt32)TCSGObjectType.BOX);
                SerializeOrient(stream, read);
                r.Write(width);
                r.Write(height);
                r.Write(length);
            }
        }
        public override void BuildTreeView(TreeNode node)
        {
            node.Tag = this;
            node.Text = "Параллелепипед";
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
        public TCSGBox(float use_height, float use_width, float use_length)
        {
            height = use_height;
            width = use_width;
            length = use_length;
            mesh = new TTriMesh();
            RebuildMesh();
        }
        void RebuildMesh()
        {
            int color = System.Drawing.Color.Beige.ToArgb();
            float hx = height / 2, lx = -height / 2;
            float hy = width / 2, ly = -width / 2;
            float hz = length / 2, lz = -length / 2;

            mesh.pos.Pop(mesh.pos.Length);
            mesh.pos.Add(new Vector3(lx, hy, hz));
            mesh.pos.Add(new Vector3(hx, hy, hz));
            mesh.pos.Add(new Vector3(hx, ly, hz));
            mesh.pos.Add(new Vector3(lx, ly, hz));

            mesh.pos.Add(new Vector3(lx, hy, lz));
            mesh.pos.Add(new Vector3(hx, hy, lz));
            mesh.pos.Add(new Vector3(hx, ly, lz));
            mesh.pos.Add(new Vector3(lx, ly, lz));

            mesh.normal.Pop(mesh.normal.Length);
            mesh.normal.Add(new Vector3(-1, 0, 0));
            mesh.normal.Add(new Vector3(1, 0, 0));
            mesh.normal.Add(new Vector3(0, -1, 0));
            mesh.normal.Add(new Vector3(0, 1, 0));
            mesh.normal.Add(new Vector3(0, 0, -1));
            mesh.normal.Add(new Vector3(0, 0, 1));

            //mesh.color.Length = 0;
            //mesh.color.Add(color);


            mesh.triangle.Pop(mesh.triangle.Length);
            mesh.triangle.Add(new TTriMesh.TMeshTri(0, 4, 3, 0, 0, 0, 0, 0, 0));
            mesh.triangle.Add(new TTriMesh.TMeshTri(3, 4, 7, 0, 0, 0, 0, 0, 0));

            mesh.triangle.Add(new TTriMesh.TMeshTri(1, 2, 5, 1, 1, 1, 0, 0, 0));
            mesh.triangle.Add(new TTriMesh.TMeshTri(2, 6, 5, 1, 1, 1, 0, 0, 0));

            mesh.triangle.Add(new TTriMesh.TMeshTri(3, 7, 2, 2, 2, 2, 0, 0, 0));
            mesh.triangle.Add(new TTriMesh.TMeshTri(2, 7, 6, 2, 2, 2, 0, 0, 0));

            mesh.triangle.Add(new TTriMesh.TMeshTri(1, 4, 0, 3, 3, 3, 0, 0, 0));
            mesh.triangle.Add(new TTriMesh.TMeshTri(1, 5, 4, 3, 3, 3, 0, 0, 0));

            mesh.triangle.Add(new TTriMesh.TMeshTri(5, 7, 4, 4, 4, 4, 0, 0, 0));
            mesh.triangle.Add(new TTriMesh.TMeshTri(5, 6, 7, 4, 4, 4, 0, 0, 0));

            mesh.triangle.Add(new TTriMesh.TMeshTri(0, 3, 1, 5, 5, 5, 0, 0, 0));
            mesh.triangle.Add(new TTriMesh.TMeshTri(1, 3, 2, 5, 5, 5, 0, 0, 0));

            need_update = true;
        }
    }
}
