using UnityEngine;

namespace PBD
{
    public class PBDUtils
    {
        public static float PointSegmentDistance(Vector3 a, Vector3 b, Vector3 p)
        {
            Vector3 d = b - a;
            float d2 = Vector3.Dot(d, d);
            float t = Vector3.Dot(d, p - a) / d2;
            Vector3 q = a + d * t;
            Vector3 n = p - q;
            return n.magnitude;
        }

        public static Vector3 ClosestPointOnSegment(Vector3 a, Vector3 b, Vector3 p)
        {
            Vector3 lineDirection = b - a;
            float d2 = Vector3.Dot(lineDirection, lineDirection);
            float t = Vector3.Dot(lineDirection, p - a) / d2;
            Vector3 q = a + lineDirection * t;
            return q;
        }

        public static Quaternion LookAtXAxis(Vector3 dir, Vector3 up)
        {
            return Quaternion.LookRotation(dir, up) * Quaternion.Euler(new Vector3(0, 90, 0));
        }
    }
}