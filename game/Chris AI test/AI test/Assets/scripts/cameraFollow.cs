using UnityEngine;
using System.Collections;

public class cameraFollow : MonoBehaviour {

    public GameObject player;

    public float smoothTimeX;
    public float smoothTimeY;
    public float smoothTimeZ;

    public Vector3 minCameraPos;
    public Vector3 maxCameraPos;

    public int cameraOffset = 5;

    private Vector3 velocity;

	// Use this for initialization
	void Start () {
	}
	
	// Update is called once per frame
	void FixedUpdate () {
        float posX = Mathf.SmoothDamp(transform.position.x, player.transform.position.x, ref velocity.x, smoothTimeX);
        float posY = Mathf.SmoothDamp(transform.position.y, player.transform.position.y, ref velocity.y, smoothTimeY);
        float posZ = Mathf.SmoothDamp(transform.position.z, player.transform.position.z - cameraOffset, ref velocity.z, smoothTimeZ);

        transform.position = new Vector3(posX, posY, posZ);
        
        transform.position = new Vector3(Mathf.Clamp(transform.position.x, minCameraPos.x, maxCameraPos.x),
                                         Mathf.Clamp(transform.position.y, minCameraPos.y, maxCameraPos.y),
                                         Mathf.Clamp(transform.position.z, minCameraPos.z, maxCameraPos.z));
        
	}
}
