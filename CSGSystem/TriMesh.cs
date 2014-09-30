using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.DirectX;
using Microsoft.DirectX.Direct3D;
using System.Diagnostics;

namespace CSGSystem
{
    class TTriMesh
    {
        public struct TMeshTri
        {
            public int p0, p1, p2;
            public int n0, n1, n2;
            // public int c0,c1,c2;
            public TMeshTri(int _p0, int _p1, int _p2, int _n0, int _n1, int _n2, int _c0, int _c1, int _c2)
            {
                p0 = _p0;
                p1 = _p1;
                p2 = _p2;

                n0 = _n0;
                n1 = _n1;
                n2 = _n2;

                //c0 = _c0;
                //c1 = _c1;
                //c2 = _c2;
            }
            public int GetNormal(int id)
            {
                Debug.Assert(id >= 0 && id <= 2);
                switch (id)
                {
                    case 0: return n0;
                    case 1: return n1;
                    case 2: return n2;
                    default: return -1;
                }
            }
            public int GetPos(int id)
            {
                Debug.Assert(id >= 0 && id <= 2);
                switch (id)
                {
                    case 0: return p0;
                    case 1: return p1;
                    case 2: return p2;
                    default: return -1;
                }
            }
        }
        public TVector<Vector3> pos;
        public TVector<Vector3> normal;
        // public TVector<int> color;
        public TVector<TMeshTri> triangle;

        public TTriMesh GetCopy()
        //создает глубокую копию
        {
            TTriMesh result = new TTriMesh();
            result.pos.Add(pos);
            result.normal.Add(normal);
            result.triangle.Add(triangle);
            //result.tri_aabb.Add(tri_aabb);
            //result.aabb=aabb;
            //result.average_tri_size=average_tri_size;
            return result;
        }
        public TTriMesh()
        {
            pos = new TVector<Vector3>(1000);
            normal = new TVector<Vector3>(1000);
            //color = new TVector<int>(1000);
            triangle = new TVector<TMeshTri>(1000);
        }
        public TAABB GetTriAABB(int tri)
        {
            TAABB result = new TAABB(pos[triangle[tri].p0], new Vector3(0, 0, 0));
            result.Extend(pos[triangle[tri].p1]);
            result.Extend(pos[triangle[tri].p2]);
            return result;
        }
        public CustomVertex.PositionNormal[] GetRenderData()
        {
            //CustomVertex.PositionNormalColored[] result=new CustomVertex.PositionNormalColored[triangle.Length*3];
            CustomVertex.PositionNormal[] result = new CustomVertex.PositionNormal[triangle.Length * 3];
            for (int i = 0; i <= triangle.High; i++)
            {
                result[i * 3 + 0].Position = pos[triangle[i].p0];
                result[i * 3 + 0].Normal = normal[triangle[i].n0];
                //result[i * 3 + 0].Color = color[triangle[i].c1];

                result[i * 3 + 1].Position = pos[triangle[i].p1];
                result[i * 3 + 1].Normal = normal[triangle[i].n1];
                //result[i * 3 + 1].Color = color[triangle[i].c2];

                result[i * 3 + 2].Position = pos[triangle[i].p2];
                result[i * 3 + 2].Normal = normal[triangle[i].n2];
                //result[i * 3 + 2].Color = color[triangle[i].c3];
            }
            return result;
        }
        public void InvertNormals()
        {
            for (int i = 0; i <= triangle.High; i++)
            {
                triangle[i] = new TMeshTri(
                    triangle[i].p0, triangle[i].p2, triangle[i].p1, triangle[i].n0, triangle[i].n2, triangle[i].n1, 0, 0, 0);
            }
            for (int i = 0; i <= normal.High; i++)
                normal[i] = -normal[i];
        }
        Vector3 Blend(Vector3 v0, Vector3 v1, float factor)
        {
            v0.Multiply(1.0f - factor);
            v1.Multiply(factor);
            return v0 + v1;
        }
        public void DelTriangles(TVector<int> tri)
        {
            tri.Sort();
            for (int i = tri.High; i >= 0; i--)
            {
                triangle.Del(tri[i]);
            }
        }
        void CutTriangleByPlane(TPlane plane, int tri)
        {
            TMeshTri t = triangle[tri];
            float
                d0 = plane.DistToPlane(pos[t.p0]),
                d1 = plane.DistToPlane(pos[t.p1]),
                d2 = plane.DistToPlane(pos[t.p2]);
            bool d0_sign = d0 >= 0;
            bool d1_sign = d1 >= 0;
            bool d2_sign = d2 >= 0;
            int vert0 = -1, vert1 = -1, vert2 = -1;
            {
                int zero_count = 0;
                if (d0 == 0.0f) zero_count++;
                if (d1 == 0.0f) zero_count++;
                if (d2 == 0.0f) zero_count++;
                if (zero_count == 3 || zero_count == 2) return;
            }
            if (d0 == 0.0f || d1 == 0.0f || d2 == 0.0f)
            {
                float x = 0;
                if (d0 == 0.0f)
                {
                    if (d1_sign == d2_sign) return;
                    vert0 = 0;
                    vert1 = 1;
                    vert2 = 2;
                    x = d1 / (d1 - d2);
                }
                else if (d1 == 0.0f)
                {
                    if (d0_sign == d2_sign) return;
                    vert0 = 1;
                    vert1 = 2;
                    vert2 = 0;
                    x = d2 / (d2 - d0);
                }
                else// if (d2 == 0.0f)
                {
                    if (d0_sign == d1_sign) return;
                    vert0 = 2;
                    vert1 = 0;
                    vert2 = 1;
                    x = d0 / (d0 - d1);
                }
                Vector3 new_pos = Blend(pos[t.GetPos(vert1)], pos[t.GetPos(vert2)], x);
                Vector3 new_normal = Blend(normal[t.GetNormal(vert1)], normal[t.GetNormal(vert2)], x);
                new_normal.Normalize();
                Debug.Assert(!float.IsNaN(new_pos.X) && !float.IsNaN(new_pos.Y) && !float.IsNaN(new_pos.Z));
                pos.Add(new_pos);
                normal.Add(new_normal);
                TMeshTri temp = triangle[tri];
                triangle[tri] = new TMeshTri(
                     temp.GetPos(vert0), temp.GetPos(vert1), pos.High,
                     temp.GetNormal(vert0), temp.GetNormal(vert1), normal.High,
                     0, 0, 0);
                triangle.Add(new TMeshTri(
                    temp.GetPos(vert0), pos.High, temp.GetPos(vert2),
                    temp.GetNormal(vert0), normal.High, temp.GetNormal(vert2),
                    0, 0, 0));
            }
            else
            {
                float x1 = 0, x2 = 0;
                if ((d0_sign == d1_sign) && (d0_sign != d2_sign))
                {
                    vert0 = 2;
                    vert1 = 0;
                    vert2 = 1;
                    x1 = d2 / (d2 - d0);
                    x2 = d2 / (d2 - d1);
                }
                else if ((d0_sign == d2_sign) && (d0_sign != d1_sign))
                {
                    vert0 = 1;
                    vert1 = 2;
                    vert2 = 0;
                    x1 = d1 / (d1 - d2);
                    x2 = d1 / (d1 - d0);
                }
                else if ((d1_sign == d2_sign) && (d0_sign != d1_sign))
                {
                    vert0 = 0;
                    vert1 = 1;
                    vert2 = 2;
                    x1 = d0 / (d0 - d1);
                    x2 = d0 / (d0 - d2);
                }
                else return;//треугольники могут пересекаются из-за неточностей 
                Vector3 new_pos1 = Blend(pos[t.GetPos(vert0)], pos[t.GetPos(vert1)], x1);
                Vector3 new_pos2 = Blend(pos[t.GetPos(vert0)], pos[t.GetPos(vert2)], x2);
                Vector3 new_normal1 = Blend(normal[t.GetNormal(vert0)], normal[t.GetNormal(vert1)], x1);
                Vector3 new_normal2 = Blend(normal[t.GetNormal(vert0)], normal[t.GetNormal(vert2)], x2);
                new_normal1.Normalize();
                new_normal2.Normalize();
                pos.Add(new_pos1);
                pos.Add(new_pos2);
                Debug.Assert(!float.IsNaN(new_pos1.X) && !float.IsNaN(new_pos1.Y) && !float.IsNaN(new_pos1.Z));
                Debug.Assert(!float.IsNaN(new_pos2.X) && !float.IsNaN(new_pos2.Y) && !float.IsNaN(new_pos2.Z));
                normal.Add(new_normal1);
                normal.Add(new_normal2);
                TMeshTri temp = triangle[tri];
                //TODO выбирать наиболее короткое ребро
                triangle[tri] = new TMeshTri(
                     temp.GetPos(vert0), pos.High - 1, pos.High,
                     temp.GetNormal(vert0), normal.High - 1, normal.High,
                     0, 0, 0);
                triangle.Add(new TMeshTri(
                    pos.High - 1, temp.GetPos(vert1), temp.GetPos(vert2),
                    normal.High - 1, temp.GetNormal(vert1), temp.GetNormal(vert2),
                    0, 0, 0));
                triangle.Add(new TMeshTri(
                    pos.High - 1, temp.GetPos(vert2), pos.High,
                    normal.High - 1, temp.GetNormal(vert2), normal.High,
                    0, 0, 0));
            }
        }
        public void Cut(Vector3 p0,Vector3 p1,Vector3 p2)
        {
            TTriangle tri = new TTriangle(p0, p1, p2);
            int high = triangle.High;
            for (int i = 0; i <= high; i++)
            {
                if(tri.Inters(pos[triangle[i].p0],pos[triangle[i].p1],pos[triangle[i].p2]))
                {
                    Vector3 normal = (Vector3.Cross((p1 - p0), (p2 - p0)));
                    normal.Normalize();
                    CutTriangleByPlane(new TPlane(normal, p0), i);
                }
            }
        }
        public void Add(TTriMesh use_mesh)
        {
            int normal_offset = normal.Length;
            int pos_offset = pos.Length;
            //int color_offset = color.Length;

            pos.Add(use_mesh.pos);
            normal.Add(use_mesh.normal);

            //scene.color.Add(use_mesh.color);
            int last_high = triangle.High;
            triangle.Add(use_mesh.triangle);

            for (int k = last_high + 1; k <= triangle.High; k++)
            {
                triangle[k] = new TMeshTri(
                triangle[k].p0 + pos_offset,
                triangle[k].p1 + pos_offset,
                triangle[k].p2 + pos_offset,
                triangle[k].n0 + normal_offset,
                triangle[k].n1 + normal_offset,
                triangle[k].n2 + normal_offset,
                0, 0, 0);//TODO
            }
        }
    }
}
