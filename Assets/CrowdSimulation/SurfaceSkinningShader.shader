Shader "Custom/SurfaceSkinningShader"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _MainTex ("Albedo (RGB)", 2D) = "white" {}
        _Glossiness ("Smoothness", Range(0,1)) = 0.5
        _Metallic ("Metallic", Range(0,1)) = 0.0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200

        CGPROGRAM
        #pragma surface surf BasicDiffuse fullforwardshadows vertex:vert

        #pragma target 5.0
        #pragma only_renderers d3d11

        sampler2D _MainTex;
        
        float4x4 _BonesMatrices[100] ;
#ifdef SHADER_API_D3D11
        StructuredBuffer<float> _VertexWeights;
        StructuredBuffer<int> _VertexBoneIndices;
#endif

        struct appdata{
            float4 vertex : POSITION;
            float3 normal : NORMAL;
            float4 texcoord : TEXCOORD0;
            float4 texcoord1 : TEXCOORD1;
            float4 texcoord2 : TEXCOORD2;
            uint id : SV_VertexID;
            float4 tangent: TANGENT;
         };
        struct Input
        {
            float2 uv_MainTex;
            float3 worldPos;
        };
        
        struct SurfaceOutputCustom
        {
            fixed3 Albedo;      // base (diffuse or specular) color
            fixed3 Normal;      // tangent space normal, if written
            half3 Emission;
            half Metallic;      // 0=non-metal, 1=metal
            half Smoothness;    // 0=rough, 1=smooth
            half Occlusion;     // occlusion (default 1)
            fixed Alpha;        // alpha for transparencies
            float3 worldPos;
        };

        half _Glossiness;
        half _Metallic;
        fixed4 _Color;
        
        void vert (inout appdata v) {

#ifdef SHADER_API_D3D11
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
            
            v.vertex = mul(unity_WorldToObject, vec);
#endif
        }
        
        inline float4 LightingBasicDiffuse (SurfaceOutputCustom s, fixed3 lightDir, fixed atten)
        {
            float3 normal = normalize(cross(ddx(s.worldPos), ddy(s.worldPos)));
            float difLight = max(0, dot (normal, lightDir));
            float4 col;
            col.rgb = s.Albedo * _LightColor0.rgb * (difLight * atten * 2);
            col.a = s.Alpha;
            return col;
        }


        void surf (Input IN, inout SurfaceOutputCustom o)
        {
            // Albedo comes from a texture tinted by color
            fixed4 c = tex2D (_MainTex, IN.uv_MainTex) * _Color;
            o.Albedo = c.rgb;
            // Metallic and smoothness come from slider variables
            o.Metallic = _Metallic;
            o.Smoothness = _Glossiness;
            o.Alpha = c.a;
            o.worldPos = IN.worldPos;
            //float3 surfaceNormal = normalize(cross(ddx(IN.worldPos.xyz), ddy(IN.worldPos.xyz)));
            //o.Normal = surfaceNormal;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
