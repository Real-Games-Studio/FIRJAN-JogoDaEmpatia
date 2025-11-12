using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections;

/// <summary>
/// Script para gerenciar a tela de resultados do Jogo da Empatia.
/// Exibe a pontuação final e as habilidades desenvolvidas.
/// </summary>
public class ScreenResult : CanvasScreen
{
    [Header("Result UI References")]
    [SerializeField] private TextMeshProUGUI empathyScoreText;
    [SerializeField] private TextMeshProUGUI activeListeningScoreText;
    [SerializeField] private TextMeshProUGUI selfAwarenessScoreText;
    [SerializeField] private TextMeshProUGUI finalMessageText;
    [SerializeField] private TextMeshProUGUI totalScoreText;
    [SerializeField] private TextMeshProUGUI topWordsText;

    [Header("Segmented Bars")]
    [SerializeField] private SegmentedBar empathyBar;
    [SerializeField] private SegmentedBar activeListeningBar;
    [SerializeField] private SegmentedBar selfAwarenessBar;

    [Header("Canvas Groups")]
    [SerializeField] private CanvasGroup waitingForCardGroup;    // Grupo com "Aproxime seu cartão"
    [SerializeField] private CanvasGroup postSuccessGroup;       // Grupo que aparece após POST
    [SerializeField] private GameObject objectToDisableAfterPost; // GameObject para desligar após POST bem-sucedido
    [SerializeField] private TextMeshProUGUI thankYouTitleText;   // Texto "OBRIGADO!" que aparece após POST
    [SerializeField] private GameObject finishButton;             // Botão "Finalizar" que aparece após POST

    [Header("Localized Text References")]
    [SerializeField] private FIRJAN.Utilities.LocalizedText thankYouLocalizedText; // LocalizedText para "OBRIGADO!"
    [SerializeField] private FIRJAN.Utilities.LocalizedText finishButtonLocalizedText; // LocalizedText do botão Finalizar

    [Header("Debug Settings")]
    [SerializeField] private KeyCode debugNFCKey = KeyCode.N;    // Tecla para simular NFC em debug

    [Header("Score Display Settings")]
    [SerializeField] private string empathyFormat = "Empatia: {0} pontos";
    [SerializeField] private string activeListeningFormat = "Escuta Ativa: {0} pontos";
    [SerializeField] private string selfAwarenessFormat = "Autoconsciência: {0} pontos";
    [SerializeField] private string totalScoreFormat = "Pontuação Total: {0}/12";

    [Header("Songs")]
    [SerializeField] private AudioSource endSongClip, nfcClip;

    [Header("Auto Restart Timer")]
    [SerializeField] private GameObject timerGameObject; // GameObject do timer que será ativado/desativado
    [SerializeField] private TextMeshProUGUI timerText; // Texto que mostra os segundos restantes
    [SerializeField] private UnityEngine.UI.Image timerFillImage; // Imagem preenchida que diminui com o tempo
    [SerializeField] private float autoRestartTime = 20f; // Tempo em segundos para reiniciar automaticamente

    // Controle do timer
    private float currentTime;
    private float initialTime;
    private bool timerActive = false;

    #region Public Methods

    /// <summary>
    /// Método público para exibir os resultados finais.
    /// Chamado pelo ScreenGame.cs ao finalizar o jogo.
    /// </summary>
    /// <param name="finalScore">Pontuação final obtida no jogo (0-12)</param>
    public void ShowFinalResults(int finalScore)
    {
        // Inicializa os CanvasGroups
        InitializeCanvasGroups();

        // Calcula os pontos de cada habilidade baseado na pontuação final
        SkillScores scores = CalculateSkillScores(finalScore);

        // Exibe os pontos de cada habilidade
        DisplaySkillScores(scores);

        // Exibe a pontuação total
        DisplayTotalScore(finalScore);

        // Exibe a mensagem final da Aya
        DisplayFinalMessage();

        // Atualiza a nuvem de palavras com as pontuações das habilidades
        UpdateWordCloudWithSkillScores(scores);

        // Exibe as palavras mais pontuadas
        DisplayTopWords();

        // Inicia a animação das barras segmentadas
        StartCoroutine(AnimateSkillBars(scores));

        // Envia dados para o sistema NFC
        SubmitToNFCSystem(scores);

        // Inicia o timer de auto-reinicialização
        StartAutoRestartTimer();

        // Log para debug
        Debug.Log($"Resultados finais - Pontuação: {finalScore}, " +
                 $"Empatia: {scores.empathy}, " +
                 $"Escuta Ativa: {scores.activeListening}, " +
                 $"Autoconsciência: {scores.selfAwareness}");
    }

    #endregion

    #region Private Methods

    public override void TurnOn()
    {
        base.TurnOn();
        endSongClip.Play();
    }

    /// <summary>
    /// Update para detectar tecla de debug do NFC e atualizar o timer.
    /// </summary>
    private void Update()
    {
        // Atualiza o timer se estiver ativo
        if (timerActive)
        {
            HandleAutoRestartTimer();
        }

        // DEBUG: Simula leitura de NFC e POST bem-sucedido (APENAS NO EDITOR)
        #if UNITY_EDITOR
        if (Input.GetKeyDown(debugNFCKey))
        {
            Debug.Log("[ScreenResult] ===DEBUG=== Tecla de debug pressionada! Simulando NFC + POST bem-sucedido");
            OnPostSuccess();
        }
        #endif
    }

    /// <summary>
    /// Estrutura para armazenar as pontuações das habilidades.
    /// </summary>
    private struct SkillScores
    {
        public int empathy;
        public int activeListening;
        public int selfAwareness;

        public SkillScores(int empathy, int activeListening, int selfAwareness)
        {
            this.empathy = empathy;
            this.activeListening = activeListening;
            this.selfAwareness = selfAwareness;
        }
    }

    /// <summary>
    /// Calcula os pontos de cada habilidade baseado na pontuação final.
    /// Os valores são proporcionais: pontuação máxima (12) preenche a barra completa (22 segmentos).
    /// Cada habilidade tem um peso diferente: Empatia 100%, Escuta Ativa 87.5%, Autoconsciência 68.75%.
    /// IMPORTANTE: Usa Mathf.CeilToInt para arredondar PARA CIMA.
    /// </summary>
    /// <param name="finalScore">Pontuação final (0-12)</param>
    /// <returns>Estrutura com os pontos de cada habilidade</returns>
    private SkillScores CalculateSkillScores(int finalScore)
    {
        // Número de segmentos da barra (22 segmentos no Inspector)
        const int maxSegments = 22;
        const int maxScore = 12;

        // Calcula a proporção da pontuação (0.0 a 1.0)
        float scoreRatio = Mathf.Clamp01((float)finalScore / maxScore);

        // Calcula os valores de cada habilidade proporcionalmente
        // Empatia: 100% da barra quando score = 12 (22 segmentos)
        int empathy = Mathf.CeilToInt(scoreRatio * maxSegments);

        // Escuta Ativa: 87.5% da barra quando score = 12 (19.25 → 20 segmentos)
        int activeListening = Mathf.CeilToInt(scoreRatio * maxSegments * 0.875f);

        // Autoconsciência: 68.75% da barra quando score = 12 (15.125 → 16 segmentos)
        int selfAwareness = Mathf.CeilToInt(scoreRatio * maxSegments * 0.6875f);

        return new SkillScores(
            empathy: empathy,
            activeListening: activeListening,
            selfAwareness: selfAwareness
        );
    }

    /// <summary>
    /// Exibe os pontos de cada habilidade na UI.
    /// </summary>
    /// <param name="scores">Pontuações das habilidades</param>
    private void DisplaySkillScores(SkillScores scores)
    {
        // Exibe pontuação de Empatia
        if (empathyScoreText != null)
        {
            empathyScoreText.text = string.Format(empathyFormat, scores.empathy);
        }

        // Exibe pontuação de Escuta Ativa
        if (activeListeningScoreText != null)
        {
            activeListeningScoreText.text = string.Format(activeListeningFormat, scores.activeListening);
        }

        // Exibe pontuação de Autoconsciência
        if (selfAwarenessScoreText != null)
        {
            selfAwarenessScoreText.text = string.Format(selfAwarenessFormat, scores.selfAwareness);
        }
    }

    /// <summary>
    /// Anima as barras segmentadas com as pontuações das habilidades.
    /// </summary>
    /// <param name="scores">Pontuações das habilidades</param>
    private System.Collections.IEnumerator AnimateSkillBars(SkillScores scores)
    {
        // Aguarda um pouco antes de iniciar as animações
        yield return new WaitForSeconds(0.5f);

        // Anima a barra de Empatia
        if (empathyBar != null)
        {
            yield return StartCoroutine(empathyBar.AnimateBar(scores.empathy));
        }

        // Anima a barra de Escuta Ativa
        if (activeListeningBar != null)
        {
            yield return StartCoroutine(activeListeningBar.AnimateBar(scores.activeListening));
        }

        // Anima a barra de Autoconsciência
        if (selfAwarenessBar != null)
        {
            yield return StartCoroutine(selfAwarenessBar.AnimateBar(scores.selfAwareness));
        }

        Debug.Log("[ScreenResult] Animação das barras segmentadas concluída");
    }

    /// <summary>
    /// Inicializa os CanvasGroups para o estado inicial.
    /// </summary>
    private void InitializeCanvasGroups()
    {
        // Mostra o grupo "aguardando cartão", esconde o grupo "sucesso"
        if (waitingForCardGroup != null)
        {
            waitingForCardGroup.alpha = 1f;
            waitingForCardGroup.interactable = true;
            waitingForCardGroup.blocksRaycasts = true;
        }

        if (postSuccessGroup != null)
        {
            postSuccessGroup.alpha = 0f;
            postSuccessGroup.interactable = false;
            postSuccessGroup.blocksRaycasts = false;
        }

        // Esconde o botão "Finalizar" inicialmente
        if (finishButton != null)
        {
            finishButton.SetActive(false);
        }
    }

    /// <summary>
    /// Método público chamado pelo NFCGameService quando o POST é bem-sucedido.
    /// Faz a transição para o segundo CanvasGroup e aguarda 5 segundos antes de voltar ao menu.
    /// </summary>
    public void OnPostSuccess()
    {
        Debug.Log("===========================================");
        Debug.Log("[ScreenResult] === OnPostSuccess CHAMADO ===");
        Debug.Log("===========================================");
        Debug.Log($"[ScreenResult] Timestamp: {System.DateTime.Now:HH:mm:ss.fff}");
        Debug.Log($"[ScreenResult] GameObject ativo? {gameObject.activeInHierarchy}");
        Debug.Log($"[ScreenResult] Component habilitado? {enabled}");
        Debug.Log("[ScreenResult] Iniciando transição para canvas de sucesso...");

        // Para o timer de auto-restart quando o NFC é lido
        StopAutoRestartTimer();

        // Toca o som do NFC (verificação de segurança)
        if (nfcClip != null)
        {
            nfcClip.Play();
            Debug.Log("[ScreenResult] Som do NFC tocando!");
        }
        else
        {
            Debug.LogWarning("[ScreenResult] nfcClip não configurado no Inspector!");
        }

        Debug.Log("[ScreenResult] Chamando StartCoroutine(TransitionToSuccessAndExit())...");
        StartCoroutine(TransitionToSuccessAndExit());
        Debug.Log("[ScreenResult] StartCoroutine chamado com sucesso!");
    }

    /// <summary>
    /// Faz a transição suave entre os CanvasGroups e volta ao menu após 30 segundos.
    /// </summary>
    private System.Collections.IEnumerator TransitionToSuccessAndExit()
    {
        Debug.Log("[ScreenResult] === Coroutine TransitionToSuccessAndExit INICIADA ===");

        float transitionDuration = 1f;
        float elapsedTime = 0f;

        Debug.Log("[ScreenResult] Iniciando fade out do waitingForCardGroup e fade in do postSuccessGroup...");

        // Transição suave entre os grupos
        while (elapsedTime < transitionDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / transitionDuration;

            if (waitingForCardGroup != null)
            {
                waitingForCardGroup.alpha = Mathf.Lerp(1f, 0f, t);
            }

            if (postSuccessGroup != null)
            {
                postSuccessGroup.alpha = Mathf.Lerp(0f, 1f, t);
            }

            yield return null;
        }

        Debug.Log("[ScreenResult] Transição de fade concluída!");

        // Garante que a transição está completa
        if (waitingForCardGroup != null)
        {
            waitingForCardGroup.alpha = 0f;
            waitingForCardGroup.interactable = false;
            waitingForCardGroup.blocksRaycasts = false;
            Debug.Log("[ScreenResult] waitingForCardGroup desativado");
        }

        if (postSuccessGroup != null)
        {
            postSuccessGroup.alpha = 1f;
            postSuccessGroup.interactable = true;
            postSuccessGroup.blocksRaycasts = true;
            Debug.Log("[ScreenResult] postSuccessGroup ativado");
        }

        // Desliga o GameObject configurado (se houver)
        if (objectToDisableAfterPost != null)
        {
            objectToDisableAfterPost.SetActive(false);
            Debug.Log("[ScreenResult] GameObject desligado após POST bem-sucedido");
        }

        // Muda o título para "OBRIGADO!" usando LocalizedText
        if (thankYouLocalizedText != null)
        {
            thankYouLocalizedText.SetLocalizationKey("game_over", "texto3");
            Debug.Log("[ScreenResult] Título mudado para LocalizedText: game_over.texto3");
        }
        else if (thankYouTitleText != null)
        {
            // Fallback se não tiver LocalizedText configurado
            thankYouTitleText.text = "OBRIGADO!";
            Debug.Log("[ScreenResult] Título mudado para 'OBRIGADO!' (fallback)");
        }

        // Mostra o botão "Finalizar" e atualiza seu texto
        if (finishButton != null)
        {
            finishButton.SetActive(true);

            // Atualiza o texto do botão usando LocalizedText
            if (finishButtonLocalizedText != null)
            {
                finishButtonLocalizedText.SetLocalizationKey("game_over", "botaoFinalizar");
                Debug.Log("[ScreenResult] Botão 'Finalizar' ativado com LocalizedText: game_over.botaoFinalizar");
            }
            else
            {
                Debug.Log("[ScreenResult] Botão 'Finalizar' ativado (sem LocalizedText configurado)");
            }
        }

        Debug.Log("[ScreenResult] ✓ Segundo canvas apareceu completamente. Aguardando 5 segundos...");

        // Aguarda 5 segundos APÓS o segundo canvas aparecer completamente
        yield return new WaitForSeconds(5f);

        // Volta para a scene 0 (menu principal)
        Debug.Log("[ScreenResult] Carregando scene 0 (menu principal)...");
        SceneManager.LoadScene(0);
        Debug.Log("[ScreenResult] LoadScene(0) chamado!");
    }

    /// <summary>
    /// Método público para ser chamado pelo botão "Finalizar".
    /// Volta imediatamente para o menu principal.
    /// </summary>
    public void OnFinishButtonClicked()
    {
        Debug.Log("[ScreenResult] Botão 'Finalizar' clicado! Voltando ao menu principal...");
        StopAutoRestartTimer();
        SceneManager.LoadScene(0);
    }

    /// <summary>
    /// Exibe a pontuação total na UI.
    /// </summary>
    /// <param name="finalScore">Pontuação final</param>
    private void DisplayTotalScore(int finalScore)
    {
        if (totalScoreText != null)
        {
            totalScoreText.text = string.Format(totalScoreFormat, finalScore);
        }
    }

    /// <summary>
    /// Exibe a mensagem final da Aya.
    /// </summary>
    private void DisplayFinalMessage()
    {
        if (finalMessageText != null)
        {
            finalMessageText.text = "Candidato, Empatia não resolve todos os problemas, mas ela muda a forma como você os enfrenta. " +
                                   "Cada decisão contribuiu para fortalecer suas habilidades de Empatia, Escuta Ativa e Autoconsciência, " +
                                   "que foram pontuadas ao longo desta experiência.<br><br>Aproxime seu cartão.";
        }
    }

    /// <summary>
    /// Exibe as palavras mais pontuadas da nuvem de palavras.
    /// </summary>
    private void DisplayTopWords()
    {
        if (topWordsText != null && WordCloudDisplay.Instance != null)
        {
            var topWords = WordCloudDisplay.Instance.GetTopWords(5);

            if (topWords.Count > 0)
            {
                string topWordsMessage = "Palavras mais selecionadas:\n";

                for (int i = 0; i < topWords.Count; i++)
                {
                    var word = topWords[i];
                    topWordsMessage += $"{i + 1}. {word.Key}: {word.Value} pontos\n";
                }

                topWordsText.text = topWordsMessage.TrimEnd('\n');
            }
            else
            {
                topWordsText.text = "Nenhuma palavra pontuada ainda.";
            }
        }
    }

    #endregion

    #region Public Utility Methods

    /// <summary>
    /// Método para reiniciar o jogo (pode ser chamado por um botão "Jogar Novamente").
    /// </summary>
    public void RestartGame()
    {
        ScreenManager.SetCallScreen("cta");
    }

    /// <summary>
    /// Método para voltar ao menu principal (pode ser usado por um botão "Menu").
    /// </summary>
    public void GoToMainMenu()
    {
        ScreenManager.SetCallScreen("cta");
    }

    #endregion

    #region Word Cloud Integration

    /// <summary>
    /// Atualiza a nuvem de palavras com as pontuações das habilidades.
    /// </summary>
    /// <param name="scores">Pontuações das habilidades</param>
    private void UpdateWordCloudWithSkillScores(SkillScores scores)
    {
        if (WordCloudDisplay.Instance != null)
        {
            // Adiciona pontos às palavras-chave de cada habilidade na nuvem de palavras

            // Empatia - palavras relacionadas
            WordCloudDisplay.Instance.AddWordPoints("Empatia", scores.empathy);
            WordCloudDisplay.Instance.AddWordPoints("Respeito ao cliente", scores.empathy);
            WordCloudDisplay.Instance.AddWordPoints("Compromisso", scores.empathy);

            // Escuta Ativa - palavras relacionadas  
            WordCloudDisplay.Instance.AddWordPoints("Colaboração", scores.activeListening);
            WordCloudDisplay.Instance.AddWordPoints("Parceria", scores.activeListening);
            WordCloudDisplay.Instance.AddWordPoints("Resolução de problemas", scores.activeListening);

            // Autoconsciência - palavras relacionadas
            WordCloudDisplay.Instance.AddWordPoints("Adaptação", scores.selfAwareness);
            WordCloudDisplay.Instance.AddWordPoints("Resiliência", scores.selfAwareness);
            WordCloudDisplay.Instance.AddWordPoints("Estratégia", scores.selfAwareness);

            Debug.Log("[ScreenResult] Pontuações das habilidades adicionadas à nuvem de palavras");
        }
        else
        {
            Debug.LogWarning("[ScreenResult] WordCloudDisplay.Instance não encontrado para atualizar pontuações");
        }
    }

    #endregion

    #region NFC Integration

    /// <summary>
    /// Envia dados das habilidades para o sistema NFC.
    /// </summary>
    /// <param name="scores">Pontuações das habilidades</param>
    private void SubmitToNFCSystem(SkillScores scores)
    {
        Debug.Log($"[ScreenResult] === SubmitToNFCSystem CHAMADO === Empathy: {scores.empathy}, ActiveListening: {scores.activeListening}, SelfAwareness: {scores.selfAwareness}");

        if (NFCGameService.Instance != null)
        {
            Debug.Log("[ScreenResult] NFCGameService.Instance encontrado! Chamando SubmitGameResult...");
            NFCGameService.Instance.SubmitGameResult(scores.empathy, scores.activeListening, scores.selfAwareness);
            Debug.Log("[ScreenResult] ✓ SubmitGameResult chamado com sucesso. Dados pendentes armazenados.");
        }
        else
        {
            Debug.LogError("[ScreenResult] ✗ NFCGameService.Instance é NULL! Sistema NFC não está inicializado!");
        }
    }

    #endregion

    #region Debug Methods

    [ContextMenu("Test Show Results")]
    public void TestShowResults()
    {
        ShowFinalResults(10); // Simula uma pontuação alta para teste
    }

    [ContextMenu("Test Show Results - Low Score")]
    public void TestShowResultsLowScore()
    {
        ShowFinalResults(3); // Simula uma pontuação baixa para teste
    }

    [ContextMenu("Test NFC Submit")]
    public void TestNFCSubmit()
    {
        SkillScores testScores = new SkillScores(9, 7, 5);
        SubmitToNFCSystem(testScores);
    }

    [ContextMenu("Test Post Success Transition")]
    public void TestPostSuccessTransition()
    {
        OnPostSuccess();
    }

    #endregion

    #region Timer Methods

    /// <summary>
    /// Inicia o timer de auto-reinicialização.
    /// </summary>
    private void StartAutoRestartTimer()
    {
        // Ativa o GameObject do timer
        if (timerGameObject != null)
        {
            timerGameObject.SetActive(true);
            Debug.Log("[ScreenResult] GameObject do timer ativado");
        }

        currentTime = autoRestartTime;
        initialTime = autoRestartTime;
        timerActive = true;
        UpdateTimerUI();
        Debug.Log($"[ScreenResult] Timer de auto-restart iniciado: {autoRestartTime} segundos");
    }

    /// <summary>
    /// Para o timer de auto-reinicialização.
    /// </summary>
    private void StopAutoRestartTimer()
    {
        timerActive = false;

        // Desativa o GameObject do timer
        if (timerGameObject != null)
        {
            timerGameObject.SetActive(false);
            Debug.Log("[ScreenResult] GameObject do timer desativado");
        }

        Debug.Log("[ScreenResult] Timer de auto-restart parado");
    }

    /// <summary>
    /// Gerencia o timer de auto-reinicialização.
    /// </summary>
    private void HandleAutoRestartTimer()
    {
        currentTime -= Time.deltaTime;

        if (currentTime <= 0)
        {
            currentTime = 0;
            timerActive = false;
            RestartApplication();
        }

        UpdateTimerUI();
    }

    /// <summary>
    /// Atualiza o timer na UI.
    /// </summary>
    private void UpdateTimerUI()
    {
        UpdateTimerFill();

        if (timerText != null)
        {
            int secondsRemaining = Mathf.CeilToInt(currentTime);
            if (secondsRemaining < 0) secondsRemaining = 0;
            timerText.text = $"{secondsRemaining}";
        }
    }

    /// <summary>
    /// Atualiza o fill da imagem do timer.
    /// </summary>
    private void UpdateTimerFill()
    {
        if (timerFillImage == null) return;

        if (initialTime <= 0f)
        {
            timerFillImage.fillAmount = 0f;
            return;
        }

        float normalizedTime = Mathf.Clamp01(currentTime / initialTime);
        timerFillImage.fillAmount = normalizedTime;
    }

    /// <summary>
    /// Reinicia a aplicação carregando a scene 0.
    /// </summary>
    private void RestartApplication()
    {
        Debug.Log("[ScreenResult] Tempo esgotado! Reiniciando aplicação...");
        SceneManager.LoadScene(0);
    }

    #endregion
}