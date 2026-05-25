using System.Collections;
using UnityEngine;
using System;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{
    // 【追加】他スクリプトから購読できる「復活時イベント」
    public static event Action OnPlayerRespawn;

    [Header("Movement (Snappy)")]
    public float moveSpeed = 8f;
    public float jumpForce = 12f;
    public float wallSlideSpeed = 2f;
    public float maxFallSpeed = 15f;

    [Header("Explosive Knockback")]
    public float knockbackSpeed = 25f;
    public float knockbackDistance = 3f;
    public float slowTimeScale = 0.2f;
    public Vector3 growScale = new Vector3(2f, 2f, 1f);

    [Header("Ledge Climb")]
    public float ledgeClimbDuration = 0.15f;
    public Vector2 ledgeClimbOffset = new Vector2(0.6f, 1.2f);

    [Header("Physics & Layers")]
    public LayerMask groundLayer;
    public LayerMask gimmickLayer;
    public LayerMask blueBlockPieceLayer;

    [Header("Death & Respawn")]
    public string damageTag = "Damage";
    public float respawnDelay = 0.5f;
    private Vector2 lastSpawnPoint;
    private bool isDead = false;

    [Header("Debug & Visuals")]
    [Tooltip("スローモーション時に表示する画面マスク（Canvas上のImage等）")]
    public GameObject slowMoMask;
    private SpriteRenderer spriteRenderer;
    private Color originalColor;

    private Rigidbody2D rb;
    private CapsuleCollider2D col;
    private Vector3 originalScale;
    private float originalGravity;

    private bool isKnockedBack = false;
    private bool isGrounded = false;
    private bool isTouchingWallLeft = false;
    private bool isTouchingWallRight = false;
    private bool isWallSliding = false;
    private bool isClimbing = false;

    private GameObject contactedGimmick = null;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<CapsuleCollider2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();

        originalScale = transform.localScale;
        originalGravity = rb.gravityScale;

        lastSpawnPoint = transform.position; // 初期位置を最初の復活地点にする

        // デバッグ用：元の色を保存し、マスクを非表示にしておく
        if (spriteRenderer != null) originalColor = spriteRenderer.color;
        if (slowMoMask != null) slowMoMask.SetActive(false);
    }

    void Update()
    {
        if (isDead) return; // 死亡中は入力を受け付けない

        CheckSurroundings();

        if (rb.linearVelocity.y < -maxFallSpeed)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, -maxFallSpeed);
        }

        if (!isKnockedBack && !isClimbing)
        {
            HandleMovement();
            HandleJumpAndClimb();
        }

        HandleTimeAndGrowth();
        UpdateDebugVisuals();
    }

    // RoomTriggerから呼ばれる復活地点の更新
    public void SetSpawnPoint(Vector2 position)
    {
        lastSpawnPoint = position;
        DebugUtil.Log("Spawn Point Updated: " + position);
    }

    // ダメージ床への接触判定
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag(damageTag))
        {
            Die();
        }
    }

    private void Die()
    {
        if (isDead) return;
        isDead = true;

        DebugUtil.Log("Player Died");

        // 動きを止める
        rb.linearVelocity = Vector2.zero;
        rb.gravityScale = 0f;

        // 巨大化していたら元に戻す
        transform.localScale = originalScale;
        Time.timeScale = 1f;

        // デバッグ演出：色を赤くする
        if (spriteRenderer != null) spriteRenderer.color = Color.red;

        // スロー中のマスクが出ていたら消す
        if (slowMoMask != null) slowMoMask.SetActive(false);

        StartCoroutine(RespawnRoutine());
    }

    private IEnumerator RespawnRoutine()
    {
        yield return new WaitForSeconds(respawnDelay);

        // 復活処理
        transform.position = lastSpawnPoint;

        // 状態のリセット
        isDead = false;
        rb.gravityScale = originalGravity;
        if (spriteRenderer != null) spriteRenderer.color = originalColor;

        // 復活直後の落下死を防ぐために速度をリセット
        rb.linearVelocity = Vector2.zero;

        // 【追加】復活した瞬間にイベントを発行し、登録されている処理を全て実行する
        OnPlayerRespawn?.Invoke();

        DebugUtil.Log("Player Respawned");
    }
    private void CheckSurroundings()
    {
        float dist = 0.1f;
        Vector2 size = col.size * transform.localScale * 0.9f;
        int mask = groundLayer | gimmickLayer | blueBlockPieceLayer;

        RaycastHit2D hitDown = Physics2D.BoxCast(transform.position, size, 0f, Vector2.down, dist, mask);
        RaycastHit2D hitLeft = Physics2D.BoxCast(transform.position, size, 0f, Vector2.left, dist, mask);
        RaycastHit2D hitRight = Physics2D.BoxCast(transform.position, size, 0f, Vector2.right, dist, mask);

        isGrounded = hitDown.collider != null;
        isTouchingWallLeft = hitLeft.collider != null;
        isTouchingWallRight = hitRight.collider != null;

        contactedGimmick = null;
        if (isTouchingWallLeft && hitLeft.collider.GetComponent<IInteractableBlock>() != null) contactedGimmick = hitLeft.collider.gameObject;
        else if (isTouchingWallRight && hitRight.collider.GetComponent<IInteractableBlock>() != null) contactedGimmick = hitRight.collider.gameObject;
        else if (isGrounded && hitDown.collider.GetComponent<IInteractableBlock>() != null) contactedGimmick = hitDown.collider.gameObject;
    }

    private void HandleMovement()
    {
        float moveInput = Input.GetAxisRaw("Horizontal");
        float targetVelocityX = moveInput * moveSpeed;

        if (!isGrounded && Mathf.Abs(rb.linearVelocity.x) > moveSpeed)
        {
            if (moveInput == 0 || Mathf.Sign(moveInput) == Mathf.Sign(rb.linearVelocity.x))
            {
                float momentumDecay = 15f;
                rb.linearVelocity = new Vector2(Mathf.MoveTowards(rb.linearVelocity.x, targetVelocityX, momentumDecay * Time.deltaTime), rb.linearVelocity.y);
            }
            else
            {
                float brakeForce = 50f;
                rb.linearVelocity = new Vector2(Mathf.MoveTowards(rb.linearVelocity.x, targetVelocityX, brakeForce * Time.deltaTime), rb.linearVelocity.y);
            }
        }
        else
        {
            rb.linearVelocity = new Vector2(targetVelocityX, rb.linearVelocity.y);
        }
    }

    private void HandleJumpAndClimb()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            if ((isWallSliding || !isGrounded) && (isTouchingWallLeft || isTouchingWallRight))
            {
                if (CheckLedge())
                {
                    StartCoroutine(LedgeClimbRoutine());
                    return;
                }
            }

            if (isGrounded)
            {
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
            }
        }

        float moveInput = Input.GetAxisRaw("Horizontal");
        if (!isGrounded && ((isTouchingWallLeft && moveInput < 0) || (isTouchingWallRight && moveInput > 0)))
        {
            isWallSliding = true;
            // 【変更1】物理演算の摩擦に負けないよう、Clampではなく強制的に下方向への定速（-wallSlideSpeed）を代入
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, -wallSlideSpeed);
        }
        else
        {
            isWallSliding = false;
        }
    }

    private void HandleTimeAndGrowth()
    {
        if (isClimbing) return;

        if (Input.GetMouseButtonDown(0))
        {
            Time.timeScale = slowTimeScale;
            Time.fixedDeltaTime = 0.02f * Time.timeScale;
            // 【変更4】スロー時にマスクを表示
            if (slowMoMask != null) slowMoMask.SetActive(true);
        }

        if (Input.GetMouseButtonUp(0))
        {
            Time.timeScale = 1f;
            Time.fixedDeltaTime = 0.02f;
            // 【変更4】スロー解除時にマスクを非表示
            if (slowMoMask != null) slowMoMask.SetActive(false);

            StartCoroutine(GrowCoroutine());
        }
    }

    private IEnumerator GrowCoroutine()
    {
        // 1. ご提案の優先順位（緑のブロック > 地面 > 壁）で吹っ飛ぶ方向を厳密に1つだけ確定させる
        Vector2 knockbackDir = Vector2.zero;
        GameObject targetGimmick = null;

        if (contactedGimmick != null)
        {
            // 【優先順位1：緑のブロック】
            targetGimmick = contactedGimmick;

            // ブロックからプレイヤーへの方向を計算
            Vector2 rawDir = transform.position - targetGimmick.transform.position;

            // 完全に縦か横に補正
            knockbackDir = Mathf.Abs(rawDir.x) > Mathf.Abs(rawDir.y)
                ? new Vector2(Mathf.Sign(rawDir.x), 0f)
                : new Vector2(0f, Mathf.Sign(rawDir.y));
        }
        else if (isGrounded)
        {
            // 【優先順位2：地面】
            knockbackDir = Vector2.up;
        }
        else if (isTouchingWallLeft)
        {
            // 【優先順位3：左壁】
            knockbackDir = Vector2.right;
        }
        else if (isTouchingWallRight)
        {
            // 【優先順位4：右壁】
            knockbackDir = Vector2.left;
        }

        // 2. 青ブロックの破壊判定
        Vector2 boxSize = col.size * growScale;
        Collider2D[] overlappedCols = Physics2D.OverlapBoxAll(transform.position, boxSize, 0f, blueBlockPieceLayer);
        bool choppedBlueBlock = false;

        foreach (Collider2D c in overlappedCols)
        {
            BlueBlockPiece piece = c.GetComponent<BlueBlockPiece>();
            if (piece != null)
            {
                piece.Chop();
                choppedBlueBlock = true;
            }
        }

        // 3. 巨大化とヒットストップ（物理演算の停止）
        transform.localScale = growScale;
        float originalTimeScale = Time.timeScale;
        Time.timeScale = 0f;

        yield return new WaitForSecondsRealtime(0.1f);

        // 4. サイズと時間を元に戻す
        Time.timeScale = originalTimeScale;
        transform.localScale = originalScale;

        // 5. 確定させておいた「たった1つの方向」へ吹っ飛ぶ
        if (!choppedBlueBlock && knockbackDir != Vector2.zero)
        {
            if (targetGimmick != null)
            {
                // 緑のブロック自身はプレイヤーと「逆方向」に吹っ飛ばす
                targetGimmick.GetComponent<IInteractableBlock>().Interact(-knockbackDir, knockbackSpeed);
            }

            // プレイヤーの吹っ飛びを実行
            StartCoroutine(DistanceBasedKnockbackRoutine(knockbackDir));
        }
    }

    private void ExecuteKnockback()
    {
        Vector2 dir = Vector2.zero;
        if (isGrounded) dir = Vector2.up;
        else if (isTouchingWallLeft) dir = Vector2.right;
        else if (isTouchingWallRight) dir = Vector2.left;

        if (dir != Vector2.zero) StartCoroutine(DistanceBasedKnockbackRoutine(dir));
    }

    private IEnumerator DistanceBasedKnockbackRoutine(Vector2 direction)
    {
        isKnockedBack = true;
        rb.gravityScale = 0f;

        Vector2 velocity = direction.normalized * knockbackSpeed;
        rb.linearVelocity = velocity;

        Vector2 startPos = transform.position;
        float traveledDistance = 0f;

        while (traveledDistance < knockbackDistance)
        {
            traveledDistance = Vector2.Distance(startPos, transform.position);
            if (rb.linearVelocity.sqrMagnitude < 0.1f) break;
            yield return null;
        }

        rb.gravityScale = originalGravity;
        isKnockedBack = false;
    }

    private bool CheckLedge()
    {
        float facingDir = isTouchingWallLeft ? -1f : 1f;
        Vector2 rayStart = (Vector2)transform.position + new Vector2(0, 1.1f);
        RaycastHit2D hit = Physics2D.Raycast(rayStart, Vector2.right * facingDir, 0.6f, groundLayer | gimmickLayer);
        return hit.collider == null;
    }

    private IEnumerator LedgeClimbRoutine()
    {
        isClimbing = true;
        rb.linearVelocity = Vector2.zero;
        rb.gravityScale = 0f;

        Vector2 startPos = transform.position;
        float facingDir = isTouchingWallLeft ? -1f : 1f;
        Vector2 endPos = startPos + new Vector2(facingDir * ledgeClimbOffset.x, ledgeClimbOffset.y);

        float elapsed = 0f;
        while (elapsed < ledgeClimbDuration)
        {
            transform.position = Vector2.Lerp(startPos, endPos, elapsed / ledgeClimbDuration);
            elapsed += Time.deltaTime;
            yield return null;
        }
        transform.position = endPos;

        rb.gravityScale = originalGravity;
        isClimbing = false;
    }

    // 【変更3】デバッグ用の色変更処理
    private void UpdateDebugVisuals()
    {
        if (spriteRenderer != null)
        {
            // 壁張り付き中はシアン（水色）に、それ以外は元の色にする
            spriteRenderer.color = isWallSliding ? Color.cyan : originalColor;
        }
    }
}