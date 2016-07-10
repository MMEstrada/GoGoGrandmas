using UnityEngine;
using System.Collections;

public class BasicEnemyNavigation : MonoBehaviour {

	public Transform targetPosition;
	NavMeshAgent agent;
	AIController controller;
	public Vector3 destination;
	public NavMeshPathStatus status;
	public float linkTime;
	public OffMeshLinkData data;
	public CharacterStats stats;

	// Use this for initialization
	void Start () {
		agent = GetComponent<NavMeshAgent>();
		controller = GetComponent<AIController>();
		stats = GetComponent<CharacterStats> ();
	}
	
	// Update is called once per frame
	void Update () {
		if (agent.enabled) agent.SetDestination (targetPosition.position);
		//destination = agent.destination; //For debugging with the Inspector
		destination = agent.path.corners[1];
		status = agent.path.status;
		data = agent.nextOffMeshLinkData;

		if (agent.isOnOffMeshLink && linkTime == 0) {
			linkTime = Time.time;
		}

		if (controller.controllable) {
			if (Vector3.Distance (targetPosition.position, gameObject.transform.position) > agent.stoppingDistance) {
				//controller.xin = (targetPosition.position.x - gameObject.transform.position.x);
				//controller.zin = (targetPosition.position.z - gameObject.transform.position.z);
				controller.xin = Mathf.Max (Mathf.Min ((agent.path.corners [1].x - gameObject.transform.position.x), 1f), -1f);
				controller.zin = Mathf.Max (Mathf.Min ((agent.path.corners [1].z - gameObject.transform.position.z), 1f), -1f);
			} else
				controller.xin = controller.zin = 0;
			
			if (agent.path.corners [1].y - gameObject.transform.position.y > 0.5) {
				agent.enabled = false;
				controller.jumpin = 1;

			} else if (controller.GroundCheck ()) {
				agent.enabled = true;
				controller.jumpin = 0;
			}

			if (Time.time - linkTime >= .5 && linkTime != 0) {
				agent.enabled = false;
				linkTime = 0;
			}
		} else {
			controller.xin = controller.zin = 0;
		}

		var nav = GetComponent<NavMeshAgent>(); //Debug Path Line
		if( nav == null || nav.path == null )
			return;
		
		var line = this.GetComponent<LineRenderer>();
		if( line == null )
		{
			line = this.gameObject.AddComponent<LineRenderer>();
			line.material = new Material( Shader.Find( "Sprites/Default" ) ) { color = Color.yellow };
			line.SetWidth( 0.5f, 0.5f );
			line.SetColors( Color.yellow, Color.yellow );
		}
		
		var path = nav.path;
		
		line.SetVertexCount( path.corners.Length );
		
		for( int i = 0; i < path.corners.Length; i++ )
		{
			line.SetPosition( i, path.corners[ i ] );
		}

	}
}
