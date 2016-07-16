using UnityEngine;
using System.Collections;
/// <summary>
/// Particle fade out. A script for fading out a particle so that it doesn't just
/// get destroyed after the duration or animation ends.
/// </summary>
public class ParticleFadeOut : MonoBehaviour
{
	// Will this start fading on its own upon starting? If not, you set it
	// manually by calling the StartFading() method when ready.
	public bool autoFade = true;
	public bool destroyMe = true;
	public bool destroyParent = false; // Only one child particle object needs this checked, if you want to destroy the parent that is.
	public float startFadingTimer = 2; // Time to start fading in, in seconds.
	bool _startFading = false;
	Color _partColor = Color.gray; // need to get the color to change alpha value.
	ParticleRenderer myPartRenderer;

	void Awake()
	{
		if(GetComponent<ParticleSystem>() == null)
			myPartRenderer = GetComponent<ParticleRenderer>();
	}

	IEnumerator Start()
	{
		// This uses the shuriken particle system.
		if(GetComponent<ParticleSystem>() != null)
		{
			_partColor = GetComponent<ParticleSystem>().startColor;
			startFadingTimer = GetComponent<ParticleSystem>().startLifetime - 0.1f;
		}
		else if(myPartRenderer != null) // Uses legacy particle system.
			// The tint color is used for most particles. Is used for mine.
			_partColor = myPartRenderer.material.GetColor("_TintColor");
		if(autoFade)
		{
			// Just a little extra time to wait to make sure everything is set
			// before hand in case a script tried to change the fading timer
			// after creating this particle.
			yield return new WaitForSeconds(0.7f);
			Invoke ("StartFading", startFadingTimer);
		}
	}

	void Update()
	{
		if(_startFading)
		{
			if(_partColor.a > 0)
			{
				// Fade out.
				_partColor.a -= 0.5f * Time.deltaTime;
				if(GetComponent<ParticleSystem>() != null)
					GetComponent<ParticleSystem>().GetComponent<Renderer>().material.SetColor("_TintColor", _partColor);
				else if(myPartRenderer != null)
					myPartRenderer.material.SetColor("_TintColor", _partColor);
			}
			else // Particle is completely invisible now.
			{
				if(destroyMe)
				{
					if(!destroyParent)
						Destroy (this.gameObject);
					else
					{
						if(transform.parent != null)
							Destroy (transform.parent.gameObject);
						else Destroy(this.gameObject);
					}
				}
				else // Don't destroy, but instead deactivate the gameObject.
					// Useful for gameObjects with a reusable particle.
				{
					if(!destroyParent)
					{
						_startFading = false;
						_partColor.a = 1; // Reset for next use.
						gameObject.SetActive(false);
					}
					else
					{
						_startFading = false;
						_partColor.a = 1; // Reset for next use.
						if(GetComponent<ParticleSystem>() != null)
							GetComponent<ParticleSystem>().GetComponent<Renderer>().material.SetColor("_TintColor", _partColor);
						else if(myPartRenderer != null)
							myPartRenderer.material.SetColor("_TintColor", _partColor);
						if(transform.parent != null)
							transform.parent.gameObject.SetActive(false);
						else gameObject.SetActive(false);
					}
				}
			}
		}
	}

	void OnEnable()
	{
		// Don't want to fade right away just in case this was re-enabled.
		_startFading = false;
		if(GetComponent<ParticleSystem>() != null)
			GetComponent<ParticleSystem>().GetComponent<Renderer>().material.SetColor("_TintColor", _partColor);
		else if(myPartRenderer != null)
			myPartRenderer.material.SetColor("_TintColor", _partColor);
		Invoke ("StartFading", startFadingTimer);
	}

	void OnDisable()
	{
		_startFading = false;
		// Update these to the current value. They are given default values
		// before being disabled(this method gets called).
		if(GetComponent<ParticleSystem>() != null)
			GetComponent<ParticleSystem>().GetComponent<Renderer>().material.SetColor("_TintColor", _partColor);
		else if(myPartRenderer != null)
			myPartRenderer.material.SetColor("_TintColor", _partColor);
	}

	// If the particle gets destroyed, remove its created material instance to free up memory.
	void OnDestroy()
	{
		if(myPartRenderer)
			DestroyImmediate(myPartRenderer.material);
		else if(GetComponent<ParticleSystem>())
			DestroyImmediate(GetComponent<ParticleSystem>().GetComponent<Renderer>().material);
	}

	// Call this when you want the particle to begin starting to fade out.
	public void StartFading()
	{
		_startFading = true;
	}
}
