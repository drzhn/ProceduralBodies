using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PBD
{
    [CreateAssetMenu(fileName = "PBDModelData", menuName = "PBDModelData", order = 5)]
    public class PBDModelData : ScriptableObject
    {
        public GameObject Prefab;
        public Mesh Mesh;
        public PBDSkeletonData SkeletonData;
        
        public Matrix4x4[] BindPoses;
        public PBDSkeletonBoneTransform[] SkeletonBoneTransforms;
        public int RootBoneIndex;

        public void GetVerticesData(out float[] vertexWeights, out int[] vertexBoneIndices)
        {
            if (Mesh == null)
            {
                throw new Exception("MESH IS NULL");
            }
            var vertexCount = Mesh.vertexCount;
            BoneWeight[] boneWeights = Mesh.boneWeights;
            vertexWeights = new float[vertexCount * 4];
            vertexBoneIndices = new int[vertexCount * 4];
            for (int i = 0; i < vertexCount; i++)
            {
                vertexWeights[i * 4 + 0] = boneWeights[i].weight0;
                vertexWeights[i * 4 + 1] = boneWeights[i].weight1;
                vertexWeights[i * 4 + 2] = boneWeights[i].weight2;
                vertexWeights[i * 4 + 3] = boneWeights[i].weight3;

                vertexBoneIndices[i * 4 + 0] = boneWeights[i].boneIndex0;
                vertexBoneIndices[i * 4 + 1] = boneWeights[i].boneIndex1;
                vertexBoneIndices[i * 4 + 2] = boneWeights[i].boneIndex2;
                vertexBoneIndices[i * 4 + 3] = boneWeights[i].boneIndex3;
            }
        }
    }
}