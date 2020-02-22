Shader "Unlit/UnlitSkinningShader"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma target 5.0

            #pragma vertex vert
            #pragma fragment frag
            // make fog work
            #pragma multi_compile_fog

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                uint id : SV_VertexID;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                UNITY_FOG_COORDS(1)
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float4x4 _BonesMatrices[100] ;
            StructuredBuffer<float> _VertexWeights;
            StructuredBuffer<int> _VertexBoneIndices;

            v2f vert (appdata v)
            {
                v2f o;
                uint id = v.id;
                
                int boneIndex0 = _VertexBoneIndices[id*4+0];
                int boneIndex1 = _VertexBoneIndices[id*4+1];
                int boneIndex2 = _VertexBoneIndices[id*4+2];
                int boneIndex3 = _VertexBoneIndices[id*4+3];
                
                float boneWeight0 = _VertexWeights[id*4+0];
                float boneWeight1 = _VertexWeights[id*4+1];
                float boneWeight2 = _VertexWeights[id*4+2];
                float boneWeight3 = _VertexWeights[id*4+3];
                
                float4 vec = v.vertex;
                vec =
                    mul(_BonesMatrices[boneIndex0],v.vertex) * boneWeight0 +
                    mul(_BonesMatrices[boneIndex1],v.vertex) * boneWeight1 +
                    mul(_BonesMatrices[boneIndex2],v.vertex) * boneWeight2 +
                    mul(_BonesMatrices[boneIndex3],v.vertex) * boneWeight3;
               
                o.vertex = mul(UNITY_MATRIX_VP, vec);
                
                //o.vertex = UnityObjectToClipPos(vec);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                UNITY_TRANSFER_FOG(o,o.vertex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // sample the texture
                fixed4 col = tex2D(_MainTex, i.uv);
                // apply fog
                UNITY_APPLY_FOG(i.fogCoord, col);
                return col;
            }
            ENDCG
        }
    }
}
