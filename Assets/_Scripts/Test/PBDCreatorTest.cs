using System;
using System.Collections.Generic;
using System.Linq;
using PBD;
using UnityEngine;

public class PBDCreatorTest : MonoBehaviour
{
    [SerializeField] private Transform _rootBone;
    [SerializeField] private Mesh _instancedMesh;
    [SerializeField] private Material _instancedMaterial;
    [SerializeField] private int _amountPerDimension;
    [SerializeField] [Range(0, 1)] private float _radius;
    [SerializeField] [Range(0, 1)] private float _stiffness;
    [SerializeField] [Range(0, 1)] private float _collisionStiffness;
    [SerializeField] [Range(0, 1)] private float _boneStiffness;
    [SerializeField] [Range(0, 1)] private float _damping;
    [SerializeField] private bool _useGravity;


    private PBDConnectionTest[] _connections;
    private Dictionary<Transform, int> _points = new Dictionary<Transform, int>();

    private PBDObject _pbd;

    private float __collisionStiffness;
    private float __boneStiffness;
    private float __damping;
    private bool __useGravity;


    private ComputeBuffer _argsBuffer;


    void Start()
    {
//        _connections = GetComponentsInChildren<PBDConnectionTest>();
//        _points = GetComponentsInChildren<Transform>().Where(x => x != transform).ToDictionary(x => x, x => 0);
//        _pbd = new PBDObject(_collisionStiffness, _boneStiffness, _damping, _useGravity, _rootBone);
//        foreach (Transform key in _points.Keys.ToList())
//        {
//            _pbd.AddPoint(key, _radius, 1, out var index);
//            _points[key] = index;
//        }
//
//        foreach (PBDConnectionTest connection in _connections)
//        {
//            connection.Radius = _radius;
////            _pbd.AddConnection(_points[connection.transform], _points[connection.connectedPoint], _stiffness);
//        }

        _pbd = new PBDObject(_collisionStiffness, _boneStiffness, _damping, _useGravity, _rootBone);

        for (int x = 0; x < _amountPerDimension; x++)
        {
            for (int y = 0; y < _amountPerDimension; y++)
            {
                for (int z = 0; z < _amountPerDimension; z++)
                {
                    _pbd.AddPoint(new Vector3(x, y, z), _radius, 1, out var index);
                }
            }
        }

        _pbd.SetSettings();

        __collisionStiffness = _collisionStiffness;
        __boneStiffness = _boneStiffness;
        __damping = _damping;
        __useGravity = _useGravity;

        int[] args = new int[5] {0, 0, 0, 0, 0};
        _argsBuffer = new ComputeBuffer(1, args.Length * sizeof(uint), ComputeBufferType.IndirectArguments);
        args[0] = (int) _instancedMesh.GetIndexCount(0);
        args[1] = _amountPerDimension * _amountPerDimension * _amountPerDimension;
        args[2] = (int) _instancedMesh.GetIndexStart(0);
        args[3] = (int) _instancedMesh.GetBaseVertex(0);

        _argsBuffer.SetData(args);
    }

    private void Update()
    {
        Graphics.DrawMeshInstancedIndirect(
            _instancedMesh, 
            0, 
            _instancedMaterial, 
            new Bounds(Vector3.zero, new Vector3(100.0f, 100.0f, 100.0f)),
            _argsBuffer);
    }


    void FixedUpdate()
    {
        _pbd.OnUpdate(Time.fixedDeltaTime);
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (!Application.isPlaying || _pbd == null) return;

        if (Math.Abs(_collisionStiffness - __collisionStiffness) > Mathf.Epsilon)
        {
            __collisionStiffness = _collisionStiffness;
            _pbd.SetNewProperty(typeof(float), "_collisionStiffness", _collisionStiffness);
        }

        if (Math.Abs(_boneStiffness - __boneStiffness) > Mathf.Epsilon)
        {
            __boneStiffness = _boneStiffness;
            _pbd.SetNewProperty(typeof(float), "_boneStiffness", _boneStiffness);
        }

        if (Math.Abs(_damping - __damping) > Mathf.Epsilon)
        {
            __damping = _damping;
            _pbd.SetNewProperty(typeof(float), "_velocityDamping", _damping);
        }

        if (_useGravity != __useGravity)
        {
            __useGravity = _useGravity;
            _pbd.SetNewProperty(typeof(bool), "_velocityDamping", _useGravity);
        }
    }

#endif


    private void OnDestroy()
    {
        _pbd.Dispose();
        _argsBuffer.Dispose();
    }
}