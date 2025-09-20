Shader "Peisen/Contour Vertex Unlit"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _MainTex ("Texture", 2D) = "white" {}

        _ContourColor ("Contour Color", Color) = (0,0,0,0)
        _Scale ("Contour Thickness", Float) = 0.5
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
                UNITY_FOG_COORDS(1)
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float4 _Color;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                UNITY_TRANSFER_FOG(o,o.vertex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // sample the texture
                fixed4 col = tex2D(_MainTex, i.uv) * _Color;
                // apply fog
                UNITY_APPLY_FOG(i.fogCoord, col);
                return col;
            }
            ENDCG
        }

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
