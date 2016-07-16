using UnityEngine;
using System.Collections.Generic;
using System.Linq;
/// <summary>
/// Item storer. The script for things like item boxes/crates that contain items
/// inside upon being broken.
/// </summary>
public class ItemStorer : MonoBehaviour, IDamageable
{
	public List<GameObject> itemsCanDrop;
	// The broken version of this. Created when this is destroyed.
	public GameObject brokenPrefab;
	public int health = 10; // This gets broken when health reaches 0.
	// If random is chosen, this is how many different items it will choose
	// from.
	int _maxRandomToChooseFrom = 3;
	int _startHealth = 10; // Used for our TakeDamage() method's max value.
	int _totalItemsToCarry = 1; // How many items are we holding?
	bool _wasDestroyed = false; // Used to help prevent having been destroyed more than once.
	bool _startFlashTimer = false; // Begin the countdown to flashing away.

	// Only for activating our flash away script when in our area number.
	public int myAreaNumber {get; set;}

	void Start ()
	{
		_startHealth = health;
	}

	void Update()
	{
		if(!_startFlashTimer)
		{
			if(Manager_BattleZone.instance.currentAreaNumber >= myAreaNumber)
			{
				_startFlashTimer = true;
				// 25 seconds before we being flashing and then 3 more after
				// that until we vanish completely.
				GetComponent<FlashAway>().ResetFlashTime(35);
			}
		}
	}

	void SpawnItem() // We were destroyed so spawn what items we have!
	{
		if(_wasDestroyed)
			return;
		_wasDestroyed = true;
		GetComponent<Collider>().enabled = false;
		// Create our broken prefab and set its size to ours.
		GameObject meBroken = Instantiate(brokenPrefab, transform.position, transform.rotation) as GameObject;
		meBroken.transform.localScale = transform.localScale;
		// As each child of the broken prefab is a rigidbody, they will push
		// each other themselves, so no force needs to be applied.
		for(int i = 0; i < meBroken.transform.childCount; i++)
		{
			Transform curChild = meBroken.transform.GetChild(i);
			if(!curChild.GetComponent<Rigidbody>())
				curChild.gameObject.AddComponent<Rigidbody>();
			// Make sure they have this script so that they will flash away
			// after a set time. No need for a bunch of items to stay active
			// when left way behind.
			if(!curChild.GetComponent<FlashAway>())
				curChild.gameObject.AddComponent<FlashAway>();
			curChild.GetComponent<Rigidbody>().drag = 5;
			curChild.GetComponent<Rigidbody>().mass = GetComponent<Rigidbody>().mass / meBroken.transform.childCount;
		}
		meBroken.transform.DetachChildren(); // Detach all the small pieces.
		Vector3 randomDir = new Vector3(Random.Range(-0.5f, 0.5f), Random.Range(0.6f, 1.5f), Random.Range(-0.5f, 0.5f));
		// Each item gets the random velocity from above.
		for(int i = 0; i < _totalItemsToCarry; i++)
		{
			GameObject itemDropped = Instantiate (itemsCanDrop [Random.Range (0, itemsCanDrop.Count)], transform.position, transform.rotation) as GameObject;
			itemDropped.GetComponent<Rigidbody>().velocity = randomDir;
		}
		if(GetComponent<AudioSource>() && GetComponent<AudioSource>().clip != null) // Play a broken sound if provided.
			AudioSource.PlayClipAtPoint (GetComponent<AudioSource>().clip, transform.position);
		Destroy(meBroken); // The children of this are now free so remove this one.
		Destroy (this.gameObject);
	}

	void OnCollisionEnter(Collision other)
	{
		// Here is where the check is made to see if this itemStorer was created
		// touching an outer boundary. If so, we move it back into the main play
		// are depending on if it was a top or bottom boundary.
		if(other.gameObject.name.Contains("Boundary"))
		{
			if(other.gameObject.name.Contains("Bot"))
				transform.position = new Vector3(transform.position.x, transform.position.y, other.collider.bounds.max.z); // Higher point.
			else if(other.gameObject.name.Contains("Top"))
				transform.position = new Vector3(transform.position.x, transform.position.y, other.collider.bounds.min.z); // Lower point.
		}
	}
	// You can choose to have random items be created or pass in a chosen list.
	// If no list is provided, then random items are chosen.
	// This gets called from ItemSpawner after it spawns this itemStorer
	// gameObject.
	public void ItemSetup(bool large, int areaNumber, GameObject[] manualItemsChosen = null)
	{
		myAreaNumber = areaNumber;
		if(manualItemsChosen == null || manualItemsChosen.Count() == 0)
		{
			if(!large) // A small itemStorer was created.
			{
				// Half values.
				transform.localScale = Vector3.one * 0.5f;
				_totalItemsToCarry = Random.Range(1, 3); // Hold up to 2 when small.
				health = Mathf.RoundToInt(health * 0.5f);
			}
			else _totalItemsToCarry = Random.Range(3, 5); // Hold up to 4 when large.
			// More random choices with a higher appear rate. Very_High = 5 and
			// Very_Low = 1
			_maxRandomToChooseFrom = (int)Manager_Game.ItemAppearRate;
			itemsCanDrop = new List<GameObject>();
			for(int i = 0; i < _maxRandomToChooseFrom || itemsCanDrop.Count == _totalItemsToCarry; i++)
			{
				if(Random.value > 0.6f)
				{
					itemsCanDrop.Add(Manager_Game.instance.ICPrefabs[Random.Range(0, Manager_Game.instance.ICPrefabs.Count)]);
					continue;
				}
				if(Random.value > 0.5f)
				{
					itemsCanDrop.Add(Manager_Game.instance.ITPrefabs[Random.Range(0, Manager_Game.instance.ITPrefabs.Count)]);
					continue;
				}
				itemsCanDrop.Add(Manager_Game.instance.IWPrefabs[Random.Range(0, Manager_Game.instance.IWPrefabs.Count)]);
			}
		}
		else // Manually items chosen to choose from
		{
			List<GameObject> itemsToCreate = manualItemsChosen.ToList();

			if(!large)
			{
				// If small we will remove half of the amount of items we have.
				transform.localScale = Vector3.one * 0.5f;
				if(itemsToCreate.Count > 1)
				{
					_totalItemsToCarry = Mathf.RoundToInt(itemsToCreate.Count / 2);
					for(int i = 0; i < _totalItemsToCarry; i++)
					{
						if(i != _totalItemsToCarry - 1)
						{
							// Random chance of removing this one if we aren't at the
							// end.
							if(Random.value > 0.5f)
								itemsToCreate.RemoveAt(i);
						}
						else itemsToCreate.RemoveAt(i);
					}
				}
			}
			// Large so keep all items chosen.
			else _totalItemsToCarry = itemsToCreate.Count;
			itemsCanDrop = itemsToCreate;
		}
		GetComponent<Rigidbody>().drag = large ? 20 : 10;
	}

	public int TakeDamage(int damage, int max)
	{
		// Make sure health doesn't go below zero (our min value)
		int curHealth = health;
		curHealth = Mathf.Clamp (curHealth - damage, 0, max);
		return curHealth;
	}

	public void GotHit(int damage)
	{
		health = TakeDamage (damage, _startHealth);

		if(health < 1 && !_wasDestroyed)
			SpawnItem(); // We were destroyed.
	}
}