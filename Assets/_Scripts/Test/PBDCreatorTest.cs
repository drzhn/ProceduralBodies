using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using PBD;
using UnityEngine;
using UnityEngine.Rendering;

public class PBDCreatorTest : MonoBehaviour
{
    [Range(0, 1)] [SerializeField] private float _radius;
    [Range(0, 1)] [SerializeField] private float _stiffness;
    [Range(0, 1)] [SerializeField] private float _collisionStiffness;
    [Range(0, 1)] [SerializeField] private float _damping;
    [SerializeField] private bool _useGravity;

    private PBDConnectionTest[] _connections;
    private Dictionary<Transform, int> _points = new Dictionary<Transform, int>();

    private PBDObject _pbd;

    void Start()
    {
        _connections = GetComponentsInChildren<PBDConnectionTest>();
        _points = GetComponentsInChildren<Transform>().Where(x => x != transform).ToDictionary(x => x, x => 0);
        _pbd = new PBDObject(_collisionStiffness, _damping, _useGravity);
        foreach (Transform key in _points.Keys.ToList())
        {
            _pbd.AddPoint(key, _radius, 1, out var index);
            _points[key] = index;
        }

        foreach (PBDConnectionTest connection in _connections)
        {
            connection.Radius = _radius;
            _pbd.AddConnection(_points[connection.transform], _points[connection.connectedPoint], _stiffness);
        }

        _pbd.SetSettings();
    }


    void FixedUpdate()
    {
        _pbd.OnUpdate(Time.fixedDeltaTime);
    }


    private void OnDestroy()
    {
        _pbd.Dispose();
    }
}