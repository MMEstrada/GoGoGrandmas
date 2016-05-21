using UnityEngine;
using System.Collections;

public class CharacterStats : MonoBehaviour {
	public int maxHealth = 100;
	public int currentHealth = 100;
	public int damage = 10;
	public float speed = 5;
	public float jumping = 8;
	public float gravity = 1.1f;
	public float recoilTime = 30f;
	public bool enemy = false;
	public float[] holdtimes;
	// Use this for initialization
	void Start () {
		holdtimes = new float[6] {10f, 20f, 30f, 30f, 30f, 30f};

	}
	
	// Update is called once per frame
	void Update () {
	
	}
}
