using UnityEngine;

public class EndSystem : MonoBehaviour
{
    // Update is called once per frame
    void Update()
    {
        EndGame();
    }

    public void EndGame()
    {
        // 終了メソッド（デバッグ用）
        if (Input.GetKey(KeyCode.Escape))
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
    Application.Quit(); // ゲーム終了
#endif
        }
    }
}
