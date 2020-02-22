﻿using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Hand : MonoBehaviour
{
    public IKHand ik;
    public float footStepRadius;
    public float footCenterOffset;
    public float raycastStep = .1f;
    private Vector3 currentContactPoint;
    private Vector3 lastContactPoint;
    private Vector3 velocityDirection;

    private Vector3 prevPosition;

    //private Vector3 contactPlaneCircleCenter;
    public bool drawGizmos;

    private List<Rigidbody> _rigidbodies;

    void Awake()
    {
        _rigidbodies = GetComponentsInChildren<Rigidbody>().ToList();
        _rigidbodies.Remove(GetComponent<Rigidbody>());
        foreach (Rigidbody r in _rigidbodies)
        {
            r.isKinematic = true;
        }
    }

    private void Start()
    {
    }


    private bool negativeXscale
    {
        get { return transform.localScale.x < 0; }
    }

    void Update()
    {
//        Ray ray = new Ray(transform.position -
//                          (negativeXscale ? -transform.right : transform.right) * footCenterOffset +
//                          transform.up * 2,
//            -transform.up);
//        RaycastHit hit;
//        if (Physics.Raycast(ray, out hit, 1 << LayerMask.NameToLayer("Ground")))
//        {
//            contactPlaneCircleCenter = hit.point;
//        }
//
//        if (Vector3.Distance(contactPlaneCircleCenter, currentContactPoint) > footStepRadius)
//        {
        RequestNewContactPoint();
//        }
    }

    private void FixedUpdate()
    {
        velocityDirection = transform.position - prevPosition;
        prevPosition = transform.position;
    }

    private bool _physicsEnabled;

    private bool physicsEnabled
    {
        get { return _physicsEnabled; }
        set
        {
            _physicsEnabled = value;
            ik.enabled = !_physicsEnabled;
            foreach (Rigidbody r in _rigidbodies)
            {
                r.isKinematic = !_physicsEnabled;
            }
        }
    }

    public void RequestNewContactPoint()
    {
//        print(Vector3.Distance(transform.position, currentContactPoint));
//        print(ik.handLength);
        if ((Vector3.Distance(transform.position, currentContactPoint) <= ik.handLength )&& !physicsEnabled)
        {
            return;
        }

        Vector3 right = (negativeXscale ? -transform.right : transform.right);
        Vector3 rayOrigin = transform.position -
                            right * footCenterOffset +
                            velocityDirection.normalized * footStepRadius +
                            transform.up * 1;
        Vector3 rayDirection = -transform.up;
        Debug.DrawLine(rayOrigin, rayOrigin + rayDirection * 10, Color.red);
        RaycastHit hit;

        Ray ray = new Ray(rayOrigin, rayDirection);
//        if (Physics.Raycast(ray, out hit, float.PositiveInfinity, 1 << LayerMask.NameToLayer("Ground")))
//        {
//            print("raycast!");
//            currentContactPoint = hit.point + hit.normal * 0.1f;
//            if (Vector3.Distance(transform.position, currentContactPoint) < ik.handLength+0.1f)
//            {
//                print("kek");
//                physicsEnabled = false;
//                StartCoroutine(FootStepCoroutine(
//                    hit.normal));
//            }
//            else
//            {
//                // if point hasn't found
//                StopAllCoroutines();
//                setiingPoint = false;
//                physicsEnabled = true;
//            }
//        }
//        else
//        {
//            StopAllCoroutines();
//            setiingPoint = false;
//            physicsEnabled = true;
//        }

        bool raycasted = false;
        for (float radius = 0; radius < footStepRadius; radius += raycastStep)
        {
            if (Physics.SphereCast(ray, radius, out hit, float.PositiveInfinity, 1 << LayerMask.NameToLayer("Ground")))
            {
                lastContactPoint = currentContactPoint;
                currentContactPoint = hit.point + hit.normal * 0.1f;
                if (Vector3.Distance(transform.position, currentContactPoint) < ik.handLength + 0.1f)
//                if (Vector3.Distance(lastContactPoint, currentContactPoint) > footStepRadius + 0.1f)
                {
                    raycasted = true;
                    physicsEnabled = false;
                    StartCoroutine(FootStepCoroutine(hit.normal));
                }
            }
        }

        if (!raycasted)
        {
            StopAllCoroutines();
            setiingPoint = false;
            physicsEnabled = true;
        }
    }

    private bool setiingPoint;
    private Vector3 currentIKpoint;

    IEnumerator FootStepCoroutine(Vector3 normal)
    {
        if (setiingPoint) yield break;
        setiingPoint = true;
        Vector3 middlePoint = (currentIKpoint + currentContactPoint) / 2 + normal * 0.8f;
        float timeStarted = Time.time;
        float t = 0;
        while (t <= 1)
        {
            ik.SetNewTarget((1 - t) * (1 - t) * currentIKpoint + 2 * t * (1 - t) * middlePoint +
                            t * t * currentContactPoint, normal);
            t = (Time.time - timeStarted) / 0.1f;
            yield return null;
        }

        setiingPoint = false;
        currentIKpoint = currentContactPoint;
        ik.SetNewTarget(currentContactPoint, normal);
    }

    private void OnDrawGizmos()
    {
        if (!drawGizmos) return;
        Gizmos.color = Color.blue;
        Gizmos.DrawSphere(currentContactPoint, 0.07f);
        Gizmos.DrawLine(transform.position, transform.position + velocityDirection.normalized);
        Gizmos.DrawWireSphere(
            transform.position - (negativeXscale ? -transform.right : transform.right) * footCenterOffset,
            footStepRadius);
    }
}