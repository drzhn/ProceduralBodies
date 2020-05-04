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
        Tags { "RenderType"="Opaque"  "Queue" = "Geometry"}
        LOD 200

        CGPROGRAM
        #define BONES_AMOUNT 100

        #pragma surface surf Standard fullforwardshadows addshadow vertex:vert exclude_path:deferred
        #pragma target 5.0
        #pragma only_renderers d3d11

        sampler2D _MainTex;
        
#ifdef SHADER_API_D3D11
        StructuredBuffer<float4x4> _BonesMatrices;
        StructuredBuffer<float> _VertexWeights;
        StructuredBuffer<int> _VertexBoneIndices;
#endif

        struct appdata{
            float4 vertex : POSITION;
            float3 normal : NORMAL;
            float4 texcoord : TEXCOORD0;
            float4 texcoord1 : TEXCOORD1;
            float4 texcoord2 : TEXCOORD2;
            uint vertexId : SV_VertexID;
            UNITY_VERTEX_INPUT_INSTANCE_ID
            uint instanceId : SV_InstanceID;
            float4 tangent: TANGENT;
         };
        
        struct Input
        {
            float2 uv_MainTex;
        };

        half _Glossiness;
        half _Metallic;
        fixed4 _Color;
        
        void vert (inout appdata v) {

#ifdef SHADER_API_D3D11
            UNITY_SETUP_INSTANCE_ID(v);
            uint vertexId = v.vertexId; 
            uint instanceId = v.instanceId;

            int boneIndex0 = _VertexBoneIndices[vertexId*4+0];
            int boneIndex1 = _VertexBoneIndices[vertexId*4+1];
            int boneIndex2 = _VertexBoneIndices[vertexId*4+2];
            int boneIndex3 = _VertexBoneIndices[vertexId*4+3];
            
            float boneWeight0 = _VertexWeights[vertexId*4+0];
            float boneWeight1 = _VertexWeights[vertexId*4+1];
            float boneWeight2 = _VertexWeights[vertexId*4+2];
            float boneWeight3 = _VertexWeights[vertexId*4+3];
            
            float4 vec =
                mul(_BonesMatrices[instanceId * BONES_AMOUNT + boneIndex0],v.vertex) * boneWeight0 +
                mul(_BonesMatrices[instanceId * BONES_AMOUNT + boneIndex1],v.vertex) * boneWeight1 +
                mul(_BonesMatrices[instanceId * BONES_AMOUNT + boneIndex2],v.vertex) * boneWeight2 +
                mul(_BonesMatrices[instanceId * BONES_AMOUNT + boneIndex3],v.vertex) * boneWeight3;
            
            v.vertex = mul(unity_WorldToObject, vec);
            
            float4 nor =
                mul(_BonesMatrices[instanceId * BONES_AMOUNT + boneIndex0],v.normal) * boneWeight0 +
                mul(_BonesMatrices[instanceId * BONES_AMOUNT + boneIndex1],v.normal) * boneWeight1 +
                mul(_BonesMatrices[instanceId * BONES_AMOUNT + boneIndex2],v.normal) * boneWeight2 +
                mul(_BonesMatrices[instanceId * BONES_AMOUNT + boneIndex3],v.normal) * boneWeight3;
               
            v.normal = nor;
#endif
        }
        
        
        // Add instancing support for this shader. You need to check 'Enable Instancing' on materials that use the shader.
        // See https://docs.unity3d.com/Manual/GPUInstancing.html for more information about instancing.
        // #pragma instancing_options assumeuniformscaling
        // UNITY_INSTANCING_BUFFER_START(Props)
        //     // put more per-instance properties here
        // UNITY_INSTANCING_BUFFER_END(Props)

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            // Albedo comes from a texture tinted by color
            fixed4 c = tex2D (_MainTex, IN.uv_MainTex) * _Color;
            o.Albedo =  c.rgb;
            // Metallic and smoothness come from slider variables
            o.Metallic = _Metallic;
            o.Smoothness = _Glossiness;
            o.Alpha = c.a;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
