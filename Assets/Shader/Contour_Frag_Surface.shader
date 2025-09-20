Shader "Peisen/Contour Fragment Surface"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _MainTex ("Albedo (RGB)", 2D) = "white" {}
        _Glossiness ("Smoothness", Range(0,1)) = 0.5
        _Metallic ("Metallic", Range(0,1)) = 0.0

        _Scale ("Contour Thickness", Float) = 1
        _DepthThreshold ("Depth Threshold", Float) = 0.2
        _NormalThreshold ("Normal Threshold", Float) = 0.2
        _ContourColor ("Contour Color", Color) = (0,0,0,1)
    }
    SubShader
    {
        //standard surface shader

        Tags { "RenderType"="Opaque" }
        LOD 200

        CGPROGRAM
        // Physically based Standard lighting model, and enable shadows on all light types
        #pragma surface surf Standard fullforwardshadows
    
        // Use shader model 3.0 target, to get nicer looking lighting
        #pragma target 3.0
    
        sampler2D _MainTex;
    
        struct Input
        {
            float2 uv_MainTex;
        };
    
        half _Glossiness;
        half _Metallic;
        fixed4 _Color;
    
        // Add instancing support for this shader. You need to check 'Enable Instancing' on materials that use the shader.
        // See https://docs.unity3d.com/Manual/GPUInstancing.html for more information about instancing.
        // #pragma instancing_options assumeuniformscaling
        UNITY_INSTANCING_BUFFER_START(Props)
            // put more per-instance properties here
        UNITY_INSTANCING_BUFFER_END(Props)
    
        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            // Albedo comes from a texture tinted by color
            fixed4 c = tex2D (_MainTex, IN.uv_MainTex) * _Color;
            o.Albedo = c.rgb;
            // Metallic and smoothness come from slider variables
            o.Metallic = _Metallic;
            o.Smoothness = _Glossiness;
            o.Alpha = c.a;
        }
        ENDCG

        //blend the outline

        Pass{

            Blend SrcAlpha OneMinusSrcAlpha

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
            };

            struct v2f
            {
                float2 screenpos: TEXCOORD1;
                float4 vertex : SV_POSITION;
            };
            
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

                //compute vertex screen space position;
                o.screenpos = ComputeScreenPos(o.vertex);
                
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

                //combine with contour color
                float4 finalCol = float4(_ContourColor.rgb, edge * _ContourColor.a);
                return finalCol;
            }
            ENDCG
        }
    }

    FallBack "Diffuse"
}
