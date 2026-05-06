using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class GreenBlock : MonoBehaviour, IInteractableBlock
{
    private Rigidbody2D rb;
    private Vector2 initialPosition; // 【追加】初期位置を記憶する変数

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.constraints = RigidbodyConstraints2D.FreezePositionX | RigidbodyConstraints2D.FreezeRotation;
    }

    void Start()
    {
        // 【追加】ゲーム開始時の座標を保存
        initialPosition = transform.position;
    }

    // 【追加】オブジェクトがアクティブになった時にイベントを登録
    private void OnEnable()
    {
        PlayerController.OnPlayerRespawn += ResetState;
    }

    // 【追加】オブジェクトが非アクティブになった時にイベントを解除（エラー防止）
    private void OnDisable()
    {
        PlayerController.OnPlayerRespawn -= ResetState;
    }

    // 【追加】プレイヤー復活時に呼ばれるリセット処理
    private void ResetState()
    {
        StopAllCoroutines(); // 動いている途中の判定コルーチンを強制停止

        // 位置と速度を初期状態に戻す
        transform.position = initialPosition;
        rb.linearVelocity = Vector2.zero;
        rb.constraints = RigidbodyConstraints2D.FreezePositionX | RigidbodyConstraints2D.FreezeRotation;
    }

    public void Interact(Vector2 dirToGimmick, float force)
    {
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        rb.linearVelocity = Vector2.zero;

        // 【修正ポイント1】斜め下に力が加わって地面に押し付けられるのを防ぐため、
        // プレイヤーがいる方向の逆（純粋な左右のどちらか）に力を補正する。
        // （dirToGimmick.x がプラスなら右(1)、マイナスなら左(-1)へ）
        float forceDirectionX = Mathf.Sign(dirToGimmick.x);
        Vector2 horizontalForceDir = new Vector2(forceDirectionX, 0f);

        // 水平方向のみに力を加える
        rb.AddForce(horizontalForceDir * force, ForceMode2D.Impulse);

        Debug.Log("Green Block Knocked Back Horizontally!");

        StopAllCoroutines();
        StartCoroutine(LockWhenStopped());
    }

    private IEnumerator LockWhenStopped()
    {
        // 【修正ポイント2】力が加わって確実に動き出すまで少し待機（0.1秒）
        // これにより、初速が出る前に一瞬でロックされてしまうバグを防ぐ
        yield return new WaitForSeconds(0.1f);

        // Y軸（落下）の速度は無視して、X軸（横移動）の速度だけで停止判定を行う
        while (Mathf.Abs(rb.linearVelocity.x) > 0.05f)
        {
            yield return null;
        }

        rb.linearVelocity = Vector2.zero;
        rb.constraints = RigidbodyConstraints2D.FreezePositionX | RigidbodyConstraints2D.FreezeRotation;
    }
}