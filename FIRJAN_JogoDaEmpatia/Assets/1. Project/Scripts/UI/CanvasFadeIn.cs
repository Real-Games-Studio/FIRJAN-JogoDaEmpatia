using UnityEngine;
using DG.Tweening;
using System.Collections;

namespace FIRJAN.UI
{
    /// <summary>
    /// Faz fade in de um CanvasGroup ao ativar, com delay opcional
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public class CanvasFadeIn : MonoBehaviour
    {
        [Header("Fade Settings")]
        [SerializeField] private float delayBeforeFade = 0.3f;
        [SerializeField] private float fadeDuration = 0.5f;
        [SerializeField] private Ease easeType = Ease.OutQuad;
        [SerializeField] private bool fadeOnEnable = true;

        private CanvasGroup canvasGroup;
        private Tween currentTween;
        private CanvasScreen parentScreen;

        private void Awake()
        {
            canvasGroup = GetComponent<CanvasGroup>();
            // Busca o CanvasScreen pai (pode estar no mesmo GameObject ou em um pai)
            parentScreen = GetComponentInParent<CanvasScreen>();
        }

        private void OnEnable()
        {
            // Só executa fade automático se fadeOnEnable estiver marcado
            // E APENAS quando reativar um objeto que estava desativado
            if (fadeOnEnable && canvasGroup != null)
            {
                StartCoroutine(FadeInWithDelay());
            }
        }

        private void OnDisable()
        {
            // Cancela o tween se a tela for desativada
            if (currentTween != null && currentTween.IsActive())
            {
                currentTween.Kill();
            }
        }

        private IEnumerator FadeInWithDelay()
        {
            // Verifica se a tela pai está ligada antes de começar o fade
            if (parentScreen != null)
            {
                Debug.Log($"<color=cyan>[FADE-IN]</color> ParentScreen encontrado: {parentScreen.name}, IsOn: {parentScreen.IsOn()}");

                if (!parentScreen.IsOn())
                {
                    Debug.Log($"<color=yellow>[FADE-IN]</color> Tela {parentScreen.name} não está ligada, abortando fade in");
                    yield break;
                }
            }
            else
            {
                Debug.LogWarning($"<color=orange>[FADE-IN]</color> ParentScreen não encontrado em {gameObject.name}");
            }

            // Começa invisível
            canvasGroup.alpha = 0f;
            Debug.Log($"<color=cyan>[FADE-IN]</color> Iniciando fade in em {gameObject.name}");

            // Aguarda o delay
            yield return new WaitForSeconds(delayBeforeFade);

            // Verifica novamente se a tela ainda está ligada após o delay
            if (parentScreen != null && !parentScreen.IsOn())
            {
                Debug.Log($"<color=yellow>[FADE-IN]</color> Tela {parentScreen.name} foi desligada durante o delay, abortando fade in");
                yield break;
            }

            // Faz o fade in
            if (currentTween != null && currentTween.IsActive())
            {
                currentTween.Kill();
            }

            Debug.Log($"<color=lime>[FADE-IN]</color> Executando DOFade para alpha 1 em {gameObject.name}");
            currentTween = canvasGroup.DOFade(1f, fadeDuration)
                .SetEase(easeType)
                .SetUpdate(true);
        }

        /// <summary>
        /// Método público para chamar o fade in manualmente
        /// </summary>
        public void DoFadeIn()
        {
            StartCoroutine(FadeInWithDelay());
        }

        /// <summary>
        /// Faz fade out (útil para quando quiser esconder)
        /// </summary>
        public void DoFadeOut(float duration = 0.3f)
        {
            if (currentTween != null && currentTween.IsActive())
            {
                currentTween.Kill();
            }

            currentTween = canvasGroup.DOFade(0f, duration)
                .SetEase(Ease.OutQuad)
                .SetUpdate(true);
        }

        private void OnDestroy()
        {
            if (currentTween != null && currentTween.IsActive())
            {
                currentTween.Kill();
            }
        }
    }
}
