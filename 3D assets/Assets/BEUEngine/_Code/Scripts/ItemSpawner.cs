using UnityEngine;
using System.Collections;
/// <summary>
/// Item spawner. Creates items in a random location based upon the size of the
/// sphere collider trigger it has. You can choose a set list of items for an
/// itemStorer to create if desired as well. Set its area number to the correct amount so
/// that it will only start creating items when players get there.
/// Amount to create is based up Manager_Game's ItemAppearRate:
/// * None : gameObject will be disabled, so no items.
/// * Very_Low: Create one small itemStorer.
/// * Low: Create one or two small ItemStorer.
/// * Medium: Create one large ItemStorer or two small ones.
/// * High: Create one large ItemStorer and one small one.
/// * Very High: Create two large ItemStorers or one Large and two small ones.
/// </summary>
[RequireComponent(typeof(SphereCollider))]
public class ItemSpawner : MonoBehaviour
{
	// Here is the manual list you can provide for this ItemSpawner. Place
	// items here that you want itemStorers spawned from here to create if
	// desired. Otherwise it will spawn random items.
	public GameObject[] itemPrefabsForItemStorer;
	public GameObject itemStorerPrefab; // The itemStorer gameObject itself. I made a crate.
	// Same as the player's version for checking which layers could be
	// considered ground.
	public LayerMask groundCheckMask; // Layers to consider being ground.
	public int myAreaNumber; // Make sure to set this accordingly to where it's placed in the scene so it will only create items and have them ready shortly before the player(s) arrive.

	private SphereCollider myTriggerRange;

	void Update()
	{
		// Only create items when we are closer to this item spawner. We can
		// create items when in the battle zone before it at the earliest.
		if(Manager_BattleZone.instance.currentAreaNumber > myAreaNumber - 1)
			CreateItems();
	}

	void CreateItems()
	{
		myTriggerRange = GetComponent<Collider>() as SphereCollider;
		GameObject itemStorerGO = null;
		GameObject itemStorer2GO = null; // A second itemStorer to create if needed.
		float rad = myTriggerRange.radius; // Max range to spawn.
		Vector3 posToCreate = transform.position + new Vector3 (Random.Range (-rad, rad), 0, Random.Range (-rad, rad));
		Vector3 addedRandom = new Vector3 (Random.Range (-3f, 3f), 0, Random.Range (-3f, 3f));
		RaycastHit rayHit;
		// Use a ray to find the ground
		if(Physics.Raycast(new Ray(transform.position, Vector3.down), out rayHit, 25, groundCheckMask))
		{
			if(rayHit.collider.gameObject.tag == "Untagged" || rayHit.collider.gameObject.tag == "Terrain")
				posToCreate = rayHit.point + Vector3.up * 0.5f;
		}
		// Create items based on the appear rating in Manager_Game.
		// We then set those items up to be small or remain large by using their
		// ItemSetup() method.
		switch(Manager_Game.ItemAppearRate)
		{
		case AmountRating.None:
			gameObject.SetActive(false);
			break;
		case AmountRating.Very_Low: // Create one small
			itemStorerGO = Instantiate(itemStorerPrefab, posToCreate, Quaternion.identity) as GameObject;
			itemStorerGO.GetComponent<ItemStorer>().ItemSetup(false, myAreaNumber, itemPrefabsForItemStorer);
			break;
		case AmountRating.Low:
			itemStorerGO = Instantiate(itemStorerPrefab, posToCreate, Quaternion.identity) as GameObject;
			itemStorerGO.GetComponent<ItemStorer>().ItemSetup(false, myAreaNumber, itemPrefabsForItemStorer);
			if(Random.value > 0.5f) // 50% chance of creating another small.
			{
				itemStorer2GO = Instantiate(itemStorerPrefab, posToCreate + addedRandom, Quaternion.identity) as GameObject;
				itemStorer2GO.GetComponent<ItemStorer>().ItemSetup(false, myAreaNumber, itemPrefabsForItemStorer);
			}
			break;
		case AmountRating.Medium:
			if(Random.value > 0.5f) // One small and one large.
			{
				itemStorerGO = Instantiate(itemStorerPrefab, posToCreate, Quaternion.identity) as GameObject;
				itemStorerGO.GetComponent<ItemStorer>().ItemSetup(false, myAreaNumber, itemPrefabsForItemStorer);
				itemStorer2GO = Instantiate(itemStorerPrefab, posToCreate + addedRandom, Quaternion.identity) as GameObject;
				itemStorer2GO.GetComponent<ItemStorer>().ItemSetup(false, myAreaNumber, itemPrefabsForItemStorer);
			}
			else // Create one large.
			{
				itemStorerGO = Instantiate(itemStorerPrefab, posToCreate, Quaternion.identity) as GameObject;
				itemStorerGO.GetComponent<ItemStorer>().ItemSetup(true, myAreaNumber, itemPrefabsForItemStorer);
			}
			break;
		case AmountRating.High:
			// One small and one large guaranteed.
			itemStorerGO = Instantiate(itemStorerPrefab, posToCreate, Quaternion.identity) as GameObject;
			itemStorerGO.GetComponent<ItemStorer>().ItemSetup(true, myAreaNumber, itemPrefabsForItemStorer);
			itemStorer2GO = Instantiate(itemStorerPrefab, posToCreate + addedRandom, Quaternion.identity) as GameObject;
			itemStorer2GO.GetComponent<ItemStorer>().ItemSetup(false, myAreaNumber, itemPrefabsForItemStorer);
			break;
		case AmountRating.Very_High:
			// Always have a large...
			itemStorerGO = Instantiate(itemStorerPrefab, posToCreate, Quaternion.identity) as GameObject;
			itemStorerGO.GetComponent<ItemStorer>().ItemSetup(true, myAreaNumber, itemPrefabsForItemStorer);
			if(Random.value > 0.5f) // Plus either another large.
			{
				itemStorerGO = Instantiate(itemStorerPrefab, posToCreate, Quaternion.identity) as GameObject;
				itemStorerGO.GetComponent<ItemStorer>().ItemSetup(true, myAreaNumber, itemPrefabsForItemStorer);
			}
			else // Or two small
			{
				itemStorer2GO = Instantiate(itemStorerPrefab, posToCreate + addedRandom, Quaternion.identity) as GameObject;
				itemStorer2GO.GetComponent<ItemStorer>().ItemSetup(false, myAreaNumber, itemPrefabsForItemStorer);
				itemStorer2GO = Instantiate(itemStorerPrefab, posToCreate + addedRandom, Quaternion.identity) as GameObject;
				itemStorer2GO.GetComponent<ItemStorer>().ItemSetup(false, myAreaNumber, itemPrefabsForItemStorer);
			}
			break;
		}
		Destroy (this.gameObject); // This spawner is no longer needed.
	}
}