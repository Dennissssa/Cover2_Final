using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneSwitcher : MonoBehaviour
{
    public string sceneName; // Ҫ�л����ĳ�������

    // ����������Ա���ť����¼�����
    public void SwitchScene()
    {
        SceneManager.LoadScene(sceneName);
    }
}