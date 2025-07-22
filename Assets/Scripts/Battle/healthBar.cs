using System;
using UnityEngine;
using UnityEngine.UI;

public class HealthBar : MonoBehaviour
{
    public Slider healthSlider;
    [SerializeField] private Stats stats; // Reference to the Stats component

    private void Start()
    {
        if (stats == null)
            Debug.Log("Error, no stats assigned");

        // Initialize the slider
        healthSlider.maxValue = stats.MaxHP;
        healthSlider.value = stats.HP;

        // Subscribe to health changes
        stats.Damaged.AddListener(UpdateHealthBar);
        stats.Healed.AddListener(UpdateHealthBar);
    }

    private void UpdateHealthBar(int health)
    {
        //can get rid of maxhp later, have it for testing
        healthSlider.maxValue = stats.MaxHP;
        healthSlider.value = health;
    }
}