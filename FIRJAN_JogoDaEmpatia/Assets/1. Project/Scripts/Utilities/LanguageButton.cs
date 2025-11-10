using UnityEngine;
using UnityEngine.UI;

namespace FIRJAN.Utilities
{
    [RequireComponent(typeof(Button))]
    public class LanguageButton : MonoBehaviour
    {
        [SerializeField] private Language targetLanguage;

        private Button button;

        private void Awake()
        {
            button = GetComponent<Button>();
        }

        private void OnEnable()
        {
            if (button != null)
            {
                button.onClick.AddListener(OnButtonClick);
            }
        }

        private void OnDisable()
        {
            if (button != null)
            {
                button.onClick.RemoveListener(OnButtonClick);
            }
        }

        private void OnButtonClick()
        {
            if (LanguageManager.Instance != null)
            {
                LanguageManager.Instance.SetLanguage(targetLanguage);
                Debug.Log($"Language button clicked! Setting language to: {targetLanguage}");
            }
            else
            {
                Debug.LogError("LanguageManager instance not found!");
            }
        }
    }
}
