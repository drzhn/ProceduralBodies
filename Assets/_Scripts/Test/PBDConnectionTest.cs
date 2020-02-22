using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace PBD
{
    public class PBDConnectionTest : MonoBehaviour
    {
        public Transform connectedPoint;
        public float Radius { private get; set; }

        private void OnDrawGizmosSelected()
        {
            Color gizmoColor = Gizmos.color;
            Gizmos.color = Color.blue;
            Gizmos.DrawSphere(transform.position, 0.02f);
            Gizmos.color = new Color(1,0,0,0.05f);
            Gizmos.DrawWireSphere(transform.position, Radius);
            if (connectedPoint != null)
            {
                Gizmos.color = Color.blue;
                Gizmos.DrawSphere(connectedPoint.position, 0.02f);
                Gizmos.color = new Color(1,0,0,0.05f);
                Gizmos.DrawWireSphere(connectedPoint.position, Radius);
                Gizmos.color = Color.blue;
                Gizmos.DrawLine(transform.position, connectedPoint.position);
            }

            Gizmos.color = gizmoColor;
        }
    }
}