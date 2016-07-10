using UnityEngine;
using System.Collections;

public class enemyAI : MonoBehaviour {

    public Transform target;
    public float fpsTargetDistance;

    /*
    public bool canJump = false;
    public bool facingRight = false;
    public bool facingLeft = false;
    */

    //private Animator anim;
    // private Rigidbody rb;
    private playerMovement player;
    private NavMeshAgent agent;
    private SpriteRenderer sprite;

	// Use this for initialization
	void Start () {
        //rb = GetComponent<Rigidbody>();
        //anim = GetComponent<Animator>();
        agent = GetComponent<NavMeshAgent>();
        sprite = GetComponent<SpriteRenderer>();
	}

    // Update is called once per frame
    void Update() {
        fpsTargetDistance = Vector3.Distance(target.position, transform.position);
        if (fpsTargetDistance < 20)
        {
            agent.enabled = true;
            agent.updateRotation = false;
            agent.SetDestination(target.position);
        }
        if (fpsTargetDistance > 20)
        {
            agent.enabled = false;
        }

        if (target.position.z > transform.position.z)
        {
            sprite.sortingOrder = 3;
        }
        if (target.position.z < transform.position.z)
        {
            sprite.sortingOrder = 1;
        }

    }

    void onCollisionEnter(Collider col)
    {
        if (col.gameObject.tag == "Ground")
        {
            Debug.Log("enemy is touching the ground");
        }

        /*
        if (col.gameObject.tag == "Player")
        {
            StartCoroutine(player.knockback(0.02f, 50, player.transform.position));
        }
        */
    }
}
