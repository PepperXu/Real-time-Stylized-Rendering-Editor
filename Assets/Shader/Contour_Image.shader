Shader "Hidden/Contour Image"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Scale ("Contour Thickness", Float) = 1
        _DepthThreshold ("Depth Threshold", Float) = 0.2
        _NormalThreshold ("Normal Threshold", Float) = 0.2
        _ContourColor ("Contour Color", Color) = (0,0,0,1)
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            // make fog work
            #pragma multi_compile_fog

            #include "UnityCG.cginc"

            

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float2 _MainTex_TexelSize;
            sampler2D _CameraDepthNormalsTexture;
            float _Scale;
            float _DepthThreshold;
            float _NormalThreshold;
            float4 _ContourColor;
            

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float halfScaleCeiling = ceil(_Scale * 0.5);
                float halfScaleFloor = floor(_Scale * 0.5);

                float2 uv0 = i.uv - _MainTex_TexelSize.xy * halfScaleFloor;
                float2 uv1 = i.uv + _MainTex_TexelSize.xy * halfScaleCeiling;
                float2 uv2 = float2(i.uv.x + _MainTex_TexelSize.x * halfScaleCeiling, i.uv.y - _MainTex_TexelSize.y * halfScaleFloor);
                float2 uv3 = float2(i.uv.x - _MainTex_TexelSize.x * halfScaleFloor, i.uv.y + _MainTex_TexelSize.y * halfScaleCeiling);
                
                float depth0, depth1, depth2, depth3;
                float3 normal0, normal1, normal2, normal3;
                DecodeDepthNormal(tex2D(_CameraDepthNormalsTexture, uv0), depth0, normal0);
                DecodeDepthNormal(tex2D(_CameraDepthNormalsTexture, uv1), depth1, normal1);
                DecodeDepthNormal(tex2D(_CameraDepthNormalsTexture, uv2), depth2, normal2);
                DecodeDepthNormal(tex2D(_CameraDepthNormalsTexture, uv3), depth3, normal3);

                float depthDifference0 = depth1 - depth0;
                float depthDifference1 = depth3 - depth2;
                float3 normalDifferenceVector0 = normal1 - normal0;
                float3 normalDifferenceVector1 = normal3 - normal2;

                float edgeDepth = sqrt(pow(depthDifference0, 2) + pow(depthDifference1, 2)) * 100 * depth0;
                edgeDepth = step(_DepthThreshold, edgeDepth);

                float edgeNormal = sqrt(dot(normalDifferenceVector0, normalDifferenceVector0) + dot(normalDifferenceVector1, normalDifferenceVector1));
                edgeNormal = step(_NormalThreshold, edgeNormal);
                float edge = max(edgeNormal, edgeDepth);

                fixed4 col = tex2D(_MainTex, i.uv);
                float4 finalCol = float4(lerp(col.rgb, _ContourColor.rgb, edge * _ContourColor.a), col.a);
                return finalCol;
            }
            ENDCG
        }
    }
}
