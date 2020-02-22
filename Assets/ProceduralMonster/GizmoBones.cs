using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GizmoBones : MonoBehaviour
{
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
    }
}