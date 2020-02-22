using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;

namespace PBD
{
    public class PBDObjectSimple : MonoBehaviour
    {
        private class Point
        {
            public Vector3 p;
            public Vector3 v;
            public float m;
            public bool collided;
        }

        private class StretchingConstraint
        {
            public Transform a;
            public Transform b;
            public float d;
            public float stiffness;
        }

        private class BendingConstraint
        {
            public Transform a; //
            public Transform b; // this two vertices will be middle 
            public Transform c;
            public Transform d;
            public float phi;
            public float bending;
        }

        private class CollisionConstraint
        {
            public Transform a;
            public Vector3 q;
            public Vector3 n;
        }

        [Range(0, 1)] public float stiffness = 0.9f;
        [Range(0, 1)] public float bending = 0.9f;
        [Range(0, 1)] public float damping = 0.9f;
        [Range(0, 1)] public float collisionDamping = 0.9f;
        [Range(0, 1)] public float pointRadius = 0.9f;
        [Range(0, 1)] public float edgeRadius = 0.9f;
        public bool useGravity;
        public int solverSteps = 3;

        private List<PBDConnectionTest> _constraints;
        
        private Dictionary<Transform, Point> points;
        private StretchingConstraint[] stretchingConstraints;
        private List<BendingConstraint> bendingConstraints = new List<BendingConstraint>();
        private float _stiffness;
        private float _bending;
        private Dictionary<Transform, List<Transform>> adjacency = new Dictionary<Transform, List<Transform>>();
        private List<HashSet<Transform>> triangles = new List<HashSet<Transform>>();
        private Dictionary<Transform, int> colors;

        private List<CollisionConstraint> collisionConstraints;

        private LayerMask excludeSelf;

        private void Start()
        {
            excludeSelf = ~(1 << (LayerMask.NameToLayer("MonsterCollider")));
            _constraints = GetComponentsInChildren<PBDConnectionTest>().ToList();
            stretchingConstraints = _constraints.Select(x => new StretchingConstraint() {a = x.transform, b = x.connectedPoint, d = Vector3.Distance(x.transform.position, x.connectedPoint.position), stiffness = stiffness}).ToArray();
            points = GetComponentsInChildren<Transform>().Where(x => x.parent == transform).ToDictionary(x => x, x => new Point() {p = x.position, v = Vector3.zero, m = 1});
            points[transform.GetChild(0)].m = 0;
            _stiffness = Mathf.Pow(1 - Mathf.Pow(1 - stiffness, 1f / solverSteps), solverSteps);
            _bending = Mathf.Pow(1 - Mathf.Pow(1 - bending, 1f / solverSteps), solverSteps);
            

            collisionConstraints = new List<CollisionConstraint>();
            foreach (StretchingConstraint collision in stretchingConstraints)
            {
                if (!adjacency.ContainsKey(collision.a)) adjacency.Add(collision.a, new List<Transform>());
                if (!adjacency.ContainsKey(collision.b)) adjacency.Add(collision.b, new List<Transform>());
                adjacency[collision.a].Add(collision.b);
                adjacency[collision.b].Add(collision.a);
            }

            foreach (StretchingConstraint collision in stretchingConstraints)
            {
                foreach (var thirdPoint in adjacency[collision.a].Intersect(adjacency[collision.b]))
                {
                    HashSet<Transform> candidate = new HashSet<Transform>() {collision.a, collision.b, thirdPoint};
                    if (!triangles.Exists(x => x.SetEquals(candidate)))
                    {
                        triangles.Add(candidate);
                    }
                }
            }

            for (var i = 0; i < triangles.Count - 1; i++)
            {
                for (var j = i + 1; j < triangles.Count; j++)
                {
                    var intersection = triangles[i].Intersect(triangles[j]).ToArray();
                    if (intersection.Length == 2)
                    {
                        var bc = new BendingConstraint()
                        {
                            a = intersection[0],
                            b = intersection[1],
                            c = triangles[i].Except(intersection).First(),
                            d = triangles[j].Except(intersection).First(),
                        };
                        bc.phi = Mathf.Acos(Vector3.Dot(
                            Vector3.Cross(bc.b.position - bc.a.position, bc.c.position - bc.a.position).normalized,
                            Vector3.Cross(bc.b.position - bc.a.position, bc.d.position - bc.a.position).normalized
                        ));
                        bendingConstraints.Add(bc);
                    }
                }
            }
        }

        void Update()
        {
            collisionConstraints.Clear();
            foreach (var t in points.Keys.ToList())
            {
                if (useGravity) points[t].v = points[t].v + Time.deltaTime * (9 * Vector3.down) * points[t].m;
                points[t].v *= damping;
                points[t].p = t.position + Time.deltaTime * points[t].v;
                points[t].collided = false;
            }


            for (int i = 0; i < solverSteps; i++)
            {
                foreach (StretchingConstraint collision in stretchingConstraints)
                {
                    float currentDistance = Vector3.Distance(points[collision.a].p, points[collision.b].p);
                    Vector3 l = (currentDistance - collision.d) * (points[collision.a].p - points[collision.b].p) / currentDistance;
                    points[collision.a].p += -(points[collision.a].m / (points[collision.a].m + points[collision.b].m)) * l * _stiffness;
                    points[collision.b].p += +(points[collision.b].m / (points[collision.a].m + points[collision.b].m)) * l * _stiffness;
                }

                foreach (BendingConstraint collision in bendingConstraints)
                {
                    //if (points[collision.a].collided || points[collision.b].collided || points[collision.c].collided || points[collision.d].collided) continue;
                    Vector3 p1 = points[collision.a].p;
                    Vector3 p2 = points[collision.b].p;
                    Vector3 p3 = points[collision.c].p;
                    Vector3 p4 = points[collision.d].p;

                    Vector3 n1 = Vector3.Cross((p2 - p1), (p3 - p1)).normalized;
                    Vector3 n2 = Vector3.Cross((p2 - p1), (p4 - p1)).normalized;

                    float d = Vector3.Dot(n1, n2);

                    Vector3 q3 = (Vector3.Cross((p2 - p1), n2) + Vector3.Cross(n1, (p2 - p1)) * d) / Vector3.Cross((p2 - p1), (p3 - p1)).magnitude;
                    Vector3 q4 = (Vector3.Cross((p2 - p1), n1) + Vector3.Cross(n2, (p2 - p1)) * d) / Vector3.Cross((p2 - p1), (p4 - p1)).magnitude;
                    Vector3 q2 = -(Vector3.Cross((p3 - p1), n2) + Vector3.Cross(n1, (p3 - p1)) * d) / Vector3.Cross((p2 - p1), (p3 - p1)).magnitude -
                                 (Vector3.Cross((p4 - p1), n1) + Vector3.Cross(n2, (p4 - p1)) * d) / Vector3.Cross((p2 - p1), (p4 - p1)).magnitude;
                    Vector3 q1 = -q2 - q3 - q4;

                    float sumQ = (q1.sqrMagnitude * points[collision.a].m + q2.sqrMagnitude * points[collision.b].m + q3.sqrMagnitude * points[collision.c].m + q4.sqrMagnitude * points[collision.d].m);
                    float s = -(1 * Mathf.Sqrt(1 - d * d) * (Mathf.Acos(d) - collision.phi)) / sumQ;
                    if (float.IsNaN(sumQ) || float.IsNaN(s)) continue;

                    points[collision.a].p += s * q1 * _bending * points[collision.a].m;
                    points[collision.b].p += s * q2 * _bending * points[collision.b].m;
                    points[collision.c].p += s * q3 * _bending * points[collision.c].m;
                    points[collision.d].p += s * q4 * _bending * points[collision.d].m;
                }
            }

            foreach (var t in points.Keys.ToList())
            {
                RaycastHit hit;
                if (Physics.SphereCast(t.position,pointRadius, points[t].p - t.position, out hit, Vector3.Distance(t.position, points[t].p) , excludeSelf))
                {
                    collisionConstraints.Add(new CollisionConstraint()
                    {
                        a = t, n = hit.normal, q = hit.point
                    });
                    Debug.DrawLine(hit.point,hit.point + hit.normal);
                }
            }

            foreach (CollisionConstraint collision in collisionConstraints)
            {
                Vector3 oldPoint = collision.a.position;
                Vector3 newPoint = points[collision.a].p;
                Vector3 realCollisionPoint = collision.q + (collision.n).normalized * (pointRadius);
                collision.a.position = realCollisionPoint + (oldPoint - newPoint).normalized * (0.001f);
                points[collision.a].p = realCollisionPoint +
                                        Vector3.ProjectOnPlane(newPoint - realCollisionPoint, collision.n) +
                                        collision.n.normalized * (0.001f);
                points[collision.a].collided = true;
            }

            foreach (var t in points.Keys.ToList())
            {
                points[t].v = (points[t].p - t.position) / Time.deltaTime * (points[t].collided ? collisionDamping : 1);
                t.position = points[t].p;
            }


            // TODO friction and restriction for velocity
        }

        private void OnDrawGizmos()
        {
            if (points == null) return;
            Gizmos.color = Color.red;
            foreach (Transform pointsKey in points.Keys)
            {
                Gizmos.DrawWireSphere(pointsKey.position,pointRadius);
            }
        }
    }
}