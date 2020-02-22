using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SkinningRenderer : MonoBehaviour
{
    [SerializeField] private SkinnedMeshRenderer _skinnedMeshRenderer;
    [SerializeField] private Material _skinnedMaterial;
    [SerializeField] private Transform _rootBone;

    private MeshFilter _meshFilter;
    private MeshRenderer _meshRenderer;
    private Transform[] _bones;
    private Matrix4x4[] _bonesMatrices;

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
        BoneWeight[] boneWeights = _meshFilter.mesh.boneWeights;
        DestroyImmediate(_skinnedMeshRenderer);

        _meshRenderer = _meshContainer.AddComponent<MeshRenderer>();
        _meshRenderer.material = _skinnedMaterial;
        _bonesMatrices = new Matrix4x4[_bones.Length];


        var vertexCount = _meshFilter.mesh.vertexCount;
        vertexWeights = new float[vertexCount * 4];
        vertexBoneIndices = new int[vertexCount * 4];
        print(vertexCount * 4);
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
    }

    void Update()
    {
        for (var i = 0; i < _bonesMatrices.Length; i++)
        {
            _bonesMatrices[i] =  _bones[i].localToWorldMatrix * _bindPoses[i];
        }

        _meshRenderer.material.SetMatrixArray("_BonesMatrices", _bonesMatrices);
    }

    private void OnDestroy()
    {
        _weightsBuffer.Dispose();
        _indicesBuffer.Dispose();
    }
}