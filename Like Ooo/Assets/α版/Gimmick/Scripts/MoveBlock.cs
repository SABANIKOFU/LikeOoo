using System.Drawing;
using Unity.VisualScripting;
using UnityEngine;
using System.Collections;


public class MoveBlock : MonoBehaviour
{
    [Header("吹っ飛び設定")]
    private Vector2 moveDir;
    public bool knockbuckHorizontalDir;
    public bool knockbuckVerticalDir;
    public float hitBlockForce = 10f; // 動かせるブロックが飛ばされる力

    [Header("状態フラグ")]
    public bool isGround;
    public bool isPlayerOnTop;
    public bool isKnockBucking; // 吹っ飛んでいる最中かどうか

    [Header("リセット設定")]
    private Vector2 initialPos;

    [Header("当たり判定設定")]
    [SerializeField] private GameObject player;
    [SerializeField] private float boxRayDistance = 0.1f;
    [SerializeField] private LayerMask playerLayer;
    [SerializeField] private LayerMask groundLayer;

    [Header("他スクリプト")]
    public Rigidbody2D rb;
    private BoxCollider2D col;
    private GrowBlink growBlink;
    private BasicOperations basicOps;


    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<BoxCollider2D>();
        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.constraints = RigidbodyConstraints2D.FreezePositionX | RigidbodyConstraints2D.FreezeRotation;

        initialPos = transform.position;
    }

    private void OnEnable()
    {
        // イベントを受信する
        BasicOperations.OnPlayerRespawn += ResetPos;
    }

    private void OnDisable()
    {
        // イベントを解除する
        BasicOperations.OnPlayerRespawn -= ResetPos;
    }

    // Update is called once per frame
    void Update()
    {
        CheckGround();
        CheckSurroundsPlayer();
        
        // 吹っ飛び中でない時だけ、状態に合わせて物理挙動を切り替える
        if (!isKnockBucking)
        {
            // 「上にプレイヤーがいる ＆ 接地している」条件
            if (isGround && isPlayerOnTop)
            {
                // 動かざる石（完全な足場）にする
                rb.bodyType = RigidbodyType2D.Kinematic;
                rb.linearVelocity = Vector2.zero;
            }
            else
            {
                // それ以外の時は重力や物理演算を有効にする
                rb.bodyType = RigidbodyType2D.Dynamic;
            }
        }
    }

    void ResetPos()
    {
        // 位置と速度を初期状態に戻す
        transform.position = initialPos;
        rb.linearVelocity = Vector2.zero;
        rb.constraints = RigidbodyConstraints2D.FreezePositionX | RigidbodyConstraints2D.FreezeRotation;
    }

    private void CheckGround()
    {
        Vector2 size = col.bounds.size;
        // 角の誤判定を防ぐために横幅を少し（10%）削る
        Vector2 checkSize = new Vector2(size.x * 0.9f, size.y);

        RaycastHit2D hitDown = Physics2D.BoxCast(transform.position, checkSize, 0f, Vector2.down, boxRayDistance, groundLayer);

        isGround = hitDown.collider != null;
    }

    private void CheckSurroundsPlayer()
    {
        Vector2 size = col.bounds.size;

        // 上方向の判定（上にプレイヤーがいるか）
        Vector2 verticalBoxSize = new Vector2(size.x * 0.9f, size.y);
        RaycastHit2D hitUp = Physics2D.BoxCast(transform.position, verticalBoxSize, 0f, Vector2.up, boxRayDistance, playerLayer);

        // 上にプレイヤーがいれば true になる
        isPlayerOnTop = hitUp.collider != null;

        RaycastHit2D hitDown = Physics2D.BoxCast(transform.position, verticalBoxSize, 0f, Vector2.down, boxRayDistance, playerLayer);
        RaycastHit2D hitLeft = Physics2D.BoxCast(transform.position, verticalBoxSize, 0f, Vector2.left, boxRayDistance, playerLayer);
        RaycastHit2D hitRight = Physics2D.BoxCast(transform.position, verticalBoxSize, 0f, Vector2.right, boxRayDistance, playerLayer);

        if(hitRight.collider != null)
        {
            player = hitRight.collider.gameObject;
        }
        else if (hitLeft.collider != null)
        {
            player = hitLeft.collider.gameObject;
        }
        else if(hitUp.collider != null)
        {
            player = hitUp.collider.gameObject;
        }
        else if(hitDown.collider != null)
        {
            player = hitDown.collider.gameObject;
        }


        if (player != null && basicOps == null)
        {
            basicOps = player.GetComponent<BasicOperations>();
            Debug.Log("スクリプトを取得しました");
        }
    }

    public void CalcMoveDir(Transform playerTransform)
    {
        Debug.Log("動く方向計算開始");


        // プレイヤーから見たこのオブジェクトの方向のみを計算
        Vector2 direction = -(playerTransform.position - transform.position).normalized;

        // 上下左右のみに飛ぶようにしたいのでMathf.Absを使って補正する
        if (Mathf.Abs(direction.x) > Mathf.Abs(direction.y))
        {
            // Xの方が大きい（横長）なので、左右のどちらかに確定
            // Mathf.Sign を使って、右(1)か左(-1)かを判定して Vector2 を作る
            moveDir = new Vector2(Mathf.Sign(direction.x), 0f);
        }
        else
        {
            // Yの方が大きい（縦長）なので、上下のどちらかに確定
            // 上(1)か下(-1)かを判定する
            moveDir = new Vector2(0f, Mathf.Sign(direction.y));
            
        }

        // 吹っ飛ばしコルーチン
        StartCoroutine(KnockBuckBlock());
    }

    private void DeletePhsics()
    {
        if(isKnockBucking)
        {
           
        }
        else
        {
           
        }
    }

    private IEnumerator KnockBuckBlock()
    {
        Debug.Log("吹き飛ばされ開始");

        if (moveDir == Vector2.zero) yield break;

        // 吹っ飛び開始（UpdateでのKinematic化を一時的に防ぐ）
        isKnockBucking = true;

        ////吹っ飛び中はプレイヤーとの物理判定を消せば押せなくなるかも
        //if (basicOps != null)
        //{
        //    Physics2D.IgnoreCollision(basicOps.col, col, true);
        //    Debug.Log("物理判定を消しました");
        //}

        if (moveDir.y < 0 && isGround)
        {
            Debug.Log("地面に叩きつけられようとしています。固定化して物理干渉を消します。");
            rb.constraints = RigidbodyConstraints2D.FreezeAll;
            rb.bodyType = RigidbodyType2D.Kinematic;
            isKnockBucking = false;

            // 飛ばす処理を行わずに終了
            yield break;
        }

        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;

        // 吹っ飛ばされる
        rb.AddForce(hitBlockForce * moveDir, ForceMode2D.Impulse);

        yield return new WaitForFixedUpdate();


        // 動きがほぼ止まったらモードを戻して固定化する
        while (rb.linearVelocity.sqrMagnitude > 0.1f)
        {
            yield return null;
        }

        // 完全に止める
        rb.linearVelocity = Vector2.zero;

        
        // 重力を反映させるためX軸とZ軸だけ固定する
        rb.constraints = RigidbodyConstraints2D.FreezePositionX | RigidbodyConstraints2D.FreezeRotation;


        //// 物理判定を戻す
        //if (basicOps != null)
        //{
        //    Physics2D.IgnoreCollision(basicOps.col, col, false);
        //    player = null;
        //    basicOps = null;
        //    Debug.Log("物理判定を戻しました");
        //}

        // 吹っ飛び終了（Updateでの監視を再開）
        isKnockBucking = false;
    }
}
