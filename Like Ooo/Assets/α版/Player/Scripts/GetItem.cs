using System.Collections.Generic;
using System.ComponentModel;
using UnityEngine;

public class GetItem : MonoBehaviour
{
    //-----インベントリスタック設定-----//
    public int maxInventory;            // 収納できるアイテムの数
    public Stack<GameObject> inventory;

    //-----アイテムリスト-----//
    [SerializeField] private List<GameObject> itemList;

    //-----アイテムの効果フラグ-----//
    private bool usedItem;  // アイテムを使ったときにtrue
    public bool canGrow;    // 巨大化アイテム
    public bool isWhite;    // 仮のアイテム用

    //-----他スクリプト-----//
    private RoomTrigger roomTrigger;

    void Awake()
    {
        canGrow = false;
        isWhite = false;
        inventory = new Stack<GameObject>();
    }

    private void OnEnable()
    {
        // イベントを受信する
        BasicOperations.OnPlayerRespawn += EraceItem;
        CameraController.MoveCamera += ResetItem;
    }

    private void OnDisable()
    {
        // イベントを解除する
        BasicOperations.OnPlayerRespawn -= EraceItem;
        CameraController.MoveCamera -= ResetItem;
    }

    void Update()
    {
        // アイテムが使われたら次のアイテムを特定する
        if (usedItem)
        {
            usedItem = false;
            IdentifyItem();
        }
    }

    public void PushItem(GameObject item)
    {
        // インベントリの上限に達していたら戻る
        if (inventory.Count >= maxInventory)
        {
            DebugUtil.Log("インベントリがいっぱいです");
            return;
        }

        inventory.Push(item);
        DebugUtil.Log("アイテムを取得しました");

        // アイテムをオフにする
        item.SetActive(false);

        IdentifyItem();
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // 衝突した相手がアイテムならスタックに入れる
        if (collision.gameObject.layer == LayerMask.NameToLayer("Item"))
        {
            PushItem(collision.gameObject);
        }

        // 現在のステージのRoomTriggerを取得する
        if (collision.gameObject.CompareTag("Stage"))
        {
            GameObject currentStage = collision.gameObject;
            roomTrigger = currentStage.GetComponent<RoomTrigger>();
        }
    }

    void EraceItem()
    {
        ResetItem();
        ResetFlag();
        inventory.Clear();
    }

    // ステージ内のアイテムをすべてオンにする
    void ResetItem()
    {
        DebugUtil.Log("リセット関数突入");
        foreach (GameObject item in roomTrigger.stageItemes)
        {
            item.SetActive(true);
            DebugUtil.Log("アイテムを戻しました");
        }
    }
    void ResetFlag()
    {
        canGrow = false;
        isWhite = false;
    }
    void IdentifyItem()
    {
        // インベントリ内にアイテムがなければ戻る
        if (inventory.Count == 0) return;

        // アイテムの効果を初期化
        ResetFlag();

        // スタックからアイテムを取り出して種類を特定する
        GameObject currentItem = inventory.Peek();
        ItemIdentifier id = currentItem.GetComponent<ItemIdentifier>();

        //==================================//
        //     巨大化アイテム               //
        //==================================//
        if (id != null)
        {
            if (id.data.itemType == ItemType.Grow)
            {
                canGrow = true;
                DebugUtil.Log("巨大化アイテムでした");
            }
            //==================================//
            //     アイテムが増えるごとに追加   //
            //==================================//
            else if (id.data.itemType == ItemType.White)
            {
                isWhite = true;
                DebugUtil.Log("白いアイテムでした");
            }
        }
    }

    // GrowBlink側で呼び出す
    public void UseGrowItem()
    {
        // スタックから取り出したアイテムが巨大化アイテムでないなら戻る
        if (!(inventory.Peek().GetComponent<ItemIdentifier>().data.itemType == ItemType.Grow)) return;

        // 巨大化出来ないようにする
        canGrow = false;

        // アイテムを消費する
        inventory.Pop();
        usedItem = true;
    }

    public void UseWhiteItem()
    {
        // スタックから取り出したアイテムが白いアイテムでないなら戻る
        if (!(inventory.Peek().GetComponent<ItemIdentifier>().data.itemType == ItemType.White)) return;

        isWhite = false;

        inventory.Pop();
        usedItem = true;
    }
}