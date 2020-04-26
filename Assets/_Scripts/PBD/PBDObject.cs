using System;
using System.Runtime.InteropServices;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.Jobs;
using UnityEngine.Rendering;

namespace PBD
{
    public class PBDObject : IDisposable
    {
        public Vector3 this[int index] => _position[index];

        // General settings
        private const int POINTS_AMOUNT = 1024; // we allocate all points data once and use special arrays for existence check
        private const int SKELETON_BONES_AMOUNT = 100;

        private const int CONNECTION_AMOUNT = 32; // how many connections may have each point
        private const int SOLVER_STEPS = 3;
        private readonly Bounds _bounds = new Bounds(Vector3.zero, Vector3.one * 100);

        // Object settings
        private readonly float _pointCollisionStiffness; // when 2 points collide
        private readonly float _boneStiffness;
        private readonly float _velocityDamping;
        private readonly bool _useGravity;


        // Object data
        private NativeArray<PBDPointInfo> _pointsData; // points data used for calculations. count = POINTS_AMOUNT
        private NativeArray<Vector3> _position; // current position of each point. count = POINTS_AMOUNT
        private NativeArray<Vector3> _velocity; // current velocity of each point. count = POINTS_AMOUNT
        private NativeArray<Vector3> _tempPosition; // temporary point position cause we can't update position immediately. count = POINTS_AMOUNT

        // stretching data
        private NativeArray<PBDConnectionInfo> _connectionData; // data containing connection information between points. count = POINTS_AMOUNT * STRETCHING_CAPACITY (2d array repr.)

        // skeleton data
        private NativeArray<PBDSkeletonInfo> _skeletonInfo;

        // compute shader data
        private readonly ComputeShader _pbdShader;
        private readonly ComputeShader _skeletonShader;
        private readonly int _pbdKernel;
        private readonly int _skeletonKernel;
        private readonly ComputeBuffer _pointsDataBuffer;
        private readonly ComputeBuffer _positionBuffer;
        private readonly ComputeBuffer _velocityBuffer;
        private readonly ComputeBuffer _tempPositionBuffer;
        private readonly ComputeBuffer _connectionDataBuffer;
        private readonly ComputeBuffer _bonesDataBuffer;
        private readonly ComputeBuffer _skeletonDataBuffer;
        private readonly ComputeBuffer _boneMatricesBuffer;
        private readonly ComputeBuffer _skeletonBoneTransformBuffer;
        private readonly ComputeBuffer _weightsBuffer;
        private readonly ComputeBuffer _indicesBuffer;
        private readonly ComputeBuffer _bindPosesBuffer;
        private readonly ComputeBuffer _argsBuffer;


        // spherecast batching data
        private NativeArray<SpherecastCommand> _sphereCastCommands; // for batching spherecast
        private NativeArray<RaycastHit> _sphereCastHits; // for results

        // connection to bones data
        private readonly Transform _rootBone;
        private TransformAccessArray _bones;
        private NativeArray<PBDBoneInfo> _bonesData;

        // Instanced drawing data
        private PBDModelData _modelData;
        private Material _instancedMaterial;

        public PBDObject(
            float pointCollisionStiffness,
            float boneStiffness,
            float velocityDamping,
            bool useGravity,
            Transform rootBone,
            PBDModelData modelData,
            Material instancedMaterial
        )
        {
            // Instanced material initialization

            _modelData = modelData;
            _instancedMaterial = instancedMaterial;
            _modelData.GetVerticesData(out var vertexWeights, out var vertexBoneIndices);
            _weightsBuffer = new ComputeBuffer(vertexWeights.Length, sizeof(float));
            _weightsBuffer.SetData(vertexWeights);

            _indicesBuffer = new ComputeBuffer(vertexBoneIndices.Length, sizeof(int));
            _indicesBuffer.SetData(vertexBoneIndices);

            _boneMatricesBuffer = new ComputeBuffer(POINTS_AMOUNT / 2 * SKELETON_BONES_AMOUNT, Marshal.SizeOf<Matrix4x4>());
            _skeletonBoneTransformBuffer = new ComputeBuffer(POINTS_AMOUNT / 2 * SKELETON_BONES_AMOUNT, Marshal.SizeOf<PBDSkeletonBoneTransform>());

            for (int i = 0; i < POINTS_AMOUNT / 2; i++)
            {
                _skeletonBoneTransformBuffer.SetData(
                    _modelData.SkeletonBoneTransforms,
                    0,
                    i * SKELETON_BONES_AMOUNT,
                    _modelData.SkeletonBoneTransforms.Length);
            }

            _bindPosesBuffer = new ComputeBuffer(SKELETON_BONES_AMOUNT, Marshal.SizeOf<Matrix4x4>());
            _bindPosesBuffer.SetData(_modelData.BindPoses, 0, 0, _modelData.BindPoses.Length);

            _instancedMaterial.SetBuffer("_VertexWeights", _weightsBuffer);
            _instancedMaterial.SetBuffer("_VertexBoneIndices", _indicesBuffer);
            _instancedMaterial.SetBuffer("_BonesMatrices", _boneMatricesBuffer);

            int[] args = new int[5] {0, 0, 0, 0, 0};
            _argsBuffer = new ComputeBuffer(1, args.Length * sizeof(uint), ComputeBufferType.IndirectArguments);
            args[0] = (int) _modelData.Mesh.GetIndexCount(0);
            args[1] = POINTS_AMOUNT / 2;
            args[2] = (int) _modelData.Mesh.GetIndexStart(0);
            args[3] = (int) _modelData.Mesh.GetBaseVertex(0);

            _argsBuffer.SetData(args);

            // PBD body bone initialization

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

            // PBD initialization

            _boneStiffness = boneStiffness;
            _pointCollisionStiffness = pointCollisionStiffness;
            _velocityDamping = velocityDamping;
            _useGravity = useGravity;

            _pointsData = new NativeArray<PBDPointInfo>(POINTS_AMOUNT, Allocator.Persistent);
            _position = new NativeArray<Vector3>(POINTS_AMOUNT, Allocator.Persistent);
            _velocity = new NativeArray<Vector3>(POINTS_AMOUNT, Allocator.Persistent);
            _tempPosition = new NativeArray<Vector3>(POINTS_AMOUNT, Allocator.Persistent);

            _connectionData = new NativeArray<PBDConnectionInfo>(POINTS_AMOUNT * CONNECTION_AMOUNT, Allocator.Persistent); // TODO make -1

            _sphereCastCommands = new NativeArray<SpherecastCommand>(POINTS_AMOUNT, Allocator.Persistent);
            _sphereCastHits = new NativeArray<RaycastHit>(POINTS_AMOUNT, Allocator.Persistent);

            _skeletonInfo = new NativeArray<PBDSkeletonInfo>(POINTS_AMOUNT / 2, Allocator.Persistent);


            for (int i = 0; i < POINTS_AMOUNT; i++)
            {
                _pointsData[i] = new PBDPointInfo()
                {
                    valid = false,
                    mass = 0,
                    radius = 0
                };
                for (int j = 0; j < CONNECTION_AMOUNT; j++)
                {
                    var c = _connectionData[i * CONNECTION_AMOUNT + j];
                    c.pointIndex = -1;
                    _connectionData[i * CONNECTION_AMOUNT + j] = c;
                }

                if (i % 2 == 0)
                {
                    _skeletonInfo[i / 2] = new PBDSkeletonInfo()
                    {
                        valid = false,
                        hipsIndex = -1,
                        neckIndex = -1,
                    };
                }
            }

            _pbdShader = (ComputeShader) Resources.Load("PBDShader");
            if (_pbdShader == null) throw new Exception($"Shader is not in Resource Folder");
            _pbdKernel = _pbdShader.FindKernel("PBD");

            _skeletonShader = (ComputeShader) Resources.Load("PBDSkeletonShader");
            if (_skeletonShader == null) throw new Exception($"Skeleton shader is not in Resource Folder");
            _skeletonKernel = _skeletonShader.FindKernel("Skeleton");

            _pointsDataBuffer = new ComputeBuffer(POINTS_AMOUNT, Marshal.SizeOf<PBDPointInfo>());
            _positionBuffer = new ComputeBuffer(POINTS_AMOUNT, Marshal.SizeOf<Vector3>());
            _velocityBuffer = new ComputeBuffer(POINTS_AMOUNT, Marshal.SizeOf<Vector3>());
            _tempPositionBuffer = new ComputeBuffer(POINTS_AMOUNT, Marshal.SizeOf<Vector3>());
            _connectionDataBuffer = new ComputeBuffer(POINTS_AMOUNT * CONNECTION_AMOUNT, Marshal.SizeOf<PBDConnectionInfo>());
            _bonesDataBuffer = new ComputeBuffer(_rootBone.childCount + 1, Marshal.SizeOf<PBDBoneInfo>());
            _skeletonDataBuffer = new ComputeBuffer(POINTS_AMOUNT / 2, Marshal.SizeOf<PBDSkeletonInfo>());

            _pbdShader.SetBuffer(_pbdKernel, "_pointsDataBuffer", _pointsDataBuffer);
            _pbdShader.SetBuffer(_pbdKernel, "_positionBuffer", _positionBuffer);
            _pbdShader.SetBuffer(_pbdKernel, "_velocityBuffer", _velocityBuffer);
            _pbdShader.SetBuffer(_pbdKernel, "_tempPositionBuffer", _tempPositionBuffer);
            _pbdShader.SetBuffer(_pbdKernel, "_connectionDataBuffer", _connectionDataBuffer);
            _pbdShader.SetBuffer(_pbdKernel, "_bonesDataBuffer", _bonesDataBuffer);

            _skeletonShader.SetBuffer(_skeletonKernel, "_positionBuffer", _positionBuffer);
            _skeletonShader.SetBuffer(_skeletonKernel, "_skeletonDataBuffer", _skeletonDataBuffer);
            _skeletonShader.SetBuffer(_skeletonKernel, "_skeletonBoneTransformBuffer", _skeletonBoneTransformBuffer);
            _skeletonShader.SetBuffer(_skeletonKernel, "_boneMatrices", _boneMatricesBuffer);
            _skeletonShader.SetBuffer(_skeletonKernel, "_bindPosesBuffer", _bindPosesBuffer);
            _skeletonShader.SetInt("_bonesAmount", _modelData.BindPoses.Length);
            _skeletonShader.SetInt("_rootBoneIndex", _modelData.RootBoneIndex);


            Shader.SetGlobalBuffer(Shader.PropertyToID("_positionBuffer"), _positionBuffer); // for debug

            _pbdShader.SetFloat("_velocityDamping", _velocityDamping);
            _pbdShader.SetBool("_useGravity", _useGravity);
            _pbdShader.SetInt("_connectionAmount", CONNECTION_AMOUNT);
            _pbdShader.SetInt("_solverSteps", SOLVER_STEPS);
            _pbdShader.SetFloat("_collisionStiffness", _pointCollisionStiffness);
            _pbdShader.SetFloat("_boneStiffness", _boneStiffness);
            _pbdShader.SetInt("_bonesAmount", _bones.length);

            _pointsDataBuffer.SetData(_pointsData);
            _skeletonDataBuffer.SetData(_skeletonInfo);
        }

        public void SetNewProperty(Type type, string name, object value) // не судите строго, если эта штука будет не для дебага, перепишу на норм
        {
            if (type == typeof(float))
            {
                _pbdShader.SetFloat(name, (float) value);
            }

            if (type == typeof(int))
            {
                _pbdShader.SetInt(name, (int) value);
            }

            if (type == typeof(bool))
            {
                var v = (bool) value;
                _pbdShader.SetBool(name, (bool) value);
            }
        }


        public void AddUnit(
            Vector3 hipsPosition,
            float hipsRadius,
            Vector3 neckPosition,
            float neckRadius,
            float stiffness
        )
        {
            AddPoint(hipsPosition, hipsRadius, 1, out var index1);
            AddPoint(neckPosition, neckRadius, 1, out var index2);
            AddConnection(index1, index2, stiffness);
            int skeletonIndex = FindUnusedSkeletonIndex();
            if (skeletonIndex == -1) throw new Exception($"Out of allocated memory for skeleton ({POINTS_AMOUNT / 2} elements), need to reallocate");

            _skeletonInfo[skeletonIndex] = new PBDSkeletonInfo()
            {
                valid = true,
                hipsIndex = index1,
                neckIndex = index2
            };
            _skeletonDataBuffer.SetData(_skeletonInfo, skeletonIndex, skeletonIndex, 1);
        }

        public void AddPoint(Vector3 position, float radius, float mass, out int index)
        {
            index = FindUnusedPointIndex();
            if (index == -1) throw new Exception($"Out of allocated memory for object ({POINTS_AMOUNT} elements), need to reallocate");

            _pointsData[index] = new PBDPointInfo()
            {
                valid = true,
                mass = mass,
                radius = radius,
            };
            _position[index] = position;
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
        private float _prevDeltaTime = 0f;


        public void OnPhysicsUpdate(float deltaTime)
        {
            _pbdShader.SetFloat("_deltaTime", deltaTime);
            _pbdShader.SetFloat("_prevDeltaTime", _prevDeltaTime);
            _prevDeltaTime = deltaTime;

            var prepareBonesJob = new PrepareBonesPositionCommands()
            {
                boneInfo = _bonesData
            };
            var prepareBonesDependency = prepareBonesJob.Schedule(_bones);
            prepareBonesDependency.Complete();
            _bonesDataBuffer.SetData(_bonesData);

            _pbdShader.Dispatch(_pbdKernel, 1, 1, 1);

            AsyncGPUReadback.RequestIntoNativeArray(ref _position, _positionBuffer);
            AsyncGPUReadback.RequestIntoNativeArray(ref _tempPosition, _tempPositionBuffer);
            AsyncGPUReadback.WaitAllRequests();

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
            _lastHandle.Complete();

            _positionBuffer.SetData(_position);
            _tempPositionBuffer.SetData(_tempPosition);

            sphereCastCommands.Dispose();
            sphereCastHits.Dispose();
        }

        public void OnGraphicsUpdate()
        {
            _skeletonShader.Dispatch(_skeletonKernel,1,1,1);
            
            Graphics.DrawMeshInstancedIndirect(
                _modelData.Mesh,
                0,
                _instancedMaterial,
                _bounds,
                _argsBuffer
            );
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

        private int FindUnusedSkeletonIndex()
        {
            // TODO find in removed first for optimization!
            for (int i = 0; i < POINTS_AMOUNT / 2; i++)
            {
                if (!_skeletonInfo[i].valid) return i;
            }

            return -1;
        }

        public void Dispose()
        {
            AsyncGPUReadback.WaitAllRequests();

            _pointsData.Dispose();
            _position.Dispose();
            _velocity.Dispose();
            _tempPosition.Dispose();
            _connectionData.Dispose();
            _sphereCastCommands.Dispose();
            _sphereCastHits.Dispose();


            _pointsDataBuffer.Dispose();
            _positionBuffer.Dispose();
            _velocityBuffer.Dispose();
            _tempPositionBuffer.Dispose();
            _connectionDataBuffer.Dispose();

            _bones.Dispose();
            _bonesData.Dispose();
            _bonesDataBuffer.Dispose();

            _skeletonDataBuffer.Dispose();
            _skeletonInfo.Dispose();

            _weightsBuffer.Dispose();
            _indicesBuffer.Dispose();
            _boneMatricesBuffer.Dispose();
            _skeletonBoneTransformBuffer.Dispose();
            _bindPosesBuffer.Dispose();
            _argsBuffer.Dispose();
        }
    }
}