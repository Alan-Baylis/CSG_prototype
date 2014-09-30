using System;
using Microsoft.DirectX;
using Microsoft.DirectX.Direct3D;
using System.Diagnostics;
using System.Windows.Forms;
using System.Collections.Generic;

namespace CSGSystem.CSGObjects
{
    class TCSGBoolOp : TCSGObject
    {
        //TODO для других логических операций доделать 
        List<TCSGObject> operand;
        public enum TBoolOpType
        {
            AND,
            OR,
            NOT
        }
        TBoolOpType op_type;
        public TBoolOpType OpType
        {
            set { op_type = value; need_update = true; }
            get { return op_type; }
        }
        public override void BuildTreeView(TreeNode node)
        {
            if (node.Text != "Деталь") node.Text = "Операция";
            node.Tag = this;
            foreach (TCSGObject v in operand)
                v.BuildTreeView(node.Nodes.Add(""));
        }
        public override void Serialize(System.IO.Stream stream, bool read)
        {
            if (read)
            {
                System.IO.BinaryReader r=new System.IO.BinaryReader(stream);
                SerializeOrient(stream, read);
                op_type = (TBoolOpType)r.ReadUInt32();
                int op_count=r.ReadInt32();
                for (int i = 0; i < op_count; i++)
                {
                    TCSGObjectType type = (TCSGObjectType)r.ReadUInt32();
                    TCSGObject obj = CreateObj(type);
                    obj.Serialize(stream, true);
                    AddOperand(obj);
                }
                need_update = true;
            }
            else
            {
                System.IO.BinaryWriter r = new System.IO.BinaryWriter(stream);
                r.Write((UInt32)TCSGObjectType.BOOL_OP);
                SerializeOrient(stream, read);
                r.Write((UInt32)op_type);
                r.Write(operand.Count);
                for (int i = 0; i < operand.Count; i++)
                {      
                    operand[i].Serialize(stream, false);
                }
            }
        }
        public void RebuildMesh()
        {
            if (operand.Count == 0) return;
            mesh = operand[0].GetTransformedMesh();
            for (int c = 1; c < operand.Count; c++)
            {
                TTriMesh right_mesh = operand[c].GetTransformedMesh();
                TTriMesh right_mesh_not_cutted = operand[c].GetTransformedMesh();
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
                    switch (op_type)
                    {
                        case TBoolOpType.NOT:
                            if (!operand[0].Contain(tri_centre)) v.Add(i);
                            else
                            {
                                for (int k = 1; k < c; k++)
                                    if (operand[k].Contain(tri_centre))
                                    {
                                        v.Add(i);
                                        break;
                                    }
                            }
                            break;
                        case TBoolOpType.OR:
                            for (int k = 0; k < c; k++)
                                if (operand[k].Contain(tri_centre))
                                {
                                    v.Add(i);
                                    break;
                                }
                            break;
                        case TBoolOpType.AND:
                            for (int k = 0; k < c; k++)
                                if (!operand[k].Contain(tri_centre))
                                {
                                    v.Add(i);
                                    break;
                                }
                            break;
                    }
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
                    switch (op_type)
                    {
                        case TBoolOpType.NOT:
                            if (operand[c].Contain(tri_centre)) v.Add(i);
                            break;
                        case TBoolOpType.OR:
                            if (operand[c].Contain(tri_centre)) v.Add(i);
                            break;
                        case TBoolOpType.AND:
                            if (!operand[c].Contain(tri_centre)) v.Add(i);
                            break;
                    }
                }
                mesh.DelTriangles(v);
                if (op_type == TBoolOpType.NOT) right_mesh.InvertNormals();

                mesh.Add(right_mesh);
            }
        }
        bool OperandNeedUpdate()
        {
            bool result = false;
            foreach (TCSGObject v in operand)
                result = v.Update() | result;
            return result;
        }
        public override bool Update()
        {
            if (OperandNeedUpdate() || need_update)
            {
                need_update = false;
                RebuildMesh();
                return true;
            }
            else return false;
        }
        public override bool Contain(Vector3 v)
        {
            switch (op_type)
            {
                case TBoolOpType.NOT:
                    if (operand.Count==0||!operand[0].Contain(v)) return false;
                    for (int i = 1; i < operand.Count; i++)
                        if (operand[i].Contain(v)) return false;
                    return true;
                    break;
                case TBoolOpType.AND:
                    for (int i = 0; i < operand.Count; i++)
                        if (!operand[i].Contain(v)) return false;
                    return true;
                    break;
                case TBoolOpType.OR:
                    for (int i = 0; i < operand.Count; i++)
                        if (operand[i].Contain(v)) return true;
                    return false;
                    break;
                default:
                    {
                        Debug.Assert(false);
                        return false;
                    }
            }
        }
        public TCSGBoolOp()
        {
            operand = new List<TCSGObject>();
            op_type = TBoolOpType.NOT;
        }
        public void AddOperand(TCSGObject use_op)
        {
            operand.Add(use_op);
            need_update = true;
        }
        public void InsertOperand(TCSGObject insert_before,TCSGObject use_op)
        {
            int index=operand.IndexOf(insert_before);
            operand.Insert(index, use_op);
        }
        public void RemoveOperand(TCSGObject use_op)
        {
            operand.Remove(use_op);
            need_update = true;
        }
        public void RemoveAllOperands()
        {
            operand.Clear();
            mesh = null;
        }
    }
}
