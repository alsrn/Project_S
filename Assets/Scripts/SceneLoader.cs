using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoader : MonoBehaviour
{
    [Header("이동할 씬 이름 (Build Profiles에 등록된 이름과 같아야 함)")]
    [SerializeField] private string gameSceneName = "GameScene";

    public void LoadGameScene()
    {
        Debug.Log("게임 시작 버튼 클릭됨!");
        SceneManager.LoadScene(gameSceneName);
    }
}