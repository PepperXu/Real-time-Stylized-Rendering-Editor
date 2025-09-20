Shader "Peisen/Contour Vertex Surface"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _MainTex ("Albedo (RGB)", 2D) = "white" {}
        _Glossiness ("Smoothness", Range(0,1)) = 0.5
        _Metallic ("Metallic", Range(0,1)) = 0.0

        _ContourColor ("Contour Color", Color) = (0,0,0,0)
        _Scale ("Contour Thickness", Float) = 0.5
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

        Pass{
            Cull Front

            CGPROGRAM

            #pragma vertex vert
            #pragma fragment frag

            float4 _ContourColor;
            float _Scale;

            struct appdata{
                float4 vertex : POSITION;
                float3 normal : NORMAL;
            };


            float4 vert(appdata v) : SV_POSITION {
                
                float4 new_vert_pos = UnityObjectToClipPos(v.vertex);
                
                //transform normal to clip space
                float3 normal_clip = mul((float3x3)UNITY_MATRIX_MVP, v.normal);

                //scale the object along the normal
                float2 offset = normal_clip.xy * _Scale * new_vert_pos.w * 0.001;
                new_vert_pos.xy += offset;

                return new_vert_pos;

            }

            

            half4 frag() : SV_TARGET {
                return _ContourColor;
            }

            ENDCG
        }
    }
    FallBack "Diffuse"
}
