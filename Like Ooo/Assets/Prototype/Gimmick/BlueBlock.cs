using UnityEngine;

public class BlueBlock : MonoBehaviour, IInteractableBlock
{
    public void Interact(Vector2 dirToGimmick, float force)
    {
        // 破壊エフェクトや音の再生処理などをここに記述
        Debug.Log("Blue Block Destroyed!");

        // オブジェクトを消滅させる
        Destroy(gameObject);
    }
}
