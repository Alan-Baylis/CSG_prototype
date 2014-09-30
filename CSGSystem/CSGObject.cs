using System;
using System.Text;
using System.Collections;
using System.Windows.Forms;
using System.ComponentModel;
using Microsoft.DirectX;
using Microsoft.DirectX.Direct3D;
using System.Diagnostics;

namespace CSGSystem
{
    abstract class TCSGObject
    {
        Matrix orient;//позиция и направляющие оси тела
        public enum TCSGObjectType
        {
            SPHERE,
            BOX,
            CYLINDER,
            BOOL_OP
        }
        protected TTriMesh mesh;
        protected bool need_update;
        public abstract void BuildTreeView(TreeNode node);
        public abstract bool Update();
        protected void SerializeOrient(System.IO.Stream stream, bool read)
        {
            if (read)
            {
                System.IO.BinaryReader r = new System.IO.BinaryReader(stream);
                orient.M11=r.ReadSingle();
                orient.M12=r.ReadSingle();
                orient.M13=r.ReadSingle();
                orient.M14=r.ReadSingle();
                orient.M21=r.ReadSingle();
                orient.M22=r.ReadSingle();
                orient.M23=r.ReadSingle();
                orient.M24=r.ReadSingle();
                orient.M31=r.ReadSingle();
                orient.M32=r.ReadSingle();
                orient.M33=r.ReadSingle();
                orient.M34=r.ReadSingle();
                orient.M41=r.ReadSingle();
                orient.M42=r.ReadSingle();
                orient.M43=r.ReadSingle();
                orient.M44=r.ReadSingle();
            }
            else
            {
                System.IO.BinaryWriter r = new System.IO.BinaryWriter(stream);
                r.Write(orient.M11);
                r.Write(orient.M12);
                r.Write(orient.M13);
                r.Write(orient.M14);
                r.Write(orient.M21);
                r.Write(orient.M22);
                r.Write(orient.M23);
                r.Write(orient.M24);
                r.Write(orient.M31);
                r.Write(orient.M32);
                r.Write(orient.M33);
                r.Write(orient.M34);
                r.Write(orient.M41);
                r.Write(orient.M42);
                r.Write(orient.M43);
                r.Write(orient.M44);
            }
        }
        public abstract void Serialize(System.IO.Stream stream,bool read);
        public TCSGObject CreateObj(TCSGObjectType type)
        {
            switch (type)
            {
                case TCSGObjectType.BOX:
                    return new CSGObjects.TCSGBox(1, 1, 1);
                    break;
                case TCSGObjectType.SPHERE:
                    return new CSGObjects.TCSGSphere(1);
                    break;
                case TCSGObjectType.CYLINDER:
                    return new CSGObjects.TCSGCylinder(1, 3,30);
                    break;
                case TCSGObjectType.BOOL_OP:
                    return new CSGObjects.TCSGBoolOp();
                    break;
            }
            return null;
        }
        /// <summary>
        /// Обновляет данное звено и дочерние звенья если need_update
        /// </summary>
        /// <returns>произошли ли какие-нибудь изменения</returns>
        public abstract bool Contain(Vector3 v);
        public TTriMesh GetMesh()
        {
            return mesh;
        }
        public Matrix GetOrient()
        {
            return orient;
        }
        public void SetRotation(Matrix use_orient)
        {
            Vector3 pos = GetPos();
            orient = use_orient;
            orient.M41 = pos.X;
            orient.M42 = pos.Y;
            orient.M43 = pos.Z;
            need_update = true;
        }
        public Vector3 GetPos()
        {
            return new Vector3(orient.M41, orient.M42, orient.M43);
        }
        public void SetPos(Vector3 use_pos)
        {
            orient.M41 = use_pos.X;
            orient.M42 = use_pos.Y;
            orient.M43 = use_pos.Z;
            need_update = true;
        }
        public TCSGObject()
        {
            orient = Matrix.Identity;
        }
        public TTriMesh GetTransformedMesh()
        {
            TTriMesh trans_mesh = new TTriMesh();
            if (mesh == null) return trans_mesh;
            trans_mesh.pos.High = mesh.pos.High;
            for (int i = 0; i <= mesh.pos.High; i++)
            {
                Vector3 t = mesh.pos[i];
                t.TransformCoordinate(orient);
                trans_mesh.pos[i] = t;
            }
            trans_mesh.normal.High = mesh.normal.High;
            for (int i = 0; i <= mesh.normal.High; i++)
            {
                Vector3 t = mesh.normal[i];
                t.TransformNormal(orient);
                trans_mesh.normal[i] = t;
            }
            trans_mesh.triangle.Add(mesh.triangle);
            return trans_mesh;
        }
        [Category("Позиция"), DisplayName("X")]
        public float PosX
        {
            set { orient.M41 = value; need_update = true; }
            get { return orient.M41; }
        }
        [Category("Позиция"), DisplayName("Y")]
        public float PosY
        {
            set { orient.M42 = value; need_update = true; }
            get { return orient.M42; }
        }
        [Category("Позиция"), DisplayName("Z")]
        public float PosZ
        {
            set { orient.M43 = value; need_update = true; }
            get { return orient.M43; }
        }
    }
}
