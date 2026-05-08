using System;
using UnityEngine;

public class RespawnPoint : MonoBehaviour
{
    [Header("アイテム取得設定")]
    public GameObject giveItem;
    public int giveItemNumber;
    private GetItem getItem;

    [Header("見た目の設定")]
    public Color inactiveColor = Color.gray;
    public Color activeColor = Color.yellow;

    private SpriteRenderer sr;
    private bool isActive = false;

    // 他のリスポーン地点を非アクティブにするためのイベント
    public static event Action<RespawnPoint> OnAnyCheckpointActivated;
    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        GetComponent<BoxCollider2D>().isTrigger = true; // 自動でTriggerに設定
        if (sr != null) sr.color = inactiveColor;
    }

    private void OnEnable()
    {
        OnAnyCheckpointActivated += HandleAnyCheckpointActivated;
    }

    // イベントの購読解除
    private void OnDisable()
    {
        OnAnyCheckpointActivated -= HandleAnyCheckpointActivated;
    }

    private void HandleAnyCheckpointActivated(RespawnPoint activatedPoint)
    {
        // 放送されたのが「自分以外」のオブジェクトだったら、自分をオフにする
        if (activatedPoint != this)
        {
            DeactivateCheckpoint();
        }
    }
    private void OnTriggerEnter2D(Collider2D collision)
    {
        // すでにアクティブなら戻る
        if (isActive) return; 

        if (collision.CompareTag("Player"))
        {
            GameObject player = collision.gameObject;

            //============================================================//
            //     プレイヤーの復活地点をこのオブジェクトの位置に更新     //
            //============================================================//
            BasicOperations basicOps = player.GetComponent<BasicOperations>();
            if (basicOps != null)
            {
                // オブジェクトの中心位置を復活地点として登録
                basicOps.SetSpawnPoint(transform.position);
            }

            //============================================================//
            //     プレイヤーにアイテムを与える                           //
            //============================================================//
            getItem = player.GetComponent<GetItem>();

            // アイテムが登録されていないまたはgetItemが取得できないなら戻る
            if (giveItem != null && getItem != null)
            {
                for (int i = 0; i < giveItemNumber; i++)
                {
                    getItem.PushItem(giveItem);
                }
            }

            // 視覚的なフィードバック
            ActivateCheckpoint();
        }
    }

    private void ActivateCheckpoint()
    {
        if (isActive) return;
        isActive = true;

        if (sr != null) sr.color = activeColor;
        Debug.Log("Respawn Point Activated!");

        // アクティブになったことを放送する
        OnAnyCheckpointActivated?.Invoke(this);
    }

    private void DeactivateCheckpoint()
    {
        isActive = false;
        if (sr != null) sr.color = inactiveColor;
    }
}
