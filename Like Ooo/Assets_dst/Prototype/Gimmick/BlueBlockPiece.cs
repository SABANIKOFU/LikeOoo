using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public class BlueBlockPiece : MonoBehaviour
{
    // プレイヤーから呼ばれる「削られる」処理
    public void Chop()
    {
        // プロトタイプなのでシンプルに破壊
        // 必要に応じてエフェクト生成などを追加
        Destroy(gameObject);

        // デバッグ用
        // Debug.Log($"Piece {gameObject.name} Chopped!");
    }
}
