using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.Jobs;
using UnityEngine.Rendering;
using Random = UnityEngine.Random;

namespace PBD
{
    public class PBDObject : IDisposable
    {
        // General settings
        private const int
            POINTS_AMOUNT = 1024; // we allocate all points data once and use special arrays for existence check

//        private const int BONES_AMOUNT = 10; // we allocate all points data once and use special arrays for existence check
        private const int CONNECTION_AMOUNT = 32; // how many connections may have each point
        private const int SOLVER_STEPS = 3;

        // Object settings
        private readonly float _pointCollisionStiffness; // when 2 points collide
        private readonly float _boneStiffness;
        private readonly float _velocityDamping;
        private readonly bool _useGravity;


        // Object data
        private TransformAccessArray _transformAccessArray; // ref to actual transforms. count = POINTS_AMOUNT
        private NativeArray<PBDPointInfo> _pointsData; // points data used for calculations. count = POINTS_AMOUNT
        private NativeArray<Vector3> _position; // current position of each point. count = POINTS_AMOUNT
        private NativeArray<Vector3> _velocity; // current velocity of each point. count = POINTS_AMOUNT
        private NativeArray<Vector3> _tempPosition; // temporary point position cause we can't update position immediately. count = POINTS_AMOUNT

        // stretching data
        private NativeArray<PBDConnectionInfo> _connectionData; // data containing connection information between points. count = POINTS_AMOUNT * STRETCHING_CAPACITY (2d array repr.)

        // compute shader data
        private readonly ComputeShader _shader;
        private readonly int _shaderKernel;
        private readonly ComputeBuffer _pointsDataBuffer;
        private readonly ComputeBuffer _positionBuffer;
        private readonly ComputeBuffer _velocityBuffer;
        private readonly ComputeBuffer _tempPositionBuffer;
        private readonly ComputeBuffer _connectionDataBuffer;
        private readonly ComputeBuffer _bonesDataBuffer;

        // spherecast batching data
        private NativeArray<SpherecastCommand> _sphereCastCommands; // for batching spherecast
        private NativeArray<RaycastHit> _sphereCastHits; // for results

        // connection to bones data
        private readonly Transform _rootBone;
        private readonly TransformAccessArray _bones;
        private readonly NativeArray<PBDBoneInfo> _bonesData;

        public PBDObject(
            float pointCollisionStiffness,
            float boneStiffness,
            float velocityDamping,
            bool useGravity,
            Transform rootBone
        )
        {
            _rootBone = rootBone;
            var bonesArray = new Transform[_rootBone.childCount + 1];
            bonesArray[0] = _rootBone;
            for (int i = 0; i < _rootBone.childCount; i++)
            {
                bonesArray[i + 1] = _rootBone.GetChild(i);
            }

            _bones = new TransformAccessArray(bonesArray);
            _bonesData = new NativeArray<PBDBoneInfo>(_rootBone.childCount + 1, Allocator.Persistent);
            _bonesData[0] = new PBDBoneInfo() {position = _rootBone.position, parentIndex = -1};
            for (var i = 1; i < bonesArray.Length; i++)
            {
                _bonesData[i] = new PBDBoneInfo() {position = bonesArray[i].position, parentIndex = Array.IndexOf(bonesArray, bonesArray[i].parent)};
                Debug.Log($"bones {i} position {_bonesData[i].position} parent {_bonesData[i].parentIndex}");
            }


            _boneStiffness = boneStiffness;
            _pointCollisionStiffness = pointCollisionStiffness;
            _velocityDamping = velocityDamping;
            _useGravity = useGravity;

            _transformAccessArray = new TransformAccessArray(new Transform[POINTS_AMOUNT]);
            _pointsData = new NativeArray<PBDPointInfo>(POINTS_AMOUNT, Allocator.Persistent);
            _position = new NativeArray<Vector3>(POINTS_AMOUNT, Allocator.Persistent);
            _velocity = new NativeArray<Vector3>(POINTS_AMOUNT, Allocator.Persistent);
            _tempPosition = new NativeArray<Vector3>(POINTS_AMOUNT, Allocator.Persistent);

            _connectionData =
                new NativeArray<PBDConnectionInfo>(POINTS_AMOUNT * CONNECTION_AMOUNT,
                    Allocator.Persistent); // TODO make -1

            _sphereCastCommands = new NativeArray<SpherecastCommand>(POINTS_AMOUNT, Allocator.Persistent);
            _sphereCastHits = new NativeArray<RaycastHit>(POINTS_AMOUNT, Allocator.Persistent);

            // _bones = new TransformAccessArray(BONES_AMOUNT);
            // _bonesDistances = new NativeArray<float>(POINTS_AMOUNT * BONES_AMOUNT, Allocator.Persistent);
            // _bonesExistence = new NativeArray<bool>(BONES_AMOUNT, Allocator.Persistent);

            for (int i = 0; i < POINTS_AMOUNT; i++)
            {
                _transformAccessArray[i] = null;
                for (int j = 0; j < CONNECTION_AMOUNT; j++)
                {
                    var c = _connectionData[i * CONNECTION_AMOUNT + j];
                    c.pointIndex = -1;
                    _connectionData[i * CONNECTION_AMOUNT + j] = c;
                }
            }

            _shader = (ComputeShader) Resources.Load("PBDShader");
            if (_shader == null) throw new Exception($"Shader is not in Resource Folder");
            _shaderKernel = _shader.FindKernel("PBD");
            _pointsDataBuffer = new ComputeBuffer(POINTS_AMOUNT, Marshal.SizeOf<PBDPointInfo>());
            _positionBuffer = new ComputeBuffer(POINTS_AMOUNT, Marshal.SizeOf<Vector3>());
            _velocityBuffer = new ComputeBuffer(POINTS_AMOUNT, Marshal.SizeOf<Vector3>());
            _tempPositionBuffer = new ComputeBuffer(POINTS_AMOUNT, Marshal.SizeOf<Vector3>());
            _connectionDataBuffer = new ComputeBuffer(POINTS_AMOUNT * CONNECTION_AMOUNT, Marshal.SizeOf<PBDConnectionInfo>());
            _bonesDataBuffer = new ComputeBuffer(_rootBone.childCount + 1, Marshal.SizeOf<PBDBoneInfo>());

            _shader.SetBuffer(_shaderKernel, "_pointsDataBuffer", _pointsDataBuffer);
            _shader.SetBuffer(_shaderKernel, "_positionBuffer", _positionBuffer);
            _shader.SetBuffer(_shaderKernel, "_velocityBuffer", _velocityBuffer);
            _shader.SetBuffer(_shaderKernel, "_tempPositionBuffer", _tempPositionBuffer);
            _shader.SetBuffer(_shaderKernel, "_connectionDataBuffer", _connectionDataBuffer);
            _shader.SetBuffer(_shaderKernel, "_bonesDataBuffer", _bonesDataBuffer);
        }

        public void SetSettings()
        {
            _shader.SetFloat("_velocityDamping", _velocityDamping);
            _shader.SetBool("_useGravity", _useGravity);
            _shader.SetInt("_connectionAmount", CONNECTION_AMOUNT);
            _shader.SetInt("_solverSteps", SOLVER_STEPS);
            _shader.SetFloat("_collisionStiffness", _pointCollisionStiffness);
            _shader.SetFloat("_boneStiffness", _boneStiffness);
            _shader.SetInt("_bonesAmount", _bones.length);
        }

        public void SetNewProperty(Type type, string name, object value) // не судите строго
        {
            if (type == typeof(float))
            {
                _shader.SetFloat(name, (float) value);
            }

            if (type == typeof(int))
            {
                _shader.SetInt(name, (int) value);
            }

            if (type == typeof(bool))
            {
                _shader.SetBool(name, (bool) value);
            }
        }

//        public Vector3 this[int index] => _points[index].p;

//        public void AddUnit(Transform hips, Transform neck, out int hipsIndex, out int neckIndex)
//        {
//            int index = FindUnusedPointIndex();
//            if (index == -1) throw new Exception("Out of allocated memory for object, need to reallocate");
//            hipsIndex = index;
//            neckIndex = index + 1;
//            Debug.Log($"new points {hipsIndex},{neckIndex}");
//
//            Vector3 middlePoint = (hips.position + neck.position) / 2f;
//
//            int[] nearestPoints = FindIndicesOfNNearestPoints(Mathf.Min(3, _untiCount), middlePoint);
//
//            foreach (int i in nearestPoints)
//            {
//                Debug.Log(i);
//            }
//
//            if (_untiCount / 2 == 1)
//            {
//                Vector3 a = _points[nearestPoints[0]].p;
//                Vector3 b = _points[nearestPoints[1]].p;
//                Vector3 c = middlePoint;
//
//                Vector3 project = Vector3.Project((c - a), (b - a));
//                Vector3 perpendicular = (c - a) - project;
//                Vector3 centerOfBody = (a + b) / 2f + _pointRadius * 2 * perpendicular.normalized;
//                Vector3 hipsPosition = centerOfBody + Quaternion.AngleAxis(Random.Range(-180f, 180), perpendicular) * ((b - a).normalized * (hips.position - neck.position).magnitude / 2f);
//
//                hips.position = hipsPosition;
//                hips.rotation = PBDUtils.LookAtXAxis((centerOfBody - hipsPosition), Vector3.up);
//            }
//
//            if (_untiCount / 2 >= 2)
//            {
//                Vector3 a = _points[nearestPoints[0]].p;
//                Vector3 b = _points[nearestPoints[1]].p;
//                Vector3 c = _points[nearestPoints[2]].p;
//                Vector3 d = middlePoint;
//                // TODO ОТТЕСТИРОВАТЬ !!! нихуя не понял но очень интересно
//                Vector3 perpendicular = Vector3.Cross((c - a), (b - a));
//                Vector3 project = Vector3.ProjectOnPlane((d - a), perpendicular.normalized);
//                Vector3 centerOfBody = (a + b + c) / 3f + _pointRadius * 2 * perpendicular.normalized;
//                Vector3 hipsPosition = centerOfBody + Quaternion.AngleAxis(Random.Range(-180f, 180), perpendicular) * ((b - a).normalized * (hips.position - neck.position).magnitude / 2f);
//
//                Debug.DrawLine(a, b, Color.red, 10);
//                Debug.DrawLine(a, c, Color.red, 10);
//                Debug.DrawLine(b, c, Color.red, 10);
//                Debug.DrawLine(centerOfBody, hipsPosition, Color.green, 10);
//
//                hips.position = hipsPosition;
//                hips.rotation = PBDUtils.LookAtXAxis((centerOfBody - hipsPosition), Vector3.up);
//            }
//
//            AddPoint(hips, hipsIndex);
//            AddPoint(neck, neckIndex);
//            Debug.Log($"add connection {hipsIndex} to {neckIndex}");
//
//            AddConnection(hipsIndex, neckIndex, _bodyStretchingStiffness);
//            foreach (int i in nearestPoints)
//            {
//                Debug.Log($"add connection {i} to {hipsIndex},{neckIndex}");
//
//                AddConnection(i, hipsIndex, _stretchingStiffness, 0.5f);
//                AddConnection(i, neckIndex, _stretchingStiffness, 0.5f);
//            }
//
//
//            _untiCount += 2;
//        }

        public void AddPoint(Transform t, float radius, float mass, out int index)
        {
            index = FindUnusedPointIndex();
            if (index == -1) throw new Exception($"Out of allocated memory for object ({POINTS_AMOUNT} elements), need to reallocate");

            _transformAccessArray[index] = t;
            _pointsData[index] = new PBDPointInfo()
            {
                mass = mass,
                radius = radius,
                collided = false,
                valid = true
            };
            _position[index] = t.position;
            _tempPosition[index] = _position[index];
            _velocity[index] = Vector3.zero;

            // TODO make this in batch
            _pointsDataBuffer.SetData(_pointsData, index, index, 1);
            _positionBuffer.SetData(_position, index, index, 1);
            _velocityBuffer.SetData(_velocity, index, index, 1);
            _tempPositionBuffer.SetData(_tempPosition, index, index, 1);
        }

        public void AddConnection(int index1, int index2, float stretchingStiffness, float? overrideDistance = null)
        {
            float distance = overrideDistance ?? Vector3.Distance(_position[index1], _position[index2]);
            int unused1 = FindUnusedConnectionIndex(index1);
            int unused2 = FindUnusedConnectionIndex(index2);
            // we compute stretching for every point separately in parallel to avoid locks
            // so we use twice as much memory to achieve twice as much speed
            _connectionData[unused1] = new PBDConnectionInfo()
            {
                distance = distance,
                pointIndex = index2,
                stiffness = stretchingStiffness
            };

            _connectionData[unused2] = new PBDConnectionInfo()
            {
                distance = distance,
                pointIndex = index1,
                stiffness = stretchingStiffness
            };

            _connectionDataBuffer.SetData(_connectionData, unused1, unused1, 1);
            _connectionDataBuffer.SetData(_connectionData, unused2, unused2, 1);
        }

//        public void RemoveUnit(int hipsIndex, int neckIndex)
//        {
//            //_untiCount -= 2;
//            // TODO держать список из только что удаленных элементов и при поиске индекса незанятого элемента
//            // сначала проходиться по нему. То же самое сделать и для connections
//        }
//
//        private void RemovePoint(int index)
//        {
//        }
//
//        private void RemoveConnection(int index)
//        {
//        }
//
//        private void AddBone(Transform bone)
//        {
//        }


        private JobHandle _lastHandle;
        private float _prevDeltaTime;
        private readonly Vector3[] _data = new Vector3[POINTS_AMOUNT];


        public void OnUpdate(float deltaTime)
        {
            _shader.SetFloat("_deltaTime", deltaTime);
            _shader.SetFloat("_prevDeltaTime", _prevDeltaTime);

            var prepareBonesJob = new PrepareBonesPositionCommands()
            {
                boneInfo = _bonesData
            };
            var prepareBonesDependency = prepareBonesJob.Schedule(_bones);
            prepareBonesDependency.Complete();
            
//            for (var i = 0; i < _bonesData.Length; i++)
//            {
//                Debug.Log($"bones {i} position {_bonesData[i].position} parent {_bonesData[i].parentIndex}");
//            }
            
            
            _bonesDataBuffer.SetData(_bonesData);

            _shader.Dispatch(_shaderKernel, 1, 1, 1);
            _prevDeltaTime = deltaTime;


            _tempPositionBuffer.GetData(_data); // TODO waiting for better api without allocation
            _tempPosition.CopyFrom(_data);

            _positionBuffer.GetData(_data);
            _position.CopyFrom(_data);

            var sphereCastCommands = new NativeArray<SpherecastCommand>(_pointsData.Length, Allocator.TempJob);
            var sphereCastHits = new NativeArray<RaycastHit>(_pointsData.Length, Allocator.TempJob);

            var setupSpherecastCommands = new PrepareSpherecastCommands()
            {
                Spherecasts = sphereCastCommands,
                pointsData = _pointsData,
                oldPositions = _position,
                newPositions = _tempPosition
            };
            var setupDependency = setupSpherecastCommands.Schedule(_pointsData.Length, 32, _lastHandle);
            _lastHandle = setupDependency;

            var sphereCastDependency = SpherecastCommand.ScheduleBatch(
                sphereCastCommands, sphereCastHits,
                32,
                _lastHandle);
            _lastHandle = sphereCastDependency;

            var integrateJob = new IntegrateCollision()
            {
                pointsData = _pointsData,
                Hits = sphereCastHits,
                oldPositions = _position,
                newPositions = _tempPosition
            };

            var integrateDependency = integrateJob.Schedule(_pointsData.Length, 32, _lastHandle);
            _lastHandle = integrateDependency;

            var updatePositionJob = new UpdatePositionJob()
            {
                pointInfo = _pointsData,
                tempPosition = _tempPosition
            };

            var positionDependency = updatePositionJob.Schedule(_transformAccessArray, _lastHandle);
            _lastHandle = positionDependency;

            _lastHandle.Complete();

            _positionBuffer.SetData(_position);
            _tempPositionBuffer.SetData(_tempPosition);

            sphereCastCommands.Dispose();
            sphereCastHits.Dispose();

//            var updateVelocityJob = new UpdateVelocityJob()
//            {
//                DeltaTime = Time.deltaTime,
//                points = _points,
//                UseGravity = _useGravity,
//                VelocityDamping = _damping
//            };
//            var velocityDependency = updateVelocityJob.Schedule(_points.Length, 32);
//            _lastHandle = velocityDependency;

//            NativeArray<JobHandle> stretchingDependencies = new NativeArray<JobHandle>(SOLVER_STEPS, Allocator.Temp);
//            NativeArray<JobHandle> updateTemporaryArrayDependencies =
//                new NativeArray<JobHandle>(SOLVER_STEPS, Allocator.Temp);
//            for (int i = 0; i < SOLVER_STEPS; i++)
//            {
//                var updateStretchingJob = new UpdateStretchingJob()
//                {
//                    pointRadius = _pointRadius,
//                    connectionAmount = CONNECTION_AMOUNT,
//                    collisionStretchingStiffness = _pointCollisionStiffness,
//                    points = _points,
//                    connectionStiffness = _connectionStiffness,
//                    connectionDistances = _connectionDistances,
//                    tempPoints = _tempPoints,
//                    connectedPoints = _connectedPoints,
//                };
//                stretchingDependencies[i] =
//                    updateStretchingJob.Schedule(_points.Length, 32, _lastHandle);
//                _lastHandle = stretchingDependencies[i];
//                
//                var updateTemporaryArrayJob = new UpdateTemporaryArrayJob()
//                {
//                    points = _points,
//                    tempPoints = _tempPoints
//                };
//                updateTemporaryArrayDependencies[i] =
//                    updateTemporaryArrayJob.Schedule(_points.Length, 32, _lastHandle);
//                _lastHandle = updateTemporaryArrayDependencies[i];
//            }
//            
        }

        private int FindUnusedPointIndex()
        {
            for (int i = 0; i < _pointsData.Length; i++)
            {
                if (!_pointsData[i].valid) return i;
            }

            return -1;
        }

        private int FindUnusedConnectionIndex(int index)
        {
            for (int i = index * CONNECTION_AMOUNT; i < (index + 1) * CONNECTION_AMOUNT; i++)
            {
                if (_connectionData[i].pointIndex == -1) return i;
            }

            Debug.Log($"index {index}");
            for (int i = index * CONNECTION_AMOUNT; i < (index + 1) * CONNECTION_AMOUNT; i++)
            {
                Debug.Log($"{_connectionData[i].pointIndex}");
            }

            return -1;
        }

//        private int[] FindIndicesOfNNearestPoints(int n, Vector3 position)
//        {
//            var result = new int[n];
//            for (int i = 0; i < n; i++)
//            {
//                result[i] = i;
//            }
//
//            for (int i = n; i < _pointsData.Length; i++)
//            {
//                if (!_pointsData[i].valid) continue;
//                float d = Vector3.Distance(_points[i].p, position);
//
//                float maxDistance = float.MinValue;
//                int maxIndex = -1;
//                for (int j = 0; j < n; j++)
//                {
//                    float dist = Vector3.Distance(_points[result[j]].p, position);
//                    if (dist > maxDistance)
//                    {
//                        maxDistance = dist;
//                        maxIndex = j;
//                    }
//                }
//
//                if (d < maxDistance)
//                {
//                    result[maxIndex] = i;
//                }
//            }
//
//
//            return result;
//        }


        public void Dispose()
        {
            _transformAccessArray.Dispose();
            _bones.Dispose();
            _pointsData.Dispose();
            _position.Dispose();
            _velocity.Dispose();
            _tempPosition.Dispose();
            _connectionData.Dispose();
            // _connectionStiffness.Dispose();
            // _connectionDistances.Dispose();
            // _bones.Dispose();
            // _bonesDistances.Dispose();
            // _bonesExistence.Dispose();
            _sphereCastCommands.Dispose();
            _sphereCastHits.Dispose();


            _pointsDataBuffer.Dispose();
            _positionBuffer.Dispose();
            _velocityBuffer.Dispose();
            _tempPositionBuffer.Dispose();
            _connectionDataBuffer.Dispose();
            _bonesData.Dispose();
        }
    }
}