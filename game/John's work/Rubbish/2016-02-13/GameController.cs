using UnityEngine;
using System.Collections;

public class GameController : MonoBehaviour {
    public GameObject Player1;
    public GameObject Player2;
    public GameObject Player3;
    public GameObject Player4;

    Player1HealthBarController p1hb;
    //Player2HealthBarController p2hb;
    //Player3HealthBarController p3hb;
    //Player4HealthBarController p4hb;

    // Use this for initialization
    void Start () {
		Application.targetFrameRate = 30;
        DontDestroyOnLoad(this);
        p1hb = GameObject.FindGameObjectWithTag("UI Master").GetComponentInChildren<Player1HealthBarController>();

    }

    // Update is called once per frame
    void Update () {
		GameObject[] enemies = GameObject.FindGameObjectsWithTag ("Enemy");
		foreach (GameObject enemy in enemies) {
			enemy.GetComponent<CharacterStats>().enemy = true;
		}
        Player1 = GameObject.FindGameObjectWithTag("Player1");
        Player2 = GameObject.FindGameObjectWithTag("Player2");
        Player3 = GameObject.FindGameObjectWithTag("Player3");
        Player4 = GameObject.FindGameObjectWithTag("Player4");

        if (Player1) p1hb.enabled = Player1;
        //p2hb.enabled = Player2;
        //p3hb.enabled = Player3;
        //p4hb.enabled = Player4;
    }
}
