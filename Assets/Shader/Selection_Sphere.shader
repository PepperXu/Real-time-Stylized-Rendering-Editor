// Upgrade NOTE: replaced '_Object2World' with 'unity_ObjectToWorld'

Shader "Peisen/Selection Sphere"
{
    Properties
    {
        _ContourColor("Contour Color", Color) = (0,0,0,0)
        _Scale("Contour Thickness", Float) = 0.5
    }
        SubShader
    {
        Tags { "Queue" = "Overlay" }
        LOD 100
        Blend SrcAlpha OneMinusSrcAlpha
        ZTest Always
        ZWrite Off

        Pass
        {
            Stencil
            {
                Ref 2
                Comp Always
                Pass Replace
            }

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            // make fog work
            #pragma multi_compile_fog

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
            };

            struct v2f
            {
                UNITY_FOG_COORDS(1)
                float4 vertex : SV_POSITION;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                UNITY_TRANSFER_FOG(o,o.vertex);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                // sample the texture
                fixed4 col = float4(0,0,0,0);
                // apply fog
                UNITY_APPLY_FOG(i.fogCoord, col);
                return col;
            }
            ENDCG
        }

        Pass{

            Stencil
            {
                Ref 2
                Comp NotEqual
                Pass Zero
            }

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

                float3 normal_world = mul((float3x3)unity_ObjectToWorld, v.normal);
                
                //transform normal to clip space
                float3 normal_clip = mul((float3x3)UNITY_MATRIX_VP, normalize(normal_world));

                //scale the object along the normal
                float2 offset = normal_clip.xy * _Scale * new_vert_pos.w / _ScreenParams.x;
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
