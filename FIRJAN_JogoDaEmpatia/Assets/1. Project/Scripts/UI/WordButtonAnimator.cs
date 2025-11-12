using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using System.Collections;
using System.Collections.Generic;

namespace FIRJAN.UI
{
    /// <summary>
    /// Anima os botões de palavras aparecendo de cima para baixo
    /// mantendo as posições finais definidas pelo Grid Layout
    /// </summary>
    public class WordButtonAnimator : MonoBehaviour
    {
        [Header("Animation Settings")]
        [SerializeField] private float delayBetweenButtons = 0.1f;
        [SerializeField] private float animationDuration = 0.5f;
        [SerializeField] private float offsetY = 200f; // Distância que os botões começam acima
        [SerializeField] private Ease easeType = Ease.OutBack;

        [Header("Fade Settings")]
        [SerializeField] private bool useFade = true;

        [Header("Test (Editor Only)")]
        [SerializeField] private CanvasFadeIn canvasFadeInTest; // Referência ao CanvasFadeIn para teste

        private List<RectTransform> buttonRects = new List<RectTransform>();
        private List<CanvasGroup> buttonCanvasGroups = new List<CanvasGroup>();
        private List<LayoutElement> buttonLayoutElements = new List<LayoutElement>();

        /// <summary>
        /// Anima os botões filhos do container aparecendo de cima para baixo
        /// </summary>
        public void AnimateButtons()
        {
            Debug.Log($"<color=magenta>[ANIM-BTN]</color> AnimateButtons chamado em {gameObject.name}");

            // Para todas as coroutines anteriores para evitar conflito
            StopAllCoroutines();

            // Mata todas as animações DOTween dos botões filhos
            for (int i = 0; i < transform.childCount; i++)
            {
                Transform child = transform.GetChild(i);
                DOTween.Kill(child);
            }

            StartCoroutine(AnimateButtonsCoroutine());
        }

        private IEnumerator AnimateButtonsCoroutine()
        {
            Debug.Log($"<color=magenta>[ANIM-BTN]</color> AnimateButtonsCoroutine iniciado");

            // Aguardar frames adicionais para o Grid Layout calcular as posições
            yield return null;
            yield return null;

            // Obter todos os botões filhos
            buttonRects.Clear();
            buttonCanvasGroups.Clear();
            buttonLayoutElements.Clear();

            for (int i = 0; i < transform.childCount; i++)
            {
                Transform child = transform.GetChild(i);
                if (!child.gameObject.activeSelf) continue;

                RectTransform rect = child.GetComponent<RectTransform>();
                if (rect != null)
                {
                    buttonRects.Add(rect);

                    // Adicionar ou obter CanvasGroup
                    CanvasGroup canvasGroup = child.GetComponent<CanvasGroup>();
                    if (canvasGroup == null && useFade)
                    {
                        canvasGroup = child.gameObject.AddComponent<CanvasGroup>();
                    }
                    buttonCanvasGroups.Add(canvasGroup);

                    // Adicionar LayoutElement se não existir
                    LayoutElement layoutElement = child.GetComponent<LayoutElement>();
                    if (layoutElement == null)
                    {
                        layoutElement = child.gameObject.AddComponent<LayoutElement>();
                    }
                    buttonLayoutElements.Add(layoutElement);
                }
            }

            Debug.Log($"<color=magenta>[ANIM-BTN]</color> {buttonRects.Count} botões encontrados");

            // Salvar as posições finais e preparar para animação
            List<Vector2> finalPositions = new List<Vector2>();
            for (int i = 0; i < buttonRects.Count; i++)
            {
                RectTransform rect = buttonRects[i];
                CanvasGroup canvasGroup = buttonCanvasGroups[i];
                LayoutElement layoutElement = buttonLayoutElements[i];

                // Salvar posição final
                finalPositions.Add(rect.anchoredPosition);

                // Preparar para animação
                layoutElement.ignoreLayout = true;
                rect.anchoredPosition = new Vector2(finalPositions[i].x, finalPositions[i].y + offsetY);

                if (useFade && canvasGroup != null)
                {
                    canvasGroup.alpha = 0f;
                }
            }

            // Animar cada botão com delay
            Debug.Log($"<color=magenta>[ANIM-BTN]</color> Iniciando animação de {buttonRects.Count} botões");

            for (int i = 0; i < buttonRects.Count; i++)
            {
                int index = i; // Capturar índice para closure
                RectTransform rect = buttonRects[index];
                CanvasGroup canvasGroup = buttonCanvasGroups[index];
                LayoutElement layoutElement = buttonLayoutElements[index];

                Debug.Log($"<color=magenta>[ANIM-BTN]</color> Animando botão {index}: {rect.name} de {rect.anchoredPosition} para {finalPositions[index]}");

                // Animar posição
                rect.DOAnchorPos(finalPositions[index], animationDuration)
                    .SetEase(easeType)
                    .OnComplete(() =>
                    {
                        // Quando a animação terminar, voltar a usar o layout
                        if (layoutElement != null)
                        {
                            layoutElement.ignoreLayout = false;
                        }
                    });

                // Animar fade se ativado
                if (useFade && canvasGroup != null)
                {
                    Debug.Log($"<color=magenta>[ANIM-BTN]</color> Fazendo fade do botão {index} de alpha {canvasGroup.alpha} para 1");
                    canvasGroup.DOFade(1f, animationDuration * 0.7f);
                }

                // Delay antes do próximo botão
                if (i < buttonRects.Count - 1)
                {
                    yield return new WaitForSeconds(delayBetweenButtons);
                }
            }

            Debug.Log($"<color=magenta>[ANIM-BTN]</color> Animação completa!");
        }

        /// <summary>
        /// Reseta os botões para o estado inicial (sem animação)
        /// </summary>
        public void ResetButtons()
        {
            // Parar todas as animações
            DOTween.Kill(transform);

            // Limpar listas
            foreach (var layoutElement in buttonLayoutElements)
            {
                if (layoutElement != null)
                {
                    layoutElement.ignoreLayout = false;
                }
            }

            foreach (var canvasGroup in buttonCanvasGroups)
            {
                if (canvasGroup != null && useFade)
                {
                    canvasGroup.alpha = 1f;
                }
            }

            buttonRects.Clear();
            buttonCanvasGroups.Clear();
            buttonLayoutElements.Clear();
        }

        private void OnDestroy()
        {
            // Limpar tweens ao destruir
            DOTween.Kill(transform);
        }

        /// <summary>
        /// Testa a animação completa (CanvasFadeIn + Botões)
        /// Clique com botão direito no componente > Test Full Animation
        /// </summary>
        [ContextMenu("Test Full Animation")]
        private void TestFullAnimation()
        {
            // Chama o fade in do canvas
            if (canvasFadeInTest != null)
            {
                canvasFadeInTest.DoFadeIn();
            }

            // Chama a animação dos botões
            AnimateButtons();
        }

        /// <summary>
        /// Reseta tudo para testar novamente
        /// Clique com botão direito no componente > Reset For Test
        /// </summary>
        [ContextMenu("Reset For Test")]
        private void ResetForTest()
        {
            ResetButtons();

            // Reseta o alpha do canvas para 0
            if (canvasFadeInTest != null && canvasFadeInTest.TryGetComponent<CanvasGroup>(out var cg))
            {
                cg.alpha = 0f;
            }
        }
    }
}
