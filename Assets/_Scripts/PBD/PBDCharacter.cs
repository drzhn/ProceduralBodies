using System;
using System.Collections;
using System.Collections.Generic;
using PBD;
using UnityEngine;

public class PBDCharacter : PBDAbstractUnit
{
    [SerializeField] private float stretchingStiffness = 0.008f;
    [SerializeField] private float bodyStretchingStiffness = 0.9f;
    [SerializeField] private float pointCollisionStiffness = 0.27f;
    [SerializeField] private float boneStiffness = 0.046f;
    [SerializeField] private float damping = 0.99f;
    [SerializeField] private float pointRadius = 0.58f;
    [SerializeField] private bool useGravity = true;
    [SerializeField] private PBDUnit[] units;

    private PBDObject _pbdObject;
    private event Action OnUpdate; 
    void Start()
    {
//        _pbdObject = new PBDObject(
//            stretchingStiffness,
//            bodyStretchingStiffness,
//            pointCollisionStiffness,
//            boneStiffness,
//            damping,
//            pointRadius,
//            useGravity);
//        int hipsIndex;
//        int neckIndex;
////        _pbdObject.AddUnit(Hips, Neck, out hipsIndex, out neckIndex);
////        HipsIndex = hipsIndex;
////        NeckIndex = neckIndex;
//        PhysicsEnabled = false;
    }

    private int nextIndex = 0;

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            int hipsIndex;
            int neckIndex;
//            _pbdObject.AddUnit(units[nextIndex].Hips, units[nextIndex].Neck, out hipsIndex, out neckIndex);
//            units[nextIndex].HipsIndex = hipsIndex;
//            units[nextIndex].NeckIndex = neckIndex;
            units[nextIndex].PhysicsEnabled = false;
            units[nextIndex].PbdObject = _pbdObject;
            OnUpdate += units[nextIndex].OnUpdate;
            nextIndex++;
        }
//        _pbdObject.OnUpdate();
        

//        Hips.position = _pbdObject[HipsIndex];
//        Hips.rotation = PBDUtils.LookAtXAxis((_pbdObject[NeckIndex] - _pbdObject[HipsIndex]), Vector3.up);
        OnUpdate?.Invoke();
    }

    private void OnDestroy()
    {
        _pbdObject.Dispose();
    }
}