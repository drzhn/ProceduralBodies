using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HandPairMover : MonoBehaviour
{

	public PositionList list;
	public float interpolator;
	
	private float distanceToRoot;
	private void Awake()
	{
		distanceToRoot = Vector3.Distance(transform.position, list.transform.position);
	}

	private void LateUpdate()
	{
		float accumulatedDistance = 0;
		int index = -1;
		if (list.positionHistory.Count == 0) return;
		
		for (var i = list.positionHistory.Count - 1; i > 0; i--)
		{
			accumulatedDistance += Vector3.Distance(list.positionHistory[i], list.positionHistory[i - 1]);
			if (accumulatedDistance > distanceToRoot)
			{
				index = i - 1;
				break;
			}
		}

		if (index != -1)
		{
			transform.position = Vector3.Lerp(transform.position, list.positionHistory[index],
				Time.deltaTime * interpolator);
			transform.rotation = Quaternion.Slerp(transform.rotation, list.rotationHistory[index],
				Time.deltaTime * interpolator);
		}
		else
		{
			transform.position = Vector3.Lerp(transform.position, list.positionHistory[0],
				Time.deltaTime * interpolator);
			transform.rotation = Quaternion.Slerp(transform.rotation, list.rotationHistory[0],
				Time.deltaTime * interpolator);
		}
	}
}
