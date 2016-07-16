using UnityEngine;
using System.Collections;

public class P1UIController : UIController{

	public GameObject selected;

	P1UIController (int num = 1) : base(num){
		playernum = num;
	}
	// Use this for initialization

}
