using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(SpringJoint))]
[ExecuteInEditMode]
public class SpringJointDebug : MonoBehaviour
{
	
	private void OnDrawGizmos()
	{
		foreach (var _joint in GetComponents<SpringJoint>())
		{
			if (_joint.connectedBody == null) return;
			Gizmos.color = Color.red;
			Gizmos.DrawLine(_joint.connectedBody.transform.position, transform.position);
			Gizmos.color = Color.blue;
			Gizmos.DrawLine(_joint.connectedBody.transform.TransformPoint(_joint.connectedAnchor), transform.TransformPoint((_joint.anchor)));
		}
	}
}
