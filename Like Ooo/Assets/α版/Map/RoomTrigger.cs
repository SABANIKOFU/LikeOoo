using System;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public class RoomTrigger : MonoBehaviour
{
    [Tooltip("この部屋でのカメラの描画サイズ（Orthographic Size）")]
    public float cameraSize = 5f;

    private BoxCollider2D col;

    private bool hasLoggedPlayer;   // プレイヤーが部屋に入ったことを一度だけログに出すためのフラグ

    [Header("ステージ内のアイテム")]
    public List<GameObject> stageItemes;

    private void Awake()
    {
        col = GetComponent<BoxCollider2D>();
        stageItemes = new List<GameObject>();
    }

    private void Reset()
    {
        // アタッチ時に自動的にTriggerをオンにする
        GetComponent<BoxCollider2D>().isTrigger = true;
    }

    private void OnTriggerStay2D(Collider2D collision)
    {
        // プレイヤーが部屋に入ったら、CameraControllerに新しい位置とサイズを渡す
        if (collision.CompareTag("Player"))
        {
            if (!hasLoggedPlayer)
            {
                DebugUtil.Log("プレイヤー発見");
                hasLoggedPlayer = true;
            }

            if (CameraController.Instance != null)
            {
                // コライダーのワールド空間での中心座標を取得してカメラに渡す
                Vector3 centerPosition = col.bounds.center;

                CameraController.Instance.MoveToRoom(centerPosition, cameraSize);
            }
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        // プレイヤーが部屋から出たら、フラグをリセットして次回入ったときにログを出すようにする
        if (collision.CompareTag("Player"))
        {
            hasLoggedPlayer = false;
        }
    }

    // ステージ内のアイテムを登録する
    private void OnTriggerEnter2D(Collider2D collision)
    {
       
        if (collision.gameObject.layer == LayerMask.NameToLayer("Item"))
        {
             // 同じアイテムなら飛ばす
            if (stageItemes.Contains(collision.gameObject)) return;

            // ステージアイテムをリストに入れる
            stageItemes.Add(collision.gameObject);
            DebugUtil.Log("アイテムを追加します");
        }
    }

    // 【開発支援】エディタ上で部屋の範囲とカメラ位置を視覚化する
    private void OnDrawGizmos()
    {
        BoxCollider2D gizmoCol = GetComponent<BoxCollider2D>();
        if (gizmoCol != null)
        {
            // 部屋の判定エリアを緑色の半透明で表示（boundsを使うとより正確に描画できます）
            Gizmos.color = new Color(0, 1, 0, 0.2f);
            Gizmos.DrawCube(gizmoCol.bounds.center, gizmoCol.bounds.size);

            // カメラの目標位置（当たり判定の中心）を赤いワイヤーフレームで表示
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(gizmoCol.bounds.center, 0.5f);
        }
    }  
}