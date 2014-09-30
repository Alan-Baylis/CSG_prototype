using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;

namespace CSGSystem
{
    class TOctree
    {
        public class TOctreeNode
        {
            public int is_end;
            public TAABB dims;
            public int[] child;
            public TOctreeNode()
            {
                is_end = -1;
                child = new int[8];
                for (int i = 0; i < 8; i++) child[i] = -1;
            }
            public void SetChild(int i, int k, int t, int child_val)
            {
                child[i * 4 + k * 2 + t] = child_val;
            }
            public int GetChild(int i, int k, int t)
            {
                //i - min\max x
                //k - min\max y
                //t - min\max z
                return child[i * 4 + k * 2 + t];
            }
        }
        TIndexedVector<TOctreeNode> nodes;
        float min_node_size;
        int root;
        TIndexedVector<TVector<int>> items;
        public TOctree(float use_min_node_size, TAABB use_dims)
        {
            nodes = new TIndexedVector<TOctreeNode>(1000);
            items = new TIndexedVector<TVector<int>>(1000);
            min_node_size = use_min_node_size;
            root = nodes.New();
            Init(root, use_dims);
        }

        void Init(int node, TAABB use_dims)
        {
            TOctreeNode new_node = new TOctreeNode();

            new_node.dims = use_dims;
            if ((new_node.dims.Get(0, 1) - new_node.dims.Get(0, 0)) < min_node_size * 2)
            {
                new_node.is_end = items.New();
                items[new_node.is_end] = new TVector<int>(5);
            }
            nodes[node] = new_node;
        }
        public void Add(TAABB use_dims, int data)
        {
            if (use_dims.Overlaps(nodes[root].dims))
                Add(nodes[root], use_dims, data);
        }

        void Add(TOctreeNode node, TAABB use_dims, int data)
        {
            if (node.is_end != -1)
            {
                for (int i = 0; i <= items[node.is_end].High; i++)
                    if (items[node.is_end][i] == data) Debug.Assert(false);//в узле дерева не должно быть повторяющихся значений
                items[node.is_end].Add(data);
            }
            else
            {
                int i, k, t;
                for (i = 0; i < 2; i++)
                    if ((use_dims.Get(0, i) < (node.dims.Get(0, 0) + node.dims.Get(0, 1)) * 0.5) != (i == 1))
                        for (k = 0; k < 2; k++)
                            if ((use_dims.Get(1, k) < (node.dims.Get(1, 0) + node.dims.Get(1, 1)) * 0.5) != (k == 1))
                                for (t = 0; t < 2; t++)
                                    if ((use_dims.Get(2, t) < (node.dims.Get(2, 0) + node.dims.Get(2, 1)) * 0.5) != (t == 1))
                                    {
                                        if (node.GetChild(i, k, t) == -1)
                                        {
                                            node.SetChild(i, k, t, nodes.New());
                                            TAABB temp = node.dims;
                                            temp.ToSubCube(i, k, t);
                                            Init(node.GetChild(i, k, t), temp);
                                        }
                                        Add(nodes[node.GetChild(i, k, t)], use_dims, data);
                                    }
            }
        }
        public void Del(TAABB use_dims, int data)
        {
            if (use_dims.Overlaps(nodes[root].dims))
                Del(nodes[root], use_dims, data);
        }

        void Del(TOctreeNode node, TAABB use_dims, int data)
        {
            if (node.is_end != -1)
            {
                bool found = false;
                for (int i = 0; i <= items[node.is_end].High; i++)
                    if (items[node.is_end][i] == data)
                    {
                        found = true;
                        items[node.is_end].Del(i);
                        break;
                    }
                if (!found) Debug.Assert(false);//значение должно присутствовать		
            }
            else
            {
                int i, k, t;
                for (i = 0; i < 2; i++)
                    if ((use_dims.Get(0, i) < (node.dims.Get(0, 0) + node.dims.Get(0, 1)) * 0.5) != (i == 1))
                        //TODO из-за погрешностей вычислений могут возникнуть проблемы в void Del(TOctreeNode node, TAABB use_dims, int data)
                        for (k = 0; k < 2; k++)
                            if ((use_dims.Get(1, k) < (node.dims.Get(1, 0) + node.dims.Get(1, 1)) * 0.5) != (k == 1))
                                for (t = 0; t < 2; t++)
                                    if ((use_dims.Get(2, t) < (node.dims.Get(2, 0) + node.dims.Get(2, 1)) * 0.5) != (t == 1))
                                        Del(nodes[node.GetChild(i, k, t)], use_dims, data);
            }
        }
        void DeleteRepeatingValues(TVector<int> result)
        {
            if (result.High >= 0)
            {
                result.Sort();
                int last_val = result[0];
                int offset = 0;
                for (int i = 1; i <= result.High; i++)
                    if (result[i] == last_val)
                        offset++;
                    else if (offset != 0)
                    {
                        last_val = result[i];
                        result[i - offset] = result[i];
                    }
                result.Pop(offset);
            }
        }
        public void RayQuery(TVector<int> result, TRay ray)
        {
            result.Pop(result.Length);
            if (nodes[root].dims.Overlaps(ray))
                RayQuery(nodes[root], result, ray);
            DeleteRepeatingValues(result);
        }
        void RayQuery(TOctreeNode node, TVector<int> result, TRay ray)
        {
            if (node.is_end != -1)
                result.Add(items[node.is_end]);
            else
                for (int i = 0; i < 8; i++)
                    if (node.child[i] != -1)
                        if (nodes[node.child[i]].dims.Overlaps(ray))
                            RayQuery(nodes[node.child[i]], result, ray);
        }
        public void AABBQuery(TVector<int> result, TAABB aabb)
        {
            result.Pop(result.Length);
            if (aabb.Overlaps(nodes[root].dims))
                AABBQuery(nodes[root], result, aabb);
            DeleteRepeatingValues(result);
        }
        void AABBQuery(TOctreeNode node, TVector<int> result, TAABB aabb)
        {
            if (node.is_end != -1)
                result.Add(items[node.is_end]);
            else
            {
                int i, k, t;
                for (i = 0; i < 2; i++)
                    if ((aabb.Get(0, i) < (node.dims.Get(0, 0) + node.dims.Get(0, 1)) * 0.5) != (i == 1))
                        for (k = 0; k < 2; k++)
                            if ((aabb.Get(1, k) < (node.dims.Get(1, 0) + node.dims.Get(1, 1)) * 0.5) != (k == 1))
                                for (t = 0; t < 2; t++)
                                    if ((aabb.Get(2, t) < (node.dims.Get(2, 0) + node.dims.Get(2, 1)) * 0.5) != (t == 1))
                                        if (node.GetChild(i, k, t) != -1)
                                            AABBQuery(nodes[node.GetChild(i, k, t)], result, aabb);
            }
        }
    }
}
