using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class SimpleBonesIK : MonoBehaviour
{
    [SerializeField] private Transform _pivot;
    [SerializeField] private BoneOuterDirection _outerDirection;
    [SerializeField] private Transform[] _bones;
    [SerializeField] private AngleLimits[] _angleLimits;

    private float _length;
    private Vector3[] _positions; // будем использовать временный массив. для костей в компьют шейдере у нас все равно будут использоваться world координаты, так что поебать 
    private Vector3[] _initPositions;
    private Vector3[] _initWorldUpDirections;
    private float[] _distances;
    private int _bonesAmount;

    void Awake()
    {
        _bonesAmount = _bones.Length;

        _distances = new float[_bonesAmount];
        _positions = new Vector3[_bonesAmount];
        _initPositions = new Vector3[_bonesAmount];
        _initWorldUpDirections = new Vector3[_bonesAmount];

        for (var i = 0; i < _bonesAmount - 1; i++)
        {
            var d = Vector3.Distance(_bones[i].position, _bones[i + 1].position);
            _length += d;
            _distances[i] = d;
            _initPositions[i] = _bones[i].position;
            switch (_outerDirection)
            {
                case BoneOuterDirection.X:
                    _initWorldUpDirections[i] = _bones[i].right;
                    break;
                case BoneOuterDirection.Y:
                    _initWorldUpDirections[i] = _bones[i].up;
                    break;
                case BoneOuterDirection.Z:
                    _initWorldUpDirections[i] = _bones[i].forward;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
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
                Vector3 dir = (_positions[i] - _positions[i - 1]).normalized;
                _positions[i] = _positions[i - 1] + dir * _distances[i - 1];
            }
        }

        for (var i = 0; i < _bones.Length - 1; i++)
        {
            switch (_outerDirection)
            {
                case BoneOuterDirection.X:
                    _initWorldUpDirections[i] = _bones[i].right;
                    break;
                case BoneOuterDirection.Y:
                    _bones[i].rotation = LookAtXAxisYUp(-(_positions[i] - _positions[i + 1]).normalized, _initWorldUpDirections[i]);
                    break;
                case BoneOuterDirection.Z:
                    _bones[i].rotation = LookAtXAxisZUp(-(_positions[i] - _positions[i + 1]).normalized,- _initWorldUpDirections[i]);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
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

    private static Quaternion LookAtXAxisYUp(Vector3 dir, Vector3 up)
    {
        return Quaternion.LookRotation(dir, up) * Quaternion.Euler(new Vector3(0, 90, 0));
    }
    
    private static Quaternion LookAtXAxisZUp(Vector3 dir, Vector3 up)
    {
        return Quaternion.LookRotation(dir, up) * Quaternion.Euler(new Vector3(0, 90, 0)) * Quaternion.Euler(new Vector3(90, 0, 0));
    }
}

[Serializable]
public struct AngleLimits
{
    public Vector2 _xLimits;
    public Vector2 _yLimits;
    public Vector2 _zLimits;
}

public enum BoneOuterDirection
{
    X,
    Y,
    Z
}