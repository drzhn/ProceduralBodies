using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SkinningRenderer : MonoBehaviour
{
    private struct BoneTransform
    {
        public Matrix4x4 Trs;
        public int ParentIndex;
    }

    [Range(-1, 1)] [SerializeField] private float _range;
    [SerializeField] private SkinnedMeshRenderer _skinnedMeshRenderer;
    [SerializeField] private Material _skinnedMaterial;

    private MeshFilter _meshFilter;
    private MeshRenderer _meshRenderer;

    private int _boneCount;
    private Transform[] _bones;
    private BoneTransform[] _boneTransforms;
    private Matrix4x4[] _bonesMatrices;
    private int _rootBoneIndex;

    private float[] vertexWeights;
    private int[] vertexBoneIndices;
    private ComputeBuffer _weightsBuffer;
    private ComputeBuffer _indicesBuffer;

    private GameObject _meshContainer;
    private Matrix4x4[] _bindPoses;

    void Start()
    {
        _meshContainer = _skinnedMeshRenderer.gameObject;
        _meshFilter = _meshContainer.AddComponent<MeshFilter>();
        _meshFilter.mesh = _skinnedMeshRenderer.sharedMesh;
        _bones = _skinnedMeshRenderer.bones;
        _boneCount = _bones.Length;
        _boneTransforms = new BoneTransform[_boneCount];
        for (var i = 0; i < _boneCount; i++)
        {
            _boneTransforms[i] = new BoneTransform()
            {
                Trs = GetTrsMatrix(_bones[i]),
                ParentIndex = _bones[i] == _skinnedMeshRenderer.rootBone ? -1 : Array.IndexOf(_bones, _bones[i].parent)
            };
            if (_boneTransforms[i].ParentIndex == -1) _rootBoneIndex = i;
        }

//        foreach (Transform bone in _bones)
//        {
//            bone.parent = null;
//        }

        BoneWeight[] boneWeights = _meshFilter.mesh.boneWeights;
        DestroyImmediate(_skinnedMeshRenderer);

        _meshRenderer = _meshContainer.AddComponent<MeshRenderer>();
        _meshRenderer.material = _skinnedMaterial;
        _bonesMatrices = new Matrix4x4[_bones.Length];


        var vertexCount = _meshFilter.mesh.vertexCount;
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

        _weightsBuffer = new ComputeBuffer(vertexCount * 4, sizeof(float));
        _weightsBuffer.SetData(vertexWeights);

        _indicesBuffer = new ComputeBuffer(vertexCount * 4, sizeof(int));
        _indicesBuffer.SetData(vertexBoneIndices);

        _meshRenderer.material.SetBuffer("_VertexWeights", _weightsBuffer);
        _meshRenderer.material.SetBuffer("_VertexBoneIndices", _indicesBuffer);

        _bindPoses = _meshFilter.mesh.bindposes;

        Debug.Log($"Bone amount: {_boneCount}");
    }


    void Update()
    {
        _boneTransforms[_rootBoneIndex].Trs = Rotate(Quaternion.Euler(0, 90f * _range, 0));

        for (var i = 0; i < _boneCount; i++)
        {
            Matrix4x4 m = _boneTransforms[i].Trs;
            int parentIndex = _boneTransforms[i].ParentIndex;
            while (parentIndex != -1)
            {
                m = _boneTransforms[parentIndex].Trs * m;
                parentIndex = _boneTransforms[parentIndex].ParentIndex;
            }

            _bonesMatrices[i] = m * _bindPoses[i];
        }

        _meshRenderer.material.SetMatrixArray("_BonesMatrices", _bonesMatrices);
    }

    private void OnDestroy()
    {
        _weightsBuffer.Dispose();
        _indicesBuffer.Dispose();
    }

    private static Matrix4x4 GetTrsMatrix(Transform transform)
    {
        return TRS(transform.localPosition, transform.localRotation, transform.localScale);
    }

    private static Matrix4x4 TRS(Vector3 pos, Quaternion q, Vector3 s)
    {
        Matrix4x4 result = new Matrix4x4();
        // Rotation and Scale
        // Quaternion multiplication can be used to represent rotation. 
        // If a quaternion is represented by qw + i qx + j qy + k qz , then the equivalent matrix for rotation is (including scale):
        // Remarks: https://forums.inovaestudios.com/t/math-combining-a-translation-rotation-and-scale-matrix-question-to-you-math-magicians/5194/2
        float sqw = q.w * q.w;
        float sqx = q.x * q.x;
        float sqy = q.y * q.y;
        float sqz = q.z * q.z;
        result.m00 = (1 - 2 * sqy - 2 * sqz) * s.x;
        result.m01 = (2 * q.x * q.y - 2 * q.z * q.w);
        result.m02 = (2 * q.x * q.z + 2 * q.y * q.w);
        result.m10 = (2 * q.x * q.y + 2 * q.z * q.w);
        result.m11 = (1 - 2 * sqx - 2 * sqz) * s.y;
        result.m12 = (2 * q.y * q.z - 2 * q.x * q.w);
        result.m20 = (2 * q.x * q.z - 2 * q.y * q.w);
        result.m21 = (2 * q.y * q.z + 2 * q.x * q.w);
        result.m22 = (1 - 2 * sqx - 2 * sqy) * s.z;
        // Translation
        result.m03 = pos.x;
        result.m13 = pos.y;
        result.m23 = pos.z;
        result.m33 = 1.0f;
        // Return result
        return result;
    }

    private static Matrix4x4 Rotate(Quaternion q)
    {
        Matrix4x4 result = new Matrix4x4();
        float sqw = q.w * q.w;
        float sqx = q.x * q.x;
        float sqy = q.y * q.y;
        float sqz = q.z * q.z;
        result.m00 = (1 - 2 * sqy - 2 * sqz);
        result.m01 = (2 * q.x * q.y - 2 * q.z * q.w);
        result.m02 = (2 * q.x * q.z + 2 * q.y * q.w);
        result.m10 = (2 * q.x * q.y + 2 * q.z * q.w);
        result.m11 = (1 - 2 * sqx - 2 * sqz);
        result.m12 = (2 * q.y * q.z - 2 * q.x * q.w);
        result.m20 = (2 * q.x * q.z - 2 * q.y * q.w);
        result.m21 = (2 * q.y * q.z + 2 * q.x * q.w);
        result.m22 = (1 - 2 * sqx - 2 * sqy);
        result.m33 = 1.0f;
        return result;
    }
}