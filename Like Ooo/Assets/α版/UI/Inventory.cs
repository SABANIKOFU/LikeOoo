using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class Inventory : MonoBehaviour
{
    [Header("インベントリ設定")]
    [SerializeField] private GameObject player;
    private GetItem getItem;

    [SerializeField] private List<Image> itemSlots;

    void Start()
    {
        player = GameObject.FindWithTag("Player");
        getItem = player.GetComponent<GetItem>();
    }

    // Update is called once per frame
    void Update()
    {
        UpdataInventoryUI();
    }

    private void UpdataInventoryUI()
    {
        // 全てのスロットを非表示にする
        foreach (var slot in itemSlots)
        {
            slot.enabled = false;
            slot.sprite = null;
        }

        // スタックの中身を配列として取得
        GameObject[] currentItems = getItem.inventory.ToArray();

        // スタックの中身にあるアイテムの数だけ、スロットにアイコンを表示する
        for (int i = 0; i < currentItems.Length; i++)
        {
            if (i >= itemSlots.Count) break;    // スロット数以上のアイテムは表示しない

            ItemIdentifier id = currentItems[i].GetComponent<ItemIdentifier>();
            if (id != null && id.data != null)
            {
                itemSlots[i].sprite = id.data.icon; // ItemDataからアイコンを取得してスロットに設定
                itemSlots[i].enabled = true;        // アイコンを表示する
            }
        }
    }
}