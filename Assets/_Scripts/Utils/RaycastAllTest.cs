using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.Jobs;

public class RaycastAllTest : MonoBehaviour
{
    
//    private struct UpdatePositionJob : IJobParallelForTransform
//    {
//
//        public void Execute(int index, TransformAccess transform)
//        {
//            transform.position = Vector3.one * index;
//        }
//    }
    void Start()
    {
        NativeArray<int> numbers = new NativeArray<int>(10,Allocator.Persistent);
        NativeMultiHashMap<int, NativeList<Vector3>> pairs = new NativeMultiHashMap<int, NativeList<Vector3>>(30,Allocator.Persistent);
        

    }

    private void Update()
    {

    }

    private void OnDrawGizmos()
    {
    }
}