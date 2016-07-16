using UnityEngine;
using System.Collections;

public class UIController : MonoBehaviour {

	public float xin;
	public float yin;
	public float damping = 1;
	public int playernum;

	UIController(int player){
		playernum = player;
	}

	// Use this for initialization
	void Start () {
		//transform.position = FindObjectOfType<Camera>().ScreenToWorldPoint(new Vector3(Screen.width/5, Screen.height*4/5, 700));
	}
	
	// Update is called once per frame
	void FixedUpdate () {
		xin = Input.GetAxis("Horizontal"+playernum);
		yin = Input.GetAxis("Vertical"+playernum);
	}

	void Update(){
		transform.position = new Vector3(transform.position.x + xin*damping, transform.position.y + yin*damping, transform.position.z);
		//transform.position.x += xin*damping;
		//transform.position.z += zin*damping;
	}
}
