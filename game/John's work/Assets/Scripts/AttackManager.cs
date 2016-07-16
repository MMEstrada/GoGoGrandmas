using UnityEngine;
using System.Collections;

public class AttackManager : MonoBehaviour {
	//public float holdTime;
	public bool enemy;
	public GameObject light1;
	public GameObject light2;
	public GameObject light3;
	public GameObject mid;
	public GameObject high;
	public GameObject special;
    public GameObject atk;

    public void Start() {
        enemy = GetComponentInParent<AIController>();
    }
	public void makeAttack(GameObject attack, int damage, float time, bool right, Vector3 pos){
		
		if (right) {
			atk = (Instantiate (attack, new Vector3 (pos.x + 1, pos.y, pos.z), Quaternion.identity) as GameObject);
			atk.transform.parent = gameObject.transform;
		} 
		else {
			atk = Instantiate (attack, new Vector3 (pos.x - 1, pos.y, pos.z), Quaternion.identity) as GameObject;
		}
		if (enemy) {
            atk.GetComponent<EnemyAttackBehavior>().damage = damage;
		}
		else {
			atk.GetComponent<PlayerAttackBehavior>().damage = damage;
		}
		Destroy(atk, time/30);
	}
}
