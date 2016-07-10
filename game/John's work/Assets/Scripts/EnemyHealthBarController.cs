using UnityEngine;
using System.Collections;

public class EnemyHealthBarController : MonoBehaviour {
	public float healthRatio = 1;
	public Vector4 barBorders;
	SpriteRenderer sRenderer;
	float baseScale;
	// Use this for initialization
	void Start () {
		sRenderer = GetComponent<SpriteRenderer> ();
		barBorders = sRenderer.sprite.border;
		baseScale = transform.localScale.x;
	}

	// Update is called once per frame
	void Update () {
		healthRatio = (float)GetComponentInParent<CharacterStats>().currentHealth / (float)GetComponentInParent<CharacterStats>().maxHealth;
		transform.localScale = new Vector3(baseScale * healthRatio, .25f, .2f);
		transform.localPosition = new Vector3(((baseScale) * healthRatio) - baseScale, 0, 0);
		if (healthRatio >= .6)
			sRenderer.color = Color.green;
		else if (healthRatio >= .3)
			sRenderer.color = Color.yellow;
		else
			sRenderer.color = Color.red;
	}
}
