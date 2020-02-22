using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class SimpleBonesIK : MonoBehaviour
{
    [SerializeField] private Transform _pivot;
    [SerializeField] private Transform[] _bones;
    [SerializeField] private AngleLimits[] _angleLimits;

    private float _length;
    private Vector3[] _positions; // будем использовать временный массив. для костей в компьют шейдере у нас все равно будут использоваться world координаты, так что поебать 
    private Vector3[] _initPositions; 
    private Quaternion[] _rotations;
    private float[] _distances;
    private int _bonesAmount;

    void Awake()
    {
        _bonesAmount = _bones.Length;

        _distances = new float[_bonesAmount];
        _positions = new Vector3[_bonesAmount];
        _rotations = new Quaternion[_bonesAmount];
        _initPositions = new Vector3[_bonesAmount];

        for (var i = 0; i < _bonesAmount - 1; i++)
        {
            var d = Vector3.Distance(_bones[i].position, _bones[i + 1].position);
            _length += d;
            _distances[i] = d;
            _rotations[i] = _bones[i].localRotation;
            _initPositions[i] = _bones[i].position;
        }

        _firstBoneIinitDir = _bones[0].right;
    }

    private float _angleLimit = 30f;
    private Vector3 _firstBoneIinitDir;

    void Update()
    {
        for (var i = 0; i < _bonesAmount; i++)
        {
            _positions[i] = _initPositions[i];
        }
        for (int k = 0; k < 3; k++)
        {


            _positions[_bonesAmount - 1] = _pivot.position;
            for (var i = _bonesAmount - 2; i >= 0; i--)
            {
                _positions[i] = _positions[i + 1] + (_positions[i] - _positions[i + 1]).normalized * _distances[i];
            }

            _positions[0] = _bones[0].position;
            for (var i = 1; i < _bones.Length; i++)
            {
                Vector3 dir = (_positions[i] - _positions[i - 1]).normalized; // это -X (world). А где он будет находиться, если мы применим ограничения?
//                Quaternion rootWorldRotation = LookAtXAxis(dir, _bones[i - 1].up);
//                Quaternion rootLocalRotation = Quaternion.Inverse(_bones[i - 1].parent.rotation)*rootWorldRotation;
//                Quaternion clampedRootLocalRotation = ClampRotationAroundAxis(rootLocalRotation, _angleLimits[i - 1]._xLimits, _angleLimits[i - 1]._yLimits, _angleLimits[i - 1]._zLimits);
//                _bones[i - 1].localRotation = clampedRootLocalRotation;
//                dir = -_bones[i - 1].right;
                _positions[i] = _positions[i - 1] + dir * _distances[i - 1];
            }
        }

        for (var i = 0; i < _bones.Length - 1; i++)
        {
            //_bones[i].position = _positions[i];
            _bones[i].rotation = LookAtXAxis(-(_positions[i] - _positions[i + 1]).normalized, _bones[i].up);
        }
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

    private static Quaternion LookAtXAxis(Vector3 dir, Vector3 up)
    {
        return Quaternion.LookRotation(dir, up) * Quaternion.Euler(new Vector3(0, 90, 0));
    }

    private void OnDrawGizmos()
    {
    }
}

[Serializable]
public struct AngleLimits
{
    public Vector2 _xLimits;
    public Vector2 _yLimits;
    public Vector2 _zLimits;
}