using System;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;

public class InventoryController : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    [HideInInspector]
    private ItemGrid selectedItemGrid;

    public ItemGrid SelectedItemGrid
    {
        get => selectedItemGrid;
        set
        {
            selectedItemGrid = value;
            inventoryHighlight.SetParent(value);
        }
    }

    InventoryItem selectedItem;
    InventoryItem overlapItem;
    RectTransform rectTransform;

    [SerializeField] List<ItemData> items;
    [SerializeField] GameObject itemPrefab;
    [SerializeField] Transform canvasTransform;

    InventoryHighlight inventoryHighlight;

    [SerializeField] InventoryDescription inventoryDescription;

    private void Awake()
    {
        inventoryHighlight = GetComponent<InventoryHighlight>();
        inventoryDescription = GetComponent<InventoryDescription>();
    }

    // Update is called once per frame
    private void Update()
    {
        ItemIconDrag();

        

        if (Input.GetKeyDown(KeyCode.Q))
        {
            if (selectedItem == null)
            {
            CreateRandomItem();           
            }
        }

        if (Input.GetKeyDown(KeyCode.W))
        {
            InsertRandomItem();
        }

        if (Input.GetKeyDown(KeyCode.R))
        {
            RotateItem();
        }

        if (selectedItemGrid == null)
        {
            inventoryHighlight.Show(false);
            // inventoryDescription.Show(false);
            return;
        }

        HandleHighlight();
        // HandleDescription();

        if (Input.GetMouseButtonDown(0))
        {
            LeftMouseButtonPress();

        }
    }

    private void RotateItem()
    {
        if (selectedItem == null) { return; }
        selectedItem.Rotate();
    }

    private void InsertRandomItem()
    {
        if (selectedItemGrid == null){ Debug.Log("selectedItemGrid is null in insertRandomItem"); return; }
        CreateRandomItem();
        InventoryItem itemToInsert = selectedItem;
        selectedItem = null;
        InsertItem(itemToInsert);
    }

    private void InsertItem(InventoryItem itemToInsert)
    {
        if (selectedItemGrid == null){ Debug.Log("selectedItemGrid is  null in insertItem"); return; }
        Vector2Int? posOnGrid = selectedItemGrid.FindSpaceForObject(itemToInsert);

        if (posOnGrid == null) { Debug.Log("posOnGrid is  null in insertItem"); return; }

        selectedItemGrid.PlaceItem(itemToInsert, posOnGrid.Value.x, posOnGrid.Value.y);
    }

    Vector2Int oldPosition;
    InventoryItem itemToHighlight;
    private void HandleHighlight()
    {
        
        Vector2Int positionOnGrid = GetTileGridPosition();
        if(oldPosition == positionOnGrid){ return; }
        HandleDescription(); 
        oldPosition = positionOnGrid;
        if (selectedItem == null)
        {
            itemToHighlight = selectedItemGrid.GetItem(positionOnGrid.x, positionOnGrid.y);
            if (itemToHighlight != null)
            {
                inventoryHighlight.Show(true);
                inventoryHighlight.SetSize(itemToHighlight);
                // inventoryHighlight.SetParent(selectedItemGrid);
                inventoryHighlight.SetPosition(selectedItemGrid, itemToHighlight);
            }
            else
            {
                inventoryHighlight.Show(false);
            }

        }
        else
        {
            inventoryHighlight.Show(selectedItemGrid.BoundaryCheck(
                positionOnGrid.x, positionOnGrid.y,
                selectedItem.WIDTH, selectedItem.HEIGHT)
                );
            inventoryHighlight.SetSize(selectedItem);
            // inventoryHighlight.SetParent(selectedItemGrid);
            inventoryHighlight.SetPosition(selectedItemGrid, selectedItem, positionOnGrid.x, positionOnGrid.y);
        }
    }
    InventoryItem itemToDescribe;
    private void HandleDescription()
    {
        Vector2Int positionOnGrid = GetTileGridPosition();
        if (oldPosition == positionOnGrid) { return; }
        // oldPosition = positionOnGrid;
        if (selectedItem == null)
        {
            itemToDescribe = selectedItemGrid.GetItem(positionOnGrid.x, positionOnGrid.y);
            if (itemToDescribe != null)
            {
                inventoryDescription.Show(true);
                inventoryDescription.SetSize(itemToDescribe);
                // inventoryDescription.SetParent(selectedItemGrid);
                inventoryDescription.SetPosition(selectedItemGrid, itemToDescribe);
                inventoryDescription.SetDescription(itemToDescribe);
            }
            else
            {
                inventoryDescription.Show(false);
            }

        }
        else
        {
            // Debug.Log("Inside the showing description part");
            inventoryDescription.Show(selectedItemGrid.BoundaryCheck(
                positionOnGrid.x, positionOnGrid.y,
                selectedItem.WIDTH, selectedItem.HEIGHT)
                );
            inventoryDescription.SetSize(selectedItem);
            // inventoryDescription.SetParent(selectedItemGrid);
            inventoryDescription.SetDescription(selectedItem);
            inventoryDescription.SetPosition(selectedItemGrid, selectedItem, positionOnGrid.x, positionOnGrid.y);
        }
    }


    public void CreateRandomItem()
    {
        InventoryItem inventoryItem = Instantiate(itemPrefab).GetComponent<InventoryItem>();
        selectedItem = inventoryItem;
        rectTransform = inventoryItem.GetComponent<RectTransform>();
        rectTransform.SetParent(canvasTransform);

        int selectedItemID = UnityEngine.Random.Range(0, items.Count);
        inventoryItem.Set(items[selectedItemID]);
    }

    private void LeftMouseButtonPress()
    {
        Vector2Int tileGridPosition = GetTileGridPosition();

        Debug.Log(selectedItemGrid.GetTileGridPosition(Input.mousePosition));
        if (selectedItem == null)
        {
            PickUpItem(tileGridPosition);
        }
        else
        {
            PlaceItem(tileGridPosition);
        }
    }

    private Vector2Int GetTileGridPosition()
    {
        Vector2 position = Input.mousePosition;
        if (selectedItem != null)
        {
            position.x -= (selectedItem.WIDTH - 1) * ItemGrid.tileSizeWidth / 2;
            position.y += (selectedItem.HEIGHT - 1) * ItemGrid.tileSizeHeight / 2;
        }

        return selectedItemGrid.GetTileGridPosition(position);
    }

    private void PlaceItem(Vector2Int tileGridPosition)
    {
        bool isPlaced = selectedItemGrid.PlaceItem(selectedItem, tileGridPosition.x, tileGridPosition.y, ref overlapItem);
        if (isPlaced)
        {
            selectedItem = null;
            if (overlapItem != null)
            {
                selectedItem = overlapItem;
                overlapItem = null;
                rectTransform = selectedItem.GetComponent<RectTransform>();
                // rectTransform.SetParent(canvasTransform);
                rectTransform.SetAsLastSibling();
            }
        }
    }

    private void PickUpItem(Vector2Int tileGridPosition)
    {
        selectedItem = selectedItemGrid.PickUpItem(tileGridPosition.x, tileGridPosition.y);
        if (selectedItem != null)
        {
            rectTransform = selectedItem.GetComponent<RectTransform>();
        }
    }

    private void ItemIconDrag()
    {
        if (selectedItem != null)
        {
            rectTransform.position = Input.mousePosition;
        }
    }
}
