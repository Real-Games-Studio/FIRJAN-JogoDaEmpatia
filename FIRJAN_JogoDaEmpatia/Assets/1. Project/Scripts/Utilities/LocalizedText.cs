using System.Collections;
using System.Linq;
using TMPro;
using UnityEngine;

namespace FIRJAN.Utilities
{
    [RequireComponent(typeof(TextMeshProUGUI))]
    public class LocalizedText : MonoBehaviour
    {
        [Header("Localization Settings")]
        [Tooltip("Section in the JSON file (e.g., cta, situation1, game_over)")]
        [SerializeField] private string section;

        [Tooltip("Key name without PT/EN suffix (e.g., titulo1, descricao1, botao1)")]
        [SerializeField] private string key;

        [Header("Dynamic Variables")]
        [Tooltip("Use {0}, {1}, etc. in JSON and set variables here. Example: 'Situação {0}' with variable '1'")]
        [SerializeField] private string[] formatVariables;

        [Header("Preview")]
        [SerializeField] private bool updateInEditor = true;

        private TextMeshProUGUI textComponent;

        private void Awake()
        {
            textComponent = GetComponent<TextMeshProUGUI>();
        }

        private void OnEnable()
        {
            SubscribeToLanguageManager();
        }

        private void OnDisable()
        {
            UnsubscribeFromLanguageManager();
        }

        private void Start()
        {
            SubscribeToLanguageManager();
            UpdateText();
        }


        private void SubscribeToLanguageManager()
        {
            if (LanguageManager.Instance != null)
            {
                // Remove first to avoid double subscription
                LanguageManager.Instance.OnLanguageChanged -= UpdateText;
                LanguageManager.Instance.OnLanguageChanged += UpdateText;
                Debug.Log($"[LocalizedText] Subscribed to OnLanguageChanged on {gameObject.name}");
            }
            else
            {
                Debug.LogWarning($"[LocalizedText] LanguageManager not found when trying to subscribe on {gameObject.name}");
            }
        }

        private void UnsubscribeFromLanguageManager()
        {
            if (LanguageManager.Instance != null)
            {
                LanguageManager.Instance.OnLanguageChanged -= UpdateText;
            }
        }

        public void UpdateText()
        {
            Debug.Log($"[LocalizedText] UpdateText() started on {gameObject.name}");

            if (LanguageManager.Instance == null)
            {
                Debug.LogWarning($"[LocalizedText] LanguageManager not found! Make sure it exists in the scene. GameObject: {gameObject.name}");
                return;
            }

            Debug.Log($"[LocalizedText] LanguageManager found on {gameObject.name}");

            if (string.IsNullOrEmpty(section) || string.IsNullOrEmpty(key))
            {
                Debug.LogWarning($"[LocalizedText] Section or key is empty in LocalizedText component on {gameObject.name}. Section: '{section}', Key: '{key}'");
                return;
            }

            Debug.Log($"[LocalizedText] Section and key validated on {gameObject.name}: section='{section}', key='{key}'");

            if (textComponent == null)
            {
                textComponent = GetComponent<TextMeshProUGUI>();
                Debug.Log($"[LocalizedText] TextMeshProUGUI component retrieved on {gameObject.name}: {(textComponent != null ? "Success" : "Failed")}");
            }

            string localizedText = LanguageManager.Instance.GetLocalizedText(section, key);
            if (!string.IsNullOrEmpty(localizedText))
            {
                // Apply format variables if any
                if (formatVariables != null && formatVariables.Length > 0)
                {
                    try
                    {
                        localizedText = string.Format(localizedText, formatVariables.Cast<object>().ToArray());
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogWarning($"[LocalizedText] Error formatting text on {gameObject.name}: {e.Message}");
                    }
                }

                textComponent.text = localizedText;
                Debug.Log($"[LocalizedText] Updated text on {gameObject.name}: {localizedText.Substring(0, Mathf.Min(20, localizedText.Length))}...");
            }
            else
            {
                Debug.LogWarning($"[LocalizedText] Empty text returned for {gameObject.name} (section: {section}, key: {key})");
            }
        }

        public void SetLocalizationKey(string newSection, string newKey)
        {
            Debug.Log($"[LocalizedText] SetLocalizationKey called on {gameObject.name}: section={newSection}, key={newKey}");
            section = newSection;
            key = newKey;

            // If LanguageManager is not ready yet, wait for it
            if (LanguageManager.Instance == null)
            {
                Debug.LogWarning($"[LocalizedText] LanguageManager not ready when SetLocalizationKey called on {gameObject.name}. Starting coroutine to wait.");
                StartCoroutine(WaitForLanguageManagerAndUpdate());
            }
            else
            {
                Debug.Log($"[LocalizedText] About to call UpdateText() on {gameObject.name}");
                UpdateText();
                Debug.Log($"[LocalizedText] UpdateText() completed on {gameObject.name}");
            }
        }

        private IEnumerator WaitForLanguageManagerAndUpdate()
        {
            // Wait until LanguageManager is ready
            while (LanguageManager.Instance == null)
            {
                Debug.Log($"[LocalizedText] Waiting for LanguageManager on {gameObject.name}...");
                yield return null;
            }

            Debug.Log($"[LocalizedText] LanguageManager ready! Updating text on {gameObject.name}");
            SubscribeToLanguageManager();
            UpdateText();
        }

        public void SetFormatVariables(params string[] variables)
        {
            formatVariables = variables;
            UpdateText();
        }

        public void SetVariable(int index, string value)
        {
            if (formatVariables == null)
            {
                formatVariables = new string[index + 1];
            }
            else if (index >= formatVariables.Length)
            {
                System.Array.Resize(ref formatVariables, index + 1);
            }

            formatVariables[index] = value;
            UpdateText();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (updateInEditor && Application.isPlaying && LanguageManager.Instance != null)
            {
                UpdateText();
            }
        }
#endif
    }
}
