Shader "Peisen/Contour Fragment Unlit"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
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

        Pass{

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"
            
            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float2 screenpos: TEXCOORD1;
                UNITY_FOG_COORDS(1)
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float4 _Color;

            
            sampler2D _CameraDepthNormalsTexture;
            float2 _CameraDepthNormalsTexture_TexelSize;
            float _Scale;
            float _DepthThreshold;
            float _NormalThreshold;
            float4 _ContourColor;
            

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);

                //compute vertex screen space position;
                o.screenpos = ComputeScreenPos(o.vertex);
                
                UNITY_TRANSFER_FOG(o,o.vertex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                //size of the kernel
                float halfScaleCeiling = ceil(_Scale * 0.5);
                float halfScaleFloor = floor(_Scale * 0.5);

                //normalize screenpos after interpolation
                float2 new_screenpos = i.screenpos/i.vertex.w;

                //with respect to current pixel - uv0: botton left; uv1: top right; uv2: bottom right; uv3: top left;
                float2 uv0 = new_screenpos - _CameraDepthNormalsTexture_TexelSize.xy * halfScaleFloor;
                float2 uv1 = new_screenpos + _CameraDepthNormalsTexture_TexelSize.xy * halfScaleCeiling;
                float2 uv2 = float2(new_screenpos.x + _CameraDepthNormalsTexture_TexelSize.x * halfScaleCeiling, new_screenpos.y - _CameraDepthNormalsTexture_TexelSize.y * halfScaleFloor);
                float2 uv3 = float2(new_screenpos.x - _CameraDepthNormalsTexture_TexelSize.x * halfScaleFloor, new_screenpos.y + _CameraDepthNormalsTexture_TexelSize.y * halfScaleCeiling);
                
                //decode the depth and the normal of the current pixel from "_CameraDepthNormalsTexture"
                float depth0, depth1, depth2, depth3;
                float3 normal0, normal1, normal2, normal3;
                DecodeDepthNormal(tex2D(_CameraDepthNormalsTexture, uv0), depth0, normal0);
                DecodeDepthNormal(tex2D(_CameraDepthNormalsTexture, uv1), depth1, normal1);
                DecodeDepthNormal(tex2D(_CameraDepthNormalsTexture, uv2), depth2, normal2);
                DecodeDepthNormal(tex2D(_CameraDepthNormalsTexture, uv3), depth3, normal3);
                
                //edge is detected with "the Roberts cross"
                float depthDifference0 = depth1 - depth0;
                float depthDifference1 = depth3 - depth2;
                float3 normalDifferenceVector0 = normal1 - normal0;
                float3 normalDifferenceVector1 = normal3 - normal2;

                float edgeDepth = sqrt(pow(depthDifference0, 2) + pow(depthDifference1, 2)) * 100;
                edgeDepth = step(_DepthThreshold, edgeDepth);

                float edgeNormal = sqrt(dot(normalDifferenceVector0, normalDifferenceVector0) + dot(normalDifferenceVector1, normalDifferenceVector1));
                edgeNormal = step(_NormalThreshold, edgeNormal);

                //combine the edge of normal and depth
                float edge = max(edgeNormal, edgeDepth);

                //combine with contour color and base color
                fixed4 col = tex2D(_MainTex, i.uv) * _Color;
                float4 finalCol = float4(col.rgb * (1 - edge) + _ContourColor.rgb * edge, col.a);
                return finalCol;
            }
            ENDCG
        }
    }
}
