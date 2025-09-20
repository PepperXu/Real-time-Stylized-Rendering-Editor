Shader "Custom/StylisedLine"
{
    Properties
    {
        _MainTex("Texture", 2D) = "white" {}
        _LineTex("Line Weight Map", 2D) = "white" {}
        //_LineColor("Line Color", Color) = (0,0,0,1)
        //_EditLineColor("Edit Color", Color) = (1,0,0,1)
        _LineThickness("Line thickness", Range(0.1, 100.0)) = 9.0
        _LooseLineThicknessMulti("Loose line thickness multiplier", Range(0.0, 3.0)) = 0.5
    }

    SubShader
    {
        Tags {"DisableBatching" = "True"}
        Pass
        {
            Name "Body"

            Cull Back

            HLSLPROGRAM
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #pragma vertex vert
            #pragma fragment frag alpha
            #pragma target 5.0

            struct appdata
            {
                float4 vertex : POSITION;
                float4 normal : NORMAL;
                float2 uv_base : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv_base : TEXCOORD0;
            };


            sampler2D _MainTex;
            float4 _MainTex_ST;
            float _WireThickness;

            v2f vert(appdata v)
            {
                v2f o;

                o.pos = TransformObjectToHClip(v.vertex.xyz);
                
                float4 c_normal = normalize(v.normal);
                

                float4 posExt = TransformObjectToHClip(v.vertex.xyz - normalize(v.normal.xyz));
                float4 diff = o.pos - posExt;
                c_normal = -normalize(diff);

                float wireThickness = (_WireThickness) / _ScreenParams.x;
                //float wireThickness = (_WireThickness) * 0.001;
                float4 offset = c_normal * wireThickness / 1.5 * o.pos.w;
                o.pos += offset;

                o.uv_base = TRANSFORM_TEX(v.uv_base, _MainTex);
                return o;
            }

            half4 frag(v2f IN) : SV_Target
            {
                return tex2D(_MainTex, IN.uv_base);
            }
            ENDHLSL
        }

        
        Pass
        {
            Name "Line"
            Cull Off
            Blend Off  
            ZWrite On

            HLSLPROGRAM

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #pragma vertex vert
            #pragma fragment frag
            #pragma geometry geom
            #pragma target 5.0 

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv_base : TEXCOORD0;
                float2 uv_export : TEXCOORD1;
                float4 lattr : COLOR0;
                uint vertexId : SV_VertexID;
            };

            struct v2g
            {
                float4 pos : SV_POSITION;
                float4 lattr : COLOR0;
                float2 uv_base : TEXCOORD0;
                int2 idxType : COLOR1; 
            };

            struct g2f
            {
                float4 pos : SV_POSITION;
                float4 color: COLOR0;
            };

            struct currentEditVert {
                float minLength;
                float3 currentHitPosition;
                uint editingIdx;
            };

            struct edge {
                uint idx1;
                uint idx2;
                float dist;
            };


            
            sampler2D _LineTex;
            float4 _LineTex_ST;
            StructuredBuffer<float3> vertsPosBuffer;


            v2g vert(appdata v)
            {
                v2g o;

                o.pos = TransformObjectToHClip(v.vertex.xyz);

                o.idxType.x = (int)v.vertexId;
                o.idxType.y = (int)v.uv_export.y;

                o.lattr = v.lattr;
                o.uv_base = v.uv_base;

                return o;
            }

            

            StructuredBuffer<uint3> triIdxBuffer;
            
            StructuredBuffer<int3> triAdjBuffer;

            StructuredBuffer<int> curRadiusVertsBuffer;


            float _LineThickness;
            float _LooseLineThicknessMulti;
            float3 _HitPosition;
            float _CurrentEditing;

             AppendStructuredBuffer<edge> renderedEdgesAppendBuffer : register(u1);
            //uniform RWStructuredBuffer<currentEditVert> currentEditVertRWBuffer : register(u1);
            //uniform AppendStructuredBuffer<edge> renderedEdgesAppendBuffer : register(u2);
             AppendStructuredBuffer<uint> adjRadiusVertsAppendBuffer : register(u2);

            void appendEdge(float4 p1, float4 p2, uint idx1, uint idx2, float lw1, float lw2, float4 lattr1, float4 lattr2, bool isLoose, inout TriangleStream<g2f> OUT)
            {
                
                
                
                //float len1 = length(vertsPosBuffer[idx1] - currentEditVertRWBuffer[0].currentHitPosition);
                //float len2 = length(vertsPosBuffer[idx2] - currentEditVertRWBuffer[0].currentHitPosition);
                //
                //if (currentEditVertRWBuffer[0].minLength < 0 || (len1 < currentEditVertRWBuffer[0].minLength && currentEditVertRWBuffer[0].editingIdx != idx1)) {
                //    currentEditVertRWBuffer[0].minLength = len1;
                //    currentEditVertRWBuffer[0].editingIdx = idx1;
                //}
                //
                //if (len2 < currentEditVertRWBuffer[0].minLength && currentEditVertRWBuffer[0].editingIdx != idx2) {
                //    currentEditVertRWBuffer[0].minLength = len2;
                //    currentEditVertRWBuffer[0].editingIdx = idx2;
                //}
                //
                if (_CurrentEditing > 0) {
                    edge e;
                    e.idx1 = idx1;
                    e.idx2 = idx2;
                    //e.dist = length(cross(vertsPosBuffer[idx1] - vertsPosBuffer[idx2], _HitPosition - vertsPosBuffer[idx2])) / length(vertsPosBuffer[idx1] - vertsPosBuffer[idx2]);
                    e.dist = length(vertsPosBuffer[idx1] - _HitPosition) + length(vertsPosBuffer[idx2] - _HitPosition);
                    renderedEdgesAppendBuffer.Append(e);


                    //renderedVertsAppendBuffer.Append(idx1);
                    //renderedVertsAppendBuffer.Append(idx2);

                    for (int i = 0; i < 5; i++) {
                        if (curRadiusVertsBuffer[i] != -1)
                        {
                            if (curRadiusVertsBuffer[i] == (int)idx1) {
                                adjRadiusVertsAppendBuffer.Append(idx2);
                            }

                            if (curRadiusVertsBuffer[i] == (int)idx2) {
                                adjRadiusVertsAppendBuffer.Append(idx1);
                            }
                        }
                    }

                }

                float lineThickness = _LineThickness;


                lineThickness = (lineThickness * (isLoose ? _LooseLineThicknessMulti : 1.0)) / _ScreenParams.x;


                float weight1 = lineThickness * (((lw1 + (lattr1.a - 0.5) / 0.5) > 0) ? (lw1 + (lattr1.a - 0.5) / 0.5) : 0);
                float weight2 = lineThickness * (((lw2 + (lattr2.a - 0.5) / 0.5) > 0) ? (lw2 + (lattr2.a - 0.5) / 0.5) : 0);

                g2f o = (g2f)0;

                float ratio = _ScreenParams.x / _ScreenParams.y;

                float2 _t = normalize((p2.xy * p1.w) - (p1.xy * p2.w)); 
                _t.y /= ratio;
                float2 t = float2(_t.y, -_t.x); 
                float4 n = normalize(p2 - p1);  

                float4 c1 = p1; 
                float4 c2 = p1; 
                float2 off1 = (-t) * p1.w * weight1;
                float2 off2 = (t) * p1.w * weight1;
                c1.xy += off1;
                c2.xy += off2;

                float4 c3 = p2; 
                float4 c4 = p2; 
                float2 off3 = (-t) * p2.w * weight2;
                float2 off4 = (t) * p2.w * weight2;
                c3.xy += off3;
                c4.xy += off4;

                o.pos = c1;
                o.color = float4(lattr1.r, lattr1.g, lattr1.b, 1);
                OUT.Append(o);

                o.pos = c2;
                o.color = float4(lattr1.r, lattr1.g, lattr1.b, 1);
                OUT.Append(o);

                o.pos = c3;
                o.color = float4(lattr2.r, lattr2.g, lattr2.b, 1);
                OUT.Append(o);

                o.pos = c4;
                o.color = float4(lattr2.r, lattr2.g, lattr2.b, 1);
                OUT.Append(o);

                OUT.RestartStrip();
            }


            bool isTriCulled(float2 p0, float2 p1, float2 p2)
            {
                float a = 0;
                a += (p1.x - p0.x) * (p1.y + p0.y);
                a += (p2.x - p1.x) * (p2.y + p1.y);
                a += (p0.x - p2.x) * (p0.y + p2.y);

                return a > 0;
            }

            bool isTriCulledByIdx(uint triIdx)
            {
                uint3 t = triIdxBuffer[triIdx];

                float4 v0 = TransformObjectToHClip(vertsPosBuffer[t.x]);
                float4 v1 = TransformObjectToHClip(vertsPosBuffer[t.y]);
                float4 v2 = TransformObjectToHClip(vertsPosBuffer[t.z]);

                float2 p0 = v0.xy / v0.w;
                float2 p1 = v1.xy / v1.w;
                float2 p2 = v2.xy / v2.w;

                return isTriCulled(p0, p1, p2);
            }

            bool isEdgeDrawn(int adjTriIdx, uint edgeType) {

                if (edgeType <= 0) return false;

                if (edgeType == 2) return true;

                if (adjTriIdx < 0 || isTriCulledByIdx(adjTriIdx)) return true;

                return false;
            }


            [maxvertexcount(12)] 
            void geom(triangle v2g IN[3], uint triangleID : SV_PrimitiveID, inout TriangleStream<g2f> OUT)
            {
                float2 p0 = IN[0].pos.xy / IN[0].pos.w;
                float2 p1 = IN[1].pos.xy / IN[1].pos.w;
                float2 p2 = IN[2].pos.xy / IN[2].pos.w;
                
                if (IN[0].idxType.y == 0) {
                    float lw1 = 1.0;
                    float lw2 = 1.0;
                    appendEdge(IN[1].pos, IN[2].pos, (uint)IN[1].idxType.x, (uint)IN[2].idxType.x, lw1, lw2, IN[1].lattr, IN[2].lattr, true, OUT);
                    return;
                }
                if (IN[1].idxType.y == 0) {
                    float lw1 = 1.0;
                    float lw2 = 1.0;
                    appendEdge(IN[2].pos, IN[0].pos, (uint)IN[2].idxType.x, (uint)IN[0].idxType.x, lw1, lw2, IN[2].lattr, IN[0].lattr, true, OUT);
                    return;
                }
                if (IN[2].idxType.y == 0) {
                    float lw1 = 1.0;
                    float lw2 = 1.0;
                    appendEdge(IN[0].pos, IN[1].pos, (uint)IN[0].idxType.x, (uint)IN[1].idxType.x, lw1, lw2, IN[0].lattr, IN[1].lattr, true, OUT);
                    return;
                }

                bool triCulled = isTriCulled(p0, p1, p2);
                if (triCulled) return;

                uint triIdx = triangleID;
                int3 adj = triAdjBuffer[triIdx]; 

                if (isEdgeDrawn(adj.x, IN[1].idxType.y)) {
                    float lw1 = tex2Dlod(_LineTex, float4(IN[0].uv_base, 0, 0)).a;
                    float lw2 = tex2Dlod(_LineTex, float4(IN[1].uv_base, 0, 0)).a;
                    appendEdge(IN[0].pos, IN[1].pos, (uint)IN[0].idxType.x, (uint)IN[1].idxType.x, lw1, lw2, IN[0].lattr, IN[1].lattr, false, OUT);
                }

                if (isEdgeDrawn(adj.y, IN[2].idxType.y)) {
                    float lw1 = tex2Dlod(_LineTex, float4(IN[1].uv_base, 0, 0)).a;
                    float lw2 = tex2Dlod(_LineTex, float4(IN[2].uv_base, 0, 0)).a;
                    appendEdge(IN[1].pos, IN[2].pos, (uint)IN[1].idxType.x, (uint)IN[2].idxType.x, lw1, lw2, IN[1].lattr, IN[2].lattr, false, OUT);
                }

                if (isEdgeDrawn(adj.z, IN[0].idxType.y)) {
                    float lw1 = tex2Dlod(_LineTex, float4(IN[2].uv_base, 0, 0)).a;
                    float lw2 = tex2Dlod(_LineTex, float4(IN[0].uv_base, 0, 0)).a;
                    appendEdge(IN[2].pos, IN[0].pos, (uint)IN[2].idxType.x, (uint)IN[0].idxType.x, lw1, lw2, IN[2].lattr, IN[0].lattr, false, OUT);
                }
            }

            half4 frag(g2f IN) : SV_Target
            {
                half4 color = (half4)IN.color;
                return color;
            }

                ENDHLSL
        }
        
    }
}
