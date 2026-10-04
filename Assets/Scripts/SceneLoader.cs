using UnityEngine;
using UnityEngine.SceneManagement;

// 파일 이름: SceneLoader.cs
// 역할: 게임시작 버튼을 클릭하면 GameScene으로 이동한다.
public class SceneLoader : MonoBehaviour
{
    [Header("이동할 씬 이름 (Build Profiles에 등록된 이름과 같아야 함)")]
    [SerializeField] private string gameSceneName = "GameScene";

    // Button의 On Click에서 호출할 함수 (반드시 public)
    public void LoadGameScene()
    {
        Debug.Log("게임 시작 버튼 클릭됨!");
        SceneManager.LoadScene(gameSceneName);
    }
}