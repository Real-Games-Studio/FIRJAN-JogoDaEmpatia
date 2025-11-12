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
            Debug.Log($"[LanguageManager] Language set to: {language}");
            OnLanguageChanged?.Invoke();
            Debug.Log($"[LanguageManager] OnLanguageChanged invoked. Subscribers: {OnLanguageChanged?.GetInvocationList().Length ?? 0}");
        }

        public void ToggleLanguage()
        {
            Language newLanguage = currentLanguage == Language.Portuguese ? Language.English : Language.Portuguese;
            SetLanguage(newLanguage);
        }

        public string GetLocalizedText(string section, string key)
        {
            if (languageData == null)
            {
                Debug.LogError("Language data not loaded!");
                return string.Empty;
            }

            string suffix = currentLanguage == Language.Portuguese ? "PT" : "EN";
            string fullKey = key + suffix;

            try
            {
                switch (section.ToLower())
                {
                    case "common":
                        return GetFieldValue(languageData.common, fullKey);
                    case "cta":
                        return GetFieldValue(languageData.cta, fullKey);
                    case "situation1":
                        return GetFieldValue(languageData.situation1, fullKey);
                    case "situation2":
                        return GetFieldValue(languageData.situation2, fullKey);
                    case "situation3":
                        return GetFieldValue(languageData.situation3, fullKey);
                    case "situation_results":
                        return GetFieldValue(languageData.situation_results, fullKey);
                    case "game_over":
                        return GetFieldValue(languageData.game_over, fullKey);
                    case "header":
                        return GetFieldValue(languageData.header, fullKey);
                    default:
                        Debug.LogWarning($"Section '{section}' not found!");
                        return string.Empty;
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Error getting localized text for section '{section}' and key '{key}': {e.Message}");
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
