using UnityEngine;

public interface IInteractableBlock
{
    // dirToGimmick: プレイヤーからブロックに向かうベクトル
    void Interact(Vector2 dirToGimmick, float force);
}