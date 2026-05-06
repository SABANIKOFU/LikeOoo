using UnityEngine;

public enum ItemType
{
    None,
    Grow,
    White,
}

[CreateAssetMenu(fileName = "NewItemData", menuName = "Inventory/ItemData")]
public class ItemData : ScriptableObject
{
    public string itemName;     // アイテムの名前
    public Sprite icon;         // アイコン
    public ItemType itemType;   // アイテムの種類
}
