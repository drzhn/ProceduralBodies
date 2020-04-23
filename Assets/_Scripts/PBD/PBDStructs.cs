using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.Jobs;

namespace PBD
{
    public struct PBDPointInfo
    {
        public bool valid;
        public float mass;
        public float radius;
    }

    struct PBDConnectionInfo
    {
        public int pointIndex;
        public float stiffness;
        public float distance;
    }

    public struct PBDBoneInfo
    {
        public Vector3 position;
        public int parentIndex;
    }

    public struct PBDSkeletonInfo
    {
        public bool valid;
        public int hipsIndex;
        public int neckIndex;
    }


    public struct PrepareBonesPositionCommands : IJobParallelForTransform
    {
        public NativeArray<PBDBoneInfo> boneInfo;

        public void Execute(int index, TransformAccess transform)
        {
            var info = boneInfo[index];
            info.position = transform.position;
            boneInfo[index] = info;
        }
    }

    public struct PrepareSpherecastCommands : IJobParallelFor
    {
        public NativeArray<SpherecastCommand> Spherecasts;
        [ReadOnly] public NativeArray<PBDPointInfo> pointsData;

        [NativeDisableContainerSafetyRestriction] [ReadOnly]
        public NativeArray<Vector3> oldPositions;

        [NativeDisableContainerSafetyRestriction] [ReadOnly]
        public NativeArray<Vector3> newPositions;


        public void Execute(int i)
        {
            if (!pointsData[i].valid) return;

            Spherecasts[i] = new SpherecastCommand(
                oldPositions[i],
                pointsData[i].radius,
                (newPositions[i] - oldPositions[i]).normalized,
                Vector3.Distance(newPositions[i], oldPositions[i]) + 0.003f);
        }
    }

    public struct IntegrateCollision : IJobParallelFor
    {
        [ReadOnly] public NativeArray<RaycastHit> Hits;
        [ReadOnly] public NativeArray<PBDPointInfo> pointsData;

        [NativeDisableContainerSafetyRestriction]
        public NativeArray<Vector3> oldPositions;

        [NativeDisableContainerSafetyRestriction]
        public NativeArray<Vector3> newPositions;

        public void Execute(int i)
        {
            if (!pointsData[i].valid) return;

            if (Hits[i].normal == Vector3.zero) return;

            Vector3 oldPoint = oldPositions[i];
            Vector3 newPoint = newPositions[i];
            Vector3 realCollisionPoint = Hits[i].point + Hits[i].normal * (pointsData[i].radius + 0.003f);
            oldPositions[i] = realCollisionPoint + (oldPoint - newPoint).normalized * (0.003f);
            newPositions[i] = realCollisionPoint +
                              Vector3.ProjectOnPlane(newPoint - realCollisionPoint, Hits[i].normal) +
                              Hits[i].normal * (0.003f);
        }
    }
}

//    public struct UpdateVelocityJob : IJobParallelFor
//    {
//        public float DeltaTime;
//        public bool UseGravity;
//        public float VelocityDamping;
//        public NativeArray<PBDPoint> points;
//
//        public void Execute(int i)
//        {
//            if (!points[i].valid) return;
//
//            var point = points[i];
//            if (UseGravity) point.velocity += point.m * 9 * DeltaTime * Vector3.down;
//            point.velocity *= VelocityDamping;
//            point.p = point.t + DeltaTime * point.velocity;
//            point.collided = false;
//            points[i] = point;
//        }
//    }

//    public struct UpdateStretchingJob : IJobParallelFor
//    {
//        public float pointRadius;
//
//        public int connectionAmount;
//
//        //public int bendingCapacity;
//        public float collisionStretchingStiffness;
//
//        //public float bendingStiffness;
//        public NativeArray<PBDPoint> points;
//        [ReadOnly] public NativeArray<float> connectionStiffness; // count = POINTS_AMOUNT * STRETCHING_CAPACITY
//        [ReadOnly] public NativeArray<float> connectionDistances; // count = POINTS_AMOUNT * STRETCHING_CAPACITY
//        [ReadOnly] public NativeArray<Vector3> tempPoints;
//        [ReadOnly] public NativeArray<int> connectedPoints;
//
//        //[ReadOnly] public NativeArray<int> connectedTrianglePoints;
//        //[ReadOnly] public NativeArray<bool> positionsInBendingSystem;
//        //[ReadOnly] public NativeArray<float> phiValues;
//
//        //[ReadOnly] public NativeArray<Vector3> bonePoints;
//        //[ReadOnly] public NativeArray<float> boneDistances;
//        //public float boneStiffness;
//
//        public void Execute(int index)
//        {
//            if (!points[index].valid) return;
//
//            var point = points[index];
//
//            for (int i = 0; i < connectionAmount; i++)
//            {
//                int connectedPointIndex = connectedPoints[index * connectionAmount + i];
//                if (connectedPointIndex == -1) continue;
//
//                float currentDistance = Vector3.Distance(tempPoints[connectedPointIndex], tempPoints[index]);
//                float initialDistance = connectionDistances[index * connectionAmount + i];
//                if (currentDistance > 2 * pointRadius)
//                {
//                    float stretchingStiffness = connectionStiffness[index * connectionAmount + i];
//                    Vector3 dir = (currentDistance - initialDistance) / currentDistance *
//                                  (tempPoints[index] - tempPoints[connectedPointIndex]);
//                    point.p -= 0.5f * stretchingStiffness * dir;
//                }
//
//                for (int j = 0; j < connectionAmount; j++)
//                {
//                    int nextPointIndex = connectedPoints[connectedPointIndex * connectionAmount + j];
//                    if (nextPointIndex == index || nextPointIndex == -1) continue;
//                    point.p -= CheckSphereCollision(index, nextPointIndex);
//                }
//
//                point.p -= CheckSphereCollision(index, connectedPointIndex);
//                /*(tempPoints[index].m / (tempPoints[index].m + tempPoints[constraintsMap[index][i].connectedPoint].m)) */
//            }
//
////            for (int i = 0; i < bendingCapacity; i++)
////            {
////                if (connectedTrianglePoints[(index * bendingCapacity + i) * 3 + 0] == -1) continue;
////
////                bool positionInBendingSystem = positionsInBendingSystem[index * bendingCapacity + i];
////                float phi = phiValues[index * bendingCapacity + i];
////                Vector3 p1;
////                Vector3 p2;
////                Vector3 p3;
////                Vector3 p4;
////
////                if (positionInBendingSystem)
////                {
////                    p1 = tempPoints[index];
////                    p2 = tempPoints[connectedTrianglePoints[(index * bendingCapacity + i) * 3 + 0]];
////                    p3 = tempPoints[connectedTrianglePoints[(index * bendingCapacity + i) * 3 + 1]];
////                    p4 = tempPoints[connectedTrianglePoints[(index * bendingCapacity + i) * 3 + 2]];
////                }
////                else
////                {
////                    p1 = tempPoints[connectedTrianglePoints[(index * bendingCapacity + i) * 3 + 1]];
////                    p2 = tempPoints[connectedTrianglePoints[(index * bendingCapacity + i) * 3 + 2]];
////                    p3 = tempPoints[index];
////                    p4 = tempPoints[connectedTrianglePoints[(index * bendingCapacity + i) * 3 + 0]];
////                }
////
////                Vector3 n1 = Vector3.Cross((p2 - p1), (p3 - p1)).normalized;
////                Vector3 n2 = Vector3.Cross((p2 - p1), (p4 - p1)).normalized;
////
////                float d = Vector3.Dot(n1, n2);
////
////                Vector3 q3 = (Vector3.Cross((p2 - p1), n2) + Vector3.Cross(n1, (p2 - p1)) * d) / Vector3.Cross((p2 - p1), (p3 - p1)).magnitude;
////                Vector3 q4 = (Vector3.Cross((p2 - p1), n1) + Vector3.Cross(n2, (p2 - p1)) * d) / Vector3.Cross((p2 - p1), (p4 - p1)).magnitude;
////                Vector3 q2 = -(Vector3.Cross((p3 - p1), n2) + Vector3.Cross(n1, (p3 - p1)) * d) / Vector3.Cross((p2 - p1), (p3 - p1)).magnitude -
////                             (Vector3.Cross((p4 - p1), n1) + Vector3.Cross(n2, (p4 - p1)) * d) / Vector3.Cross((p2 - p1), (p4 - p1)).magnitude;
////                Vector3 q1 = -q2 - q3 - q4;
////
////                float sumQ = (q1.sqrMagnitude * 1 + q2.sqrMagnitude * 1 + q3.sqrMagnitude * 1 + q4.sqrMagnitude * 1);
////                float s = -(1 * Mathf.Sqrt(1 - d * d) * (Mathf.Acos(d) - phi)) / sumQ;
////                if (float.IsNaN(sumQ) || float.IsNaN(s)) continue;
////
////                if (positionInBendingSystem)
////                {
////                    point.p += s * bendingStiffness * q1;
////                }
////                else
////                {
////                    point.p += s * bendingStiffness * q3;
////                }
////            }
////              for (int i = 0; i < bonePoints.Length; i++)
////              {
////                  float currentDistance = Vector3.Distance(bonePoints[i], tempPoints[index]);
////                  Vector3 dir = (currentDistance - boneDistances[index * bonePoints.Length + i]) / currentDistance *
////                                (tempPoints[index] - bonePoints[i]);
////                  point.p -= boneStiffness * dir;
////              }
//
//            points[index] = point;
//        }
//
//        private Vector3 CheckSphereCollision(int index, int collisionIndex)
//        {
//            float currentDistance = Vector3.Distance(tempPoints[collisionIndex], tempPoints[index]);
//            if (currentDistance < 2 * pointRadius)
//            {
//                Vector3 dir = 0.5f * (currentDistance - 2 * pointRadius) / currentDistance *
//                              (tempPoints[index] - tempPoints[collisionIndex]);
//                return 0.5f * collisionStretchingStiffness * dir;
//            }
//
//            return Vector3.zero;
//        }
//    }
//
//     public struct UpdateTemporaryArrayJob : IJobParallelFor
//     {
//         [ReadOnly] public NativeArray<PBDPointInfo> points;
//         public NativeArray<Vector3> tempPoints;
//
//         public void Execute(int i)
//         {
//             if (!points[i].valid) return;
//
// //            tempPoints[i] = points[i].p;
//         }
// //     }
//
//     public struct UpdatePositionJob : IJobParallelForTransform
//     {
//         [ReadOnly] public NativeArray<PBDPointInfo> pointInfo;
//         [ReadOnly] public NativeArray<Vector3> tempPosition;
//
//         public void Execute(int index, TransformAccess transform)
//         {
//             if (!pointInfo[index].valid) return;
//             transform.position = tempPosition[index];
//         }
//     }