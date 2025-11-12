using UnityEngine;
using UnityEngine.UI;

namespace FIRJAN.Utilities
{
    [RequireComponent(typeof(Button))]
    [RequireComponent(typeof(Image))]
    public class LanguageButton : MonoBehaviour
    {
        [SerializeField] private Language targetLanguage;

        [Header("Visual Settings")]
        [Tooltip("Sprite quando o botão está selecionado (idioma ativo)")]
        [SerializeField] private Sprite selectedSprite;

        [Tooltip("Sprite quando o botão NÃO está selecionado")]
        [SerializeField] private Sprite normalSprite;

        private Button button;
        private Image buttonImage;
        private static LanguageButton currentSelectedButton;

        private void Awake()
        {
            button = GetComponent<Button>();
            buttonImage = GetComponent<Image>();

            // Português é o idioma padrão
            if (targetLanguage == Language.Portuguese)
            {
                SetSelected(true);
                currentSelectedButton = this;
            }
            else
            {
                SetSelected(false);
            }
        }

        private void OnEnable()
        {
            if (button != null)
            {
                button.onClick.AddListener(OnButtonClick);
            }

            // Se este for o botão selecionado, garante que o sprite está correto
            if (currentSelectedButton == this)
            {
                SetSelected(true);
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

            // Desmarca o botão anterior
            if (currentSelectedButton != null && currentSelectedButton != this)
            {
                currentSelectedButton.SetSelected(false);
            }

            // Marca este botão como selecionado
            SetSelected(true);
            currentSelectedButton = this;
        }

        /// <summary>
        /// Define se este botão está visualmente selecionado ou não.
        /// </summary>
        private void SetSelected(bool isSelected)
        {
            if (buttonImage == null) return;

            if (isSelected && selectedSprite != null)
            {
                buttonImage.sprite = selectedSprite;
            }
            else if (!isSelected && normalSprite != null)
            {
                buttonImage.sprite = normalSprite;
            }
        }
    }
}
