using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ClampingAroundAxis : MonoBehaviour
{
    [SerializeField] private Transform _pivot;
    [SerializeField] private Vector2 _xLimits;
    [SerializeField] private Vector2 _yLimits;
    [SerializeField] private Vector2 _zLimits;

    void Start()
    {
    }

    void Update()
    {
        transform.localRotation = LookAtXAxis((_pivot.position - transform.position), Vector3.up, false);

        transform.localRotation = ClampRotationAroundAxis(transform.localRotation, _xLimits, _yLimits, _zLimits);

        Debug.DrawLine(transform.position, transform.position - transform.right, Color.red);
    }

    Quaternion ClampRotationAroundAxis(Quaternion q, Vector2 xLimits, Vector2 yLimits, Vector2 zLimits)
    {
        q.x /= q.w;
        q.y /= q.w;
        q.z /= q.w;
        q.w = 1.0f;

        float angleX = 2.0f * Mathf.Rad2Deg * Mathf.Atan(q.x);
        angleX = Mathf.Clamp(angleX, xLimits.x, xLimits.y);
        q.x = Mathf.Tan(0.5f * Mathf.Deg2Rad * angleX);

        float angleY = 2.0f * Mathf.Rad2Deg * Mathf.Atan(q.y);
        angleY = Mathf.Clamp(angleY, yLimits.x, yLimits.y);
        q.y = Mathf.Tan(0.5f * Mathf.Deg2Rad * angleY);

        float angleZ = 2.0f * Mathf.Rad2Deg * Mathf.Atan(q.z);
        angleZ = Mathf.Clamp(angleZ, zLimits.x, zLimits.y);
        q.z = Mathf.Tan(0.5f * Mathf.Deg2Rad * angleZ);
        
        return q;
    }

    Quaternion ClampRotationAroundYAxis(Quaternion q, float MinimumY, float MaximumY)
    {
        q.x /= q.w;
        q.y /= q.w;
        q.z /= q.w;
        q.w = 1.0f;

        float angleY = 2.0f * Mathf.Rad2Deg * Mathf.Atan(q.y);

        angleY = Mathf.Clamp(angleY, MinimumY, MaximumY);

        q.y = Mathf.Tan(0.5f * Mathf.Deg2Rad * angleY);

        return q;
    }

    private static Quaternion LookAtXAxis(Vector3 dir, Vector3 up, bool negativeXscale)
    {
        return Quaternion.LookRotation(negativeXscale ? -dir : dir, up) * Quaternion.Euler(new Vector3(0, 90, 0));
    }
}