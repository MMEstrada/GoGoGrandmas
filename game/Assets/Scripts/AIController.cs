using UnityEngine;
using System.Collections;

public class AIController : PlayerController {
	//Needs

	public NavMeshAgent agent;
    public BasicEnemyNavigation nav;
    public float distance;
    public bool playerNear;

	public float attackin;
	public float jumpin;
	
	// Use this for initialization
	void Start () {
		//Set up references to other components
		stats = GetComponent<CharacterStats>();
		anim = GetComponent<Animator>();
		agent = GetComponent<NavMeshAgent>();
		manager = GetComponentInChildren<AttackManager>();
        nav = GetComponent<BasicEnemyNavigation>();

		//controller = GetComponent<CharacterController>();
		collider = GetComponent<CapsuleCollider>();
		gravity = stats.gravity;
	}
	
	// FixedUpdate is called before the frame, best used for inputs and other direct gameplay
	void FixedUpdate() {
		if (stats.currentHealth <= 0)
			anim.SetBool ("alive", false);
		//check if hit and/or on ground
		grounded = GroundCheck();
        if (grounded == false)
        {
            jumpin = 1;
        }
        if (grounded == true)
        {
            agent.enabled = true;
            jumpin = 0;
        }
		//if hit, set control off
		if (controlTimer > 0)
			controllable = false;
        if (controllable) {
            if (playerNear)
            {
                attackin = 1;
            }
            if (charging) {
                float atkRand = Random.value;
                if (atkRand >= .90) {
                    attackin = 0;
                }
            }    
        }
        if (attacking) {
            attackin = 0;
        }
        leftBound = transform.position.x - 20;
        rightBound = transform.position.x + 20;
	}
	
	// Update is called once per frame, best used for animations and internal functions
	void Update () {
		ActionManagement(attackin);
		MovementManagement(xin, zin, jumpin);
	}

    // LateUpdate is called at the end of the frame, best used for camera control and responses to gameplay
    void LateUpdate() {
        //reset controllability and jumping
        if (controlTimer > 0)
            controlTimer--;
        else if (controlTimer < 0)
            controlTimer = 0;
        //else if (attacking)
        //	controllable = false;
        else if (!anim.GetBool("alive")) controllable = false;
        else
        {
            controllable = true;
            anim.SetBool("injured", false);
        }
        distance = (transform.position - nav.targetPosition.position).magnitude;
        playerNear = OpponentInRange();

        //Reset inputs
        //xin = 0;
        //zin = 0;
        //attack = 0;
        //jump = 0;
        //set last known altitude for ground check
        lastY = transform.position.y;
        grounded = GroundCheck();
        if (grounded == false)
        {
            jumpin = 1;
        }
        if (grounded == true)
        {
            agent.enabled = true;
            jumpin = 0;
        }
		//if hit animation has finished playing, return control
	}


    bool OpponentInRange() {
        return Physics.Raycast(transform.position, nav.targetPosition.position - transform.position, 2.0f);
    }
		
}
