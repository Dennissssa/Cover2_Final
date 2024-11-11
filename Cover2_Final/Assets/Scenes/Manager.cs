using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro; // 引入 TextMeshPro 命名空间
using System.Collections;
using Cinemachine;

public class FishingGame : MonoBehaviour
{
    public GameObject fishingRod; // 鱼竿对象
    public GameObject splashEffect; // 水花效果Prefab
    public Transform hookSpawnPoint; // 鱼钩发射点
    public GameObject hookPrefab; // 鱼钩的Prefab
    public AudioClip backgroundMusic; // 背景音乐
    public AudioClip splashSound; // 水花音效
    public AudioClip reelSound; // 拉鱼音效
    public AudioClip reel;
    public float hookWaitMin = 2f; // 投钩最小等待时间
    public float hookWaitMax = 4f; // 投钩最大等待时间
    public Slider progressSlider; // 进度条Slider
    public Slider inputCountSlider; // 当前输入计数Slider
    public int maxInputCount = 10; // 最大滚轮输入数量限制
    public float inputDecayRate = 1f; // 输入减少的速率
    public float maxReelSpeed = 1f; // 拉鱼的最大速度
    public CinemachineVirtualCamera virtualCamera1; // 默认虚拟摄像机
    public CinemachineVirtualCamera virtualCamera2; // 拉鱼时的虚拟摄像机
    public CinemachineVirtualCamera virtualCamera3; // 鱼被成功吊起时的虚拟摄像机

    public Fish[] fishTypes; // 鱼类数组
    public TMP_Text currentFishText; // 用于显示当前鱼的名称

    private GameObject currentHook; // 当前钩子的实例
    private AudioSource audioSource; // AudioSource组件
    private bool fishCaught = false; // 是否抓到鱼
    private bool isReeling = false; // 用于管理拉鱼状态
    private int requiredScrollCount; // 捕捉鱼所需的滚动圈数
    private bool isCountingDown = true; // 用于管理计时器的状态
    private int currentInputCount = 0; // 当前滚轮输入计数
    private bool hookThrown = false; // 用于检查是否已投钩

    [System.Serializable]
    public class Fish
    {
        public string fishName; // 鱼的名称
        public int requiredScrollCount; // 捕捉该鱼所需的滚动数量
    }

    void Start()
    {
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.clip = backgroundMusic;
        audioSource.loop = true;
        audioSource.Play();

        InitializeGame();
    }

    private void InitializeGame()
    {
        progressSlider.value = 0f; // 初始化进度条
        inputCountSlider.maxValue = maxInputCount; // 设置输入计数Slider的最大值
        inputCountSlider.value = 0f; // 初始化输入计数Slider

        StartCoroutine(InputCountMonitor()); // 启动输入计数监控
    }

    public void ThrowHook()
    {
        if (!hookThrown) // 检查是否已经投钩
        {
            hookThrown = true; // 设置为已投钩
            isCountingDown = true; // 开始计时
            StartCoroutine(ThrowHookCoroutine());
            audioSource.PlayOneShot(splashSound);
        }
    }

    private IEnumerator ThrowHookCoroutine()
    {
        currentHook = Instantiate(hookPrefab, hookSpawnPoint.position, Quaternion.identity);
        fishingRod.SetActive(true);

        float waitTime = Random.Range(hookWaitMin, hookWaitMax);
        yield return new WaitForSeconds(waitTime);

        GameObject splash = Instantiate(splashEffect, currentHook.transform.position, Quaternion.Euler(90, 0, 90));
        splash.SetActive(true);
        audioSource.PlayOneShot(splashSound); // 播放水花音效

        float splashDuration = 0.5f;
        yield return new WaitForSeconds(splashDuration);
        Destroy(splash);

        // 随机选择一种鱼并设置所需的滚动数量
        Fish selectedFish = fishTypes[Random.Range(0, fishTypes.Length)];
        requiredScrollCount = selectedFish.requiredScrollCount;

        // 更新当前鱼的名称
        currentFishText.text = selectedFish.fishName;

        // 检查计时器状态
        if (isCountingDown)
        {
            LoseGame();
        }
    }

    public void ReelFish()
    {
        if (!isReeling) // 只有在没有拉鱼时才允许调用
        {
            isCountingDown = false; // 停止计时器
            isReeling = true; // 设置为正在拉鱼状态
            StartCoroutine(PullUpFish());
        }
    }

    private IEnumerator PullUpFish()
    {
        virtualCamera2.Priority = 10; // 切换到拉鱼时的虚拟摄像机
        virtualCamera1.Priority = 0;
        audioSource.Stop();
        audioSource.PlayOneShot(reelSound);
        audioSource.PlayOneShot(reel); // 播放拉鱼音效

        // 等待一秒钟
        yield return new WaitForSeconds(2f);

        // 切换回默认虚拟摄像机
        virtualCamera2.Priority = 0;
        virtualCamera1.Priority = 10;

        float progress = 0f; // 初始化进度

        while (progress < requiredScrollCount)
        {
            float scrollInput = Input.GetAxis("Mouse ScrollWheel");

            if (scrollInput < 0) // 滚轮向下滚动
            {
                progress += Time.deltaTime * maxReelSpeed; // 增加进度
                MoveHookTowardsRod(progress, requiredScrollCount); // 更新钩子位置
            }
            else if (scrollInput > 0) // 滚轮向上滚动
            {
                progress -= Time.deltaTime * maxReelSpeed * 0.5f; // 减少进度
                if (progress < 0)
                {
                    progress = 0; // 防止负值
                }
            }

            // 更新进度条显示钩子与鱼竿之间的距离
            if (currentHook != null)
            {
                float distance = Vector3.Distance(currentHook.transform.position, fishingRod.transform.position);
                progressSlider.value = Mathf.Clamp01(1 - (distance / Vector3.Distance(hookSpawnPoint.position, fishingRod.transform.position))); // 计算并更新进度条
            }

            // 检查并更新输入计数
            if (scrollInput != 0)
            {
                currentInputCount++;
                inputCountSlider.value = currentInputCount; // 更新输入计数Slider
            }

            // 检查是否成功抓到鱼
            if (progress >= requiredScrollCount)
            {
                fishCaught = true;

                

                virtualCamera3.Priority = 10; // 切换到成功捕捉的虚拟摄像机
                virtualCamera1.Priority = 0;

                // 等待一秒钟
                yield return new WaitForSeconds(3f);

                // 切换回默认虚拟摄像机
                virtualCamera3.Priority = 0;
                virtualCamera1.Priority = 10;

                WinGame(); // 成功抓到鱼
                yield break;
            }

            yield return null; // 等待下一帧
        }

        if (!fishCaught)
        {
            LoseGame();
        }

        isReeling = false; // 重置为未拉鱼状态
    }

    private IEnumerator InputCountMonitor()
    {
        while (true)
        {
            yield return new WaitForSeconds(0.1f);

            // 检查是否超过最大输入限制
            if (currentInputCount > maxInputCount)
            {
                LoseGame(); // 输入数量超过限制，游戏失败
                yield break; // 退出协程
            }

            // 持续减少输入计数
            currentInputCount = Mathf.Max(0, currentInputCount - (int)inputDecayRate); // 逐渐减少输入计数
        }
    }

    private void MoveHookTowardsRod(float progress, int requiredScrollCount)
    {
        float lerpFactor = Mathf.Clamp01(progress / requiredScrollCount); // 计算插值因子
        if (currentHook != null)
        {
            // 使用线性插值将钩子位置朝向鱼竿移动
            currentHook.transform.position = Vector3.Lerp(hookSpawnPoint.position, fishingRod.transform.position, lerpFactor);
        }
    }

    private void LoseGame()
    {
        SceneManager.LoadScene("LoseScene"); // 加载失败场景
    }

    private void WinGame()
    {
        StartCoroutine(WinGameCoroutine());
    }

    private IEnumerator WinGameCoroutine()
    {
        yield return new WaitForSeconds(2f); // 等待 2 秒
        SceneManager.LoadScene("WinScene"); // 加载成功场景
    }

    // 当按下第二个按钮时调用此方法
    public void StopCountingDown()
    {
        isCountingDown = false; // 停止计时
    }
}