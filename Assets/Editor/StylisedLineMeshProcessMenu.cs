using System.IO;
using UnityEditor;
using UnityEngine;
using System.Linq;

public class StylisedLineMeshProcessMenu : EditorWindow
{

    private string rootName;
    private int objectCount;

    private StylisedLineRenderer[] lineObjects = new StylisedLineRenderer[0];

    private bool isAlembic;

    private int[] triAdjs;

    [MenuItem("Tools/Stylised Line Mesh Processing")]
    private static void Init()
    {
        EditorWindow.GetWindow(typeof(StylisedLineMeshProcessMenu));
    }

    private void OnGUI()
    {
        GUILayout.Label("Select an object to load secondary UV");

        rootName = EditorGUILayout.TextField("Root Name", rootName);
        objectCount = EditorGUILayout.IntField("Line Object Count", objectCount);

        if(lineObjects.Length != objectCount)
        {
            lineObjects = new StylisedLineRenderer[objectCount];
        }

        for (int i = 0; i < objectCount; i++)
        {
            lineObjects[i] = (StylisedLineRenderer)EditorGUILayout.ObjectField("Stylised Line Object", lineObjects[i], typeof(StylisedLineRenderer), true);
        }

        isAlembic = EditorGUILayout.Toggle("Is Alembic?", isAlembic);

        if (GUILayout.Button("Full Process"))
        {
            if (lineObjects.Length == 0)
            {
                Debug.LogError("No object selected");
                return;
            }

            foreach(StylisedLineRenderer lineObject in lineObjects)
                Postprocess(lineObject);  
        }

        if (GUILayout.Button("Revert Vertex Thickness"))
        {
            if (lineObjects.Length == 0)
            {
                Debug.LogError("No object selected");
                return;
            }

            foreach (StylisedLineRenderer lineObject in lineObjects)
            {
                ResetVertexColors(lineObject);
            }
        }
    }

    void ResetVertexColors(StylisedLineRenderer lineObject)
    {
        if (lineObject.customData == null)
        {
            Debug.LogError("Custom Data not found! Please process the mesh first");
            return;
        }
        
        float[] vertsInitThicks = Enumerable.Repeat(.5f, GetMesh(lineObject).vertexCount).ToArray();
        GetMesh(lineObject).colors = vertsInitThicks.Select(val => new Color(lineObject.lineColor.r, lineObject.lineColor.g, lineObject.lineColor.b, val)).ToArray();


        
        
        EditorUtility.SetDirty(lineObject.customData);
        lineObject.customData.vertsThiKeyframes = new StylisedLineCustomData.FloatArray[1];
        lineObject.customData.vertsThiKeyframes[0] = new StylisedLineCustomData.FloatArray();
        lineObject.customData.vertsThiKeyframes[0].time = 0f;
        lineObject.customData.vertsThiKeyframes[0].values = vertsInitThicks;
        
        AssetDatabase.SaveAssets();
    }

    void Postprocess(StylisedLineRenderer lineObject)
    {
        Mesh mesh = GetMesh(lineObject);
        

        StylisedLineCustomData lineData = ScriptableObject.CreateInstance<StylisedLineCustomData>();

        string path = "Assets/CustomData/" + rootName + "_" + lineObject.gameObject.name + ".asset";
        int index = 1;

        while (File.Exists(path))
        {
            path = "Assets/CustomData/" + rootName + "_" + lineObject.gameObject.name + "_" + index + ".asset";
            index++;
        }

        lineObject.customData = lineData;

        ResetVertexColors(lineObject);

        AssetDatabase.CreateAsset(lineData, path);

        EditorUtility.SetDirty(lineData);

        lineData.vertsCount = mesh.vertexCount;
        lineData.triangles = (uint[])(object)mesh.triangles;
        int triangleIdxCount = mesh.triangles.Length;

        triAdjs = new int[triangleIdxCount];
        for (int i = 0; i < triAdjs.Length; i++) triAdjs[i] = -1;

        uint[] meshTris = new uint[triangleIdxCount];
        for (int i = 0; i < triangleIdxCount; i++)
        {
            meshTris[i] = (uint)mesh.uv2[mesh.triangles[i]].x;
        }

        for (int i = 0; i < triangleIdxCount; i += 3)
        {
            GetAdjacentTris(meshTris, i);
        }

        lineData.triangleIdxCount = triangleIdxCount;
        lineData.triangleAdjs = triAdjs;

        AssetDatabase.SaveAssets();

    }

    private Mesh GetMesh(StylisedLineRenderer lineObject)
    {
        Mesh mesh;
        var skinnedMeshRenderer = lineObject.GetComponent<SkinnedMeshRenderer>();
        if (skinnedMeshRenderer)
        {
            mesh = skinnedMeshRenderer.sharedMesh;
        }
        else
        {
            mesh = lineObject.GetComponent<MeshFilter>().sharedMesh;
        }
        if (mesh == null)
        {
            Debug.LogError("Mesh is null");
            return null;
        } else
        {
            return mesh;
        }
    }

    private void GetAdjacentTris(uint[] meshTris, int curTriVertIdx)
    {
        int t0 = triAdjs[curTriVertIdx];
        int t1 = triAdjs[curTriVertIdx + 1];
        int t2 = triAdjs[curTriVertIdx + 2];

        if (t0 > 0 && t1 > 0 && t2 > 0) return;

        uint v0 = meshTris[curTriVertIdx];
        uint v1 = meshTris[curTriVertIdx + 1];
        uint v2 = meshTris[curTriVertIdx + 2];

        for (int i = curTriVertIdx; i < meshTris.Length; i += 3)
        {
            if (i == curTriVertIdx) continue;

            uint[] t = new uint[] { meshTris[i], meshTris[i + 1], meshTris[i + 2] };

            if (t0 < 0)
            {
                int a = CheckTriContainsEdge(t, new uint[] { v0, v1 });
                if (a >= 0)
                {
                    t0 = i / 3;
                    triAdjs[i + a] = curTriVertIdx / 3;
                    continue;
                }
            }

            if (t1 < 0)
            {
                int a = CheckTriContainsEdge(t, new uint[] { v1, v2 });
                if (a >= 0)
                {
                    t1 = i / 3;
                    triAdjs[i + a] = curTriVertIdx / 3;
                    continue;
                }
            }

            if (t2 < 0)
            {
                int a = CheckTriContainsEdge(t, new uint[] { v2, v0 });
                if (a >= 0)
                {
                    t2 = i / 3;
                    triAdjs[i + a] = curTriVertIdx / 3;
                    continue;
                }
            }
        }

        triAdjs[curTriVertIdx] = t0;
        triAdjs[curTriVertIdx + 1] = t1;
        triAdjs[curTriVertIdx + 2] = t2;

    }

    private int CheckTriContainsEdge(uint[] triVertIdxs, uint[] edgeVertIdxs)
    {
        if (triVertIdxs[0] == edgeVertIdxs[0] && triVertIdxs[1] == edgeVertIdxs[1]) return 0;
        if (triVertIdxs[0] == edgeVertIdxs[1] && triVertIdxs[1] == edgeVertIdxs[0]) return 0;

        if (triVertIdxs[1] == edgeVertIdxs[0] && triVertIdxs[2] == edgeVertIdxs[1]) return 1;
        if (triVertIdxs[1] == edgeVertIdxs[1] && triVertIdxs[2] == edgeVertIdxs[0]) return 1;

        if (triVertIdxs[2] == edgeVertIdxs[0] && triVertIdxs[0] == edgeVertIdxs[1]) return 2;
        if (triVertIdxs[2] == edgeVertIdxs[1] && triVertIdxs[0] == edgeVertIdxs[0]) return 2;

        return -1;
    }

}
