using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;

namespace CSGSystem
{
    class TCSGEngine
    {
        List<TCSGObject> objects;
        TTriMeshWithTree scene;
        bool need_props_refresh = false;
        public TCSGEngine()
        {
            objects = new List<TCSGObject>(10);
        }
        public void Add(TCSGObject use_object)
        {
            objects.Add(use_object);
        }
        public void Del(TCSGObject use_object)
        {
            objects.Remove(use_object);
        }
        public bool NeedPropsRefresh()
        {
            bool result = need_props_refresh;
            need_props_refresh = false;
            return result;
        }
        public void RebuildGeometry()
        {
            //need_props_refresh = true;//TODO если обновлять окно свойст здесь, то почему-то не происходит перерисовка моделей

            int last_high = -1,
                normal_offset = 0,
                pos_offset = 0,
                color_offset = 0;
            if (objects.Count < 1) return;
            TTriMeshWithTree[] meshes = new TTriMeshWithTree[objects.Count];
            TTriMesh[] not_cutted_meshes = new TTriMesh[meshes.Length];
            float node_size = 0;
            TVector<int> buffer = new TVector<int>(1000);
            TAABB scene_box = new TAABB();
            for (int i = 0; i < meshes.Length; i++)
            {
                not_cutted_meshes[i] = objects[i].GetTransformedMesh();
                meshes[i] = new TTriMeshWithTree(not_cutted_meshes[i].GetCopy());
                node_size += meshes[i].GetAverageTriSize();
                if (i == 0) scene_box = meshes[i].GetAABB();
                else scene_box.Extend(meshes[i].GetAABB());
            }
            node_size /= meshes.Length;

            scene = new TTriMeshWithTree(node_size*2, scene_box);


            TVector<int> query_result = new TVector<int>(1000);
            TVector<int> query_result1 = new TVector<int>(1000);
            for (int i = 0; i < meshes.Length; i++)
            {
                if (i != 0)
                {
                    //сечём треугольники объекта треугольниками сцены
                    scene.AABBQuery(query_result, meshes[i].GetAABB());
                    for (int k = 0; k <= query_result.High; k++)
                    {
                        meshes[i].Cut(buffer, scene.GetMesh(), query_result[k], scene.GetMesh().GetTriAABB(query_result[k]));
                    }
                    //сечём треугольники сцены треугольниками объекта(треугольники объекта не сечённые)
                    for (int k = 0; k <= not_cutted_meshes[i].triangle.High; k++)
                    {
                        scene.Cut(buffer, not_cutted_meshes[i], k, not_cutted_meshes[i].GetTriAABB(k));
                    }
                    scene.TrianglesInMeshQuery(buffer, query_result, meshes[i], true);
                    meshes[i].TrianglesInMeshQuery(buffer, query_result1, scene, false);
                    scene.DelTriangles(query_result);
                    meshes[i].DelTriangles(query_result1);
                    meshes[i].InverseNormals();
                }
                //добавляем объект в сцену
                scene.Add(meshes[i]);
            }
        }
        public TTriMesh GetScene()
        {
            return scene.GetMesh();
        }
    }
}
