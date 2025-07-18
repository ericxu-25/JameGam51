using System;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UI;


public class InventoryItem : MonoBehaviour
{
    public ItemData itemData;

    public int HEIGHT
    {
        get
        {
            if (rotated == false)
            {
                return itemData.height;
            }
            return itemData.width;
        }
    }

    public int WIDTH
    {
        get
        {
            if (rotated == false)
            {
                return itemData.width;
            }
            return itemData.height;
        }
    }

    public int onGridPositionX;
    public int onGridPositionY;

    public bool rotated = false;

    internal void Rotate()
    {
        rotated = !rotated;
        RectTransform rectTransform = GetComponent<RectTransform>();
        rectTransform.localEulerAngles = new Vector3(0, 0, rotated ? 90f : 0f);

    }

    // Shape is defined from top-left corner
    //For irregular shapes, maybe define in itemData
    // public bool[,] shape = new bool[,]
    // {
    //     { true, true, false },
    //     { true, false, false },
    // };
    // public int Width => shape.GetLength(0);
    // public int Height => shape.GetLength(1);


    internal void Set(ItemData itemData)
    {
        this.itemData = itemData;
        GetComponent<Image>().sprite = itemData.itemIcon;
        Vector2 size = new Vector2();
        size.x = WIDTH * ItemGrid.tileSizeWidth;
        size.y = HEIGHT * ItemGrid.tileSizeHeight;
        GetComponent<RectTransform>().sizeDelta = size;
        fireRate = itemData.fireRate;
    }

    [Header("Combat Stats")]
    [SerializeField] private float fireRate = 2f; // seconds
    // [SerializeField] private int damage = 5;

    private float fireTimer;

    public void UpdateTimer(float deltaTime)
    {
        fireTimer += deltaTime;
    }

    public bool IsReadyToFire()
    {
        return fireTimer >= fireRate;
    }

    // public void Fire(Stats target)
    // {
    //     target.TakeDamage(damage);
    //     Debug.Log($"{name} fired for {damage} damage!");
    // }

    public int AttackDmg()
    {
        return itemData.attack;
    }

    public int shieldAmt()
    {
        return itemData.shield;

    }

    public int regenHP()
    {
        return itemData.hpRegen;
    }

    public void ResetTimer()
    {
        fireTimer = 0f;
    }
}
