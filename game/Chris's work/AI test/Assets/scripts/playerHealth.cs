using UnityEngine;
using System.Collections;

public class playerHealth : MonoBehaviour {

    public float maxHealth = 100f;
    public float currentHealth = 0f;
    public GameObject healthBar;

	// Use this for initialization
	void Start () {
        currentHealth = maxHealth;
	}
	
	// Update is called once per frame
	void Update () {
	
	}

    void OnCollisionEnter (Collision col)
    {
        if(col.gameObject.tag == "Enemy")
        {
            decreaseHealth();
        }
    }

    void decreaseHealth()
    {
        currentHealth -= 5;
        float calculateHealth = currentHealth / maxHealth;
        setHealthBar(calculateHealth);
    }

    void setHealthBar(float health)
    {
        healthBar.transform.localScale = new Vector3(health, healthBar.transform.localScale.y, healthBar.transform.localScale.z);
    }
}
