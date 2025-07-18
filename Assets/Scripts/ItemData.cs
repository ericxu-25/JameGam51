using UnityEngine;

[CreateAssetMenu]
public class ItemData : ScriptableObject
{
    public int width = 1;
    public int height = 1;

    public Sprite itemIcon;

    public int attack = 1;
    public int shield = 1;
    public int hpRegen = 1;
    public float fireRate = 1f;

    public (int attack, int shield, int hpRegen, float fireRate) GetItemStats()
    {
        return (attack, shield, hpRegen, fireRate);
    }


}
