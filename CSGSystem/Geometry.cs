using System;
using Microsoft.DirectX;


namespace CSGSystem
{
    public struct TTriangle
    {
        public Vector3 p0, p1, p2;
        public TTriangle(Vector3 use_p0, Vector3 use_p1, Vector3 use_p2)
        {
            p0 = use_p0;
            p1 = use_p1;
            p2 = use_p2;
        }
        public bool Inters(Vector3 use_p0, Vector3 use_p1, Vector3 use_p2)
        {
            return Inters(new TTriangle(use_p0, use_p1, use_p2));
        }
        float GetInterval(Vector3 dir,Vector3 p0,Vector3 p1,float d0,float d1)
        {
            float p0_proj = Vector3.Dot(dir,p0);
            float p1_proj = Vector3.Dot(dir, p1);
            return p0_proj + (p1_proj - p0_proj) * (d0 / (d0 - d1));
        }
        public bool Inters(TTriangle tri)
        {
            //плоскости треугольников
            Vector3 N0 = Vector3.Cross(p0 - p1, p0 - p2), N1 = Vector3.Cross(tri.p0 - tri.p1, tri.p0 - tri.p2);
            float d0 = -Vector3.Dot(p0, N0);
            float d1 = -Vector3.Dot(tri.p0, N1);
            //расстояние от вершин первого треугольника до плоскости второго
            float d0T0 = Vector3.Dot(p0, N1) + d1;
            float d1T0 = Vector3.Dot(p1, N1) + d1;
            float d2T0 = Vector3.Dot(p2, N1) + d1;
            Vector3 line = Vector3.Cross(N0, N1);

            if(d0T0==0&&d1T0==0&&d2T0==0)return false; //треугольники лежат в одной плоскости
            if (((d0T0 > 0) == (d1T0 > 0)) && ((d0T0 > 0) == (d2T0 > 0))) return false;

            float d0T1 = Vector3.Dot(tri.p0, N0) + d0;
            float d1T1 = Vector3.Dot(tri.p1, N0) + d0;
            float d2T1 = Vector3.Dot(tri.p2, N0) + d0;

            if (((d0T1 > 0) == (d1T1 > 0)) && ((d0T1 > 0) == (d2T1 > 0))) return false;

            //вычисляем проекции ребер треугольников на линию
            float t0T0, t1T0, t0T1, t1T1;
            if ((d0T0 > 0) == (d1T0 > 0))
            {
                t0T0 = GetInterval(line, p1, p2, d1T0, d2T0);
                t1T0 = GetInterval(line, p0, p2, d0T0, d2T0);
            }
            else if ((d2T0 > 0) == (d1T0 > 0))
            {
                t0T0 = GetInterval(line, p2, p0, d2T0, d0T0);
                t1T0 = GetInterval(line, p1, p0, d1T0, d0T0);
            }
            else// if ((d0T0 > 0) == (d2T0 > 0))
            {
                t0T0 = GetInterval(line, p0, p1, d0T0, d1T0);
                t1T0 = GetInterval(line, p2, p1, d2T0, d1T0);
            }

            if ((d0T1 > 0) == (d1T1 > 0))
            {
                t0T1 = GetInterval(line, tri.p1, tri.p2, d1T1, d2T1);
                t1T1 = GetInterval(line, tri.p0, tri.p2, d0T1, d2T1);
            }
            else if ((d2T1 > 0) == (d1T1 > 0))
            {
                t0T1 = GetInterval(line, tri.p2, tri.p0, d2T1, d0T1);
                t1T1 = GetInterval(line, tri.p1, tri.p0, d1T1, d0T1);
            }
            else// if ((d0T1 > 0) == (d2T1 > 0))
            {
                t0T1 = GetInterval(line, tri.p0, tri.p1, d0T1, d1T1);
                t1T1 = GetInterval(line, tri.p2, tri.p1, d2T1, d1T1);
            }
            //определяем пересечение проекций
            float temp;
            if (t0T0 > t1T0) { temp = t0T0; t0T0 = t1T0; t1T0 = temp; }
            if (t0T1 > t1T1) { temp = t0T1; t0T1 = t1T1; t1T1 = temp; }
            return (t0T1 <= t1T0 && t0T0 <= t1T1);
        }
    }
    public struct TPlane
    {
        public Vector3 normal;
        public float dist;//расстояние до начала координат
        public TPlane(Vector3 use_normal, Vector3 point_of_plane)
        {
            normal = use_normal;
            dist = Vector3.Dot(normal, point_of_plane);
        }
        public float DistToPlane(Vector3 pos)
        {
            return Vector3.Dot(pos, normal) - dist;
        }
    }
    public struct TRay
    {
        public Vector3 pos, dir;
        public TRay(Vector3 use_pos, Vector3 use_dir)
        {
            pos = use_pos;
            dir = use_dir;
        }
        public bool Inters(TPlane plane, ref float t)
        {
            float v = Vector3.Dot(plane.normal, dir);
            if (v == 0) return false;
            t = (plane.dist - Vector3.Dot(pos, plane.normal)) / v;
            return t > 0;
        }
        public void PointNearestToOtherRay(TRay r, ref float point)
        {
            Vector3 t = Vector3.Cross(dir, r.dir);
            TPlane p = new TPlane(Vector3.Cross(t, r.dir), r.pos);
            Inters(p, ref point);
        }
        public bool TriangleInters(Vector3 v0, Vector3 v1, Vector3 v2, ref float x, float offset)
        {
            Vector3 normal = (Vector3.Cross((v1 - v0), (v2 - v0)));
            normal.Normalize();
            x = 0;
            if (Inters(new TPlane(normal, v0), ref x))
            {
                Vector3 point = dir;
                point.Multiply(x);
                point += pos;
                return
                    Vector3.Dot(point - v0, Vector3.Cross(v1 - v0, normal)) <= offset &&
                    Vector3.Dot(point - v1, Vector3.Cross(v2 - v1, normal)) <= offset &&
                    Vector3.Dot(point - v2, Vector3.Cross(v0 - v2, normal)) <= offset;
            }
            else return false;
        }
        public bool TriangleInters(Vector3 v0, Vector3 v1, Vector3 v2, ref float x)
        {
            Vector3 normal = (Vector3.Cross((v1 - v0), (v2 - v0)));
            normal.Normalize();
            x = 0;
            if (Inters(new TPlane(normal, v0), ref x))
            {
                Vector3 point = dir;
                point.Multiply(x);
                point += pos;
                return
                    Vector3.Dot(point - v0, Vector3.Cross(v1 - v0, normal)) <= 0.0f &&
                    Vector3.Dot(point - v1, Vector3.Cross(v2 - v1, normal)) <= 0.0f &&
                    Vector3.Dot(point - v2, Vector3.Cross(v0 - v2, normal)) <= 0.0f;
            }
            else return false;
        }
    }
    public struct TAABB
    {
        Vector3 pos, widths;
        float min_x, max_x;
        float min_y, max_y;
        float min_z, max_z;
        bool NotOverlay(float min1, float max1, float min2, float max2)
        {
            return max1 < min2 || min1 > max2;
        }
        public float Get(int dim, int max)
        {
            if (dim == 0)
            { if (max == 0)return min_x; else return max_x; }
            else if (dim == 1)
            { if (max == 0)return min_y; else return max_y; }
            else
            { if (max == 0)return min_z; else return max_z; }
        }
        void InitLimits()
        {
            min_x = pos.X - widths.X / 2.0f;
            max_x = pos.X + widths.X / 2.0f;

            min_y = pos.Y - widths.Y / 2.0f;
            max_y = pos.Y + widths.Y / 2.0f;

            min_z = pos.Z - widths.Z / 2.0f;
            max_z = pos.Z + widths.Z / 2.0f;
        }
        void InitPosWidth()
        {
            pos.X = (max_x + min_x) * 0.5f;
            pos.Y = (max_y + min_y) * 0.5f;
            pos.Z = (max_z + min_z) * 0.5f;
            widths.X = max_x - min_x;
            widths.Y = max_y - min_y;
            widths.Z = max_z - min_z;
        }
        public TAABB(Vector3 use_pos, Vector3 use_widths)
        {
            pos = use_pos;
            widths = use_widths;
            min_x = 0; max_x = 0;
            min_y = 0; max_y = 0;
            min_z = 0; max_z = 0;
            InitLimits();
        }
        public TAABB(float min0, float max0, float min1, float max1, float min2, float max2)
        {
            pos = new Vector3(0, 0, 0);
            widths = new Vector3(0, 0, 0);
            min_x = min0; max_x = max0;
            min_y = min1; max_y = max1;
            min_z = min2; max_z = max2;
            InitPosWidth();
        }
        public Vector3 Pos
        {
            set { pos = value; InitLimits(); }
            get { return pos; }
        }
        public Vector3 Widths
        {
            set { widths = value; InitLimits(); }
            get { return widths; }
        }
        public void Extend(TAABB v)
        {
            bool need_change = false;
            if (v.max_x > max_x) { need_change = true; max_x = v.max_x; }
            if (v.min_x < min_x) { need_change = true; min_x = v.min_x; }
            if (v.max_y > max_y) { need_change = true; max_y = v.max_y; }
            if (v.min_y < min_y) { need_change = true; min_y = v.min_y; }
            if (v.max_z > max_z) { need_change = true; max_z = v.max_z; }
            if (v.min_z < min_z) { need_change = true; min_z = v.min_z; }
            if (need_change) InitPosWidth();
        }
        public bool Contain(Vector3 v)
        {
            return DistanceToPoint(v) <= 0;
        }
        public float DistanceToPoint(Vector3 v)
        {
            float distance = 0;
            float temp;
            if (v.X < min_x)
            {
                temp = min_x - v.X;
                distance += temp * temp;
            }
            else if (v.X > max_x)
            {
                temp = v.X - max_x;
                distance += temp * temp;
            }
            if (v.Y < min_y)
            {
                temp = min_y - v.Y;
                distance += temp * temp;
            }
            else if (v.Y > max_y)
            {
                temp = v.Y - max_y;
                distance += temp * temp;
            }
            if (v.Z < min_z)
            {
                temp = min_z - v.Z;
                distance += temp * temp;
            }
            else if (v.Z > max_z)
            {
                temp = v.Z - max_z;
                distance += temp * temp;
            }
            return distance;
        }
        public float GetAverageSize()
        {
            return (widths.X + widths.Y + widths.Z) * (1.0f / 3.0f);
        }
        public void Extend(Vector3 v)
        {
            bool need_change = false;
            if (v.X > max_x) { need_change = true; max_x = v.X; }
            if (v.X < min_x) { need_change = true; min_x = v.X; }
            if (v.Y > max_y) { need_change = true; max_y = v.Y; }
            if (v.Y < min_y) { need_change = true; min_y = v.Y; }
            if (v.Z > max_z) { need_change = true; max_z = v.Z; }
            if (v.Z < min_z) { need_change = true; min_z = v.Z; }
            if (need_change) InitPosWidth();
        }
        public bool Overlaps(TBoundingSphere sphere)
        {
            return sphere.Radius * sphere.Radius > DistanceToPoint(sphere.Pos);
        }
        public bool Overlaps(TAABB use_aabb)
        {
            for (int i = 0; i < 3; i++)
                if (NotOverlay(Get(i, 0), Get(i, 1), use_aabb.Get(i, 0), use_aabb.Get(i, 1)))
                    return false;
            return true;
        }
        public bool Overlaps(TRay ray)
        {
            //
            float max_length = 10000;
            Vector3 mid = ray.pos + ray.dir * (max_length / 2.0f);
            Vector3 dir = ray.dir;
            float hl = (max_length / 2.0f);
            Vector3 T = pos - mid;
            Vector3 E = widths;
            E.Multiply(0.5f);
            float r;

            // проверяем, является ли одна из осей X,Y,Z разделяющей
            if ((Math.Abs(T.X) > E.X + hl * Math.Abs(dir.X)) ||
                (Math.Abs(T.Y) > E.Y + hl * Math.Abs(dir.Y)) ||
                (Math.Abs(T.Z) > E.Z + hl * Math.Abs(dir.Z)))
                return false;

            // проверяем X ^ dir
            r = E.Y * Math.Abs(dir.Z) + E.Z * Math.Abs(dir.Y);
            if (Math.Abs(T.Y * dir.Z - T.Z * dir.Y) > r)
                return false;

            // проверяем Y ^ dir
            r = E.X * Math.Abs(dir.Z) + E.Z * Math.Abs(dir.X);
            if (Math.Abs(T.Z * dir.X - T.X * dir.Z) > r)
                return false;

            // проверяем Z ^ dir
            r = E.X * Math.Abs(dir.Y) + E.Y * Math.Abs(dir.X);
            if (Math.Abs(T.X * dir.Y - T.Y * dir.X) > r)
                return false;

            return true;
        }
        public void ToSubCube(int i, int k, int t)
        {
            if (i == 0) max_x = (min_x + max_x) * 0.5f;
            else min_x = (min_x + max_x) * 0.5f;
            if (k == 0) max_y = (min_y + max_y) * 0.5f;
            else min_y = (min_y + max_y) * 0.5f;
            if (t == 0) max_z = (min_z + max_z) * 0.5f;
            else min_z = (min_z + max_z) * 0.5f;
        }
    }
    public struct TBoundingSphere
    {
        float radius;
        Vector3 pos;
        public TBoundingSphere(Vector3 use_pos, float use_radius)
        {
            pos = use_pos;
            radius = use_radius;
        }
        public bool Overlaps(TBoundingSphere sphere)
        {
            float t = radius + sphere.radius;
            return (pos - sphere.pos).LengthSq() < t * t;
        }
        bool PointIn(Vector3 point)
        {
            return (pos - point).LengthSq() < radius * radius;
        }
        public TAABB GetAABB()
        {
            return new TAABB(pos, new Vector3(1,1,1) * radius * 2.0f);
        }
        //
        public Vector3 Pos { set { pos = value; } get { return pos; } }
        public float Radius { set { radius = value; } get { return radius; } }
        public bool Overlaps(TRay ray)
        {
            float b; Vector3 t;
            t = ray.pos - pos;
            b = (Vector3.Dot(ray.dir, t)) * 2.0f;
            return b * b - 4 * Vector3.Dot(ray.dir, ray.dir) * (Vector3.Dot(t, t) - radius * radius) >= 0;
        }
    }
    public struct TOBB
    {
        public Matrix orient;
        TAABB aabb;
        public TOBB(Vector3 use_pos, Vector3 use_widths)
        {
            aabb = new TAABB(use_pos, use_widths);
            orient = Matrix.Identity;
        }
        public void SetOrient(Matrix use_orient)
        {
            orient = use_orient;
        }
        public bool Contain(Vector3 v)
        {
            Matrix m = orient;
            m.Invert();//TODO можно лишь транспонировать матрицу 3x3,а смещение вычислять вручную
            return aabb.Contain(Vector3.TransformCoordinate(v, m));
        }
        public bool Overlaps(TRay ray)
        {
            Matrix m = orient;
            m.Invert();//TODO можно лишь транспонировать матрицу 3x3,а смещение вычислять вручную
            TRay modified_ray = new TRay(Vector3.TransformCoordinate(ray.pos, m), Vector3.TransformNormal(ray.dir, m));
            return aabb.Overlaps(modified_ray);
        }
    }
}
