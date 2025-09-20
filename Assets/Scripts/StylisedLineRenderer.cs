using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using System.Linq;
using UnityEditor;

public class StylisedLineRenderer : MonoBehaviour
{
    private ComputeBuffer vertsPosBuffer, triIdxBuffer, triAdjBuffer;//, currentEditVertRWBuffer;
    //private ComputeBuffer renderedEdgesAppendBuffer, argBuffer;
    private ComputeBuffer renderedEdgesAppendBuffer, curRadiusVertsBuffer, adjRadiusVertsAppendBuffer, argEdgesBuffer, argRadBuffer;
    int[] argsEdges, argsRad;
    

    private Material material;

    private Animator animator;
    [HideInInspector]public bool paused = false;

    private MeshCollider collider;

    public StylisedLineCustomData customData;

    [HideInInspector] public float currentAnimFrameTime;
    private float previousAnimFrameTime;
    private float frameRate, clipLength;

    private int radius = 5;

    public static StylisedLineRenderer currentEditing, currentSelecting;
    private static bool isEditing = false;
    private Coroutine currentEditingCoroutine; 

    private Vector3 localHitPosition;

    //private uint[] renderedVerts;
    private uint editingIdx;

    //struct CurrentEditVert
    //{
    //    public float minLength;
    //    public float3 currentHitPosition;
    //    public uint editingIdx;
    //};

    //CurrentEditVert[] editVertData = new CurrentEditVert[1];

    struct edge
    {
        public uint idx1;
        public uint idx2;
        public float dist;
    }

    private edge[] renderedEdges;

    //CurrentEditVert curEditVert;

    public Color lineColor = Color.black, editColor = Color.red;

    private bool erasing = false, filling = false;

    public bool isAlembic = false;

    private UnityEvent restart = new UnityEvent();

    #region Init
    private void OnEnable()
    {
        EditorUtility.SetDirty(customData);
        restart.AddListener(Restart);
        Init();
    }
    void Init()
    {
        if (customData == null)
        {
            Debug.LogError(name + " do not have custom data!");
            return;
        }

        Graphics.ClearRandomWriteTargets();
        currentEditing = null;
        isEditing = false;

        material = GetMaterial();
        if (isAlembic)
        {
            animator = GetComponentInParent<Animator>();
            if (!animator)
            {
                Debug.LogError("Animator not found!");
            }
            PlayAnimation();
            frameRate = animator.runtimeAnimatorController.animationClips[0].frameRate;
            clipLength = animator.runtimeAnimatorController.animationClips[0].length;
            collider = GetComponent<MeshCollider>();
        } else {
            Mesh mesh = GetMesh();
            if (customData.vertsThiKeyframes[0].values.Length == mesh.vertexCount)
                mesh.colors = customData.vertsThiKeyframes[0].values.Select(val=>new Color(lineColor.r, lineColor.g, lineColor.b, val)).ToArray();
        }

        StylisedLineController.Instance.SetSliderValue(StylisedLineController.SliderType.Radius, radius);

        triIdxBuffer = new ComputeBuffer(customData.triangleIdxCount, sizeof(uint), ComputeBufferType.Default);
        triIdxBuffer.SetData(customData.triangles);

        triAdjBuffer = new ComputeBuffer(customData.triangleIdxCount, sizeof(int), ComputeBufferType.Default);
        triAdjBuffer.SetData(customData.triangleAdjs);

        vertsPosBuffer = new ComputeBuffer(customData.vertsCount, System.Runtime.InteropServices.Marshal.SizeOf(typeof(Vector3)), ComputeBufferType.Default);

        //currentEditVertRWBuffer = new ComputeBuffer(1, System.Runtime.InteropServices.Marshal.SizeOf(typeof(CurrentEditVert)), ComputeBufferType.Default);
        //InitCurtVertStruct();
        //currentEditVertRWBuffer.SetData(new CurrentEditVert[1] { curEditVert });

        renderedEdgesAppendBuffer = new ComputeBuffer(customData.vertsCount, System.Runtime.InteropServices.Marshal.SizeOf(typeof(edge)), ComputeBufferType.Append);
        renderedEdgesAppendBuffer.SetCounterValue(0);
        argEdgesBuffer = new ComputeBuffer(4, sizeof(int), ComputeBufferType.IndirectArguments);
        argsEdges = new int[] { 0, 1, 0, 0 };
        argEdgesBuffer.SetData(argsEdges);

        //renderedEdgesAppendBuffer = new ComputeBuffer(customData.GetVertsCount() * 2, System.Runtime.InteropServices.Marshal.SizeOf(typeof(edge)), ComputeBufferType.Append);
        //renderedEdgesAppendBuffer.SetCounterValue(0);

        curRadiusVertsBuffer = new ComputeBuffer(5, System.Runtime.InteropServices.Marshal.SizeOf(typeof(int)), ComputeBufferType.Default);
        curRadiusVertsBuffer.SetData(Enumerable.Repeat(-1, 5).ToArray());

        adjRadiusVertsAppendBuffer = new ComputeBuffer(128, System.Runtime.InteropServices.Marshal.SizeOf(typeof(uint)), ComputeBufferType.Append);
        adjRadiusVertsAppendBuffer.SetCounterValue(0);

        argRadBuffer = new ComputeBuffer(4, sizeof(int), ComputeBufferType.IndirectArguments);
        argsRad = new int[] { 0, 1, 0, 0 };
        argRadBuffer.SetData(argsRad);


        material.SetBuffer("triIdxBuffer", triIdxBuffer);
        material.SetBuffer("triAdjBuffer", triAdjBuffer);
        material.SetBuffer("vertsPosBuffer", vertsPosBuffer);
        material.SetBuffer("curRadiusVertsBuffer", curRadiusVertsBuffer);
        material.SetBuffer("renderedEdgesAppendBuffer", renderedEdgesAppendBuffer);
        material.SetBuffer("adjRadiusVertsAppendBuffer", adjRadiusVertsAppendBuffer);

    }
    #endregion

    void Update()
    {
        Mesh mesh = GetMesh();
        if(mesh == null)
        {
            Debug.LogError("Mesh not found!");
            return;
        }
        vertsPosBuffer.SetData(mesh.vertices);

        if (isAlembic && !isEditing)
        {
            currentAnimFrameTime = Mathf.FloorToInt(clipLength * frameRate * (animator.GetCurrentAnimatorStateInfo(0).normalizedTime % 1)) / frameRate;
            if (!paused)
            {
                UpdateAnimatedLine(mesh, false);
            } else
            {
                if(previousAnimFrameTime != currentAnimFrameTime)
                {
                    UpdateAnimatedLine(mesh, true);
                }
            }
            previousAnimFrameTime = currentAnimFrameTime;
            collider.sharedMesh = mesh;
        }


        if (currentEditing == this)
        {
            material.SetFloat("_CurrentEditing", 1f);
            if (!isEditing)
            {
                Graphics.ClearRandomWriteTargets();
                Graphics.SetRandomWriteTarget(1, renderedEdgesAppendBuffer, false);
                Graphics.SetRandomWriteTarget(2, adjRadiusVertsAppendBuffer, false);
                currentEditingCoroutine = StartCoroutine(ProcessDataFromShader(true));
            } else
            {
                int currentRadius = (int)StylisedLineController.Instance.GetSliderValue(StylisedLineController.SliderType.Radius);
                if (radius != currentRadius)
                {
                    radius = currentRadius;
                    if (currentEditingCoroutine != null)
                        StopCoroutine(currentEditingCoroutine);
                    currentEditingCoroutine = StartCoroutine(ProcessDataFromShader(false));
                }
                
                renderedEdgesAppendBuffer.SetCounterValue(0);
            }
        } else
        {
            material.SetFloat("_CurrentEditing", 0f);
            ResetVertexColor();
            if (currentEditing == null)
            {
                isEditing = false;
                Graphics.ClearRandomWriteTargets();
            }       
        }
    }



    private void OnRenderObject()
    {
        if (currentEditing == this && isEditing)
        {
            ComputeBuffer.CopyCount(renderedEdgesAppendBuffer, argEdgesBuffer, 0);
            argEdgesBuffer.GetData(argsEdges);
            Debug.Log(argsEdges[0]);
            renderedEdges = new edge[argsEdges[0]];
            renderedEdgesAppendBuffer.GetData(renderedEdges);
        }
    }

    private void UpdateAnimatedLine(Mesh mesh, bool debug)
    {
        int nextKeyframe = 0;
        for (int i = 1; i < customData.vertsThiKeyframes.Length; i++)
        {
            if (currentAnimFrameTime < customData.vertsThiKeyframes[i].time)
            {
                nextKeyframe = i;
                break;
            }
        }

        if (nextKeyframe == 0)
            mesh.colors = customData.vertsThiKeyframes[customData.vertsThiKeyframes.Length-1].values.Select(val => new Color(lineColor.r, lineColor.g, lineColor.b, val)).ToArray();
        else
        {
            float preTime = customData.vertsThiKeyframes[nextKeyframe - 1].time;
            float nextTime = customData.vertsThiKeyframes[nextKeyframe].time;
            float ratio = (currentAnimFrameTime - preTime) / (nextTime - preTime);
            Color[] colors = new Color[mesh.vertexCount];
            for (int i = 0; i < mesh.vertexCount; i++)
            {
                float preVal = customData.vertsThiKeyframes[nextKeyframe - 1].values[i];
                float nextVal = customData.vertsThiKeyframes[nextKeyframe].values[i];
                colors[i] = new Color(lineColor.r, lineColor.g, lineColor.b, (nextVal - preVal) * ratio + preVal);
            }

            mesh.colors = colors;
        }
    }



    #region EditingCoroutine
    IEnumerator ProcessDataFromShader(bool initialBuffer)
    {
        //Find center vertex

        isEditing = true;

        if (animator)
            PauseAnimation();

        Mesh mesh = GetMesh();


        if (initialBuffer)
        {
            initialBuffer = false;
            yield return new WaitForEndOfFrame();
            //Graphics.ClearRandomWriteTargets();
            //currentEditVertRWBuffer.GetData(editVertData);
            float minLength = float.PositiveInfinity;

            edge closestEdge = new edge();

            foreach(edge edge in renderedEdges)
            {
                if(edge.dist < minLength)
                {
                    minLength = edge.dist;
                    closestEdge = edge;
                }
            }
            float dist1 = Vector3.Distance(mesh.vertices[closestEdge.idx1], localHitPosition);
            float dist2 = Vector3.Distance(mesh.vertices[closestEdge.idx2], localHitPosition);

            if(dist1 < dist2)
            {
                editingIdx = closestEdge.idx1;
            } else
            {
                editingIdx = closestEdge.idx2;
            }

        }



        //Vector3 centerPos = transform.TransformPoint(mesh.vertices[editVertData[0].editingIdx]);
        Vector3 centerPos = transform.TransformPoint(mesh.vertices[editingIdx]);
        StylisedLineController.Instance.editingSphere.transform.position = centerPos;

        //Select edges within a radius

        List<uint>[] editingIdxs = new List<uint>[radius + 1];
        editingIdxs[0] = new List<uint>();
        //editingIdxs[0].Add(editVertData[0].editingIdx);
        editingIdxs[0].Add(editingIdx);

        int[] curRadiusVerts = Enumerable.Repeat(-1, 5).ToArray();

        //curRadiusVerts[0] = (int)editVertData[0].editingIdx;
        curRadiusVerts[0] = (int)editingIdx;

        curRadiusVertsBuffer.SetData(curRadiusVerts);

        

        float maxRadius = 0f;

        for (int i = 0; i < radius; i++)
        {
            adjRadiusVertsAppendBuffer.SetCounterValue(0);
            yield return new WaitForEndOfFrame();
            ComputeBuffer.CopyCount(adjRadiusVertsAppendBuffer, argRadBuffer, 0);
            argRadBuffer.GetData(argsRad);
            uint[] verts = new uint[argsRad[0]];
            adjRadiusVertsAppendBuffer.GetData(verts);
            //Graphics.ClearRandomWriteTargets();

            editingIdxs[i + 1] = new List<uint>();
            for (int j = 0; j < 128; j++)
            {
                if (j < verts.Length)
                {
                    if (!DoesIdxExist(verts[j], editingIdxs))
                    {
                        editingIdxs[i + 1].Add(verts[j]);
                        float newRadius = Vector3.Distance(transform.TransformPoint(mesh.vertices[verts[j]]), centerPos);
                        if (newRadius > maxRadius)
                            maxRadius = newRadius;
                    }
                } else
                {
                    break;
                }
            }

            for(int j = 0; j < 5; j++)
            {
                if (j < editingIdxs[i + 1].Count)
                    curRadiusVerts[j] = (int)editingIdxs[i + 1][j];
                else
                    curRadiusVerts[j] = -1;
            }
            curRadiusVertsBuffer.SetData(curRadiusVerts);
        }

        StylisedLineController.Instance.editingSphere.transform.localScale = Vector3.one * maxRadius * 2f;


        //CPU Traversal
        //for(int i = 0; i < radius; i++)
        //{
        //    editingIdxs[i+1] = new List<uint>();
        //    //List<uint> currentRadiusIdxs = new List<uint>();
        //    for (int j = 0; j < renderedEdges.Length; j++)
        //    {
        //        foreach(uint editingIdx in editingIdxs[i])
        //        {
        //            if(renderedEdges[j].idx1 == editingIdx)
        //            {
        //                uint curIdx = renderedEdges[j].idx2;
        //                if (!DoesIdxExist(curIdx, editingIdxs))
        //                    editingIdxs[i+1].Add(curIdx);
        //            }
        //            if (renderedEdges[j].idx2 == editingIdx)
        //            {
        //                uint curIdx = renderedEdges[j].idx1;
        //                if (!DoesIdxExist(curIdx, editingIdxs))
        //                    editingIdxs[i + 1].Add(curIdx);
        //            }
        //        }
        //    }
        //
        //    yield return new WaitForEndOfFrame();
        //}

        //for (int i = 0; i < radius + 1; i++)
        //{
        //    Debug.Log("order: " + i + "; values: " +  ListToString(editingIdxs[i]));
        //}

        //Edit line segment

        Color[] colors = mesh.colors;
        colors = colors.Select(col => col = new Color(lineColor.r, lineColor.g, lineColor.b, col.a)).ToArray();
        float[] initialValues = colors.Select(col => col.a).ToArray();
        StylisedLineController.Instance.SetSliderValue(StylisedLineController.SliderType.Thickness, initialValues[editingIdxs[0][0]]);
        StylisedLineController.Instance.UpdateUI();


        while (isEditing)
        {

            if (erasing)
            {
                erasing = false;

                Vector2[] uv = mesh.uv;
                Texture2D lineTexture = (Texture2D)material.GetTexture("_LineTex");

                for (int i = 0; i < radius + 1; i++)
                {
                    foreach (uint idx in editingIdxs[i])
                    {
                        Color c = lineTexture.GetPixelBilinear(uv[idx].x, uv[idx].y);
                        colors[idx] = new Color(editColor.r, editColor.g, editColor.b, 0.5f - c.a * 0.5f);
                    }
                }

                initialValues = colors.Select(col => col.a).ToArray();
                StylisedLineController.Instance.SetSliderValue(StylisedLineController.SliderType.Thickness, initialValues[editingIdxs[0][0]]);

            }
            else if (filling)
            {
                filling = false;

                Color[] cols = mesh.colors;

                Vector2[] uv = mesh.uv;
                Texture2D lineTexture = (Texture2D)material.GetTexture("_LineTex");

                float maxValue = 0f;

                for (int i = 0; i < radius + 1; i++)
                {
                    foreach (uint idx in editingIdxs[i])
                    {
                        Color c = lineTexture.GetPixelBilinear(uv[idx].x, uv[idx].y);
                        float currentAlpha = (cols[idx].a - 0.5f) / 0.5f + c.a;
                        maxValue = currentAlpha > maxValue ? currentAlpha : maxValue;
                    }
                }

                for (int i = 0; i < radius + 1; i++)
                {
                    foreach (uint idx in editingIdxs[i])
                    {
                        Color c = lineTexture.GetPixelBilinear(uv[idx].x, uv[idx].y);
                        colors[idx] = new Color(editColor.r, editColor.g, editColor.b, (maxValue-c.a) * 0.5f + 0.5f);
                    }
                }

                initialValues = colors.Select(col => col.a).ToArray();
                StylisedLineController.Instance.SetSliderValue(StylisedLineController.SliderType.Thickness, initialValues[editingIdxs[0][0]]);

            }
            else
            {
                float currentValue = StylisedLineController.Instance.GetSliderValue(StylisedLineController.SliderType.Thickness);

                colors[editingIdxs[0][0]] = new Color(editColor.r, editColor.g, editColor.b, currentValue);

                for (int i = 0; i < radius; i++)
                {
                    float xsq = (i + 1f) / (radius + 1f);
                    float falloff = 1f - 3f * Mathf.Pow(xsq, 4f) + 3f * Mathf.Pow(xsq, 8f) - Mathf.Pow(xsq, 12f);
                    foreach (uint idx in editingIdxs[i + 1])
                    {
                        float newValue = Mathf.Clamp01(initialValues[idx] + (currentValue - initialValues[editingIdxs[0][0]]) * falloff);
                        colors[idx] = new Color(editColor.r, editColor.g, editColor.b, newValue);
                    }
                }
            }

            mesh.colors = colors;
            yield return new WaitForEndOfFrame();
            //Graphics.ClearRandomWriteTargets();
        }
    }

    private void ResetVertexColor()
    {
        Color[] colors = GetMesh().colors;
        colors = colors.Select(col => col = new Color(lineColor.r, lineColor.g, lineColor.b, col.a)).ToArray();
        GetMesh().colors = colors;
    }

    #endregion

    #region Public
    public void CheckCurrentEditVert(Vector3 hitPosition)
    {
        isEditing = false;
        this.localHitPosition = transform.InverseTransformPoint(hitPosition);
        material.SetVector("_HitPosition", localHitPosition);
        //InitCurtVertStruct();
        //currentEditVertRWBuffer.SetData(new CurrentEditVert[1] { curEditVert });
    }

    public void SaveVertexColorsToCustomData()
    {
        Color[] colors = GetMesh().colors;

        if (!isAlembic)
        {
            customData.vertsThiKeyframes[0].values = colors.Select(val=>val.a).ToArray();
        } else
        {
            StylisedLineCustomData.FloatArray keyframe = new StylisedLineCustomData.FloatArray();
            keyframe.time = currentAnimFrameTime;
            keyframe.values = colors.Select(val => val.a).ToArray();
            bool assigned = false;
            for (int i = 0; i < customData.vertsThiKeyframes.Length; i++)
            {
                if (currentAnimFrameTime < customData.vertsThiKeyframes[i].time)
                {
                    List<StylisedLineCustomData.FloatArray> keyframeList = customData.vertsThiKeyframes.ToList();
                    keyframeList.Insert(i, keyframe);
                    customData.vertsThiKeyframes = keyframeList.ToArray();
                    assigned = true;
                    break;
                } else if (currentAnimFrameTime == customData.vertsThiKeyframes[i].time)
                {
                    customData.vertsThiKeyframes[i] = keyframe;
                    assigned = true;
                    break;
                }
            }
            if (!assigned)
            {
                List<StylisedLineCustomData.FloatArray> keyframeList = customData.vertsThiKeyframes.ToList();
                keyframeList.Add(keyframe);
                customData.vertsThiKeyframes = keyframeList.ToArray();
            }

        }


        isEditing = false;
        currentEditing = null;
        StylisedLineController.Instance.UpdateUI();
        restart.Invoke();
    }

    public void EraseCurrentLine()
    {
        erasing = true;
    }

    public void FillCurrentLine()
    {
        filling = true;
    }

    public void SetAnimationInfo(float progress)
    {
        animator.Play(0, 0, progress);
    }
    public float GetAnimationInfo()
    {
        return currentAnimFrameTime / clipLength;
    }

    public void PlayAnimation()
    {
        paused = false;
        currentEditing = null;
        StylisedLineController.Instance.UpdateUI();
        animator.speed = 1;
    }

    public void PauseAnimation()
    {
        paused = true;
        animator.speed = 0;
    }

    #endregion

    void Dump()
    {

        Graphics.ClearRandomWriteTargets();
        

        vertsPosBuffer.Release();
        triIdxBuffer.Release();
        triAdjBuffer.Release();
        //currentEditVertRWBuffer.Release();
        //renderedEdgesAppendBuffer.Release();
        renderedEdgesAppendBuffer.Release();
        argRadBuffer.Release();
        argEdgesBuffer.Release();
        curRadiusVertsBuffer.Release();
        adjRadiusVertsAppendBuffer.Release();

        vertsPosBuffer.Dispose();
        triIdxBuffer.Dispose();
        triAdjBuffer.Dispose();
        //currentEditVertRWBuffer.Dispose();
        //renderedEdgesAppendBuffer.Dispose();
        renderedEdgesAppendBuffer.Dispose();
        argRadBuffer.Dispose();
        argEdgesBuffer.Dispose();
        curRadiusVertsBuffer.Dispose();
        adjRadiusVertsAppendBuffer.Dispose();
    }

    private void OnDisable()
    {
        AssetDatabase.SaveAssets();
        restart.RemoveListener(Restart);
        Dump();
    }

    private void Restart()
    {
        Dump();
        Init();
    }

    #region Utilities
    bool DoesIdxExist(uint idx, List<uint>[] idxLists)
    {
        foreach(List<uint> idxList in idxLists)
        {
            if(idxList != null && idxList.Count > 0)
            {
                foreach(uint curIdx in idxList)
                {
                    if(curIdx == idx)
                    {
                        return true;
                    }
                }
            }
        }
        return false;
    }

    string ListToString<T>(List<T> list)
    {
        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        foreach (T v in list)
        {
            sb.Append(v.ToString() + Environment.NewLine);
        }

        return sb.ToString();
    }

    private Material GetMaterial()
    {
        var skinnedMeshRenderer = GetComponent<SkinnedMeshRenderer>();
        if (skinnedMeshRenderer)
        {
            return skinnedMeshRenderer.material;
        }

        return GetComponent<MeshRenderer>().material;
    }

    private Mesh GetMesh()
    {
        var skinnedMeshRenderer = GetComponent<SkinnedMeshRenderer>();
        if (skinnedMeshRenderer)
        {
            return skinnedMeshRenderer.sharedMesh;
        }

        return GetComponent<MeshFilter>().sharedMesh;
    }

    #endregion
}
