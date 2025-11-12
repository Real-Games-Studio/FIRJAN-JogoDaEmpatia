using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using DG.Tweening;
using System.Collections;

namespace FIRJAN.UI
{
    /// <summary>
    /// Adiciona efeitos visuais universais para todos os botões do jogo:
    /// - Efeito de "punch" ao clicar
    /// - Efeito de hover (leve aumento ao passar o mouse)
    /// - Animação suave e responsiva
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class ButtonClickEffect : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [Header("Click Animation Settings")]
        [SerializeField] private float _punchScale = 0.1f;
        [SerializeField] private float _punchDuration = 0.2f;
        [SerializeField] private int _punchVibrato = 10;
        [SerializeField] private float _punchElasticity = 0.5f;

        [Header("Audio Settings")]
        [SerializeField] private AudioSource _clickAudioSource;

        [Header("Hover Settings")]
        [SerializeField] private bool _enableHoverEffect = true;
        [SerializeField] private float _hoverScale = 1.05f;
        [SerializeField] private float _hoverDuration = 0.15f;

        private Button _button;
        private Vector3 _originalScale;
        private bool _isAnimating = false;
        private Tween _currentTween;

        private void Awake()
        {
            _button = GetComponent<Button>();
            _originalScale = transform.localScale;

            // Adicionar listener para tocar animação ao clicar
            _button.onClick.AddListener(OnButtonClickedInternal);
        }

        private void OnDestroy()
        {
            _button.onClick.RemoveListener(OnButtonClickedInternal);

            // Limpar tweens
            if (_currentTween != null && _currentTween.IsActive())
            {
                _currentTween.Kill();
            }
        }

        private void OnButtonClickedInternal()
        {
            // Tocar a animação e o áudio
            PlayClickAnimation();
            PlayClickAudio();
        }

        private void PlayClickAudio()
        {
            if (_clickAudioSource != null)
            {
                _clickAudioSource.Play();
            }
        }

        public void PlayClickAnimation()
        {
            if (!_isAnimating)
            {
                if (_currentTween != null && _currentTween.IsActive())
                {
                    _currentTween.Kill();
                }

                _currentTween = transform.DOPunchScale(
                    Vector3.one * _punchScale,
                    _punchDuration,
                    _punchVibrato,
                    _punchElasticity
                ).SetUpdate(true);
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (!_button.interactable)
                return;

            // Efeito de hover (leve aumento)
            if (_enableHoverEffect && !_isAnimating)
            {
                if (_currentTween != null && _currentTween.IsActive())
                {
                    _currentTween.Kill();
                }

                _currentTween = transform.DOScale(_originalScale * _hoverScale, _hoverDuration)
                    .SetEase(Ease.OutQuad)
                    .SetUpdate(true);
            }
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            // Voltar ao tamanho original
            if (_enableHoverEffect && !_isAnimating)
            {
                if (_currentTween != null && _currentTween.IsActive())
                {
                    _currentTween.Kill();
                }

                _currentTween = transform.DOScale(_originalScale, _hoverDuration)
                    .SetEase(Ease.OutQuad)
                    .SetUpdate(true);
            }
        }

    }
}
