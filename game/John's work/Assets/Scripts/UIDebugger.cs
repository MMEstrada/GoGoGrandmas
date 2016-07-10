using UnityEngine;
using System.Collections;

public class UIDebugger : MonoBehaviour {
	public UnityEngine.EventSystems.EventSystem current;
	public GameObject selected;
	public UnityEngine.EventSystems.EventSystem[] all;
	public GameObject MM;
	public GameObject OP;
	public GameObject LB;
	// Use this for initialization
	void Start () {
		all = FindObjectsOfType<UnityEngine.EventSystems.EventSystem> ();
		MM = GameObject.Find ("EventSystemMM");
		OP = GameObject.Find ("EventSystemOP");
		LB = GameObject.Find ("EventSystemLB");
	}
	
	// Update is called once per frame
	void Update () {
		current = UnityEngine.EventSystems.EventSystem.current;
		selected = current.currentSelectedGameObject;

	}
}
