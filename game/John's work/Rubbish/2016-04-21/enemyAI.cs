using UnityEngine;
using System.Collections;

public class enemyAI : MonoBehaviour {

    public Transform target;
    public float fpsTargetDistance;

    public bool canJump = false;
    public bool facingRight = false;
    public bool facingLeft = false;
    

    private Animator anim;
    private Rigidbody rb;
    private NavMeshAgent agent;

	// Use this for initialization
	void Start () {
        rb = GetComponent<Rigidbody>();
        anim = GetComponent<Animator>();
        agent = GetComponent<NavMeshAgent>();
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
    }

    void onCollisionEnter(Collider col)
    {
        if (col.gameObject.tag == "Ground")
        {
            canJump = true;
        }
    }
}
