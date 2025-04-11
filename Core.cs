using MelonLoader;
using LibSM64;
using UnityEngine;
using System.Security.Cryptography;
using System.Text;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using ObjLoader.Loader.Loaders;

[assembly: MelonInfo(typeof(SM64Mod.Core), "mario-06", "1.0.0", "Headshotnoby/headshot2017", null)]
[assembly: MelonGame("Nights of Kronos", "Sonic the Hedgehog")]

namespace SM64Mod
{
    public class Core : MelonMod
    {
        static List<SM64Mario> _marios = new List<SM64Mario>();
        static List<SM64DynamicTerrain> _surfaceObjects = new List<SM64DynamicTerrain>();

        public override void OnInitializeMelon()
        {
            byte[] rom;

            try
            {
                rom = File.ReadAllBytes("sm64.z64");
            }
            catch (FileNotFoundException)
            {
                LoggerInstance.Msg("Super Mario 64 US ROM 'sm64.z64' not found");
                return;
            }

            using (var cryptoProvider = new SHA1CryptoServiceProvider())
            {
                byte[] hash = cryptoProvider.ComputeHash(rom);
                StringBuilder result = new StringBuilder(4 * 2);

                for (int i = 0; i < 4; i++)
                    result.Append(hash[i].ToString("x2"));

                string hashStr = result.ToString();

                if (hashStr != "9bef1128")
                {
                    LoggerInstance.Msg($"Super Mario 64 US ROM 'sm64.z64' SHA-1 mismatch\nExpected: 9bef1128\nYour copy: {hashStr}\n\nPlease supply the correct ROM.");
                    return;
                }
            }

            Interop.GlobalInit(rom);
            Application.logMessageReceived += logCallback;
        }

        void logCallback(string condition, string stackTrace, LogType type)
        {
            LoggerInstance.Msg($"'{condition}' {stackTrace}");
        }

        public override void OnSceneWasUnloaded(int buildIndex, string sceneName)
        {
            _surfaceObjects.Clear();
            _marios.Clear();
        }

        public override void OnSceneWasInitialized(int buildIndex, string sceneName)
        {
            LoggerInstance.Msg($"{buildIndex} {sceneName}");

            if (buildIndex >= 9 && buildIndex <= 70)
            {
                List<GameObject> allSurfaces = new List<GameObject>();
                MeshCollider[] meshCols = GameObject.FindObjectsOfType<MeshCollider>();
                BoxCollider[] boxCols = GameObject.FindObjectsOfType<BoxCollider>();

                for (int i = 0; i < meshCols.Length; i++)
                {
                    MeshCollider c = meshCols[i];
                    if (c.isTrigger)
                        continue;

                    GameObject surfaceObj = new GameObject($"SM64_SURFACE_MESH ({c.name})");
                    MeshCollider surfaceMesh = surfaceObj.AddComponent<MeshCollider>();
                    //MeshRenderer rend = surfaceObj.AddComponent<MeshRenderer>();
                    //MeshFilter filt = surfaceObj.AddComponent<MeshFilter>();
                    surfaceObj.AddComponent<SM64StaticTerrain>();
                    surfaceObj.transform.rotation = c.transform.rotation;
                    surfaceObj.transform.position = c.transform.position;

                    Mesh ogMesh = c.sharedMesh;
                    Mesh mesh = ogMesh;

                    if (!ogMesh.isReadable)
                    {
                        /*
                        Mesh copy = ReadOBJFile(buildIndex, c.name);
                        List<int> _tris = new List<int>();
                        for (int j = 0; j < ogMesh.subMeshCount; j++)
                        {
                            ogMesh.SetTriangles(copy.GetTriangles(j), j);
                        }
                        */
                        mesh = ReadOBJFile(buildIndex, c.name);
                    }
                    else
                    {
                        List<int> tris = new List<int>();
                        LoggerInstance.Msg($"{ogMesh.name} {ogMesh.subMeshCount} {ogMesh.vertexCount} {ogMesh.vertices.Length} {ogMesh.triangles.Length}");
                        for (int j = 0; j < ogMesh.subMeshCount; j++)
                        {
                            int[] sub = ogMesh.GetTriangles(j);
                            for (int k = 0; k < sub.Length; k++)
                                tris.Add(sub[k]);
                        }

                        mesh = new Mesh();
                        mesh.name = $"SM64_MESH {i}";
                        mesh.SetVertices(new List<Vector3>(ogMesh.vertices));
                        mesh.SetTriangles(tris, 0);
                    }

                    surfaceMesh.sharedMesh = mesh;
                    //filt.sharedMesh = mesh;
                    allSurfaces.Add(surfaceObj);
                }

                LoggerInstance.Msg($"{meshCols.Length} meshcol");

                /*
                for (var i = 0; i < boxCols.Length; i++)
                {
                    // This isn't perfect but it kinda works for now
                    BoxCollider c = boxCols[i];
                    if (c.isTrigger)
                        continue;

                    GameObject surfaceObj = new GameObject($"SM64_SURFACE_BOX ({c.name})");
                    MeshCollider surfaceMesh = surfaceObj.AddComponent<MeshCollider>();
                    MeshFilter filter = surfaceObj.AddComponent<MeshFilter>();
                    MeshRenderer renderer = surfaceObj.AddComponent<MeshRenderer>();
                    surfaceObj.AddComponent<SM64StaticTerrain>();
                    surfaceObj.transform.rotation = c.transform.rotation;
                    surfaceObj.transform.position = c.transform.position;

                    Mesh mesh = new Mesh();
                    mesh.name = $"SM64_MESH {i}";
                    mesh.SetVertices(GetColliderVertexPositions(c));
                    mesh.SetTriangles(new int[] {
                        // min Y
                        0, 1, 4,
                        5, 4, 1,

                        // max Y
                        2, 3, 6,
                        7, 6, 3,

                        // min X
                        //2, 1, 0,
                        //1, 2, 3,

                        // max X
                        //4, 5, 6,
                        //7, 6, 5,

                        // min Z
                        //4, 2, 0,
                        //2, 4, 6,
                    }, 0);
                    surfaceMesh.sharedMesh = mesh;
                    filter.sharedMesh = mesh;
                }
                RefreshStaticTerrain();
                */

                // "p" is the player object/component in this case.
                // You'll need to get this object yourself
                PlayerBase p = GameObject.FindWithTag("Player").GetComponent<PlayerBase>();
                if (p != null)
                {
                    Renderer[] r = p.GetComponentsInChildren<Renderer>();
                    Material material = null;
                    for (int i = 0; i < r.Length; i++)
                    {
                        //LoggerInstance.Msg($"MAT NAME {i} '{r[i].material.name}' '{r[i].material.shader.name}'");

                        // Make the original player object invisible by forcing the material to not render
                        //r[i].forceRenderingOff = true;

                        // Change this with the shader that you want. You'll have to play around a bit
                        if (material == null && r[i].material.shader.name.StartsWith("Standard"))
                            material = Material.Instantiate<Material>(r[i].material);
                    }

                    if (material != null)
                    {
                        material.SetTexture("_BaseMap", Interop.marioTexture);
                        material.SetColor("_BaseColor", Color.white);
                    }

                    // Uncomment this to create a test SM64 surface at the player's spawn position
                    int w = 16;
                    Vector3 P = p.transform.position;
                    P.y -= 2;
                    GameObject surfaceObj = new GameObject("SM64_SURFACE");
                    MeshCollider surfaceMesh = surfaceObj.AddComponent<MeshCollider>();
                    surfaceObj.AddComponent<SM64StaticTerrain>();
                    Mesh mesh = new Mesh();
                    mesh.name = "TEST_MESH";
                    mesh.SetVertices(
                        new List<Vector3>
                        {
                            new Vector3(P.x-w,P.y,P.z-w), new Vector3(P.x+w,P.y,P.z+w), new Vector3(P.x+w,P.y,P.z-w),
                            new Vector3(P.x+w,P.y,P.z+w), new Vector3(P.x-w,P.y,P.z-w), new Vector3(P.x-w,P.y,P.z+w),
                        }
                    );
                    mesh.SetTriangles(new int[] { 0, 1, 2, 3, 4, 5 }, 0);
                    surfaceMesh.sharedMesh = mesh;
                    allSurfaces.Add(surfaceObj);
                    RefreshStaticTerrain();

                    foreach (GameObject obj in allSurfaces)
                        GameObject.Destroy(obj);

                    GameObject marioObj = new GameObject("SM64_MARIO");
                    marioObj.transform.position = p.transform.position + new Vector3(0, 0.4f, 0);
                    LoggerInstance.Msg($"spawn {p.transform.position.x} {p.transform.position.y} {p.transform.position.z}");
                    SM64InputGame input = marioObj.AddComponent<SM64InputGame>();
                    SM64Mario mario = marioObj.AddComponent<SM64Mario>();
                    if (mario.spawned)
                    {
                        mario.SetMaterial(material);
                        RegisterMario(mario);
                        mario.p06 = p;
                        input.p06 = p;
                        mario.keepLocked = Time.fixedTime + 0.1f;
                    }
                    else
                        LoggerInstance.Msg("Failed to spawn Mario");
                }
            }
        }

        public override void OnUpdate()
        {
            foreach (var o in _surfaceObjects)
                o.contextUpdate();

            foreach (var o in _marios)
            {
                if (!o.p06)
                    continue;

                bool IsDead = (bool)typeof(PlayerBase).GetField("IsDead", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(o.p06);
                if (IsDead)
                    o.Kill();
                else
                    o.SetHealth(0x800);
                o.contextUpdate();

                bool LockControls = 
                    (bool)typeof(PlayerBase).GetField("LockControls", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(o.p06) ||
                    o.p06.GetPrefab("sonic_fast") ||
                    o.p06.GetState() == "Pole";
                bool overrideSM64 = LockControls || Time.fixedTime < o.keepLocked;
                SM64InputGame input = (SM64InputGame)o.inputProvider;
                input.locked = overrideSM64;

                if (overrideSM64)
                {
                    Quaternion rot = (Quaternion)typeof(PlayerBase).GetField("GeneralMeshRotation", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(o.p06);
                    float angle = rot.eulerAngles.y;
                    //if (angle > 180) angle -= 360;
                    //if (angle < -180) angle += 360;

                    o.SetPosition(o.p06.transform.position + new Vector3(0,-0.25f,0));
                    o.SetVelocity(new Vector3(0, o.p06._Rigidbody.velocity.y*3, 0));
                    o.SetFaceAngle(-angle / 180 * Mathf.PI);
                    o.marioRendererObjectRoot.transform.eulerAngles = rot.eulerAngles;
                    o.marioRendererObject.transform.localEulerAngles = new Vector3(0, rot.eulerAngles.y, 0);
                    if (LockControls)
                        o.keepLocked = Time.fixedTime+0.1f;
                }
                else
                {
                    typeof(PlayerBase).GetField("CurSpeed", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(o.p06, 0f);
                    o.p06.transform.position = o.transform.position + new Vector3(0, 0.25f, 0);
                    o.p06.transform.eulerAngles = new Vector3(o.p06.transform.eulerAngles.x, -o.marioState.faceAngle / Mathf.PI * 180, o.p06.transform.eulerAngles.z);
                    o.marioRendererObjectRoot.transform.eulerAngles = Vector3.zero;
                    o.marioRendererObject.transform.eulerAngles = Vector3.zero;
                }
            }
        }

        public override void OnFixedUpdate()
        {
            foreach (var o in _surfaceObjects)
                o.contextFixedUpdate();

            foreach (var o in _marios)
                o.contextFixedUpdate();
        }

        public override void OnApplicationQuit()
        {
            Interop.GlobalTerminate();
        }

        public void RefreshStaticTerrain()
        {
            Interop.StaticSurfacesLoad(Utils.GetAllStaticSurfaces());
        }

        public void RegisterMario(SM64Mario mario)
        {
            if (!_marios.Contains(mario))
                _marios.Add(mario);
        }

        public void UnregisterMario(SM64Mario mario)
        {
            if (_marios.Contains(mario))
                _marios.Remove(mario);
        }

        public void RegisterSurfaceObject(SM64DynamicTerrain surfaceObject)
        {
            if (!_surfaceObjects.Contains(surfaceObject))
                _surfaceObjects.Add(surfaceObject);
        }

        public void UnregisterSurfaceObject(SM64DynamicTerrain surfaceObject)
        {
            if (_surfaceObjects.Contains(surfaceObject))
                _surfaceObjects.Remove(surfaceObject);
        }

        List<Vector3> GetColliderVertexPositions(BoxCollider col)
        {
            var trans = col.transform;
            var min = (col.center - col.size * 0.5f);
            var max = (col.center + col.size * 0.5f);

            Vector3 savedPos = trans.position;

            var P000 = trans.TransformPoint(new Vector3(min.x, min.y, min.z));
            var P001 = trans.TransformPoint(new Vector3(min.x, min.y, max.z));
            var P010 = trans.TransformPoint(new Vector3(min.x, max.y, min.z));
            var P011 = trans.TransformPoint(new Vector3(min.x, max.y, max.z));
            var P100 = trans.TransformPoint(new Vector3(max.x, min.y, min.z));
            var P101 = trans.TransformPoint(new Vector3(max.x, min.y, max.z));
            var P110 = trans.TransformPoint(new Vector3(max.x, max.y, min.z));
            var P111 = trans.TransformPoint(new Vector3(max.x, max.y, max.z));

            return new List<Vector3> { P000, P001, P010, P011, P100, P101, P110, P111 };
            /*
            var vertices = new Vector3[8];
            var thisMatrix = col.transform.localToWorldMatrix;
            var storedRotation = col.transform.rotation;
            col.transform.rotation = Quaternion.identity;

            var extents = col.bounds.extents;
            vertices[0] = thisMatrix.MultiplyPoint3x4(-extents);
            vertices[1] = thisMatrix.MultiplyPoint3x4(new Vector3(-extents.x, -extents.y, extents.z));
            vertices[2] = thisMatrix.MultiplyPoint3x4(new Vector3(-extents.x, extents.y, -extents.z));
            vertices[3] = thisMatrix.MultiplyPoint3x4(new Vector3(-extents.x, extents.y, extents.z));
            vertices[4] = thisMatrix.MultiplyPoint3x4(new Vector3(extents.x, -extents.y, -extents.z));
            vertices[5] = thisMatrix.MultiplyPoint3x4(new Vector3(extents.x, -extents.y, extents.z));
            vertices[6] = thisMatrix.MultiplyPoint3x4(new Vector3(extents.x, extents.y, -extents.z));
            vertices[7] = thisMatrix.MultiplyPoint3x4(extents);

            col.transform.rotation = storedRotation;
            return vertices;
            */
        }

        Mesh ReadOBJFile(int buildIndex, string name)
        {
            string filename = $"{Application.streamingAssetsPath}/mario_06/stage_collision/{buildIndex}/{name}.obj";
            LoggerInstance.Msg(filename);

            Mesh mesh = new Mesh();
            mesh.name = name;
            if (!File.Exists(filename))
                return mesh;

            ObjLoaderFactory factory = new ObjLoaderFactory();
            IObjLoader objLoader = factory.Create();
            FileStream fileStream = new FileStream($"{Application.streamingAssetsPath}/mario_06/stage_collision/{buildIndex}/{name}.obj", FileMode.Open);
            LoadResult result = objLoader.Load(fileStream);

            int indexCount = 0;
            for (int i=0; i<result.Groups.Count; i++)
            {
                for (int j=0; j < result.Groups[i].Faces.Count; j++)
                {
                    indexCount += result.Groups[i].Faces[j].Count;
                }
            }
            List<Vector3> vertices = new List<Vector3>();
            List<Vector3> normals = new List<Vector3>();
            int[] indices = new int[indexCount];

            for (int i = 0; i < result.Vertices.Count; i++)
                vertices.Add(new Vector3(-result.Vertices[i].X, result.Vertices[i].Y, result.Vertices[i].Z));
            //for (int i=0; i<result.Normals.Count; i++)
                //normals.Add(new Vector3(-result.Normals[i].X, result.Normals[i].Y, result.Normals[i].Z));
            for (int i = 0, c2 = 0; i < result.Groups.Count; i++)
            {
                for (int j = 0; j < result.Groups[i].Faces.Count; j++)
                {
                    for (int k = 0; k < result.Groups[i].Faces[j].Count; k++, c2++)
                    {
                        indices[c2] = result.Groups[i].Faces[j][k].VertexIndex;
                        if (indices[c2] < 0) indices[c2] = -(indices[c2]+1);
                        indices[c2] = result.Vertices.Count - indices[c2] - 1;
                        if (indices[c2] >= vertices.Count)
                            LoggerInstance.Msg($"WARNING: indices[{c2}] >= vertices.Count. {indices[c2]} {vertices.Count}");
                    }
                }
            }
            for (int i=0; i<indexCount; i+=3)
            {
                int old = indices[i + 0];
                indices[i + 0] = indices[i + 2];
                indices[i + 2] = old;
            }

            mesh.SetVertices(vertices);
            //mesh.SetNormals(normals);
            mesh.SetIndices(indices, MeshTopology.Triangles, 0);
            mesh.UploadMeshData(false);

            mesh.RecalculateBounds();
            mesh.RecalculateNormals();
            mesh.RecalculateTangents();

            return mesh;
        }
    }
}