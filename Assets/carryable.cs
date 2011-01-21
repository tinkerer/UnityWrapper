using UnityEngine;
using System.Collections;

public class carryable : MonoBehaviour {

	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {
		if (hand1 && hand2)
		{
			transform.position = Vector3.Lerp(hand1.position, hand2.position, .5f);
		}
	}
	
	public Transform hand1;
	public Transform hand2;
	
	void OnTriggerEnter(Collider hit)
	{
		Debug.Log("Collision in");
		if(hit.transform.GetComponent(typeof(Hand)))
		{
			Debug.Log(hit.gameObject.name + " enter");
			if (hand1)
			{
				hand2 = hit.transform;
			}
			else
			{
				hand1 = hit.transform;
			}
			
		}
	}
	void OnTriggerExit(Collider hit)
	{
				Debug.Log("Collision out");
		if(hit.transform.GetComponent(typeof(Hand)))
		{
			Debug.Log(hit.gameObject.name + " exit");	
			if (hit.transform == hand1)
			{
				hand1 = null;
				Debug.Log("Hand1 OUT");
			}	
			if (hit.transform == hand2)
			{
				hand2 = null;
				Debug.Log("Hand2 OUT");
			}	
		}
	}
}
