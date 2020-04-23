using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using BoneTransform = PBD.PBDSkeletonBoneTransform;

public class SkinningRenderer : MonoBehaviour
{
    [SerializeField] [Range(-1, 1)] private float _range;
    [SerializeField] private SkinnedMeshRenderer _skinnedMeshRenderer;
    [SerializeField] private Material _skinnedMaterial;
    [SerializeField] private Transform _testRootBone;

    private MeshFilter _meshFilter;
    private MeshRenderer _meshRenderer;

    private int _boneCount;
    private Transform[] _bones;
    private BoneTransform[] _boneTransforms;
    private Matrix4x4[] _bonesMatrices;
    private int _rootBoneIndex;

    private float[] _vertexWeights;
    private int[] _vertexBoneIndices;
    private ComputeBuffer _weightsBuffer;
    private ComputeBuffer _indicesBuffer;

    private GameObject _meshContainer;
    private Matrix4x4[] _bindPoses;
    private static readonly int BonesMatrices = Shader.PropertyToID("_BonesMatrices");

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
                Trs = Utils.GetTRS(_bones[i].localPosition, _bones[i].localRotation, _bones[i].localScale),
                ParentIndex = _bones[i] == _skinnedMeshRenderer.rootBone ? -1 : Array.IndexOf(_bones, _bones[i].parent)
            };
            
            if (_boneTransforms[i].ParentIndex == -1) _rootBoneIndex = i;
        }

        BoneWeight[] boneWeights = _meshFilter.mesh.boneWeights;
        DestroyImmediate(_skinnedMeshRenderer);

        _meshRenderer = _meshContainer.AddComponent<MeshRenderer>();
        _meshRenderer.material = _skinnedMaterial;
        _bonesMatrices = new Matrix4x4[_bones.Length];


        var vertexCount = _meshFilter.mesh.vertexCount;
        _vertexWeights = new float[vertexCount * 4];
        _vertexBoneIndices = new int[vertexCount * 4];
        for (int i = 0; i < vertexCount; i++)
        {
            _vertexWeights[i * 4 + 0] = boneWeights[i].weight0;
            _vertexWeights[i * 4 + 1] = boneWeights[i].weight1;
            _vertexWeights[i * 4 + 2] = boneWeights[i].weight2;
            _vertexWeights[i * 4 + 3] = boneWeights[i].weight3;

            _vertexBoneIndices[i * 4 + 0] = boneWeights[i].boneIndex0;
            _vertexBoneIndices[i * 4 + 1] = boneWeights[i].boneIndex1;
            _vertexBoneIndices[i * 4 + 2] = boneWeights[i].boneIndex2;
            _vertexBoneIndices[i * 4 + 3] = boneWeights[i].boneIndex3;
        }

        _weightsBuffer = new ComputeBuffer(vertexCount * 4, sizeof(float));
        _weightsBuffer.SetData(_vertexWeights);

        _indicesBuffer = new ComputeBuffer(vertexCount * 4, sizeof(int));
        _indicesBuffer.SetData(_vertexBoneIndices);

        _meshRenderer.material.SetBuffer("_VertexWeights", _weightsBuffer);
        _meshRenderer.material.SetBuffer("_VertexBoneIndices", _indicesBuffer);

        _bindPoses = _meshFilter.mesh.bindposes;

        Debug.Log($"Bone amount: {_boneCount}");
    }


    void Update()
    {
        _boneTransforms[_rootBoneIndex].Trs = Utils.GetTRS(_testRootBone.position,_testRootBone.rotation, _testRootBone.localScale);

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

        _meshRenderer.material.SetMatrixArray(BonesMatrices, _bonesMatrices);
    }

    private void OnDestroy()
    {
        _weightsBuffer.Dispose();
        _indicesBuffer.Dispose();
    }
}