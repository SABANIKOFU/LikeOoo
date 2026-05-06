using UnityEngine;

public class ItemIdentifier : MonoBehaviour
{
    // このアイテムがどのアイテムかを識別するためのスクリプト
    [Tooltip("ItemData をインポート")]
    public ItemData data;

    private void Awake()
    {
        // ItemDataが設定されていない場合、警告を出す
        if (data == null)
        {
            Debug.LogWarning("ItemDataが設定されていません: " + gameObject.name);
        }

        // アイテムのタイプがNoneの場合、警告を出す
        if (data != null && data.itemType == ItemType.None)
        {
            Debug.LogWarning("ItemTypeがNoneに設定されています: " + gameObject.name);
        }
    }
}
