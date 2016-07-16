using UnityEngine;
/// <summary>
/// Interfaces. I place all of my interfaces here.
/// </summary>
public interface IItem // For all items that can be picked up and dropped.
{
	void WasGrabbed(Transform grabbedMount);
	void WasDropped(int manualDrop);
}

public interface IDamageable // For anything that can take damage.
{
	// max is the max amount of health/condition rating that the object
	// can have.
	int TakeDamage(int damage, int max);
}