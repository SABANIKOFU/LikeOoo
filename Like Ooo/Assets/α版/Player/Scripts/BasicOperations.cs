using System;
using System.Collections;
using UnityEngine;

public class BasicOperations : MonoBehaviour
{
    enum STATE
    {
        GROUNDED,   // 地面時
        AIRBORNE,   // 空中時
        GROWING     // 巨大時
    }

    [Header("ステート管理")]
    private STATE currentState = STATE.GROUNDED;

    [Header("プレイヤーの状態フラグ")]
    public bool isGround;
    public bool isTouchLeftWall;
    public bool isTouchRightWall;
    public bool isPushingWall;
    private bool isJumpping;
    public bool canClimb;

    [Header("プレイヤーの移動設定")]
    private float inputDir;
    [SerializeField] private float moveSpeed = 8f;
    [SerializeField] private float jumpForce = 10f;
    [SerializeField] private float coyoteTime = 0.2f;
    [SerializeField] private float coyoteTimeCounter = 0f;
    [SerializeField] private float jumpBufferTime = 0.1f;
    [SerializeField] private float jumpBufferCounter = 0f;

    public float realGravity;
    private float previousVelocityY;
    [SerializeField] private float reduceGravityTime = 0.2f;    // ジャンプ頂点で重力を減らす時間
    private Coroutine reduceGravityCoroutine;   // コルーチンを停止する用

    private SpriteRenderer spriteRenderer;  // プレイヤーの画像
    private Color originalColor;    // プレイヤーの色

    [Header("崖よじ登り・ずり落ち")]
    [SerializeField] private float slideSpeed = -2f;
    public Vector2 ledgeClimbOffset = new Vector2(0.6f, 1.2f);
    [SerializeField] private float climbDuration = 0.15f;


    [Header("死亡・復活設定")]
    public bool isDead;
    [SerializeField] private float respawnTime = 0.5f;
    private Vector2 lastSpawnPoint;
    public static event Action OnPlayerRespawn; // リスポーンイベント


    [Header("レイヤー")]
    public LayerMask groundLayer;

    [Header("当たり判定設定")]
    public float boxRayDistance = 0.15f;
    [SerializeField] private float rayDistance = 2f;
    public RaycastHit2D hitDown;
    public RaycastHit2D hitLeft;
    public RaycastHit2D hitRight;

    public Rigidbody2D rb;
    public BoxCollider2D col;
    private GrowBlink growBlink;

    [Header("仮のアイテムを試す")]
    private GetItem getItem;

    [Header("コライダー自動調整設定")]
    [SerializeField, Tooltip("キャラクターの本来の当たり判定サイズ")]
    private Vector2 targetColliderSize = new Vector2(1f, 1f);

    [SerializeField, Tooltip("角の丸みの大きさ")]
    private float targetEdgeRadius = 0.02f;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<BoxCollider2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        growBlink = GetComponent<GrowBlink>();
        getItem = GetComponent<GetItem>();

        // 角に丸みを持たせたコライダーのサイズを調節してプレイヤーに合わせる計算を行う
        AdjustCollider();

        originalColor = spriteRenderer.color;
        realGravity = rb.gravityScale;
        lastSpawnPoint = transform.position;
    }

    // Update is called once per frame
    void Update()
    {
        CheckSurroundings();
        JumpBuffer();

        // リスタート
        if (Input.GetKeyDown(KeyCode.R))
        {
            transform.position = lastSpawnPoint;
            // イベントを放送する
            OnPlayerRespawn?.Invoke();
        }

        if(Input.GetKeyDown(KeyCode.E))
        {
            if (!getItem.isWhite) return;

            getItem.UseWhiteItem();
        }

        //====================================================================//
        //     地面時の時                                                     //
        //====================================================================//
        if (currentState == STATE.GROUNDED)
        {
            // 地面にいるときに壁方向への入力を検知しないように初期化する
            isPushingWall = false;

            // 左右入力の検知だけはUpdateで行い、操作の感度を上げる
            inputDir = InputHorizontalMove();

            // コヨーテタイムが残っている限りジャンプできる
            if (coyoteTimeCounter > 0)
            {
                // ジャンプの先行入力がある時に即ジャンプを実行する
                if (Input.GetKeyDown(KeyCode.Space) || jumpBufferCounter > 0)
                {
                    isJumpping = true;
                    coyoteTimeCounter = 0f;
                    jumpBufferCounter = 0f;
                }
            }

            // ステート変更
            if (growBlink.isGrow) currentState = STATE.GROWING;
            else if (isGround == false) currentState = STATE.AIRBORNE;
        }
        //====================================================================//
        //     空中の時　                                                     //
        //====================================================================//
        else if (currentState == STATE.AIRBORNE)
        {
            // 空中での横移動とすべり落ち検知に使う
            inputDir = InputHorizontalMove();

            // ステート変更
            if (growBlink.isGrow) currentState = STATE.GROWING;
            else if (isGround) currentState = STATE.GROUNDED;
        }
        //====================================================================//
        //     巨大化の時                                                     //
        //====================================================================//
        else if (currentState == STATE.GROWING)
        {
            // 巨大化時に巨大化できないように初期化する
            isPushingWall = false;

            // ブリンク時は重力を切る
            if (growBlink.isKnockBucked)
            {
                rb.gravityScale = 0f;
            }
            else
            {
                rb.gravityScale = realGravity;
            }

            // ステート変更  
            if (growBlink.isGrow == false)
            {
                if (isGround) currentState = STATE.GROUNDED;
                else currentState = STATE.AIRBORNE;
            }
        }
        // デバッグ用のプレイヤーの色を変える関数
        UpdateDebugVisuals();
    }

    void FixedUpdate()
    {
        //====================================================================//
        //     地面の時　　                                                   //
        //====================================================================//
        if (currentState == STATE.GROUNDED)
        {
            // 左右移動入力を実行
            UpdateHorizontalMove(inputDir);

            if (isJumpping)
            {
                Jump();
            }

            // 地面にいる時は重力は変わらない
            rb.gravityScale = realGravity;

            // ジャンプの頂点での重力を減らすコルーチンが起動していたら止める
            if (reduceGravityCoroutine == null) return;
            StopCoroutine(reduceGravityCoroutine);
            reduceGravityCoroutine = null;
        }
        //====================================================================//
        //     空中の時　　                                                   //
        //====================================================================//
        else if (currentState == STATE.AIRBORNE)
        {
            //OnJumpApex();

            // 左右移動入力を実行
            UpdateHorizontalMove(inputDir);

            // 崖よじ登りは改良したほうが良いかも
            // よじ登る時の上への移動が定数だから崖の位置によって上の登り量が変わるとぽいかも
            // 今のままだと壁にめり込んでいるときがある
            if (isTouchLeftWall || isTouchRightWall)
            {
                SlipWall(inputDir);
            }
            else
            {
                canClimb = false;
            }
        }
    }

    private void AdjustCollider()
    {
        // 丸みをコライダーの設定に代入
        col.edgeRadius = targetEdgeRadius;

        // 本来のサイズから (Edge Radius × 2) を引き算する
        float calculatedX = targetColliderSize.x - (targetEdgeRadius * 2f);
        float calculatedY = targetColliderSize.y - (targetEdgeRadius * 2f);

        // 計算したコライダーのサイズに変更する
        col.size = new Vector2(calculatedX, calculatedY);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Damage"))
        {
            Debug.Log("Playerがトゲに当たりました");
            Die();
        }
    }

    public void Die()
    {
        if (isDead) return;
        isDead = true;

        // 動きを止める
        rb.linearVelocity = Vector2.zero;
        rb.gravityScale = 0f;

        // ヒットストップ
        Time.timeScale = 0f;

        // デバッグ演出：色を赤くする
        if (spriteRenderer != null) spriteRenderer.color = Color.red;


        Debug.Log("Player Died");

        StartCoroutine(RespawnRoutine());
    }

    public void SetSpawnPoint(Vector2 position)
    {
        lastSpawnPoint = position;
        Debug.Log("Spawn Point Updated: " + position);
    }

    private IEnumerator RespawnRoutine()
    {
        // 復活時間を待つ
        yield return new WaitForSecondsRealtime(respawnTime);

        // 復活処理
        transform.position = lastSpawnPoint;

        // 状態フラグをリセットする
        canClimb = false;
        isJumpping = false;
        isDead = false;

        // 重力と時間の流れを戻す
        rb.gravityScale = realGravity;
        Time.timeScale = 1f;

        // プレイヤーの色を戻す
        if (spriteRenderer != null) spriteRenderer.color = originalColor;

        // イベントを放送する
        OnPlayerRespawn?.Invoke();

        Debug.Log("Player Respawned");
    }

    // 方向と距離を渡すとプレイヤーからその方向にその距離BoxCastを飛ばして指定のLayerを探す
    public RaycastHit2D BoxCast(Vector2 Dir, float rayDistanse, LayerMask layer)
    {
        Vector2 size = col.size * transform.localScale * 0.9f;

        RaycastHit2D hit = Physics2D.BoxCast(transform.position, size, 0f, Dir, rayDistanse, layer);

        return hit;
    }
    void CheckSurroundings()
    {
         hitDown = BoxCast(Vector2.down, boxRayDistance, groundLayer);
         hitLeft = BoxCast(Vector2.left, boxRayDistance, groundLayer);
        hitRight = BoxCast(Vector2.right, boxRayDistance, groundLayer);

        // rayがぶつかっていたらtrue/空振りならfalse
        isGround = hitDown.collider != null;
        isTouchLeftWall = hitLeft.collider != null;
        isTouchRightWall = hitRight.collider != null;

        // 地面にいる間はコヨーテタイムカウンターは常に最大値に保つ
        if (isGround)
        {
            coyoteTimeCounter = coyoteTime;
        }
        else
        {
            coyoteTimeCounter -= Time.deltaTime;
        }
    }

    float InputHorizontalMove()
    {
        // 左右入力を検知
        float inputMove = Input.GetAxisRaw("Horizontal");

        return inputMove;
    }

    void UpdateHorizontalMove(float inputMove)
    {
        float velocity_x = inputMove * moveSpeed;

        rb.linearVelocity = new Vector2(velocity_x, rb.linearVelocity.y);
    }

    void SlipWall(float inputMove)
    {
        // 初期化する
        isPushingWall = false;

        // 壁方向に入力があればtrue
        isPushingWall = (isTouchRightWall && inputMove > 0) || (isTouchLeftWall && inputMove < 0);

        if (isPushingWall)
        {
            if (rb.linearVelocity.y < 0)
            {
                float slideSpeed = -2f; // ずり落ちる最大速度（マイナス値）

                // すべり落ち状態になったら壁にめり込みすぎないようにX座標の速度を0.1fにする
                // 0にしないのは壁との接触で押されたときに0だと壁とプレイヤーの隙間が空いたままになってしまうため
                if (inputMove > 0)
                {
                    // Mathf.Maxを使うことで、現在の落下速度と -2f を比べて「大きい方（ゼロに近い方）」を採用する
                    // 例: -10f（猛スピードで落下中）と -2f を比べると、-2f が採用されブレーキがかかる
                    rb.linearVelocity = new Vector2(0.1f, Mathf.Max(rb.linearVelocity.y, slideSpeed));
                }
                else
                {
                    rb.linearVelocity = new Vector2(-0.1f, Mathf.Max(rb.linearVelocity.y, slideSpeed));
                }
            }
            JudgeCanClimb();
        }
    }

    void Jump()
    {
        isJumpping = false;

        rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
    }

    void JumpBuffer()
    {
        // ジャンプの先行入力を実行する最大時間を代入
        if (Input.GetKeyDown(KeyCode.Space))
        {
            jumpBufferCounter = jumpBufferTime;
        }

        if (jumpBufferCounter > 0)
        {
            jumpBufferCounter -= Time.deltaTime;
        }
        else
        {
            jumpBufferCounter = 0;
        }
    }

    public void OnJumpApex()
    {
        float currentVelocityY = rb.linearVelocity.y;

        // 前のフレームのY軸上の速度が正/現在のフレームが負なら頂点だと推測できる
        if (previousVelocityY > 0f && currentVelocityY <= 0f)
        {
            Debug.Log("ジャンプの頂点に達しました");
            if (reduceGravityCoroutine != null) StopCoroutine(reduceGravityCoroutine);
            reduceGravityCoroutine = StartCoroutine(ReduceGravity());
        }

        previousVelocityY = currentVelocityY;
    }

    public IEnumerator ReduceGravity()
    {
        // ジャンプの頂点で重力を減らしてプレイヤーが入力の選択をする時間を与える
        rb.gravityScale = realGravity / 2;

        yield return new WaitForSeconds(reduceGravityTime);

        rb.gravityScale = realGravity;
    }

    void JudgeCanClimb()
    {
        //Debug.Log("よじ登り関数突入");

        Vector2 Dir = Vector2.zero;
        if (isTouchRightWall)
        {
            //Debug.Log("右よじ登り判定");
            Dir = Vector2.right;
        }
        else if (isTouchLeftWall)
        {
            //Debug.Log("左よじ登り判定");
            Dir = Vector2.left;
        }

        // よじ登れるかの判定Rayを飛ばす
        // プレイヤー１個分上までよじ登れる
        Vector2 rayPoint = (Vector2)transform.position + new Vector2(0f, 1.5f);
        RaycastHit2D judgeCanClimb = Physics2D.Raycast(rayPoint, Dir, rayDistance, groundLayer);

        // デバッグ用にRayが見える
        Debug.DrawRay(rayPoint, Dir * rayDistance, Color.red);

        canClimb = judgeCanClimb.collider == null;

        // Ratが壁にぶつかっていたら登れないので戻る
        if (judgeCanClimb.collider != null) return;

        //Debug.Log("よじ登り可能");

        Vector2 startPos = transform.position;

        if (jumpBufferCounter > 0f)
        {
            // ジャンプ入力を消費して、二重ジャンプを防ぐ
            jumpBufferCounter = 0f;
            Debug.Log("よじ登り成功");
            if (Dir == Vector2.right)
            {
                // 現在地によじ登り後の速度を足す
                Vector2 endPos = startPos + new Vector2(ledgeClimbOffset.x, ledgeClimbOffset.y);
                Vector2 upPos = new Vector2(startPos.x, endPos.y);

                StartCoroutine(ClimbEdge(startPos, endPos, upPos, climbDuration));
            }
            else if (Dir == Vector2.left)
            {
                // 左側によじ登るのでXはマイナスに
                Vector2 endPos = startPos + new Vector2(-ledgeClimbOffset.x, ledgeClimbOffset.y);
                Vector2 upPos = new Vector2(startPos.x, endPos.y);
                StartCoroutine(ClimbEdge(startPos, endPos, upPos, climbDuration));
            }
        }
    }

    IEnumerator ClimbEdge(Vector2 startPos, Vector2 endPos, Vector2 upPos, float duration)
    {
        // 重力・コライダーを切る
        rb.gravityScale = 0f;
        col.enabled = false;

        // 現在の慣性を消す
        rb.linearVelocity = Vector2.zero;

        float time = 0f;

        // まずは上方向への移動
        while (time < duration)
        {
            // time(経過時間)をduration(移動にかかる時間)で割って
            // Lerp関数で使うため0.0～1.0の割合を作る
            float t = time / duration;

            // 間の距離を直接割り出しているのでそのままpositionを動かす
            transform.position = Vector2.Lerp(startPos, upPos, t);
            time += Time.deltaTime;

            // 次のフレームまで待機
            yield return null;
        }
        // 最後に誤差を補正
        transform.position = upPos;

        time = 0f;

        // 横方向への移動
        while (time < duration)
        {
            float t = time / duration;

            transform.position = Vector2.Lerp(upPos, endPos, t);
            time += Time.deltaTime;

            yield return null;
        }
        transform.position = endPos;

        // 重力・コライダーをオンにする
        rb.gravityScale = realGravity;
        col.enabled = true;


    }

    private void UpdateDebugVisuals()
    {
        if (spriteRenderer == null || isDead) return;

        if (getItem.isWhite)
        {
            spriteRenderer.color = Color.white;
            return;
        }

        // 地面にいるときは色を変えない
        if (isGround)
        {
            spriteRenderer.color = originalColor;
            return;
        }

        if (canClimb)
        {
            // 壁よじ登りが可能な時に青色にする
            spriteRenderer.color = Color.blue;
            return;
        }
        else if (isTouchRightWall || isTouchLeftWall)
        {
            if (isPushingWall)
            {
                // 壁張り付き中は黄色にする
                spriteRenderer.color = Color.yellow;
                return;
            }
        }

        // 壁に接地していない時はオリジナルカラー
        spriteRenderer.color = originalColor;
    }
}
