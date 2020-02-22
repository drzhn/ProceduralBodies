using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class Spring : MonoBehaviour
{
    [SerializeField] private Rigidbody[] connectedBodies;
    [SerializeField] private float springForce;
    [SerializeField] private float springDamper;
    private Rigidbody _rigidbody;
    private float[] _initDistances;

    private void Awake()
    {
        _rigidbody = GetComponent<Rigidbody>();
        _initDistances = new float[connectedBodies.Length];
        for (var i = 0; i < connectedBodies.Length; i++)
        {
            _initDistances[i] = Vector3.Distance(transform.position, connectedBodies[i].position);
        }
    }

    private void FixedUpdate()
    {
        for (var i = 0; i < connectedBodies.Length; i++)
        {
            float distance = Vector3.Distance(transform.position, connectedBodies[i].position);
            if (Mathf.Abs(distance - _initDistances[i]) > Mathf.Epsilon)
            {
                int dir = distance - _initDistances[i] > 0 ? 1 : -1;
                _rigidbody.AddForce((connectedBodies[i].position - transform.position).normalized * dir * springForce * distance  - _rigidbody.velocity * springDamper, ForceMode.Force);
                connectedBodies[i].AddForce((transform.position - connectedBodies[i].position).normalized * dir * springForce * distance  - connectedBodies[i].velocity * springDamper, ForceMode.Force);
            }
        }
    }

    private void OnDrawGizmos()
    {
        if (connectedBodies == null) return;

        foreach (Rigidbody body in connectedBodies)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawLine(transform.position, body.position);
        }
    }
}