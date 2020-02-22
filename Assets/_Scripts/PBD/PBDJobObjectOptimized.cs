using System;
using System.Collections.Generic;
using System.Linq;
using PBD;
using UnityEngine;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine.Jobs;

public class PBDJobObjectOptimized : MonoBehaviour
{
    private struct Point
    {
        public Vector3 t;
        public Vector3 p;
        public Vector3 v;
        public float m;
        public bool collided;
    }

    private struct UpdateVelocityJob : IJobParallelFor
    {
        public float DeltaTime;
        public bool UseGravity;
        public float VelocityDamping;
        public NativeArray<Point> points;

        public void Execute(int i)
        {
            var point = points[i];
            if (UseGravity) point.v += point.m * 9 * DeltaTime * Vector3.down;
            point.v *= VelocityDamping;
            point.p = point.t + DeltaTime * point.v;
            point.collided = false;
            points[i] = point;
        }
    }

    private struct UpdateStretchingJob : IJobParallelFor
    {
        public float pointRadius;
        public int stretchingCapacity;
        public int bendingCapacity;
        public float stretchingStiffness;
        public float collisionStretchingStiffness;
        public float bendingStiffness;
        public NativeArray<Point> points;
        [ReadOnly] public NativeArray<Vector3> tempPoints;
        [ReadOnly] public NativeArray<int> connectedPoints;
        [ReadOnly] public NativeArray<float> distances;

        [ReadOnly] public NativeArray<int> connectedTrianglePoints;
        [ReadOnly] public NativeArray<bool> positionsInBendingSystem;
        [ReadOnly] public NativeArray<float> phiValues;

        [ReadOnly] public NativeArray<Vector3> bonePoints;
        [ReadOnly] public NativeArray<float> boneDistances;
        public float boneStiffness;

        public void Execute(int index)
        {
            var point = points[index];
            for (int i = 0; i < stretchingCapacity; i++)
            {
                int connectedPointIndex = connectedPoints[index * stretchingCapacity + i];
                if (connectedPointIndex == -1) continue;

                float currentDistance = Vector3.Distance(tempPoints[connectedPointIndex], tempPoints[index]);
                if (currentDistance > 2 * pointRadius)
                {
                    Vector3 dir = (currentDistance - distances[index * stretchingCapacity + i]) / currentDistance *
                                  (tempPoints[index] - tempPoints[connectedPointIndex]);
                    point.p -= 0.5f * stretchingStiffness * dir;
                }

                for (int j = 0; j < stretchingCapacity; j++)
                {
                    int nextPointIndex = connectedPoints[connectedPointIndex * stretchingCapacity + j];
                    if (nextPointIndex == index || nextPointIndex == -1) continue;
                    point.p -= CheckSphereCollision(index, nextPointIndex);
                }

                point.p -= CheckSphereCollision(index, connectedPointIndex);
                /*(tempPoints[index].m / (tempPoints[index].m + tempPoints[constraintsMap[index][i].connectedPoint].m)) */
            }

//            for (int i = 0; i < bendingCapacity; i++)
//            {
//                if (connectedTrianglePoints[(index * bendingCapacity + i) * 3 + 0] == -1) continue;
//
//                bool positionInBendingSystem = positionsInBendingSystem[index * bendingCapacity + i];
//                float phi = phiValues[index * bendingCapacity + i];
//                Vector3 p1;
//                Vector3 p2;
//                Vector3 p3;
//                Vector3 p4;
//
//                if (positionInBendingSystem)
//                {
//                    p1 = tempPoints[index];
//                    p2 = tempPoints[connectedTrianglePoints[(index * bendingCapacity + i) * 3 + 0]];
//                    p3 = tempPoints[connectedTrianglePoints[(index * bendingCapacity + i) * 3 + 1]];
//                    p4 = tempPoints[connectedTrianglePoints[(index * bendingCapacity + i) * 3 + 2]];
//                }
//                else
//                {
//                    p1 = tempPoints[connectedTrianglePoints[(index * bendingCapacity + i) * 3 + 1]];
//                    p2 = tempPoints[connectedTrianglePoints[(index * bendingCapacity + i) * 3 + 2]];
//                    p3 = tempPoints[index];
//                    p4 = tempPoints[connectedTrianglePoints[(index * bendingCapacity + i) * 3 + 0]];
//                }
//
//                Vector3 n1 = Vector3.Cross((p2 - p1), (p3 - p1)).normalized;
//                Vector3 n2 = Vector3.Cross((p2 - p1), (p4 - p1)).normalized;
//
//                float d = Vector3.Dot(n1, n2);
//
//                Vector3 q3 = (Vector3.Cross((p2 - p1), n2) + Vector3.Cross(n1, (p2 - p1)) * d) / Vector3.Cross((p2 - p1), (p3 - p1)).magnitude;
//                Vector3 q4 = (Vector3.Cross((p2 - p1), n1) + Vector3.Cross(n2, (p2 - p1)) * d) / Vector3.Cross((p2 - p1), (p4 - p1)).magnitude;
//                Vector3 q2 = -(Vector3.Cross((p3 - p1), n2) + Vector3.Cross(n1, (p3 - p1)) * d) / Vector3.Cross((p2 - p1), (p3 - p1)).magnitude -
//                             (Vector3.Cross((p4 - p1), n1) + Vector3.Cross(n2, (p4 - p1)) * d) / Vector3.Cross((p2 - p1), (p4 - p1)).magnitude;
//                Vector3 q1 = -q2 - q3 - q4;
//
//                float sumQ = (q1.sqrMagnitude * 1 + q2.sqrMagnitude * 1 + q3.sqrMagnitude * 1 + q4.sqrMagnitude * 1);
//                float s = -(1 * Mathf.Sqrt(1 - d * d) * (Mathf.Acos(d) - phi)) / sumQ;
//                if (float.IsNaN(sumQ) || float.IsNaN(s)) continue;
//
//                if (positionInBendingSystem)
//                {
//                    point.p += s * bendingStiffness * q1;
//                }
//                else
//                {
//                    point.p += s * bendingStiffness * q3;
//                }
//            }

            for (int i = 0; i < bonePoints.Length; i++)
            {
                float currentDistance = Vector3.Distance(bonePoints[i], tempPoints[index]);
                Vector3 dir = (currentDistance - boneDistances[index * bonePoints.Length + i]) / currentDistance *
                              (tempPoints[index] - bonePoints[i]);
                point.p -= boneStiffness * dir;
            }

            points[index] = point;
        }

        private Vector3 CheckSphereCollision(int index, int collisionIndex)
        {
            float currentDistance = Vector3.Distance(tempPoints[collisionIndex], tempPoints[index]);
            if (currentDistance < 2 * pointRadius)
            {
                Vector3 dir = 0.5f * (currentDistance - 2 * pointRadius) / currentDistance *
                              (tempPoints[index] - tempPoints[collisionIndex]);
                return 0.5f * collisionStretchingStiffness * dir;
            }

            return Vector3.zero;
        }
    }

    private struct UpdateTemporaryArrayJob : IJobParallelFor
    {
        [ReadOnly] public NativeArray<Point> points;
        public NativeArray<Vector3> tempPoints;

        public void Execute(int i)
        {
            tempPoints[i] = points[i].p;
        }
    }

    struct PrepareSpherecastCommands : IJobParallelFor
    {
        public float Radius;
        public NativeArray<SpherecastCommand> Spherecasts;
        [ReadOnly] public NativeArray<Point> points;


        public void Execute(int i)
        {
            Spherecasts[i] = new SpherecastCommand(
                points[i].t,
                Radius,
                (points[i].p - points[i].t).normalized,
                Vector3.Distance(points[i].p, points[i].t) + 0.001f);
        }
    }

    struct IntegrateCollision : IJobParallelFor
    {
        [ReadOnly] public NativeArray<RaycastHit> Hits;
        public NativeArray<Point> points;
        public float Radius;

        public void Execute(int i)
        {
            var point = points[i];
            if (Hits[i].normal != Vector3.zero)
            {
                Vector3 oldPoint = point.t;
                Vector3 newPoint = point.p;
                Vector3 realCollisionPoint = Hits[i].point + Hits[i].normal * Radius;
                point.t = realCollisionPoint + (oldPoint - newPoint).normalized * (0.001f);
                point.p = realCollisionPoint +
                          Vector3.ProjectOnPlane(newPoint - realCollisionPoint, Hits[i].normal) +
                          Hits[i].normal * (0.001f);
                point.collided = true;
            }

            points[i] = point;
        }
    }

    private struct UpdatePositionJob : IJobParallelForTransform
    {
        public float DeltaTime;
        public NativeArray<Point> points;

        public void Execute(int index, TransformAccess transform)
        {
            var point = points[index];
            point.v = (point.p - point.t) / DeltaTime; // * (point.collided ? collisionDamping : 1);
            point.t = point.p;
            points[index] = point;
            transform.position = point.p;
        }
    }


    [SerializeField] [Range(0, 1)] private float stretching = 0.9f;
    [SerializeField] [Range(0, 1)] private float collisionStretching = 0.9f;
    [SerializeField] [Range(0, 1)] private float bending = 0.9f;
    [SerializeField] [Range(0, 1)] private float boneStiffness = 0.9f;
    [SerializeField] [Range(0, 1)] private float damping = 0.9f;
    [SerializeField] [Range(0, 1)] private float pointRadius = 0.9f;
    [SerializeField] private bool useGravity;
    [SerializeField] private int solverSteps = 3;
    [SerializeField] private Transform[] bones;

    private const int STRETCHING_CAPACITY = 15;
    private const int BENDING_CAPACITY = 15;

    private Transform[] transforms;
    private TransformAccessArray accessArray;
    private NativeArray<Point> points;
    private NativeArray<Vector3> tempPoints;

    // stretching data
    private NativeArray<int> connectedPoints;
    private NativeArray<float> distances;

    //bending data
    private NativeArray<int> connectedTrianglePoints;
    private NativeArray<bool> positionsInBendingSystem;
    private NativeArray<float> phi;

    // connection to bones data
    private NativeArray<Vector3> bonePoints;
    private NativeArray<float> boneDistances;

    void Start()
    {
        var _stiffness = stretching; //Mathf.Pow(1 - Mathf.Pow(1 - stiffness, 1f / solverSteps), solverSteps);
        List<PBDConnectionTest> stretchingConstraints =
            GetComponentsInChildren<PBDConnectionTest>().Where(x => x.connectedPoint != null).ToList();

        transforms = GetComponentsInChildren<Transform>().Where(x => x.parent == transform).ToArray();
        points = new NativeArray<Point>(transforms.Select(t =>
            new Point()
            {
                t = t.position,
                p = t.position,
                m = 1,
                v = Vector3.zero,
                collided = false,
            }).ToArray(), Allocator.Persistent);
        tempPoints = new NativeArray<Vector3>(transforms.Select(x => x.position).ToArray(), Allocator.Persistent);

        connectedPoints = new NativeArray<int>(transforms.Length * STRETCHING_CAPACITY, Allocator.Persistent);
        distances = new NativeArray<float>(transforms.Length * STRETCHING_CAPACITY, Allocator.Persistent);
        connectedTrianglePoints = new NativeArray<int>(transforms.Length * BENDING_CAPACITY * 3, Allocator.Persistent);
        positionsInBendingSystem = new NativeArray<bool>(transforms.Length * BENDING_CAPACITY, Allocator.Persistent);
        phi = new NativeArray<float>(transforms.Length * BENDING_CAPACITY, Allocator.Persistent);


        var adjacency = new Dictionary<Transform, List<Transform>>();
        Dictionary<Transform, List<int>> map;
        Dictionary<Transform, List<bool>> connectionType;
        Dictionary<Transform, List<float>> phiValues;
        for (int i = 0; i < transforms.Length; i++)
        {
            var pointConnectionTransforms = stretchingConstraints
                .Where(c => c.transform == transforms[i] || c.connectedPoint == transforms[i])
                .Select(x => x.transform == transforms[i] ? x.connectedPoint : x.transform)
                .ToList();
            adjacency.Add(transforms[i], pointConnectionTransforms);
        }

        PrepareBendingMap(transforms, stretchingConstraints, adjacency, out map, out connectionType, out phiValues);

        for (int i = 0; i < transforms.Length; i++)
        {
            var point = transforms[i];
            var pointConnections = adjacency[point].Select(x => Array.IndexOf(transforms, x))
                .ToArray();

            var pointDistances = adjacency[point]
                .Select(x => Vector3.Distance(point.position, x.position)).ToArray();

            if (pointConnections.Length > STRETCHING_CAPACITY)
            {
                Debug.LogError($"there is point with more than {STRETCHING_CAPACITY} connections");
            }

            for (int j = 0; j < STRETCHING_CAPACITY; j++)
            {
                connectedPoints[i * STRETCHING_CAPACITY + j] = j < pointConnections.Length ? pointConnections[j] : -1;
                distances[i * STRETCHING_CAPACITY + j] = j < pointDistances.Length ? pointDistances[j] : -1;
            }

            for (int j = 0; j < BENDING_CAPACITY; j++)
            {
                int count = phiValues[point].Count;
                if (j < count)
                {
                    connectedTrianglePoints[(i * BENDING_CAPACITY + j) * 3 + 0] = map[point][j * 3 + 0];
                    connectedTrianglePoints[(i * BENDING_CAPACITY + j) * 3 + 1] = map[point][j * 3 + 1];
                    connectedTrianglePoints[(i * BENDING_CAPACITY + j) * 3 + 2] = map[point][j * 3 + 2];
                    positionsInBendingSystem[i * BENDING_CAPACITY + j] = connectionType[point][j];
                    phi[i * BENDING_CAPACITY + j] = phiValues[point][j];
                }
                else
                {
                    connectedTrianglePoints[(i * BENDING_CAPACITY + j) * 3 + 0] = -1;
                    connectedTrianglePoints[(i * BENDING_CAPACITY + j) * 3 + 1] = -1;
                    connectedTrianglePoints[(i * BENDING_CAPACITY + j) * 3 + 2] = -1;
                }
            }
        }

        boneDistances = new NativeArray<float>(transforms.Length * bones.Length, Allocator.Persistent);
        for (int i = 0; i < transforms.Length; i++)
        {
            for (int j = 0; j < bones.Length; j++)
            {
                boneDistances[i * bones.Length + j] = Vector3.Distance(transforms[i].position, bones[j].position);
            }
        }


        accessArray = new TransformAccessArray(transforms);
    }

    void Update()
    {
        var updateVelocityJob = new UpdateVelocityJob()
        {
            DeltaTime = Time.deltaTime,
            points = points,
            UseGravity = useGravity,
            VelocityDamping = damping
        };
        var velocityDependency = updateVelocityJob.Schedule(points.Length, 32);
        NativeArray<Vector3> tempBonesPositions =
            new NativeArray<Vector3>(bones.Select(x => x.transform.position).ToArray(), Allocator.TempJob);
        NativeArray<JobHandle> stretchingDependencies = new NativeArray<JobHandle>(solverSteps, Allocator.Temp);
        NativeArray<JobHandle> updateTemporaryArrayDependencies =
            new NativeArray<JobHandle>(solverSteps, Allocator.Temp);
        for (int i = 0; i < solverSteps; i++)
        {
            var updateStretchingJob = new UpdateStretchingJob()
            {
                stretchingStiffness = stretching,
                collisionStretchingStiffness = collisionStretching,
                stretchingCapacity = STRETCHING_CAPACITY,
                bendingCapacity = BENDING_CAPACITY,
                bendingStiffness = bending,
                pointRadius = pointRadius,
                distances = distances,
                points = points,
                tempPoints = tempPoints,
                connectedPoints = connectedPoints,
                boneDistances = boneDistances,
                bonePoints = tempBonesPositions,
                boneStiffness = boneStiffness,
                connectedTrianglePoints = connectedTrianglePoints,
                positionsInBendingSystem = positionsInBendingSystem,
                phiValues = phi
            };
            stretchingDependencies[i] =
                updateStretchingJob.Schedule(points.Length, 32,
                    i > 0 ? updateTemporaryArrayDependencies[i - 1] : velocityDependency);
            var updateTemporaryArrayJob = new UpdateTemporaryArrayJob()
            {
                points = points,
                tempPoints = tempPoints
            };
            updateTemporaryArrayDependencies[i] =
                updateTemporaryArrayJob.Schedule(points.Length, 32, stretchingDependencies[i]);
        }

        var sphereCastCommands = new NativeArray<SpherecastCommand>(points.Length, Allocator.TempJob);
        var sphereCastHits = new NativeArray<RaycastHit>(points.Length, Allocator.TempJob);

        var setupRaycastsJob = new PrepareSpherecastCommands()
        {
            points = points,
            Radius = pointRadius,
            Spherecasts = sphereCastCommands,
        };
        var setupDependency = setupRaycastsJob.Schedule(points.Length, 32,
            updateTemporaryArrayDependencies[solverSteps - 1]);
//            velocityDependency);

        var sphereCastDependency = SpherecastCommand.ScheduleBatch(
            sphereCastCommands, sphereCastHits,
            32,
            setupDependency);

        var integrateJob = new IntegrateCollision()
        {
            points = points,
            Hits = sphereCastHits,
            Radius = pointRadius
        };

        var integrateDependency = integrateJob.Schedule(points.Length, 32, sphereCastDependency);

        var updatePositionJob = new UpdatePositionJob()
        {
            DeltaTime = Time.deltaTime,
            points = points,
        };

        var positionDependency = updatePositionJob.Schedule(accessArray, integrateDependency);
        positionDependency.Complete();


        stretchingDependencies.Dispose();
        sphereCastCommands.Dispose();
        sphereCastHits.Dispose();
        tempBonesPositions.Dispose();
    }

    private void OnDisable()
    {
        points.Dispose();
        tempPoints.Dispose();
        connectedPoints.Dispose();
        accessArray.Dispose();
    }

    #region BENDING

    private void PrepareBendingMap(
        Transform[] transforms,
        List<PBDConnectionTest> stretchingConstraints,
        Dictionary<Transform, List<Transform>> adjacency,
        out Dictionary<Transform, List<int>> map,
        out Dictionary<Transform, List<bool>> connectionType,
        out Dictionary<Transform, List<float>> phiValues)
    {
        map = new Dictionary<Transform, List<int>>();
        phiValues = new Dictionary<Transform, List<float>>();
        connectionType = new Dictionary<Transform, List<bool>>();

        foreach (var t in transforms)
        {
            map.Add(t, new List<int>());
            phiValues.Add(t, new List<float>());
            connectionType.Add(t, new List<bool>());
        }

        var indices = transforms.ToDictionary(x => x, x => Array.IndexOf(transforms, x));
        List<HashSet<Transform>> triangles = new List<HashSet<Transform>>();


        foreach (PBDConnectionTest constraint in stretchingConstraints)
        {
            foreach (var thirdPoint in adjacency[constraint.transform].Intersect(adjacency[constraint.connectedPoint]))
            {
                HashSet<Transform> candidate = new HashSet<Transform>()
                    {constraint.transform, constraint.connectedPoint, thirdPoint};
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
                    var a = intersection[0];
                    var b = intersection[1];
                    var c = triangles[i].Except(intersection).First();
                    var d = triangles[j].Except(intersection).First();
                    var phiValue = Mathf.Acos(Vector3.Dot(
                        Vector3.Cross(b.position - a.position, c.position - a.position).normalized,
                        Vector3.Cross(b.position - a.position, d.position - a.position).normalized
                    ));
                    map[a].AddRange(new[] {indices[b], indices[c], indices[d]});
                    map[b].AddRange(new[] {indices[a], indices[d], indices[c]});
                    map[c].AddRange(new[] {indices[d], indices[a], indices[b]});
                    map[d].AddRange(new[] {indices[c], indices[a], indices[b]});
                    phiValues[a].Add(phiValue);
                    phiValues[b].Add(phiValue);
                    phiValues[c].Add(phiValue);
                    phiValues[d].Add(phiValue);
                    connectionType[a].Add(true);
                    connectionType[b].Add(true);
                    connectionType[c].Add(false);
                    connectionType[d].Add(false);
                }
            }
        }
    }

    private void OnDrawGizmos()
    {
        if (transforms == null) return;
        Gizmos.color = new Color(1, 0, 0, 0.3f);
        foreach (Transform t in transforms)
        {
            Gizmos.DrawWireSphere(t.position, pointRadius);
        }
    }

    #endregion
}