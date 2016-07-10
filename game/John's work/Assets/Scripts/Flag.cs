using UnityEngine;
using System.Collections;

public class Flag : MonoBehaviour {

	public bool active = false;

	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {
	
	}
	void OnTriggerEnter(Collider other){
		if (other.gameObject.tag == "Player") {
			active = true;
		}
	}

}
