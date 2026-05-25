using UnityEngine;
using System.Collections;

public class EraceBlock : MonoBehaviour
{
    [Header("設定")]
    private bool isErace;
    [SerializeField] private float respawnTime = 3f;
    private BoxCollider2D boxCollider;
    private SpriteRenderer spriteRenderer;

    [Header("死亡判定設定")]
    [SerializeField] private LayerMask playerLayer;

    [Header("連動設定")]
    [SerializeField] private LayerMask eraceBlockLayer; // Inspectorで設定する対象レイヤー
    [SerializeField] private float rayDistance = 0.1f;  // Rayの長さ（解説後述）
    void Start()
    {
        boxCollider = GetComponent<BoxCollider2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();    
    }

 

    public void Erace()
    {
        DebugUtil.Log("消える関数突入");

        isErace = true;

        // コライダーとレンダラーを切る
        boxCollider.enabled = false;
        spriteRenderer.enabled = false;

        // 上下左右にRayを飛ばし、隣接ブロックを連鎖的に消す
        CheckAndEraseAdjacent(Vector2.up);
        CheckAndEraseAdjacent(Vector2.down);
        CheckAndEraseAdjacent(Vector2.left);
        CheckAndEraseAdjacent(Vector2.right);

        // リスポーンコルーチン開始
        StartCoroutine(ReSpawnBlock());
    }

    private void CheckAndEraseAdjacent(Vector2 direction)
    {
        Vector2 size = boxCollider.size * transform.localScale * 0.9f;

        // 指定方向にRayを飛ばす
        RaycastHit2D hit = Physics2D.BoxCast(transform.position, size, 0f, direction, rayDistance, eraceBlockLayer);

        // 当たったオブジェクトがあってEraceBlockだったら
        if (hit.collider != null && hit.collider.CompareTag("EraceBlock"))
        {
            // そのオブジェクトからEraceBlockスクリプトを取得
            EraceBlock adjacentBlock = hit.collider.GetComponent<EraceBlock>();

            if (adjacentBlock != null)
            {
                // 隣のブロックの消去関数を呼び出す
                adjacentBlock.Erace();
            }
        }
    }

    private IEnumerator ReSpawnBlock()
    {
        DebugUtil.Log("リスポーンコルーチン突入");

        yield return new WaitForSeconds(respawnTime);

        //  自オブジェクトの範囲内にプレイヤーがいるかを確認
        CheckPlayerOverlap();

        boxCollider.enabled = true;
        spriteRenderer.enabled = true;

        isErace=false;
    }

    private void CheckPlayerOverlap()
    {
        DebugUtil.Log("範囲内にプレイヤーがいるか確認");

        // 自分のBoxColliderと同じサイズと位置で重なりをチェック
        Vector2 size = boxCollider.size * transform.localScale * 0.9f;
        Collider2D hit = Physics2D.OverlapBox(transform.position, size, 0f, playerLayer);

        if (hit != null)
        {
            // プレイヤーのスクリプトを取得して死亡処理を呼ぶ
            BasicOperations player = hit.GetComponent<BasicOperations>();
            if (player != null)
            {
                DebugUtil.Log("ブロックに押しつぶされました！");
                player.Die();
            }
        }
    }
}
