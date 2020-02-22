using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ClothHandler : MonoBehaviour
{
    [Range(0, 1)] public float stretchingStiffness = 1;
    [Range(0, 1)] public float bendingStiffness = 1;
    [Range(0, 1)] public float damping = 1;

    public float worldVelocityScale = 1;
    public float worldAccelerationScale = 1;

    public float zAccelerationMultiplier;

    private Cloth[] cloths;

    private void Awake()
    {
        cloths = GetComponentsInChildren<Cloth>();
        foreach (var cloth in cloths)
        {
            cloth.bendingStiffness = bendingStiffness;
            cloth.stretchingStiffness = stretchingStiffness;
            cloth.damping = damping;
            cloth.worldAccelerationScale = worldAccelerationScale;
            cloth.worldVelocityScale = worldVelocityScale;
        }
    }

    void Update()
    {
        foreach (var cloth in cloths)
        {
            cloth.externalAcceleration = -transform.forward * zAccelerationMultiplier;
        }
    }
}