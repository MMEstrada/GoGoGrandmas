using UnityEngine;
using System.Collections;

public class Player1HealthBarController : MonoBehaviour {
	public float healthRatio = 1;
	public Vector4 barBorders;
    UnityEngine.UI.Image sRenderer;
	float baseScale;
	// Use this for initialization
	void Start () {
		sRenderer = GetComponent<UnityEngine.UI.Image> ();
		barBorders = sRenderer.sprite.border;
		baseScale = transform.localScale.x;
	}
	
	// Update is called once per frame
	void Update () {
		healthRatio = (float)GameObject.FindWithTag("Player1").GetComponentInParent<CharacterStats>().currentHealth / (float)GameObject.FindWithTag("Player1").GetComponentInParent<CharacterStats>().maxHealth;
		transform.localScale = new Vector3(baseScale * healthRatio, 1f, 2f);
		//transform.localPosition = new Vector3(((baseScale) * healthRatio) - baseScale, 0, 0);
		if (healthRatio >= .6)
			sRenderer.color = Color.green;
		else if (healthRatio >= .3)
			sRenderer.color = Color.yellow;
		else
			sRenderer.color = Color.red;
	}
}
