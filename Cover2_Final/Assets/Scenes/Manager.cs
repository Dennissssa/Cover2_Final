using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro; // ���� TextMeshPro �����ռ�
using System.Collections;
using Cinemachine;

public class FishingGame : MonoBehaviour
{
    public GameObject fishingRod; // ��Ͷ���
    public GameObject splashEffect; // ˮ��Ч��Prefab
    public Transform hookSpawnPoint; // �㹳�����
    public GameObject hookPrefab; // �㹳��Prefab
    public AudioClip backgroundMusic; // ��������
    public AudioClip splashSound; // ˮ����Ч
    public AudioClip reelSound; // ������Ч
    public AudioClip reel;
    public float hookWaitMin = 2f; // Ͷ����С�ȴ�ʱ��
    public float hookWaitMax = 4f; // Ͷ�����ȴ�ʱ��
    public Slider progressSlider; // ������Slider
    public Slider inputCountSlider; // ��ǰ�������Slider
    public int maxInputCount = 10; // ������������������
    public float inputDecayRate = 1f; // ������ٵ�����
    public float maxReelSpeed = 1f; // ���������ٶ�
    public CinemachineVirtualCamera virtualCamera1; // Ĭ�����������
    public CinemachineVirtualCamera virtualCamera2; // ����ʱ�����������
    public CinemachineVirtualCamera virtualCamera3; // �㱻�ɹ�����ʱ�����������

    public Fish[] fishTypes; // ��������
    public TMP_Text currentFishText; // ������ʾ��ǰ�������

    private GameObject currentHook; // ��ǰ���ӵ�ʵ��
    private AudioSource audioSource; // AudioSource���
    private bool fishCaught = false; // �Ƿ�ץ����
    private bool isReeling = false; // ���ڹ�������״̬
    private int requiredScrollCount; // ��׽������Ĺ���Ȧ��
    private bool isCountingDown = true; // ���ڹ����ʱ����״̬
    private int currentInputCount = 0; // ��ǰ�����������
    private bool hookThrown = false; // ���ڼ���Ƿ���Ͷ��

    [System.Serializable]
    public class Fish
    {
        public string fishName; // �������
        public int requiredScrollCount; // ��׽��������Ĺ�������
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
        progressSlider.value = 0f; // ��ʼ��������
        inputCountSlider.maxValue = maxInputCount; // �����������Slider�����ֵ
        inputCountSlider.value = 0f; // ��ʼ���������Slider

        StartCoroutine(InputCountMonitor()); // ��������������
    }

    public void ThrowHook()
    {
        if (!hookThrown) // ����Ƿ��Ѿ�Ͷ��
        {
            hookThrown = true; // ����Ϊ��Ͷ��
            isCountingDown = true; // ��ʼ��ʱ
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
        audioSource.PlayOneShot(splashSound); // ����ˮ����Ч

        float splashDuration = 0.5f;
        yield return new WaitForSeconds(splashDuration);
        Destroy(splash);

        // ���ѡ��һ���㲢��������Ĺ�������
        Fish selectedFish = fishTypes[Random.Range(0, fishTypes.Length)];
        requiredScrollCount = selectedFish.requiredScrollCount;

        // ���µ�ǰ�������
        currentFishText.text = selectedFish.fishName;

        // ����ʱ��״̬
        if (isCountingDown)
        {
            LoseGame();
        }
    }

    public void ReelFish()
    {
        if (!isReeling) // ֻ����û������ʱ���������
        {
            isCountingDown = false; // ֹͣ��ʱ��
            isReeling = true; // ����Ϊ��������״̬
            StartCoroutine(PullUpFish());
        }
    }

    private IEnumerator PullUpFish()
    {
        virtualCamera2.Priority = 10; // �л�������ʱ�����������
        virtualCamera1.Priority = 0;
        audioSource.Stop();
        audioSource.PlayOneShot(reelSound);
        audioSource.PlayOneShot(reel); // ����������Ч

        // �ȴ�һ����
        yield return new WaitForSeconds(2f);

        // �л���Ĭ�����������
        virtualCamera2.Priority = 0;
        virtualCamera1.Priority = 10;

        float progress = 0f; // ��ʼ������

        while (progress < requiredScrollCount)
        {
            float scrollInput = Input.GetAxis("Mouse ScrollWheel");

            if (scrollInput < 0) // �������¹���
            {
                progress += Time.deltaTime * maxReelSpeed; // ���ӽ���
                MoveHookTowardsRod(progress, requiredScrollCount); // ���¹���λ��
            }
            else if (scrollInput > 0) // �������Ϲ���
            {
                progress -= Time.deltaTime * maxReelSpeed * 0.5f; // ���ٽ���
                if (progress < 0)
                {
                    progress = 0; // ��ֹ��ֵ
                }
            }

            // ���½�������ʾ���������֮��ľ���
            if (currentHook != null)
            {
                float distance = Vector3.Distance(currentHook.transform.position, fishingRod.transform.position);
                progressSlider.value = Mathf.Clamp01(1 - (distance / Vector3.Distance(hookSpawnPoint.position, fishingRod.transform.position))); // ���㲢���½�����
            }

            // ��鲢�����������
            if (scrollInput != 0)
            {
                currentInputCount++;
                inputCountSlider.value = currentInputCount; // �����������Slider
            }

            // ����Ƿ�ɹ�ץ����
            if (progress >= requiredScrollCount)
            {
                fishCaught = true;

                

                virtualCamera3.Priority = 10; // �л����ɹ���׽�����������
                virtualCamera1.Priority = 0;

                // �ȴ�һ����
                yield return new WaitForSeconds(3f);

                // �л���Ĭ�����������
                virtualCamera3.Priority = 0;
                virtualCamera1.Priority = 10;

                WinGame(); // �ɹ�ץ����
                yield break;
            }

            yield return null; // �ȴ���һ֡
        }

        if (!fishCaught)
        {
            LoseGame();
        }

        isReeling = false; // ����Ϊδ����״̬
    }

    private IEnumerator InputCountMonitor()
    {
        while (true)
        {
            yield return new WaitForSeconds(0.1f);

            // ����Ƿ񳬹������������
            if (currentInputCount > maxInputCount)
            {
                LoseGame(); // ���������������ƣ���Ϸʧ��
                yield break; // �˳�Э��
            }

            // ���������������
            currentInputCount = Mathf.Max(0, currentInputCount - (int)inputDecayRate); // �𽥼����������
        }
    }

    private void MoveHookTowardsRod(float progress, int requiredScrollCount)
    {
        float lerpFactor = Mathf.Clamp01(progress / requiredScrollCount); // �����ֵ����
        if (currentHook != null)
        {
            // ʹ�����Բ�ֵ������λ�ó�������ƶ�
            currentHook.transform.position = Vector3.Lerp(hookSpawnPoint.position, fishingRod.transform.position, lerpFactor);
        }
    }

    private void LoseGame()
    {
        SceneManager.LoadScene("LoseScene"); // ����ʧ�ܳ���
    }

    private void WinGame()
    {
        StartCoroutine(WinGameCoroutine());
    }

    private IEnumerator WinGameCoroutine()
    {
        yield return new WaitForSeconds(2f); // �ȴ� 2 ��
        SceneManager.LoadScene("WinScene"); // ���سɹ�����
    }

    // �����µڶ�����ťʱ���ô˷���
    public void StopCountingDown()
    {
        isCountingDown = false; // ֹͣ��ʱ
    }
}