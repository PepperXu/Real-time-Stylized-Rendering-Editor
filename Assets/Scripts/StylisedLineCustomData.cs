using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StylisedLineCustomData : ScriptableObject
{
    public int triangleIdxCount;
    public int vertsCount;
    public uint[] triangles;
    public int[] triangleAdjs;

    [System.Serializable]
    public class FloatArray
    {
        public float time;
        public float[] values;
    }

    public FloatArray[] vertsThiKeyframes;

}
