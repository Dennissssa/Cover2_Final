using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneSwitcher : MonoBehaviour
{
    public string sceneName; // 要切换到的场景名称

    // 这个方法可以被按钮点击事件调用
    public void SwitchScene()
    {
        SceneManager.LoadScene(sceneName);
    }
}