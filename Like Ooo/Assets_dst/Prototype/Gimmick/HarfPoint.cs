using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public class HarfPoint : MonoBehaviour
{
    [Header("Visual Settings")]
    public Color inactiveColor = Color.gray;
    public Color activeColor = Color.yellow;

    private SpriteRenderer sr;
    private bool isActive = false;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        GetComponent<BoxCollider2D>().isTrigger = true; // 自動でTriggerに設定
        if (sr != null) sr.color = inactiveColor;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            // プレイヤーの復活地点をこのオブジェクトの位置に更新
            PlayerController player = collision.GetComponent<PlayerController>();
            if (player != null)
            {
                // オブジェクトの中心位置を復活地点として登録
                player.SetSpawnPoint(transform.position);

                // 視覚的なフィードバック
                ActivateCheckpoint();
            }
        }
    }

    private void ActivateCheckpoint()
    {
        if (isActive) return;
        isActive = true;

        if (sr != null) sr.color = activeColor;
        Debug.Log("Respawn Point Activated!");

        // 他のチェックポイントを非アクティブにしたい場合は、ここで通知を送る処理を追加可能
    }
}