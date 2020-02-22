using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SphereCastTester : MonoBehaviour
{
    public bool casted = false;
    public float radius = 1f;
    public float step = 0.1f;
    private Vector3 castPosition;

    void Update()
    {
        Ray ray = new Ray(transform.position, transform.forward);
        RaycastHit hit;

        for (float i = 0; i < radius; i += step)
        {
            if (Physics.SphereCast(ray, i, out hit, step))
            {
                casted = true;
                castPosition = hit.point;
                break;
            }
            else
            {
                casted = false;
            }
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, radius);
        if (casted)
        {
            Gizmos.DrawSphere(castPosition, 0.05f);
        }
    }
}