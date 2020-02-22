using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IKHand : MonoBehaviour
{
    public Transform root;
    public Transform first;
    public Transform second;
    public Transform third;

    private Vector3 target;
    private Vector3 normal;
    float a;
    float b;

    public float handLength
    {
        get { return a + b; }
    }

    private bool negativeXscale;

    private void Awake()
    {
        a = Vector3.Distance(first.position, second.position); //elbow
        b = Vector3.Distance(second.position, third.position); //shoulder
        negativeXscale = root.localScale.x < 0;
    }

    void Update()
    {
        Vector3 vectorToTarget = target - first.position;
        float c = vectorToTarget.magnitude;
        if (c > handLength) // ну все пизда
        {
            first.rotation = LookAtXAxis(target - first.position, root.up, negativeXscale);
            second.rotation = LookAtXAxis(target - first.position, root.up, negativeXscale);
            third.rotation = LookAtXAxis(root.forward, normal, negativeXscale);
            return;
        }
        float elbowAngle = Mathf.Acos((a * a + c * c - b * b) / (2 * a * c))*180/Mathf.PI; //cosine theorem
        float shouderAngle = Mathf.Acos((a * a + b * b - c * c) / (2 * a * b))*180/Mathf.PI;
        
        Vector3 cross = Vector3.Cross(target - first.position, root.up);
        first.rotation = LookAtXAxis(Quaternion.AngleAxis(elbowAngle, cross)*vectorToTarget, root.up, negativeXscale);
        second.rotation = LookAtXAxis(Quaternion.AngleAxis(shouderAngle, cross)*(negativeXscale ? -first.right:first.right), root.up, negativeXscale);
        third.rotation = LookAtXAxis(root.forward, normal, negativeXscale);

    }


    public void SetNewTarget(Vector3 newTarget, Vector3 newNormal)
    {
        target = newTarget;
        normal = newNormal;
    }
    

    public static Quaternion LookAtXAxis(Vector3 dir, Vector3 up, bool negativeXscale)
    {
        return Quaternion.LookRotation(negativeXscale ? -dir : dir, up) * Quaternion.Euler(new Vector3(0, 90, 0));
    }
}