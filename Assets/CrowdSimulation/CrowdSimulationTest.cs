using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CrowdSimulationTest : MonoBehaviour
{
    [SerializeField] private Mesh _mesh;
    [SerializeField] private Material _material;

    private Matrix4x4[] _matrices;

    private void Awake()
    {
        _matrices = new Matrix4x4[100];
        for (int i = 0; i < 100; i++)
        {
            _matrices[i] = Matrix4x4.TRS( transform.position + Vector3.forward * i, Quaternion.identity, Vector3.one);
        }
    }

    private void Update()
    {
        Graphics.DrawMeshInstanced(_mesh, 0, _material, _matrices);
    }
}