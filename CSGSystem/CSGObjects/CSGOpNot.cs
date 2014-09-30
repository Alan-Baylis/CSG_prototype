using System;
using Microsoft.DirectX;
using Microsoft.DirectX.Direct3D;
using System.Diagnostics;

namespace CSGSystem.CSGObjects
{
    class TCSGOpNot : TCSGObject
    {
        TCSGObject left, right;
        public override void RebuildGeometry()
        {
            if (left == null) return;
            if(left!=null)mesh = left.GetTransformedMesh();
            TTriMesh right_mesh = right.GetTransformedMesh();
            TTriMesh right_mesh_not_cutted = right.GetTransformedMesh();
            TVector<int> v = new TVector<int>(100);

            for (int i = 0; i <= mesh.triangle.High; i++)
            {
                right_mesh.Cut(mesh.pos[mesh.triangle[i].p0], mesh.pos[mesh.triangle[i].p1], mesh.pos[mesh.triangle[i].p2]);
            }

            for (int i = 0; i <= right_mesh_not_cutted.triangle.High; i++)
            {
                mesh.Cut(
                    right_mesh_not_cutted.pos[right_mesh_not_cutted.triangle[i].p0],
                    right_mesh_not_cutted.pos[right_mesh_not_cutted.triangle[i].p1],
                    right_mesh_not_cutted.pos[right_mesh_not_cutted.triangle[i].p2]);
            }

            for (int i = 0; i <= right_mesh.triangle.High; i++)
            {
                 Vector3 tri_centre =
                    right_mesh.pos[right_mesh.triangle[i].p0] +
                    right_mesh.pos[right_mesh.triangle[i].p1] +
                    right_mesh.pos[right_mesh.triangle[i].p2];
                tri_centre.Multiply(1.0f / 3.0f);
                if (!left.Contain(tri_centre)) v.Add(i);
            }
            right_mesh.DelTriangles(v);

            v.Pop(v.Length);
            for (int i = 0; i <= mesh.triangle.High; i++)
            {
                Vector3 tri_centre =
                   mesh.pos[mesh.triangle[i].p0] +
                   mesh.pos[mesh.triangle[i].p1] +
                   mesh.pos[mesh.triangle[i].p2];
                tri_centre.Multiply(1.0f / 3.0f);
                if (right.Contain(tri_centre)) v.Add(i);
            }
            mesh.DelTriangles(v);
           right_mesh.InvertNormals();

            mesh.Add(right_mesh);
        }
        public override bool Contain(Vector3 v)
        {
            return left.Contain(v) && !right.Contain(v);
        }
        public TCSGOpNot(TCSGObject use_parent)
            : base(use_parent)
        {
        }
        public void  SetLeft(TCSGObject use_left)
        {
            left=use_left;
        }
        public void SetRight(TCSGObject use_right)
        {
            right = use_right;
        }
    }
}
