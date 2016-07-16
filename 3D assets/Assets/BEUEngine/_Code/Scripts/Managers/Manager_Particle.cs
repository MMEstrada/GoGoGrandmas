using UnityEngine;
using UnityEngine.UI;
using System.Collections;
/// <summary>
/// Manager_ particle. The manager that creates particles for you. It can create
/// both particles and 3D numbers, provided you add both in the inspector. You
/// need to add them in the order that they appear in the ParticleTypes enum on
/// the Enums.cs file.
/// Note: When adding more particles, add them to the Manager_Game prefab and
/// not the Manager_Game in the scene! That way the prefab will update. Adding
/// things to the gameObject in the scene view will only have it affect that
/// scene.
/// </summary>
public class Manager_Particle : MonoBehaviour
{
	public static Manager_Particle instance;
	// Place all of your particle prefabs in here in the inspector in the order
	// that they appear in the ParticleTypes enum on the Enums.cs file.
	public GameObject[] particles;
	public GameObject number3D; // For 3D numbers, such as damage. I have prefab for this.
	
	void Awake()
	{
		if(instance == null)
			instance = this;
	}
	
	public GameObject CreateParticle(Vector3 pos, ParticleTypes particleToCreate, float sizeRatio)
	{
		int indexOfParticle = (int)particleToCreate;
		// Check to see if the particle has a ParticleRenderer component since the older particles used those. The newer ones use a
		// ParticleSystem component instead. If they have a ParticleRenderer component then they are considered a Legacy particle.

		// No need to make anything a smaller ratio than this for the overall
		// size of the particle. If you want to you could though, I just prefer
		// not to.
		if(sizeRatio < 0.4f)
			sizeRatio = 0.4f;
		GameObject newPart = Instantiate(particles[indexOfParticle], pos, particles[indexOfParticle].transform.rotation) as GameObject;
		bool isLegacyParticle = (newPart.GetComponent<ParticleSystem>() == null);

		if(isLegacyParticle)
		{
			// Here we find the particle emitter(s) on the particle being created.
			ParticleEmitter partEmitter = newPart.GetComponent<ParticleEmitter>();
			ParticleEmitter[] partEmitters = new ParticleEmitter[]{};
			if(partEmitter == null) // If there wasn't one found on the particle being created.
			{
				// Then it must be somewhere else so we find it/them in the children.
				partEmitters = newPart.transform.GetComponentsInChildren<ParticleEmitter>();
				if(partEmitters != null && partEmitters.Length > 0) // When found we then adjust them using the sizeRatio.
				{
					foreach(ParticleEmitter emitter in partEmitters)
					{
						emitter.minSize *= sizeRatio;
						emitter.maxSize *= sizeRatio;
						emitter.minEmission *= sizeRatio;
						emitter.maxEmission *= sizeRatio;
					}
				}
			}
			else if(partEmitter != null)
			{
				// This is where the sizeRatio makes its use.  It increases the size and emission of the particle to create a different size
				// and amount of the same particle.
				partEmitter.minSize *= sizeRatio;
				partEmitter.maxSize *= sizeRatio;
				partEmitter.minEmission *= sizeRatio;
				partEmitter.maxEmission *= sizeRatio;
			}
		}
		else // A Particle System component must be used instead!
		{
			ParticleSystem partSystem = newPart.GetComponent<ParticleSystem>();
			if(partSystem != null)
			{
				partSystem.startSize *= sizeRatio; // Increase the size using the Particle System's way.
				// Next I simply just change the color of the material for the
				// guard particle to blue to give it more of a blue effect. Just
				// something I wanted. This is also an example of how you can
				// edit individual particles. My guard hitspark uses a particle
				// system so editing it would go here. All particles that don't
				// use a particle system component would be edited in the above
				// part where you see partEmitter != null.
				if(particleToCreate == ParticleTypes.HitSpark_Guard)
					partSystem.GetComponent<Renderer>().material.color = Color.blue;
			}
			// Make sure the particle gets destroyed after its duration.
			Destroy (newPart, partSystem.duration + 0.1f);
		}
		return newPart; // This new particle gets returned if desired so you can access it in a script that called this method.
	}
	// I created a 3D number prefab with Unity's UI system so that is what gets
	// created here to show a 3D number after dealing damage or healing.
	public void Create3DNumber(int number, Vector3 pos, Vector3 moveDir, bool heal, float destroyTime = 2)
	{
		GameObject newNumber = Instantiate(number3D, pos, Quaternion.Euler(0, Camera.main.transform.eulerAngles.y, 0)) as GameObject;
		GameObject theNumber = newNumber.transform.GetChild (0).gameObject;
		Text textCompo = theNumber.GetComponent<Text> ();
		// If heal is true, the color of the text will be green, otherwise it will
		// be red to indicate damage since I only use numbers for healing and
		// damage.
		textCompo.color = heal ? Color.green : Color.red;
		textCompo.text = number.ToString ();
		theNumber.GetComponent<Rigidbody>().useGravity = !heal;
		theNumber.GetComponent<Collider>().enabled = !heal;
		// The number has a rigidbody so it will move in the direction given.
		// It will not collide with characters since I put it on a separate layer
		// that ignores characters. It will collide with the ground.
		theNumber.GetComponent<Rigidbody>().velocity = moveDir;
		Destroy (newNumber, destroyTime);
	}
}
