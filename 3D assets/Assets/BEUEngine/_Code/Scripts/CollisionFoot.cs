using UnityEngine;
using System.Collections;
/// <summary>
/// Collision foot. This script is used for collision with the ground in order to
/// make little smoke/dust particles created when running. You could also have a
/// sound play. This script gets placed on a child gameObject of both of the
/// character's feet.
/// </summary>
public class CollisionFoot : MonoBehaviour
{
	Animator _anim;
	bool isRunning { get { return (_anim.GetBool ("OnGround") && _anim.GetCurrentAnimatorStateInfo(0).IsName("Locomotion") && _anim.GetFloat ("Move") > 0.9f); } }

	void Awake ()
	{
		_anim = transform.root.GetComponent<Animator> ();
	}

	void OnTriggerEnter (Collider other)
	{
		if(other.isTrigger)
			return;
		if(other.gameObject.tag == "Untagged" || other.gameObject.tag == "Terrain")
		{
			if(isRunning)
			{
				// Create a dust particle since our foot touched the ground and we
				// are running.
				// You could have a sound play here as well. I didn't have any.
				Manager_Particle.instance.CreateParticle(other.ClosestPointOnBounds(transform.position), ParticleTypes.Dust_Run, 1);
			}
		}
	}
}