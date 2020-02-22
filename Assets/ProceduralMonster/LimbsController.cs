using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LimbsController : MonoBehaviour
{

	public Hand rightHand;
	public Hand leftHand;
	IEnumerator Start () {
		yield return new WaitForSeconds(0.05f);
		leftHand.RequestNewContactPoint();
	}
	
	// Update is called once per frame
	void Update () {
		
	}
}
