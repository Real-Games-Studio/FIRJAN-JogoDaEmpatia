using System;
using System.IO;
using UnityEngine;

namespace FIRJAN.Utilities
{
    public enum Language
    {
        Portuguese,
        English
    }

    public class LanguageManager : MonoBehaviour
    {
        public static LanguageManager Instance { get; private set; }

        [SerializeField] private Language currentLanguage = Language.Portuguese;

        private LanguageData languageData;

        public event Action OnLanguageChanged;

        public Language CurrentLanguage => currentLanguage;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                LoadLanguageData();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void Start()
        {
            // Trigger initial language change event after one frame
            // This ensures all LocalizedText components are subscribed before the event fires
            StartCoroutine(TriggerInitialLanguageUpdate());
        }

        private System.Collections.IEnumerator TriggerInitialLanguageUpdate()
        {
            // Wait one frame to ensure all OnEnable/Start subscriptions are registered
            yield return null;

            Debug.Log($"[LanguageManager] Triggering initial language update. Current language: {currentLanguage}");

            // Fire the event to update all LocalizedText components with current language
            OnLanguageChanged?.Invoke();
        }

        private void LoadLanguageData()
        {
            string filePath = Path.Combine(Application.streamingAssetsPath, "language.json");

            if (File.Exists(filePath))
            {
                string jsonContent = File.ReadAllText(filePath);
                languageData = JsonUtility.FromJson<LanguageData>(jsonContent);
                Debug.Log("Language data loaded successfully!");
            }
            else
            {
                Debug.LogError($"Language file not found at: {filePath}");
            }
        }

        public void SetLanguage(Language language)
        {
            currentLanguage = language;
            // Debug.Log($"[Problema2][LanguageManager] Language set to: {language}");

            if (OnLanguageChanged != null)
            {
                // Debug.Log($"[Problema2][LanguageManager] BEFORE Invoke - Subscribers: {OnLanguageChanged.GetInvocationList().Length}");
                // foreach (var subscriber in OnLanguageChanged.GetInvocationList())
                // {
                //     Debug.Log($"[Problema2][LanguageManager] Subscriber: {subscriber.Target?.GetType().Name ?? "NULL"}.{subscriber.Method.Name}");
                // }
                OnLanguageChanged.Invoke();
                // Debug.Log($"[Problema2][LanguageManager] AFTER Invoke - Event fired successfully");
            }
            else
            {
                Debug.LogWarning("[Problema2][LanguageManager] OnLanguageChanged is NULL! No subscribers!");
            }
        }

        public void ToggleLanguage()
        {
            Language newLanguage = currentLanguage == Language.Portuguese ? Language.English : Language.Portuguese;
            SetLanguage(newLanguage);
        }

        public string GetLocalizedText(string section, string key)
        {
            // Debug.Log($"[Problema1][LanguageManager] GetLocalizedText called - section: '{section}', key: '{key}'");

            if (languageData == null)
            {
                Debug.LogError("[Problema1] Language data not loaded!");
                return string.Empty;
            }

            string suffix = currentLanguage == Language.Portuguese ? "PT" : "EN";
            string fullKey = key + suffix;

            // Debug.Log($"[Problema1][LanguageManager] Full key with suffix: '{fullKey}', Current language: {currentLanguage}");

            try
            {
                string result = string.Empty;
                switch (section.ToLower())
                {
                    case "common":
                        result = GetFieldValue(languageData.common, fullKey);
                        break;
                    case "cta":
                        result = GetFieldValue(languageData.cta, fullKey);
                        break;
                    case "situation1":
                        result = GetFieldValue(languageData.situation1, fullKey);
                        break;
                    case "situation2":
                        result = GetFieldValue(languageData.situation2, fullKey);
                        break;
                    case "situation3":
                        result = GetFieldValue(languageData.situation3, fullKey);
                        break;
                    case "situation_results":
                        result = GetFieldValue(languageData.situation_results, fullKey);
                        break;
                    case "game_over":
                        result = GetFieldValue(languageData.game_over, fullKey);
                        break;
                    case "header":
                        result = GetFieldValue(languageData.header, fullKey);
                        break;
                    default:
                        Debug.LogWarning($"[Problema1] Section '{section}' not found!");
                        return string.Empty;
                }

                // Debug.Log($"[Problema1][LanguageManager] Returning text (first 50 chars): '{(result.Length > 50 ? result.Substring(0, 50) + "..." : result)}'");
                return result;
            }
            catch (Exception e)
            {
                Debug.LogError($"[Problema1] Error getting localized text for section '{section}' and key '{key}': {e.Message}");
                return string.Empty;
            }
        }

        private string GetFieldValue(object obj, string fieldName)
        {
            var field = obj.GetType().GetField(fieldName);
            if (field != null)
            {
                return field.GetValue(obj) as string ?? string.Empty;
            }

            Debug.LogWarning($"Field '{fieldName}' not found in object of type {obj.GetType().Name}");
            return string.Empty;
        }

        public LanguageData GetLanguageData()
        {
            return languageData;
        }
    }
}
