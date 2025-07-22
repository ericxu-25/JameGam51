using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class InventoryDescription : MonoBehaviour
{
    public TextMeshProUGUI infoText;
    public RectTransform descriptionBox;

    private string description;
    public void Show(bool b)
    {
        descriptionBox.gameObject.SetActive(b);
    }

    public void SetSize(InventoryItem targetItem)
    {
        Vector2 size = new Vector2();
        size.x = 3 * ItemGrid.tileSizeWidth;
        size.y = 2 * ItemGrid.tileSizeHeight;

        // Optionally adjust based on text length
        infoText.ForceMeshUpdate();
        Vector2 textSize = infoText.GetRenderedValues(false);
        size.x = Mathf.Max(size.x, textSize.x + 20); // Add padding
        size.y = Mathf.Max(size.y, textSize.y + 20);

        descriptionBox.sizeDelta = size;
        // Vector2 size = new Vector2();
        // size.x = targetItem.WIDTH * ItemGrid.tileSizeWidth;
        // size.y = targetItem.HEIGHT * ItemGrid.tileSizeHeight;
        // descriptionBox.sizeDelta = size;
    }

    public void SetDescription(InventoryItem targetItem)
    {
        description = $"Attack: {targetItem.AttackDmg()} and Heal: {targetItem.regenHP()}";
        infoText.text = description;
    }

    public void SetPosition(ItemGrid targetGrid, InventoryItem targetItem)
    {
        Vector2 pos = targetGrid.CalculatePositionOnGrid(
            targetItem,
            targetItem.onGridPositionX,
            targetItem.onGridPositionY);
        pos += new Vector2(ItemGrid.tileSizeWidth * 3, -ItemGrid.tileSizeHeight);
        descriptionBox.localPosition = pos;
        // SetTextPosition();
    }

    public void SetPosition(ItemGrid targetGrid, InventoryItem targetItem, int posX, int posY)
    {
        SetParent(targetGrid);
        Vector2 pos = targetGrid.CalculatePositionOnGrid(
            targetItem,
            posX,
            posY);
        pos += new Vector2(ItemGrid.tileSizeWidth * 3, -ItemGrid.tileSizeHeight);
        descriptionBox.localPosition = pos;
        // SetTextPosition();
    }

    public void SetParent(ItemGrid targetGrid)
    {
        if (targetGrid == null) { return; }
        descriptionBox.SetParent(targetGrid.GetComponent<RectTransform>());
    }

    public void SetTextPosition()
    {
        RectTransform rt = infoText.rectTransform;
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;

    }
}
