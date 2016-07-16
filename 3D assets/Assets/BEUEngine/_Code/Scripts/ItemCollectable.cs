using UnityEngine;
using System.Collections;
/// <summary>
/// Item collectable. For all items that get destroyed after being picked up.
/// These can be healing items or maybe a story item for example.
/// </summary>
public class ItemCollectable : Base_Item
{
	public enum ItemCollectableTypes
	{
		Heal = 0
	}
	public ItemCollectableTypes myType;
	// The value to use for this item when obtained. I use the word "Max" because
	// of the idea of making the item have a smaller value based on its condition
	// but I felt that would be a bit weird and unfair to keep getting bruised
	// food items...
	public int MaxValue;

	void Awake()
	{
		ItemType = ItemTypes.Collectable; // We are a collectable.
	}
}