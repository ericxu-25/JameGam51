using UnityEngine;
using UnityEngine.UI;

public class healthBar : MonoBehaviour
{

    public Slider healthSlider;
    public float maxHealth = 100f;
    public float health;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        health = maxHealth;
        healthSlider.maxValue = maxHealth;
    }

    // Update is called once per frame
    void Update()
    {
        if (healthSlider.value != health)
        {
            healthSlider.value = health;
        }

        if (Input.GetKeyDown(KeyCode.Space))
        {
            takeDamage(33);
        }
    }

    void takeDamage(int damage)
    {
        health -= Mathf.Min(health, damage);
    }
}
