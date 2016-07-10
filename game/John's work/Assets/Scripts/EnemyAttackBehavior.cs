using UnityEngine;
using System.Collections;

public class EnemyAttackBehavior : MonoBehaviour
{
    public int damage;

    // Use this for initialization
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }

    void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.GetComponent<CharacterStats>())
        {
            if (!other.gameObject.GetComponent<CharacterStats>().enemy)
            {
                other.gameObject.GetComponent<CharacterStats>().currentHealth -= damage;

            }
            if (other.gameObject.GetComponent<PlayerController>())
            {
                if (!other.gameObject.GetComponent<PlayerController>().stats.enemy)
                {
                    other.gameObject.GetComponent<PlayerController>().controlTimer = other.gameObject.GetComponent<CharacterStats>().recoilTime;
                    other.gameObject.GetComponent<PlayerController>().anim.SetBool("injured", true);
                }
            }
            //else if (other.gameObject.GetComponent<AIController>())
            //{
           //     other.gameObject.GetComponent<AIController>().controlTimer = other.gameObject.GetComponent<CharacterStats>().recoilTime;
           //     other.gameObject.GetComponent<AIController>().anim.SetBool("injured", true);
           // }
        }

    }
}
