using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// использую этот класс для того чтобы протестировать ik на целом теле, как это будет делаться в compute shader
public class SimpleIKBody : MonoBehaviour
{
    [SerializeField] private Transform _hips;
    [SerializeField] private Transform _head;
    [SerializeField] private Transform _neck;
    [SerializeField] private SimpleIKLimb _leftHandLimb;
    [SerializeField] private SimpleIKLimb _rightHandLimb;
    [SerializeField] private SimpleIKLimb _leftFootLimb;
    [SerializeField] private SimpleIKLimb _rightFootLimb;
    [SerializeField] private Transform _hipsPivot;
    [SerializeField] private Transform _neckPivot;
    [SerializeField] private Transform _headPivot;
    [SerializeField] private List<Transform> _handLimbsPivots;
    [SerializeField] private List<Transform> _footLimbsPivots;

    void Start()
    {
    }

    void Update()
    {
        UpdateHips();
        UpdateHead();
    }

    private void UpdateHips()
    {
        Vector3 hipsUp = Vector3.zero;
        foreach (Transform limbsPivot in _handLimbsPivots)
        {
            hipsUp += limbsPivot.position - _hipsPivot.position;
        }
        foreach (Transform limbsPivot in _footLimbsPivots)
        {
            hipsUp += limbsPivot.position - _hipsPivot.position;
        }

        hipsUp /= _handLimbsPivots.Count + _footLimbsPivots.Count;
        _hips.position = _hipsPivot.position;
        _hips.rotation = LookAtXAxisYUp(_neckPivot.position - _hipsPivot.position, hipsUp);
    }

    private void UpdateHead()
    {
        _head.rotation = LookAtXAxisYForward(_head.position - _headPivot.position, _neck.position - _head.position);
    }

    private void UpdateLimb(SimpleIKLimb limb, Transform pivot)
    {
    }

    private static Quaternion LookAtXAxisYUp(Vector3 dir, Vector3 up)
    {
        return Quaternion.LookRotation(dir, up) * Quaternion.Euler(new Vector3(0, 90, 0));
    }

    private static Quaternion LookAtXAxisYForward(Vector3 dir, Vector3 up)
    {
        return Quaternion.LookRotation(dir, up) * Quaternion.Euler(new Vector3(-90, 0, 0)) * Quaternion.Euler(new Vector3(0, -90, 0));
    }
}

[Serializable]
public class SimpleIKLimb
{
    public List<Transform> Bones;
}