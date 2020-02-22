using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GizmoDrawer : MonoBehaviour
{
    [SerializeField] private float size;
    [SerializeField] private bool wired;
    [SerializeField] private GizmoType type;
    [SerializeField] private Color color;
    private void OnDrawGizmos()
    {
        var _color = Gizmos.color;
        Gizmos.color = color;
        switch (type)
        {
            case GizmoType.Cube:
                if (wired)
                {
                    Gizmos.DrawWireCube(transform.position,Vector3.one*size);
                }
                else
                {
                    Gizmos.DrawCube(transform.position,Vector3.one*size);
                }
                break;
            case GizmoType.Sphere:
                if (wired)
                {
                    Gizmos.DrawWireSphere(transform.position,size);
                }
                else
                {
                    Gizmos.DrawSphere(transform.position,size);
                }
                break;
        }

        Gizmos.color = _color;
    }
}

public enum GizmoType
{
    Sphere,
    Cube
}