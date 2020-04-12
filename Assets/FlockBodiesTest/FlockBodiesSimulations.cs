using System;
using System.Collections.Generic;
using UnityEngine;

public class FlockBodiesSimulations : MonoBehaviour
{
    [SerializeField] private ForceMode _forceMode;
    [SerializeField] [Range(0, 10)] private float _forceMultiplier;
    [SerializeField] private bool _normalize;
    [SerializeField] private GameObject _bodyPrefab;
    [SerializeField] private uint _initialBodyCount;

    private readonly List<Transform> _childs = new List<Transform>();
    private readonly List<Rigidbody> _rigidbodies = new List<Rigidbody>();

    private void Awake()
    {
        for (int i = 0; i < transform.childCount; i++)
        {
            _childs.Add(transform.GetChild(i));
        }
    }

    private void Start()
    {
        uint i = 0;
        while (i < _initialBodyCount)
        {
            AddBody();
            i++;
        }
    }

    void OnGUI()
    {
        if (GUILayout.Button("Add Body"))
        {
            AddBody();
        }
    }

    private void FixedUpdate()
    {
        foreach (Rigidbody body in _rigidbodies)
        {
            Vector3 pos = _childs[0].position;
            for (var i = 0; i < _childs.Count - 1; i++)
            {
                var p = ClosestPointOnSegment(body.position, _childs[i].position, _childs[i + 1].position);
                if (Vector3.Distance(p, body.position) < Vector3.Distance(pos, body.position))
                {
                    pos = p;
                }
            }

            Vector3 to = (pos - body.position);
            if (_normalize) to = to.normalized;
            body.AddForce(to * _forceMultiplier, _forceMode);
//            body.MovePosition(pos);
        }
    }

    private void AddBody()
    {
        var r = Instantiate(_bodyPrefab).GetComponent<Rigidbody>();
        _rigidbodies.Add(r);
    }

    private static Vector3 ClosestPointOnSegment(Vector3 point, Vector3 a, Vector3 b)
    {
        var v = b - a;
        var d = Vector3.Dot(point - a, v) / Vector3.Dot(v, v);
        if (d <= 0) return a;
        if (d >= 1) return b;
        return a + d * v;
    }
}