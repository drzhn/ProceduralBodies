using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PositionList : MonoBehaviour
{
    public List<Vector3> positionHistory = new List<Vector3>();
    public List<Quaternion> rotationHistory = new List<Quaternion>();
    public int maxQueueLength;
    public bool drawGizmos;
    private void Start()
    {
        InvokeRepeating("CreateHistory",0,0.05f);
    }

    void CreateHistory()
    {
        positionHistory.Add(transform.position);
        if (positionHistory.Count > maxQueueLength)
        {
            positionHistory.RemoveAt(0);
        }
        
        rotationHistory.Add(transform.rotation);
        if (rotationHistory.Count > maxQueueLength)
        {
            rotationHistory.RemoveAt(0);
        }
    }

    private void OnDrawGizmos()
    {
        if (!drawGizmos) return;
        foreach (Vector3 pos in positionHistory)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(pos,0.1f);
        }
    }
}