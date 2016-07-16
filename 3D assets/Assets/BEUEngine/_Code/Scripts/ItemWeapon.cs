using UnityEngine;
using System.Collections;
/// <summary>
/// Item weapon. For all items that can be used as a weapon. They will break
/// after being fully used up (Condition reaches 0)
/// </summary>
public class ItemWeapon : Base_Item, IDamageable
{
	public enum WeaponTypes
	{
		Sword = 0, Staff = 1
	}
	public GameObject brokenPrefab;
	public int Strength = 8;

	public WeaponTypes myWeaponType {get; private set;}

	void Awake()
	{
		flashAway = GetComponent<FlashAway> ();
		ItemType = ItemTypes.Weapon;
		Condition = Random.Range(80, 101);
	}

	void Start ()
	{
		// Setup which type of weapon we are based on our name.
		if(gameObject.name.Contains("Sword"))
			myWeaponType = WeaponTypes.Sword;
		else if(gameObject.name.Contains("Staff"))
			myWeaponType = WeaponTypes.Staff;
	}

	// The main damage method for all damageable objects.
	public int TakeDamage(int damage, int max)
	{
		if(damage > max) damage = max;
		int condition = Condition;
		condition = Mathf.Clamp (condition - damage, 0, 100);
		return condition;
	}

	// Hit a character and gave ourselves damage in the process.
	public void StruckHit(int damage)
	{
		Condition = TakeDamage(damage, 100);
		transform.root.SendMessage("ItemTookDamage", Condition);
		if(Condition <= 0)
		{
			// The character holding this item now drops it as it breaks, which
			// is done below.
			transform.root.SendMessage("DroppedBrokenItem");
			GameObject meBroken = Instantiate(brokenPrefab, transform.position, transform.rotation) as GameObject;
			for(int i = 0; i < meBroken.transform.childCount; i++)
			{
				Transform curChild = meBroken.transform.GetChild(i);
				if(!curChild.GetComponent<Rigidbody>())
					curChild.gameObject.AddComponent<Rigidbody>();
				Vector2 randomDir = new Vector3(Random.Range(-1f, 1f), Random.Range(0.6f, 1.5f), Random.Range(-1, 1f));
				curChild.GetComponent<Rigidbody>().velocity = randomDir;
				// I give the mass of each character a fraction of the
				// main mass of the gameObject this script is attached to.
				curChild.GetComponent<Rigidbody>().mass = GetComponent<Rigidbody>().mass / meBroken.transform.childCount;
			}
			meBroken.transform.DetachChildren(); // Free the pieces.
			// No longer need this as the children are what matter.
			Destroy(meBroken);
			Destroy (this.gameObject);
		}
	}
}