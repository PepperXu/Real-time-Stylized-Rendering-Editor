using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
[RequireComponent(typeof(Camera))]
public class RenderContour : MonoBehaviour
{
    [SerializeField]
    Shader _shader;
    Material _material;

    [SerializeField]
    bool useImageEffect = true;

    [SerializeField]
    int contourThickness = 1;
    [SerializeField]
    float depthThreshold = 0.2f;
    [SerializeField]
    float normalThreshold = 1.0f;
    [SerializeField]
    Color contourColor = Color.white;

    private void Update()
    {
        GetComponent<Camera>().depthTextureMode = DepthTextureMode.DepthNormals;
    }

    void OnRenderImage(RenderTexture src, RenderTexture dest)
    {
        if (useImageEffect)
        {
            if (_material == null)
            {
                _material = new Material(_shader);
            }
            _material.SetFloat("_Scale", (float)contourThickness);
            _material.SetFloat("_DepthThreshold", depthThreshold);
            _material.SetFloat("_NormalThreshold", normalThreshold);
            _material.SetColor("_ContourColor", contourColor);
            Graphics.Blit(src, dest, _material);
        }
        else
        {
            Graphics.Blit(src, dest);
        }
    }
}
