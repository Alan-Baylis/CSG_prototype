using System;
using Microsoft.DirectX;
using Microsoft.DirectX.Direct3D;
using System.Diagnostics;
using System.Windows.Forms;

namespace CSGSystem.CSGObjects
{
    class TCSGSphere : TCSGObject
    {
        float radius;
        int sub_div;
        float min_rad;
        public TCSGSphere(float use_radius)
        {
            radius = use_radius;
            sub_div = 3;
            mesh = new TTriMesh();
            RebuildMesh();
        }
        public override void Serialize(System.IO.Stream stream, bool read)
        {
            if (read)
            {
                System.IO.BinaryReader r = new System.IO.BinaryReader(stream);
                SerializeOrient(stream, read);
                radius = r.ReadSingle();
                sub_div = r.ReadInt32();
                RebuildMesh();
            }
            else
            {
                System.IO.BinaryWriter r = new System.IO.BinaryWriter(stream);
                r.Write((UInt32)TCSGObjectType.SPHERE);
                SerializeOrient(stream, read);
                r.Write(radius);
                r.Write(sub_div);
            }
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
            node.Text = "Сфера";
        }
        bool PreciseContain(Vector3 v)
        {
            //TODO пока что просто перебираются все грани сферы
            for (int i = 0; i <= mesh.triangle.High; i++)
            {
                Vector3 p0=mesh.pos[mesh.triangle[i].p0];
                Vector3 p1=mesh.pos[mesh.triangle[i].p1];
                Vector3 p2=mesh.pos[mesh.triangle[i].p2];
                Vector3 n = Vector3.Cross(p1 - p0, p2 - p0);
                n.Normalize();
                //if (Vector3.Dot(n, v) > min_rad) 
                if (Vector3.Dot(n, v) > Vector3.Dot(n, p0)) 
                    return false;
            }
            return true;
        }
        public override bool Contain(Vector3 v)
        {
            float sqr_len=(GetPos() - v).LengthSq();
            if (sqr_len > radius * radius) return false;
            else if (sqr_len > min_rad * min_rad) return PreciseContain(v-GetPos());//TODO хотя и можно поворачивать тела, это нигде не учитывется поэтому будут косяки в  геометрии
            else return true;
        }
        public float Radius
        {
            set { radius = value; RebuildMesh(); }
            get { return radius; }
        }
        public int SubDivLevel
        {
            set
            {
                sub_div = Math.Abs(value);
                if (sub_div > 10) sub_div = 10;
                RebuildMesh();
            }
            get { return sub_div; }
        }
        struct TRib
        {
            public int v0, v1;
            public int part0, part1;//если cutted==true, то индексы TRib половинок ребра
            public int middle;
            public bool cutted;//ребро было поделено и больше не используется
            public TRib(int use_v0, int use_v1)
            {
                cutted = false;
                v0 = use_v0;
                v1 = use_v1;
                part0 = -1;
                part1 = -1;
                middle = -1;
            }
            public int GetOtherVert(int v)
            {
                Debug.Assert(v0 == v || v1 == v);
                if (v0 != v) return v0;
                else return v1;
            }
            public bool HasVert(int v)
            {
                return v0 == v || v1 == v;
            }
            public int Get(int i)
            {
                Debug.Assert(i == 0 || i == 1);
                if (i == 0) return v0;
                else return v1;
            }
            public int GetPart(int i)
            {
                Debug.Assert(i == 0 || i == 1);
                if (i == 0) return part0;
                else return part1;
            }
            public int GetCommonPart(TVector<TRib> rib, TRib neighbour_rib)
            {
                //возвращает индекс part данного rib соседствующего с neighbour_rib
                int p = GetCommonVert(neighbour_rib);
                if (rib[part0].HasVert(p)) return part0;
                else
                {
                    Debug.Assert(rib[part1].HasVert(p));
                    return part1;
                }
            }
            public int GetCommonVert(TRib neighbour_rib)
            {
                for (int i = 0; i <= 1; i++)
                    for (int k = 0; k <= 1; k++)
                        if (Get(i) == neighbour_rib.Get(k)) return Get(i);
                Debug.Assert(false);
                return -1;
            }
        }
        struct TTri
        {
            public int r0, r1, r2;//индексы ребер треугольника
            public TTri(int use_r0, int use_r1, int use_r2)
            {
                r0 = use_r0;
                r1 = use_r1;
                r2 = use_r2;
            }
        }
        void SubDiv(TVector<Vector3> vertex, TVector<TRib> rib, TVector<TTri> tri)
        {
            int high = rib.High;//т.к. будем добавлять в этот массив
            for (int i = 0; i <= high; i++)
                if (!rib[i].cutted)
                {
                    Vector3 new_vert = vertex[rib[i].v0] + vertex[rib[i].v1];
                    new_vert.Multiply(0.5f);
                    vertex.Add(new_vert);
                    TRib new_rib = rib[i];
                    new_rib.middle = vertex.High;
                    new_rib.cutted = true;

                    rib.Add(new TRib(vertex.High, new_rib.v0));
                    rib.Add(new TRib(vertex.High, new_rib.v1));
                    new_rib.part0 = rib.High - 1;
                    new_rib.part1 = rib.High;
                    rib[i] = new_rib;
                }
            //TODO кучу неиспользуемых посеченных ребер хранить не обязательно
            high = tri.High;
            for (int i = 0; i <= high; i++)
            {
                TTri last_tri = tri[i];
                {
                    int v0 = rib[tri[i].r0].middle;
                    int v1 = rib[tri[i].r1].middle;
                    int v2 = rib[tri[i].r2].middle;
                    rib.Add(new TRib(v0, v1));
                    rib.Add(new TRib(v1, v2));
                    rib.Add(new TRib(v2, v0));
                    tri.Add(new TTri(rib.High - 2, rib.High - 1, rib.High));
                }
                //
                int p0 = rib[last_tri.r0].GetCommonPart(rib, rib[last_tri.r1]);
                int p1 = rib[last_tri.r1].GetCommonPart(rib, rib[last_tri.r0]);
                tri[i] = new TTri(p0, p1, rib.High - 2);
                //
                p0 = rib[last_tri.r0].GetCommonPart(rib, rib[last_tri.r2]);
                p1 = rib[last_tri.r2].GetCommonPart(rib, rib[last_tri.r0]);
                tri.Add(new TTri(p0, rib.High, p1));
                //
                p0 = rib[last_tri.r1].GetCommonPart(rib, rib[last_tri.r2]);
                p1 = rib[last_tri.r2].GetCommonPart(rib, rib[last_tri.r1]);
                tri.Add(new TTri(p0, p1, rib.High - 1));
            }
        }
        void RebuildMesh()
        {
            TVector<Vector3> vertex = new TVector<Vector3>(6 * (int)Math.Pow(2, sub_div));
            TVector<TRib> rib = new TVector<TRib>(100);//TODO размер динамически от sub_div
            TVector<TTri> tri = new TVector<TTri>(100);
            //
            vertex.Add(new Vector3(0, 0, radius));
            vertex.Add(new Vector3(-radius, 0, 0));
            vertex.Add(new Vector3(0, 0, -radius));
            vertex.Add(new Vector3(radius, 0, 0));
            vertex.Add(new Vector3(0, radius, 0));
            vertex.Add(new Vector3(0, -radius, 0));
            //
            rib.Add(new TRib(0, 4));
            rib.Add(new TRib(1, 4));
            rib.Add(new TRib(2, 4));
            rib.Add(new TRib(3, 4));
            rib.Add(new TRib(0, 1));
            rib.Add(new TRib(1, 2));
            rib.Add(new TRib(2, 3));
            rib.Add(new TRib(3, 0));
            rib.Add(new TRib(0, 5));
            rib.Add(new TRib(1, 5));
            rib.Add(new TRib(2, 5));
            rib.Add(new TRib(3, 5));
            //
            tri.Add(new TTri(0, 1, 4));
            tri.Add(new TTri(1, 2, 5));
            tri.Add(new TTri(2, 3, 6));
            tri.Add(new TTri(3, 0, 7));
            tri.Add(new TTri(8, 4, 9));
            tri.Add(new TTri(9, 5, 10));
            tri.Add(new TTri(10, 6, 11));
            tri.Add(new TTri(11, 7, 8));
            //
            for (int i = 0; i < sub_div; i++)
                SubDiv(vertex, rib, tri);

            mesh.pos.Pop(mesh.pos.Length);
            mesh.normal.Pop(mesh.normal.Length);
            mesh.triangle.Pop(mesh.triangle.Length);
            for (int i = 0; i <= vertex.High; i++)
            {
                Vector3 v = vertex[i];
                v.Normalize();
                v.Multiply(radius);
                vertex[i] = v;
            }
            mesh.pos.Add(vertex);
            mesh.normal.Add(vertex);
            for (int i = 0; i <= vertex.High; i++)
            {
                Vector3 norm = vertex[i];
                norm.Normalize();
                mesh.normal[i] = norm;
            }
            //float max_rib_length_sq = 0;
            min_rad = radius;
            //for (int i = 0; i <= rib.High; i++)
            //{
            //    if (rib[i].cutted) continue;
            //    Vector3 p0 = vertex[rib[i].v0];
            //    Vector3 p1 = vertex[rib[i].v1];
            //    Vector3 p2 = vertex[rib[i].v2];
            //    Vector3 n = Vector3.Cross(p1 - p0, p2 - p0);
            //    n.Normalize();
            //    float temp  = Vector3.Dot(n,p0);
            //    //if (temp > max_rib_length_sq) max_rib_length_sq = temp;
            //    //if (temp < min_rib_length_sq) min_rib_length_sq = temp;
            //}
            //min_rad = (float)Math.Sqrt(radius * radius - max_rib_length_sq /4);
            for (int i = 0; i <= tri.High; i++)
            {
                int v0 = rib[tri[i].r0].GetCommonVert(rib[tri[i].r1]);
                int v1 = rib[tri[i].r1].GetOtherVert(v0);
                int v2 = rib[tri[i].r2].GetOtherVert(v1);
                mesh.triangle.Add(new TTriMesh.TMeshTri(v0, v1, v2, v0, v1, v2, 0, 0, 0));
            }
            for (int i = 0; i <= tri.High; i++)
            {
                if (rib[i].cutted) continue;
                Vector3 p0 = mesh.pos[mesh.triangle[i].p0];
                Vector3 p1 = mesh.pos[mesh.triangle[i].p1];
                Vector3 p2 = mesh.pos[mesh.triangle[i].p2];
                Vector3 n = Vector3.Cross(p1 - p0, p2 - p0);
                n.Normalize();
                float temp = Vector3.Dot(n, p0);
                if (temp < min_rad) min_rad = temp;
                //if (temp > max_rib_length_sq) max_rib_length_sq = temp;
                //if (temp < min_rib_length_sq) min_rib_length_sq = temp;
            }
            need_update = true;
        }
    }
}
