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
    public bool iswhite;    // 仮のアイテム用

    //-----他スクリプト-----//
    private RoomTrigger roomTrigger;

    void Awake()
    {
        canGrow = false;
        iswhite = false;
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

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // 現在のステージのRoomTriggerを取得する
        if (collision.gameObject.CompareTag("Stage"))
        {
            GameObject currentStage = collision.gameObject;
            roomTrigger = currentStage.GetComponent<RoomTrigger>();
        }

        // 衝突した相手がアイテムならスタックに入れる
        if (collision.gameObject.layer == LayerMask.NameToLayer("Item"))
        {
            // インベントリの上限に達していたら戻る
            if (inventory.Count >= maxInventory)
            {
                Debug.Log("インベントリがいっぱいです");
                return;
            }

            inventory.Push(collision.gameObject);
            Debug.Log("アイテムを取得しました");

            // アイテムをオフにする
            collision.gameObject.SetActive(false);

            IdentifyItem();
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
        Debug.Log("リセット関数突入");
        foreach (GameObject item in roomTrigger.stageItemes)
        {
            item.SetActive(true);
            Debug.Log("アイテムを戻しました");
        }
    }
    void ResetFlag()
    {
        canGrow = false;
        iswhite = false;
    }
    void IdentifyItem()
    {
        // インベントリ内にアイテムがなければ戻る
        if (inventory.Count == 0) return;

        // アイテムの効果を初期化
        ResetFlag();

        // スタックからアイテムを取り出して種類を特定する

        GameObject currentItem = inventory.Peek();

        //==================================//
        //     巨大化アイテム               //
        //==================================//
        if (inventory.Peek().CompareTag("GrowItem"))
        {
            canGrow = true;
            Debug.Log("巨大化アイテムでした");
        }
        //==================================//
        //     アイテムが増えるごとに追加   //
        //==================================//
        else if (inventory.Peek().CompareTag("WhiteItem"))
        {
            iswhite = true;
            Debug.Log("白いアイテムでした");
        }
    }

    // GrowBlink側で呼び出す
    public void UseGrowItem()
    {
        // スタックから取り出したアイテムが巨大化アイテム出ないなら戻る
        if (!inventory.Peek().CompareTag("GrowItem")) return;

        // 巨大化出来ないようにする
        canGrow = false;

        // アイテムを消費する
        inventory.Pop();
        usedItem = true;
    }

    public void UseWhiteItem()
    {
        if (!inventory.Peek().CompareTag("WhiteItem")) return;

        iswhite = false;

        inventory.Pop();
        usedItem = true;
    }
}
