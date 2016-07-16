using UnityEngine;
using System.Collections;

public class PlayerController : MonoBehaviour {

	public float speedDampTime = 0.1f;	// Speed Damping
	public Vector3 movementVector = Vector3.zero;
	//Needs
	public CharacterStats stats;
	protected CapsuleCollider collider;
	public AttackManager manager;
	public float altitude;
	public float lastY = 0;

	public Animator anim;
	public bool controllable = true;
	public bool grounded = true;
	public bool attacking = false;
	public bool charging = false;
	public float gravity;
	public float controlTimer = 0;
	public long score = 0;

	public float leftBound;
	public float rightBound;

	//public Queue<Action> actionQueue;
	//inputs
	public float xin;
	public float zin;
	public float attack;
	public float jump;



	// Use this for initialization
	void Start () {
		//Set up references to other components
		stats = GetComponent<CharacterStats>();
		anim = GetComponent<Animator>();
		//controller = GetComponent<CharacterController>();
		collider = GetComponent<CapsuleCollider>();
		manager = GetComponentInChildren<AttackManager>();
		manager.enemy = false;
		gravity = stats.gravity;




	}

	// FixedUpdate is called before the frame, best used for inputs and other direct gameplay
	void FixedUpdate() {
        if (stats.currentHealth <= 0)
            anim.SetBool("alive", false);
        //Get inputs
        xin = Input.GetAxis("Horizontal");
		zin = Input.GetAxis("Vertical");
		attack = Input.GetAxis("Attack");
		jump = Input.GetAxis("Jump");
		//check if hit and/or on ground
		grounded = GroundCheck();
        ActionManagement(attack);
        MovementManagement(xin, zin, jump);
        //if hit, set control off
        if (controlTimer > 0)
			controllable = false;
		leftBound = Camera.main.ViewportToWorldPoint (new Vector3 (0, 0, transform.position.z - Camera.main.transform.position.z)).x;
		rightBound = Camera.main.ViewportToWorldPoint (new Vector3 (1, 0, transform.position.z - Camera.main.transform.position.z)).x;
	}

	// Update is called once per frame, best used for animations and internal functions
	void Update () {
		
		//Still need to pause movement while attacking
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
		else {
			controllable = true;
			anim.SetBool("injured", false);
		}

		//set last known altitude for ground check
		lastY = transform.position.y;
	}

	protected void MovementManagement(float x, float z, float jump){
		if (controllable) {
			anim.SetBool ("controllable", controllable);
			if (!charging) {
				if (x > 0)
					anim.SetBool ("right_face", true);
				else if (x < 0)
					anim.SetBool ("right_face", false);
			}
			anim.SetFloat ("horiz", x);
			if (x == 0) {
				if (anim.GetBool ("right_face"))
					anim.SetFloat ("horiz", Mathf.Abs (z));
				else
					anim.SetFloat ("horiz", -(Mathf.Abs (z)));
			}
			movementVector.x = x * stats.speed;
			if (charging)
				movementVector.x /= 2;
			movementVector.z = z * stats.speed;
			if (charging)
				movementVector.z /= 2;
		} else if (grounded){
			movementVector.x = 0;
			movementVector.z = 0;
			anim.SetFloat("horiz", 0);
		}

		if (jump !=0 && GroundCheck() && controllable){
			movementVector.y = 5 * stats.jumping;
			if (!charging) anim.Play ("Jumping"); //Force jump animation 
		}
		else if (GroundCheck()) movementVector.y = 0;
		else if (movementVector.y > -15) movementVector.y -= gravity;
		anim.SetFloat ("vert", movementVector.y);


        //if (leftBound!=null && rightBound!=null)
        if (transform.position.x <= leftBound) movementVector.x = 0;
        if (transform.position.x >= rightBound) movementVector.x = 0;

        GetComponent<Rigidbody>().velocity = movementVector;
	}

	protected void ActionManagement(float attack){
		attacking = anim.GetBool("attacking");
		//if (actionQueue(0) != null) {
		//
		//	return;
		//} 
		if (controllable && !attacking && !charging){
			if (attack != 0){
				charging = true;
				anim.SetInteger("charge_level", 1);
				if (anim.GetBool("right_face")) anim.Play("Charge Right"); //Force attack charge animation
				else anim.Play ("Charge Left");
			}
		}
		else if (controllable && anim.GetBool("attacking") && attack != 0){
			if (anim.GetBool("right_face")){
				if (anim.GetCurrentAnimatorStateInfo(0).IsName("Light Attack Right 1") || anim.GetCurrentAnimatorStateInfo(0).IsName("Light Attack Left 1") ){
					anim.Play("Light Attack Right 2");
					manager.makeAttack(manager.light2, stats.damage, stats.holdtimes[1], anim.GetBool("right_face"), new Vector3(transform.position.x, transform.position.y, transform.position.z));
					controllable = false;
					controlTimer = stats.holdtimes[1]-5;
				}
				else if (anim.GetCurrentAnimatorStateInfo(0).IsName("Light Attack Right 2") || anim.GetCurrentAnimatorStateInfo(0).IsName("Light Attack Left 2")){
					anim.Play("Light Attack Right 3");
					manager.makeAttack(manager.light3, stats.damage*2, stats.holdtimes[3], anim.GetBool("right_face"), new Vector3(transform.position.x, transform.position.y, transform.position.z));
					controllable = false;
					controlTimer = stats.holdtimes[2]-5;
				}
			}
			else{
				if (anim.GetCurrentAnimatorStateInfo(0).IsName("Light Attack Left 1") || anim.GetCurrentAnimatorStateInfo(0).IsName("Light Attack Left 1") ){
					anim.Play("Light Attack Left 2");
					manager.makeAttack(manager.light2, stats.damage, stats.holdtimes[1], anim.GetBool("right_face"), new Vector3(transform.position.x, transform.position.y, transform.position.z));
					controllable = false;
					controlTimer = stats.holdtimes[1]-5;
				}
				else if (anim.GetCurrentAnimatorStateInfo(0).IsName("Light Attack Left 2") || anim.GetCurrentAnimatorStateInfo(0).IsName("Light Attack Left 2")){
					anim.Play("Light Attack Left 3");
					manager.makeAttack(manager.light3, stats.damage*2, stats.holdtimes[3], anim.GetBool("right_face"), new Vector3(transform.position.x, transform.position.y, transform.position.z));
					controllable = false;
					controlTimer = stats.holdtimes[2]-5;
				}
			}
		}
		else if (controllable && charging){
			if (attack != 0) anim.SetInteger("charge_level", anim.GetInteger("charge_level") + 1); //Charge attack if button is pressed
			else { //If attack button is released...
				attacking = true;
				if (anim.GetBool ("right_face")){ //Attack right
					if (anim.GetInteger("charge_level") < 10){
						anim.Play ("Light Attack Right 1");
						manager.makeAttack(manager.light1, stats.damage, stats.holdtimes[0], anim.GetBool("right_face"), new Vector3(transform.position.x, transform.position.y, transform.position.z));
						controllable = false;
						controlTimer = stats.holdtimes[0]-5;
					}
					else if (anim.GetInteger("charge_level") < 60){
						anim.Play ("Mid Attack Right");
						manager.makeAttack(manager.mid, stats.damage, stats.holdtimes[0], anim.GetBool("right_face"), new Vector3(transform.position.x, transform.position.y, transform.position.z));
						controllable = false;
						controlTimer = stats.holdtimes[3]-5;
					}
					else {
						anim.Play ("High Attack Right");
						manager.makeAttack(manager.high, stats.damage, stats.holdtimes[0], anim.GetBool("right_face"), new Vector3(transform.position.x, transform.position.y, transform.position.z));
						controllable = false;
						controlTimer = stats.holdtimes[4]-5;
					}
				}
				else{ //Or left
					if (anim.GetInteger("charge_level") < 10){
						anim.Play ("Light Attack Left 1");
						manager.makeAttack(manager.light1, stats.damage, stats.holdtimes[0], anim.GetBool("right_face"), new Vector3(transform.position.x, transform.position.y, transform.position.z));
						controllable = false;
						controlTimer = stats.holdtimes[0]-5;
					}
					else if (anim.GetInteger("charge_level") < 60){
						anim.Play ("Mid Attack Left");
						manager.makeAttack(manager.mid, stats.damage, stats.holdtimes[0], anim.GetBool("right_face"), new Vector3(transform.position.x, transform.position.y, transform.position.z));
						controllable = false;
						controlTimer = stats.holdtimes[3]-5;
					}
					else {
						anim.Play ("High Attack Left");
						manager.makeAttack(manager.high, stats.damage, stats.holdtimes[0], anim.GetBool("right_face"), new Vector3(transform.position.x, transform.position.y, transform.position.z));
						controllable = false;
						controlTimer = stats.holdtimes[4]-5;
					}
				}
			
				charging = false;
				anim.SetInteger("charge_level", 0);
			}
		}


	}

	public virtual bool GroundCheck(){
		bool onGround = false;
		altitude = collider.transform.position.y - Terrain.activeTerrain.SampleHeight(collider.transform.position);
		RaycastHit ground;
		Physics.Raycast(transform.position, Vector3.down, out ground, .9f);
		if (Mathf.Abs(GetComponent<Rigidbody>().velocity.y) <= .001f) onGround = true;
		if (Mathf.Abs(transform.position.y - lastY) <= .1) onGround = true;
		if (altitude >= 0.9f) onGround = false;
		if (ground.rigidbody) onGround = true;
		anim.SetBool("grounded", onGround);
		return onGround;
	}
}
