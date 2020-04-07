using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GizmoBones : MonoBehaviour
{
    [SerializeField] private bool _showAxes;
    [Range(0,1)]
    [SerializeField] private float _axesLength;
    public float radius;
    public Color color;
    private void OnDrawGizmos()
    {
        DrawGizmo(transform);
    }

    void DrawGizmo(Transform obj)
    {
        Gizmos.color = color;
        Gizmos.DrawSphere(obj.position, radius);
        for (int i = 0; i < obj.childCount; i++)
        {
            var child = obj.GetChild(i);
            Gizmos.DrawLine(obj.position,child.position);
            DrawGizmo(child);
        }

        if (_showAxes)
        {
                Gizmos.color = Color.blue;
                Gizmos.DrawLine(obj.position, obj.position + obj.forward * _axesLength);
                Gizmos.color = Color.green;
                Gizmos.DrawLine(obj.position, obj.position + obj.up * _axesLength);
                Gizmos.color = Color.red;
                Gizmos.DrawLine(obj.position, obj.position + obj.right * _axesLength);
        }
        
    }
}