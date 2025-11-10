using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using TMPro;
using UnityEngine.UI;
using RealGames;
using UnityEngine.SceneManagement;
public class ScreenCanvasController : MonoBehaviour
{
    public static ScreenCanvasController instance;
    public AppConfig appConfig;

    public string previusScreen;
    public string currentScreen;
    public string inicialScreen;
    public float inactiveTimer = 0;

    public CanvasGroup DEBUG_CANVAS;
    public TMP_Text timeOut;
    [SerializeField] private Image timeoutFillImage;

    private void OnEnable()
    {
        // Registra o m�todo CallScreenListner como ouvinte do evento CallScreen
        ScreenManager.CallScreen += OnScreenCall;

    }
    private void OnDisable()
    {
        // Remove o m�todo CallScreenListner como ouvinte do evento CallScreen
        ScreenManager.CallScreen -= OnScreenCall;

    }
    // Start is called before the first frame update
    void Start()
    {
        Screen.sleepTimeout = SleepTimeout.NeverSleep;
        instance = this;
        ScreenManager.SetCallScreen(inicialScreen);

        float totalTime = appConfig != null ? appConfig.maxInactiveTime : 0f;
        UpdateTimerUI(totalTime);
    }
    // Update is called once per frame
    void Update()
    {
        float totalTime = appConfig != null ? appConfig.maxInactiveTime : 0f;

        if (currentScreen != inicialScreen)
        {
            inactiveTimer += Time.deltaTime;

            float remainingTime = totalTime > 0f
                ? Mathf.Clamp(totalTime - inactiveTimer, 0f, totalTime)
                : 0f;

            UpdateTimerUI(remainingTime);

            if (totalTime > 0f && inactiveTimer >= totalTime)
            {
                ResetGame();
            }
        }
        else
        {
            inactiveTimer = 0;
            UpdateTimerUI(totalTime);
        }
    }
    public void ResetGame()
    {
        Debug.Log("Tempo de inatividade extrapolado!");
        inactiveTimer = 0;
        float totalTime = appConfig != null ? appConfig.maxInactiveTime : 0f;
        UpdateTimerUI(totalTime);
        ScreenManager.CallScreen(inicialScreen);
        SceneManager.LoadScene(0);
    }
    public void OnScreenCall(string name)
    {
        inactiveTimer = 0;
        previusScreen = currentScreen;
        currentScreen = name;
        float totalTime = appConfig != null ? appConfig.maxInactiveTime : 0f;
        UpdateTimerUI(totalTime);
    }
    public void NFCInputHandler(string obj)
    {
        inactiveTimer = 0;
    }

    public void CallAnyScreenByName(string name)
    {
        ScreenManager.CallScreen(name);
    }

    private void UpdateTimerUI(float remainingTime)
    {
        float totalTime = appConfig != null ? appConfig.maxInactiveTime : 0f;

        if (timeOut != null)
        {
            if (remainingTime > 0f)
            {
                int totalSeconds = Mathf.CeilToInt(remainingTime);
                timeOut.text = totalSeconds.ToString();
            }
            else if (totalTime > 0f)
            {
                timeOut.text = "0";
            }
            else
            {
                timeOut.text = string.Empty;
            }
        }

        if (timeoutFillImage != null)
        {
            float normalized = (totalTime > 0f)
                ? Mathf.Clamp01(remainingTime / totalTime)
                : 0f;
            timeoutFillImage.fillAmount = normalized;
        }
    }
}
