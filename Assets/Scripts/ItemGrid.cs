using System;
using UnityEngine;
using UnityEngine.PlayerLoop;
using System.Collections.Generic;


public class ItemGrid : MonoBehaviour
{
    public const float tileSizeWidth = 32;
    public const float tileSizeHeight = 32;

    [SerializeField] int gridSizeWidth = 5; //gridSizeWidth
    [SerializeField] int gridSizeHeight = 5; //gridSizeHeight
    // [SerializeField] GameObject inventoryItemPrefab;

    InventoryItem[,] inventoryItemSlot;
    RectTransform rectTransform;

    private void Start()
    {
        rectTransform = GetComponent<RectTransform>();
        Init(gridSizeWidth, gridSizeHeight);

    }

    public InventoryItem PickUpItem(int x, int y)
    {
        InventoryItem toReturn = inventoryItemSlot[x, y];

        if (toReturn == null) { return null; }

        CleanGrid(toReturn);

        return toReturn;
    }

    private void CleanGrid(InventoryItem toReturn)
    {
        for (int ix = 0; ix < toReturn.WIDTH; ix++)
        {
            for (int iy = 0; iy < toReturn.HEIGHT; iy++)
            {
                inventoryItemSlot[toReturn.onGridPositionX + ix, toReturn.onGridPositionY + iy] = null;
            }
        }
    }

    private void Init(int width, int height)
    {
        inventoryItemSlot = new InventoryItem[width, height];
        Vector2 size = new Vector2(width * tileSizeWidth, height * tileSizeHeight);
        rectTransform.sizeDelta = size;
    }

    Vector2 positionOnTheGrid = new Vector2();
    Vector2Int tileGridPosition = new Vector2Int();

    public Vector2Int GetTileGridPosition(Vector2 mousePosition)
    {
        positionOnTheGrid.x = mousePosition.x - rectTransform.position.x;
        positionOnTheGrid.y = rectTransform.position.y - mousePosition.y;

        tileGridPosition.x = (int)(positionOnTheGrid.x / tileSizeWidth);
        tileGridPosition.y = (int)(positionOnTheGrid.y / tileSizeHeight);

        return tileGridPosition;

    }

    public bool PlaceItem(InventoryItem inventoryItem, int posX, int posY, ref InventoryItem overlapItem)
    {
        //item out of bounds
        if (BoundaryCheck(posX, posY, inventoryItem.WIDTH, inventoryItem.HEIGHT) == false)
        {
            return false;
        }

        if (OverlapCheck(posX, posY, inventoryItem.WIDTH, inventoryItem.HEIGHT, ref overlapItem) == false)
        {
            overlapItem = null;
            return false;
        }

        if (overlapItem != null)
        {
            CleanGrid(overlapItem);
        }

        PlaceItem(inventoryItem, posX, posY);

        return true;
    }

    public void PlaceItem(InventoryItem inventoryItem, int posX, int posY)
    {
        RectTransform rectTransform = inventoryItem.GetComponent<RectTransform>();
        rectTransform.SetParent(this.rectTransform);

        for (int x = 0; x < inventoryItem.WIDTH; x++)
        {
            for (int y = 0; y < inventoryItem.HEIGHT; y++)
            {
                inventoryItemSlot[posX + x, posY + y] = inventoryItem;
            }
        }

        inventoryItem.onGridPositionX = posX;
        inventoryItem.onGridPositionY = posY;

        Vector2 position = CalculatePositionOnGrid(inventoryItem, posX, posY);

        rectTransform.localPosition = position;
    }

    public Vector2 CalculatePositionOnGrid(InventoryItem inventoryItem, int posX, int posY)
    {
        Vector2 position = new Vector2();
        position.x = posX * tileSizeWidth + tileSizeWidth * inventoryItem.WIDTH / 2;
        position.y = -(posY * tileSizeHeight + tileSizeHeight * inventoryItem.HEIGHT / 2);
        return position;
    }

    private bool OverlapCheck(int posX, int posY, int width, int height, ref InventoryItem overlapItem)
    {
        // dont like this overlapcheck, iterates through the whole grid, prob can do something else
        //nm this checks for more than one item, honestly not bad w/ limited inventory size
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (inventoryItemSlot[posX + x, posY + y] != null)
                {
                    if (overlapItem == null)
                    {
                        overlapItem = inventoryItemSlot[posX + x, posY + y];
                    }
                    else
                    {
                        if (overlapItem != inventoryItemSlot[posX + x, posY + y])
                        {
                            return false;
                        }
                    }
                }
            }
        }
        return true;
    }
    private bool CheckAvailableSpace(int posX, int posY, int width, int height)
    {
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (inventoryItemSlot[posX + x, posY + y] != null)
                {
                    return false;
                }
            }
        }
        return true;
    }


    //FOR IRREGULAR SHAPES (IMPLEMENT LATER(?))
    // private bool CheckAvailableSpace(int posX, int posY, InventoryItem item)
    // {
    //     for (int x = 0; x < item.Width; x++)
    //     {
    //         for (int y = 0; y < item.Height; y++)
    //         {
    //             if (!item.shape[x, y]) continue; // Skip unused parts of the shape

    //             if (inventoryItemSlot[posX + x, posY + y] != null)
    //             {
    //                 return false;
    //             }
    //         }
    //     }
    //     return true;
    // }

    bool PositionCheck(int posX, int posY)
    {
        if (posX < 0 || posY < 0)
        {
            return false;
        }

        if (posX >= gridSizeWidth || posY >= gridSizeHeight)
        {
            return false;
        }

        return true;
    }

    public bool BoundaryCheck(int posX, int posY, int width, int height)
    {
        if (PositionCheck(posX, posY) == false) { return false; }
        posX += width - 1;
        posY += height - 1;
        if (PositionCheck(posX, posY) == false) { return false; }
        return true;
    }

    internal InventoryItem GetItem(int x, int y)
    {
        return inventoryItemSlot[x, y];
    }

    public Vector2Int? FindSpaceForObject(InventoryItem itemToInsert)
    {
        // Can optimize
        int height = gridSizeHeight - itemToInsert.HEIGHT + 1;
        int width = gridSizeWidth - itemToInsert.WIDTH + 1;
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                if (CheckAvailableSpace(x, y, itemToInsert.WIDTH, itemToInsert.HEIGHT))
                {
                    return new Vector2Int(x, y);
                }
            }
        }

        return null;
    }
    
    public List<InventoryItem> GetAllInventoryItems()
    {
        List<InventoryItem> items = new();

        int width = inventoryItemSlot.GetLength(0);
        int height = inventoryItemSlot.GetLength(1);

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                InventoryItem item = inventoryItemSlot[x, y];
                if (item != null && !items.Contains(item)) // avoid duplicates from multi-slot items
                {
                    items.Add(item);
                }
            }
        }

        return items;
    }


}
