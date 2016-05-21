using UnityEngine;
using System.Collections;

public class LevelController : MonoBehaviour {
	public int checkpoint = 0;
	
	
	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
		
	}
	
	void EnemySpawn(Object enemy, Vector3 position){
		Instantiate (enemy, position, Quaternion.identity);
	}
	
	public void ReloadFromCheckpoint(){
		GameObject[] progress = GameObject.FindGameObjectsWithTag ("Checkpoint");
		int x = 0;
		while (progress[x].GetComponent<Flag>().active == true) {
			x++;
		}
		x--;
		
		Application.LoadLevel (Application.loadedLevelName);
	}
	
	
}