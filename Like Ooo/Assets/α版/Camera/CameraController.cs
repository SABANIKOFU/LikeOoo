using System;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    public static CameraController Instance { get; private set; }

    [Header("Settings")]
    [Tooltip("カメラが目標位置に到達するまでの滑らかさ（秒）")]
    public float smoothTime = 0.25f;

    private Vector3 targetPosition;
    private Vector3 currentVelocity = Vector3.zero;
    private Camera cam;
    private float targetSize;
    private float sizeVelocity = 0f;

    private Vector3 previousPos;

    // カメラが遷移した時のイベント
    public static event Action MoveCamera;

    void Awake()
    {
        // シングルトンの設定
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        cam = GetComponent<Camera>();
        targetPosition = transform.position;
        targetSize = cam.orthographicSize;
        previousPos = Vector3.zero ;
    }

    // プレイヤーの移動（UpdateやFixedUpdate）が終わった後にカメラを動かすためLateUpdateを使用
    void LateUpdate()
    {
        // 位置のスムーズな移動（Z軸は現在のカメラのZを維持）
        transform.position = Vector3.SmoothDamp(transform.position, targetPosition, ref currentVelocity, smoothTime);

        // カメラの描画サイズ（広さ）のスムーズな変更
        cam.orthographicSize = Mathf.SmoothDamp(cam.orthographicSize, targetSize, ref sizeVelocity, smoothTime);
    }

    // RoomTriggerから呼ばれる関数
    public void MoveToRoom(Vector3 newPosition, float newSize)
    {
        // 同じ位置なら戻る
        if (newPosition == previousPos) return;

        previousPos = newPosition;

        // Z座標はそのまま維持する（2DゲームのカメラがZ=0になると絵が消えるため）
        targetPosition = new Vector3(newPosition.x, newPosition.y, transform.position.z);
        targetSize = newSize;

        MoveCamera?.Invoke();

        DebugUtil.Log("カメラが動きました");
    }
}