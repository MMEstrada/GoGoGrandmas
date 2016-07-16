using UnityEngine;
using System.Collections;
/// <summary>
/// Active when needed. A script for enabling and disabling components on a gameObject or
/// child gameObjects.  It can check for particles, animation, light, and colliders.  It disables them
/// when there are no players within the distToUse range to help speed up the frame rate since certain
/// objects only need their components active when close by, such as a point light source.  Particles and
/// animation can be played only when the main renderer is visible too.  Check the OnBecameVisible and 
/// OnBecameInvisible methods for that.
/// </summary>
public class ActiveWhenNeeded : MonoBehaviour
{
	public bool usingParticle = true; // Will a particle emitter need to be disabled for this?
	public bool usingAnimation; // Will an animation need to be disabled for this?
	public bool usingCollider; // Disable a collider?
	public bool usingGroup;
	public bool disableChildren; // Any children gameObjects to disable?
	bool usingLight; // Need to disable a light?
	bool activateMe;
	string myAnimName; // Animation clip name that will be found for animations
	Animation _animationCom; // Animation component using if desired.
	ParticleSystem partSystem;
	GameObject childGO;
    bool isActivated;

	void Start()
	{
		if (usingGroup)
		{
			if (transform.childCount > 0)
				childGO = transform.GetChild (0).gameObject;
			if (!childGO)
				Debug.LogWarning ("NO GROUP FOUND FOR ACTIVE GROUP " + gameObject.name);
			else
				childGO.SetActive (false);
		}
		if(usingAnimation) // If we checked this in the inspector for animation.
		{
			if(!GetComponent<Animation>()) // Animation component wasn't found.
			{
				_animationCom = transform.parent.GetComponent<Animation>(); // We check for the parent's animation component.
				if(!_animationCom) // If the parent's animation was also not found.
				{
					Debug.LogWarning("ANIMATION CHOSEN WITH NO ANIMATION COMPONENT FOR " + gameObject.name);
				}
			}
			else _animationCom = GetComponent<Animation>();
			myAnimName = _animationCom.clip.name; // Assign the animation clip name after finding the animation component.
		}
		usingLight = gameObject.GetComponent<Light>() != null; // If the gameObject has a light source, this will be true.
		if (usingParticle)
			partSystem = GetComponent<ParticleSystem> ();
	}
		
	// We became invisible from all cameras in the scene.
	void OnBecameInvisible()
	{
		// Stop an animation
		if(usingAnimation)
		{
			if(_animationCom && _animationCom.isPlaying)
			{
				_animationCom[_animationCom.clip.name].time = 0;
				_animationCom.Stop ();
			}
		}
		if(usingParticle)
		{
			if(disableChildren)
			{
				ChildrenEnableParticleChange(false);
			}
		}
	}

	// A camera sees us again.
	void OnBecameVisible()
	{
		if(usingAnimation)
		{
			if (_animationCom && !_animationCom.isPlaying) {
				_animationCom.Play ();
			}
		}
		if(usingParticle)
		{
			if(disableChildren)
			{
				ChildrenEnableParticleChange(true);
			}
		}
	}

	void OnTriggerEnter(Collider other)
	{
        if (!usingAnimation && other.gameObject.tag == "Player" && !isActivated)
            ActivateChange(true);
	}

	void OnTriggerExit(Collider other)
	{
		if (!usingAnimation && other.gameObject.tag == "Player")
			ActivateChange (false);
	}

	void ActivateChange(bool activate)
	{
        isActivated = activate;
		if (!activate)
		{
			if (usingCollider)
				GetComponent<Collider> ().enabled = false;
			else if (usingParticle) {
				// Disabling children is done on the OnBecameInvisible method since the parent can
				// be rendered.
				if (!disableChildren) {
					// Disable particles from emitting particles.
					if (GetComponent<ParticleEmitter> ()) {
						GetComponent<ParticleEmitter> ().emit = false;
					} else if (GetComponent<ParticleSystem> ()) {
						var enableEmis = partSystem.emission;
						enableEmis.enabled = false;
					}
				}
			}
			if (usingGroup)
				childGO.SetActive (false);
			if (usingLight)
				GetComponent<Light> ().enabled = false;
		}
		else
		{
			if(usingCollider)
				GetComponent<Collider>().enabled = true;
			else if(usingParticle)
			{
				if(!disableChildren)
				{
					if(GetComponent<ParticleEmitter>())
					{
						GetComponent<ParticleEmitter>().emit = true;
					}
					else if(GetComponent<ParticleSystem>())
					{
						var enableEmis = partSystem.emission;
						enableEmis.enabled = true;
					}
				}
			}
			if (usingGroup)
				childGO.SetActive (true);
			// Reset animation speed back to normal (1) if using an animation.
			if(_animationCom && _animationCom[myAnimName].speed == 0)
				_animationCom[myAnimName].speed = 1;
			if(usingLight)
				GetComponent<Light>().enabled = true;
		}
	}
	// Go through all children and disable or enable their particles.
	void ChildrenEnableParticleChange(bool enableThem)
	{
		for(int i = 0; i < transform.childCount; i++)
		{
			Transform curChild = transform.GetChild(i);
			if(curChild.GetComponent<ParticleEmitter>())
			{
				if(!curChild.GetComponent<ParticleEmitter>().emit)
					curChild.GetComponent<ParticleEmitter>().emit = enableThem;
			}
			else if(curChild.GetComponent<ParticleSystem>())
			{
				var enableEmis = partSystem.emission;
				if(!enableEmis.enabled)
					enableEmis.enabled = enableThem;
			}
		}
	}
}