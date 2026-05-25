using System.Collections;
using System.Drawing;
using Unity.VisualScripting;
using UnityEngine;

public class GrowBlink : MonoBehaviour
{
    [Header("状態フラグ")]
    public bool isGrow;
    public bool isKnockBucked;
    private bool isBreakBlock;
    public bool isCollideBlock;
    private bool get_away;

    [Header("スロー設定")]
    [SerializeField] private float slowTimeScale = 0.2f;
    [SerializeField] private GameObject slowTimeMask;   // スロー演出用のImage

    [Header("巨大化設定")]
    [SerializeField] private GameObject growPlayer;
    [SerializeField] private float growTime = 0.1f; // 巨大化の時間


    [Header("ブリンク設定")]
    private Vector2 knockbuckDir;
    [SerializeField] private float knockbuckForce = 10f;
    [SerializeField] private float uncontrollDistance = 5f;
    private Vector2 startPos;      // 吹っ飛ぶ前の位置
    private float currentDistance; // 現在の吹っ飛んだ距離

    [Header("ギミック設定")]
    [SerializeField] private LayerMask GimmickBlockLayer;

    [Header("構造体")]
    private BasicOperations basicOps;
    private GetItem getItem;
    private MoveBlock moveBlock;
    private EraceBlock eraceBlock;


    void Start()
    {
        basicOps = GetComponent<BasicOperations>();
        getItem = GetComponent<GetItem>();
    }

    void Update()
    {
        // 死亡時は操作を受け付けないようにする
        if (basicOps.isDead)
        {
            ResetSlow();
            Time.timeScale = 0f;
            return;
        }

        // 巨大化時とアイテムを持っていない時に
        // 巨大化できないようにする
        if (isGrow || getItem.canGrow == false) return;

        // 地面にいるときとすべり落ち状態以外は巨大化できないようにする
        if (!basicOps.isGround && !basicOps.isPushingWall)
        {
            get_away = true;
            ResetSlow();
            return;
        }

        // 左クリックを入力中
        if (Input.GetMouseButtonDown(0))
        {
            get_away = false;
            // 時間の流れを変える
            // 物理演算の計算間隔を１秒に50回にするためにfixedDeltaTimeも変える
            Time.timeScale = slowTimeScale;
            Time.fixedDeltaTime = 0.02f * Time.timeScale;

            // マスクをかけてわかりやすくする
            if (slowTimeMask != null) slowTimeMask.SetActive(true);
        }

        // 左クリックを離したとき
        if(Input.GetMouseButtonUp(0) && !get_away)
        {
            // 時間の流れ・物理演算の間隔を戻す
            Time.timeScale = 1f;
            Time.fixedDeltaTime = 0.02f;

            // マスクを解除する
            if (slowTimeMask != null) slowTimeMask.SetActive(false);

            // 巨大化コルーチン
            StartCoroutine(GrowPlayer());
        }
    }

    private void ResetSlow()
    {
        if (slowTimeMask != null) slowTimeMask.SetActive(false);
        Time.timeScale = 1f;
    }

    private IEnumerator GrowPlayer()
    {
        // アイテムを消費する。
        getItem.UseGrowItem();

        isGrow = true;

        float originalTimeScale = Time.timeScale;

        // ヒットストップ
        Time.timeScale = 0f;

        // 巨大化時のオブジェクトを表示
        if (growPlayer != null)
        {
            growPlayer.SetActive(true);
        }

        yield return new WaitForSecondsRealtime(growTime);

        Time.timeScale = originalTimeScale;

        // 巨大化オブジェクトを消す
        if (growPlayer != null)
        {
            growPlayer.SetActive(false);
        }

        knockbuckDir = Vector2.zero;
        moveBlock = null;

        //===================================================//
        //     ギミックオブジェクトを検知するRayを飛ばす     //
        //===================================================//
        RaycastHit2D hitDown = basicOps.BoxCast(Vector2.down, basicOps.boxRayDistance, GimmickBlockLayer);
        RaycastHit2D hitLeft = basicOps.BoxCast(Vector2.left, basicOps.boxRayDistance, GimmickBlockLayer);
        RaycastHit2D hitRight = basicOps.BoxCast(Vector2.right, basicOps.boxRayDistance, GimmickBlockLayer);

        // コライダーが存在し、指定のタグを持っているかを判定するローカル関数
        bool IsTag(RaycastHit2D hit, string tag)
        {
            return hit.collider != null && hit.collider.CompareTag(tag);
        }

        // EraceBlockの場合
        bool TryHitEraceBlock(RaycastHit2D hit)
        {
            if (IsTag(hit, "EraceBlock"))
            {
                isBreakBlock = true;

                knockbuckDir = Vector2.zero;
                eraceBlock = hit.collider.GetComponent<EraceBlock>();
                eraceBlock.Erace();

                isGrow = false;
                return true;
            }

            isBreakBlock = false;
            return false;
        }

        // MoveBlockだった場合の処理
        bool TryHitMoveBlock(RaycastHit2D hit, Vector2 dir)
        {
            if (IsTag(hit, "MoveBlock"))
            {
                knockbuckDir = dir;
                moveBlock = hit.collider.GetComponent<MoveBlock>();
                moveBlock.CalcMoveDir(this.transform);
                return true;
            }
            return false;
        }

        //================================================================================//
        //     接地の優先度による条件分岐 消えるブロック >動くブロック >下 > 右 > 左     //
        //================================================================================//
        // 1.消えるブロック
        if (TryHitEraceBlock(hitDown)) { }
        else if (TryHitEraceBlock(hitRight)) { }
        else if (TryHitEraceBlock(hitLeft)) { }
        // 2.動くブロック
        else if (TryHitMoveBlock(hitDown, Vector2.up)) { }
        else if (TryHitMoveBlock(hitRight, Vector2.left)) { }
        else if (TryHitMoveBlock(hitLeft, Vector2.right)) { }
        // 3.普通のブロック
        else if (basicOps.isGround)
        {
            knockbuckDir = Vector2.up;

            // 地面と壁に接地しているときには壁から0.01f左右にずらしてから上方向にブリンク
            // プレイヤーがタイルマップにめり込んで斜め上方向にブリンクするのを防ぐ目的
            if (basicOps.isTouchLeftWall)
            {
                transform.position = new Vector3(transform.position.x + 0.01f, transform.position.y, transform.position.z);
            }
            else if (basicOps.isTouchRightWall)
            {
                transform.position = new Vector3(transform.position.x - 0.01f, transform.position.y, transform.position.z);
            }
        }
        else if (basicOps.isTouchRightWall)
        {
            knockbuckDir = Vector2.left;
        }
        else if (basicOps.isTouchLeftWall)
        {
            knockbuckDir = Vector2.right;
        }

        // ブロックを壊すときはプレイヤーはブリンクしない
        if (isBreakBlock) yield break; ;

        StartCoroutine(Blink(knockbuckDir));
    }

    private IEnumerator Blink(Vector2 knockbuckDir)
    {
        // 空中にいて方向がゼロの場合は吹っ飛ばない
        if (knockbuckDir == Vector2.zero)
        {
            isGrow = false;
            yield break;
        }

        isKnockBucked = true;

        // 適応されるまで1フレーム待つ
        yield return new WaitForFixedUpdate();

        // 初期位置をセット
        startPos = transform.position;

        Vector2 velocity = knockbuckDir.normalized * knockbuckForce;

        // トップスピードで飛ばす
        basicOps.rb.linearVelocity = velocity;

        currentDistance = 0f;

        //Debug.Log("吹っ飛び中");

        //吹っ飛んだ距離が設定した値を超えたときに重力と空中制御を戻す
        while (currentDistance < uncontrollDistance)
        {
            // startPosから現在の位置の距離を返す
            currentDistance = Vector2.Distance(startPos, transform.position);

            if (basicOps.rb.linearVelocity.sqrMagnitude < 0.5f)
            {
                DebugUtil.Log("壁に激突して停止しました");
                isKnockBucked = false;
                break;
            }

            yield return new WaitForFixedUpdate();
        }

        //Debug.Log("吹っ飛び終わり");

        isKnockBucked = false;
        isGrow = false;
    }

}
