using UnityEngine;
using System.Collections;

public class wallmove : MonoBehaviour {

    public float speed = 3.0f;
    public float pacelength = 5.0f;
    public float originY;

	// Use this for initialization
	void Start () {

        originY = transform.position.y;

	}
	
	// Update is called once per frame
	void Update () {

        transform.Translate(0, speed * Time.deltaTime, 0);
        if (Mathf.Abs(originY - transform.position.y) > pacelength)
        {
            speed *= -1.0f;
            transform.position = new Vector3(transform.position.x,
                                             transform.position.y + 2 * speed * Time.deltaTime,
                                             transform.position.z);
        }
	}
}
