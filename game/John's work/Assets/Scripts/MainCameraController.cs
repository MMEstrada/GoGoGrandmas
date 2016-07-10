using UnityEngine;
using System.Collections;

public class MainCameraController : MonoBehaviour {
    public GameController controller;
    public GameObject[] players;
    public Vector3 focus;
    public float zoom;
    public float xmin;
    public float xmax;
    public float cameraDamping = 1.5f;
    float[] playerXs;

    // Use this for initialization
    void Start () {

        controller = GameObject.FindObjectOfType<GameController>();
        
	}
	
	// Update is called once per frame

	void Update () {
        players = GameObject.FindGameObjectsWithTag("Player");
        playerXs = new float[players.Length];
        for (int n = 0; n < players.Length; n++)
        {
            playerXs[n] = players[n].transform.position.x;
        }
        xmin = Mathf.Min(playerXs);
        xmax = Mathf.Max(playerXs);
        focus = CameraFocus();
        transform.position = Vector3.Lerp(transform.position, focus, (transform.position - focus).magnitude/100);//Time.deltaTime*cameraDamping);

	}

    Vector3 CameraFocus() {
        int targets = 0;
        Vector3 avgPos = Vector3.zero;
        if (controller.Player1) { if (controller.Player1.transform.parent.gameObject.activeSelf) { avgPos += controller.Player1.transform.position; targets++; } }
        if (controller.Player2) { if (controller.Player2.transform.parent.gameObject.activeSelf) { avgPos += controller.Player2.transform.position; targets++; } }
        if (controller.Player3) { if (controller.Player3.transform.parent.gameObject.activeSelf) { avgPos += controller.Player3.transform.position; targets++; } }
        if (controller.Player4) { if (controller.Player4.transform.parent.gameObject.activeSelf) { avgPos += controller.Player4.transform.position; targets++; } }

        avgPos.x /= targets;
        avgPos.y /= targets;
        avgPos.z /= targets;

        //transform.LookAt(avgPos);
        zoom = Mathf.Min (Mathf.Max(Mathf.Abs(xmax - xmin), 5), 10);

        avgPos.z -= 2*zoom;
        avgPos.y += .5f*zoom;

        return avgPos;
    }
}
