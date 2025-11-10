using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using FIRJAN.Utilities;
using FIRJAN.UI;

/// <summary>
/// Script para gerenciar a lógica principal do Jogo da Empatia.
/// Controla as 3 rodadas do jogo e calcula a pontuação de empatia.
/// 
/// COMPORTAMENTO DAS RODADAS:
/// 1. Mostra primeira imagem com fade in e descrição da situação específica da rodada
/// 2. Após 10 segundos, segunda imagem aparece ao lado com fade in e pergunta "Qual sua opinião sobre isso?"
/// 3. Exibe as opções de palavras para seleção
/// 4. Após confirmação, avança para próxima rodada ou vai para resultados
/// 
/// SITUAÇÕES DAS RODADAS:
/// Situação 1: Em uma reunião online importante com um cliente, Marcos não liga a câmera.
/// Situação 2: Funcionário chega atrasado e de moletom numa reunião super importante com clientes.
/// Situação 3: Funcionária toma café com outro colaborador enquanto o resto da equipe trabalha sem parar.
/// 
/// CONFIGURAÇÃO DAS IMAGENS:
/// - Arraste as 6 sprites nos campos: round1Image1, round1Image2, round2Image1, round2Image2, round3Image1, round3Image2
/// - UI References: imageDisplay1 + CanvasGroup, imageDisplay2 + CanvasGroup, descriptionText, wordsContainer, etc.
/// - Efeito de fade controlado por fadeDuration (padrão 1 segundo)
/// </summary>
public class ScreenGame : CanvasScreen
{
    #region Data Structures

    /// <summary>
    /// Estrutura para armazenar dados de uma escolha de palavra.
    /// </summary>
    [System.Serializable]
    public class WordChoice
    {
        [Tooltip("Texto da palavra que será exibida")]
        public string wordText;

        [Tooltip("Define se esta palavra é considerada empática")]
        public bool isEmpathetic;
    }

    /// <summary>
    /// Estrutura para armazenar dados de uma rodada do jogo.
    /// </summary>
    [System.Serializable]
    public class RoundData
    {
        [Tooltip("Lista de palavras disponíveis para escolha nesta rodada")]
        public List<WordChoice> words = new List<WordChoice>();
    }

    #endregion

    #region Public Fields

    [Header("Game Data")]
    [Tooltip("Dados das 3 rodadas do jogo")]
    public List<RoundData> gameRounds = new List<RoundData>();

    [Header("Round Images - 6 Sprites Total")]
    [Tooltip("Imagem 1 da Rodada 1")]
    public Sprite round1Image1;
    [Tooltip("Imagem 2 da Rodada 1")]
    public Sprite round1Image2;
    [Tooltip("Imagem 1 da Rodada 2")]
    public Sprite round2Image1;
    [Tooltip("Imagem 2 da Rodada 2")]
    public Sprite round2Image2;
    [Tooltip("Imagem 1 da Rodada 3")]
    public Sprite round3Image1;
    [Tooltip("Imagem 2 da Rodada 3")]
    public Sprite round3Image2;

    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI headerText;
    [SerializeField] private Image singleImageDisplay;
    [SerializeField] private CanvasGroup singleImageCanvasGroup;
    [SerializeField] private Image imageDisplay1;
    [SerializeField] private CanvasGroup imageDisplay1CanvasGroup;
    [SerializeField] private Image imageDisplay2;
    [SerializeField] private CanvasGroup imageDisplay2CanvasGroup;
    [SerializeField] private TextMeshProUGUI descriptionText;
    [SerializeField] private Transform wordsContainer;
    [SerializeField] private WordButtonAnimator wordButtonAnimator; // Componente para animar os botões
    [SerializeField] private CanvasFadeIn wordsContainerFadeIn; // Componente para fazer fade in do container de palavras
    [SerializeField] private Button wordButtonPrefab;
    [SerializeField] private Button confirmButton;
    [SerializeField] private TextMeshProUGUI feedbackText;
    [Header("Word Cloud Summary References")]
    [SerializeField] private CanvasGroup inRoundCanvasGroup;
    [SerializeField] private CanvasGroup wordCloudSummaryCanvasGroup;
    [SerializeField] private TextMeshProUGUI confirmButtonText;
    [SerializeField] private string confirmButtonDefaultLabel = "Confirmar";
    [SerializeField] private string confirmButtonContinueLabel = "Continuar";

    [Header("Screen Settings")]
    [SerializeField] private string resultScreenName = "results";
    [SerializeField] private float imageTransitionDelay = 10f;
    [SerializeField] private float fadeDuration = 1f;
    [SerializeField] private string questionDescription = "Qual sua opinião sobre isso?";

    // [Header("Situações das Rodadas")] // DEPRECATED: Agora usa LocalizedText
    // [TextArea(3, 5)]
    // [SerializeField] private string situation1Description = "Em uma reunião online importante com um cliente, Marcos não liga a câmera.";
    // [TextArea(3, 5)]
    // [SerializeField] private string situation2Description = "Funcionário chega atrasado e de moletom numa reunião super importante com clientes.";
    // [TextArea(3, 5)]
    // [SerializeField] private string situation3Description = "Funcionária toma café com outro colaborador enquanto o resto da equipe trabalha sem parar.";

    [Header("Localized Text")]
    [SerializeField] private LocalizedText localizedTextHeader;
    [SerializeField] private LocalizedText descriptionLocalizedText;
    [SerializeField] private LocalizedText confirmButtonLocalizedText;

    #endregion

    #region Private Fields

    private int currentRoundIndex = 0;
    private int totalEmpatheticScore = 0;
    private List<Button> currentWordButtons = new List<Button>();
    private List<bool> selectedWords = new List<bool>();
    private bool isAwaitingSummaryContinue = false;
    private List<WordData> currentRoundWordData = new List<WordData>(); // Dados da rodada atual

    #endregion

    #region Unity Callbacks

    public override void OnEnable()
    {
        base.OnEnable();

        // Reinicia o jogo quando a tela é ativada
        ResetGame();
    }

    #endregion

    #region Game Logic

    /// <summary>
    /// Reinicia o jogo para o estado inicial.
    /// </summary>
    private void ResetGame()
    {
        // Garante que as rodadas estejam inicializadas
        if (gameRounds.Count == 0)
        {
            InitializeDefaultRounds();
        }

        currentRoundIndex = 0;
        totalEmpatheticScore = 0;
        isAwaitingSummaryContinue = false;

        // Limpa a UI
        ClearWordButtons();
        if (feedbackText != null)
            feedbackText.text = "";
        if (descriptionText != null)
            descriptionText.text = "";
        if (headerText != null)
            headerText.text = "";

        if (wordCloudSummaryCanvasGroup != null)
        {
            wordCloudSummaryCanvasGroup.alpha = 0f;
            wordCloudSummaryCanvasGroup.interactable = false;
            wordCloudSummaryCanvasGroup.blocksRaycasts = false;
            wordCloudSummaryCanvasGroup.gameObject.SetActive(false);
        }

        if (inRoundCanvasGroup != null)
        {
            inRoundCanvasGroup.alpha = 1f;
            inRoundCanvasGroup.interactable = true;
            inRoundCanvasGroup.blocksRaycasts = true;
        }

        // Reseta os displays de imagem
        if (singleImageDisplay != null)
        {
            singleImageDisplay.sprite = null;
            singleImageDisplay.gameObject.SetActive(false);
        }
        if (singleImageCanvasGroup != null)
            singleImageCanvasGroup.alpha = 0f;

        if (imageDisplay1 != null)
        {
            imageDisplay1.sprite = null;
            imageDisplay1.gameObject.SetActive(false);
        }
        if (imageDisplay1CanvasGroup != null)
            imageDisplay1CanvasGroup.alpha = 0f;

        if (imageDisplay2 != null)
        {
            imageDisplay2.sprite = null;
            imageDisplay2.gameObject.SetActive(false);
        }
        if (imageDisplay2CanvasGroup != null)
            imageDisplay2CanvasGroup.alpha = 0f;

        UpdateConfirmButtonLabel(true); // true = "Confirmar"

        // Inicia a primeira rodada
        StartRound(currentRoundIndex);
    }

    /// <summary>
    /// Inicia uma rodada específica do jogo.
    /// </summary>
    /// <param name="roundIndex">Índice da rodada (0-2)</param>
    private void StartRound(int roundIndex)
    {
        if (roundIndex >= gameRounds.Count)
        {
            Debug.LogError($"Índice de rodada inválido: {roundIndex}");
            return;
        }

        RoundData currentRound = gameRounds[roundIndex];
        isAwaitingSummaryContinue = false;

        // Carrega os dados salvos da rodada ou cria dados padrão
        LoadRoundWordData(roundIndex, currentRound);

        // Esconde as palavras e botão de confirmar inicialmente
        if (wordsContainer != null)
            wordsContainer.gameObject.SetActive(false);
        if (confirmButton != null)
        {
            confirmButton.gameObject.SetActive(false);
            confirmButton.interactable = true; // Sempre deixa interativo quando for aparecer
        }

        // Limpa o texto de feedback
        if (feedbackText != null)
            feedbackText.text = "";

        // Inicia a sequência de imagens
        StartCoroutine(RoundImageSequence(roundIndex, currentRound));
    }

    /// <summary>
    /// Corrotina para gerenciar a sequência de imagens de uma rodada.
    /// Mostra primeira imagem sozinha, depois as duas imagens lado a lado após confirmar.
    /// </summary>
    private IEnumerator RoundImageSequence(int roundIndex, RoundData roundData)
    {
        Sprite image1 = GetImage1ForRound(roundIndex);

        // Fase 1: Mostra primeira imagem sozinha no componente separado
        if (singleImageDisplay != null && image1 != null)
        {
            singleImageDisplay.sprite = image1;
            singleImageDisplay.gameObject.SetActive(true);
        }

        // Atualiza o header com o número da situação
        if (headerText != null)
        {
            var roundNumber = roundIndex + 1;
            headerText.text = $"Situação {roundNumber}";
            localizedTextHeader.SetFormatVariables(roundNumber.ToString());
        }

        // Atualiza a descrição da situação usando LocalizedText
        UpdateSituationDescription(roundIndex);

        // Fade in da primeira imagem
        if (singleImageCanvasGroup != null)
        {
            yield return StartCoroutine(FadeCanvasGroup(singleImageCanvasGroup, 0f, 1f, fadeDuration));
        }

        // Cria os botões de palavras e mostra opções imediatamente após a primeira imagem
        CreateWordButtons(roundData.words);

        if (wordsContainer != null)
            wordsContainer.gameObject.SetActive(true);

        // Aguarda a tela estar ligada antes de fazer o fade in
        while (!IsOn())
        {
            yield return null;
        }

        // Faz o fade in do container de palavras
        if (wordsContainerFadeIn != null)
        {
            wordsContainerFadeIn.DoFadeIn();
        }

        if (confirmButton != null)
        {
            confirmButton.gameObject.SetActive(true);
            confirmButton.interactable = true;
            UpdateConfirmButtonLabel(true); // true = "Confirmar"
            Debug.Log("Botão de confirmar ativado e interativo");
        }
    }

    /// <summary>
    /// Corrotina para fazer fade in/out de um CanvasGroup.
    /// </summary>
    /// <param name="canvasGroup">O CanvasGroup a ser animado</param>
    /// <param name="startAlpha">Valor inicial do alpha</param>
    /// <param name="endAlpha">Valor final do alpha</param>
    /// <param name="duration">Duração da animação</param>
    private IEnumerator FadeCanvasGroup(CanvasGroup canvasGroup, float startAlpha, float endAlpha, float duration)
    {
        float elapsedTime = 0f;
        canvasGroup.alpha = startAlpha;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float progress = elapsedTime / duration;
            canvasGroup.alpha = Mathf.Lerp(startAlpha, endAlpha, progress);
            yield return null;
        }

        canvasGroup.alpha = endAlpha;
    }

    /// <summary>
    /// Cria os botões de palavras para a rodada atual.
    /// </summary>
    private void CreateWordButtons(List<WordChoice> words)
    {
        // Limpa botões anteriores
        ClearWordButtons();

        // Inicializa as listas
        selectedWords.Clear();

        // Determina qual section usar baseado na rodada atual
        string section = $"situation{currentRoundIndex + 1}";

        for (int i = 0; i < words.Count; i++)
        {
            WordChoice word = words[i];

            if (wordButtonPrefab != null && wordsContainer != null)
            {
                Button wordButton = Instantiate(wordButtonPrefab, wordsContainer);

                // Tenta configurar o LocalizedText se existir no prefab
                LocalizedText localizedText = wordButton.GetComponentInChildren<LocalizedText>();
                if (localizedText != null)
                {
                    // Define a key como opcao1, opcao2, etc.
                    string key = $"opcao{i + 1}";
                    localizedText.SetLocalizationKey(section, key);
                }
                else
                {
                    // Fallback: usa o texto diretamente se não tiver LocalizedText
                    TextMeshProUGUI buttonText = wordButton.GetComponentInChildren<TextMeshProUGUI>();
                    if (buttonText != null)
                    {
                        buttonText.text = word.wordText;
                    }
                    Debug.LogWarning($"LocalizedText não encontrado no wordButtonPrefab. Usando texto hardcoded.");
                }

                // Configura o toggle behavior
                int wordIndex = currentWordButtons.Count;
                selectedWords.Add(false);

                wordButton.onClick.AddListener(() => ToggleWordSelection(wordIndex));

                currentWordButtons.Add(wordButton);
            }
        }

        // Animar os botões aparecendo de cima para baixo (com delay para aguardar fade-in da tela)
        if (wordButtonAnimator != null)
        {
            StartCoroutine(AnimateButtonsAfterDelay());
        }
    }

    /// <summary>
    /// Anima os botões após um delay para aguardar o fade-in da tela.
    /// </summary>
    private IEnumerator AnimateButtonsAfterDelay()
    {
        // Aguarda a tela estar ligada antes de animar
        while (!IsOn())
        {
            yield return null;
        }

        yield return new WaitForSeconds(0.5f); // Aguarda o fade-in da tela
        if (wordButtonAnimator != null)
        {
            wordButtonAnimator.AnimateButtons();
        }
    }

    /// <summary>
    /// Alterna a seleção de uma palavra.
    /// </summary>
    private void ToggleWordSelection(int wordIndex)
    {
        if (wordIndex >= 0 && wordIndex < selectedWords.Count)
        {
            selectedWords[wordIndex] = !selectedWords[wordIndex];

            // Atualiza o visual do botão
            Button button = currentWordButtons[wordIndex];
            ColorBlock colors = button.colors;

            if (selectedWords[wordIndex])
            {
                colors.normalColor = Color.green;
            }
            else
            {
                colors.normalColor = Color.white;
            }

            button.colors = colors;
        }
    }

    /// <summary>
    /// Remove todos os botões de palavras da UI.
    /// </summary>
    private void ClearWordButtons()
    {
        foreach (Button button in currentWordButtons)
        {
            if (button != null)
            {
                DestroyImmediate(button.gameObject);
            }
        }

        currentWordButtons.Clear();
        selectedWords.Clear();
    }

    /// <summary>
    /// Método público chamado pelo botão principal. Controla fases de confirmação e resumo.
    /// </summary>
    public void OnConfirmButtonPressed()
    {
        if (isAwaitingSummaryContinue)
        {
            HandleSummaryContinue();
        }
        else
        {
            ConfirmChoices();
        }
    }

    /// <summary>
    /// Processa as escolhas da rodada atual.
    /// </summary>
    private void ConfirmChoices()
    {
        Debug.Log($"ConfirmChoices chamado - Rodada atual: {currentRoundIndex}, Total de rodadas: {gameRounds.Count}");

        if (currentRoundIndex >= gameRounds.Count)
        {
            Debug.LogError($"Índice de rodada inválido: {currentRoundIndex} >= {gameRounds.Count}");
            return;
        }

        RoundData currentRound = gameRounds[currentRoundIndex];
        int roundEmpatheticScore = 0;

        Debug.Log($"Palavras selecionadas: {selectedWords.Count}, Palavras da rodada: {currentRound.words.Count}");

        // Atualiza os dados das palavras selecionadas
        for (int i = 0; i < selectedWords.Count && i < currentRound.words.Count && i < currentRoundWordData.Count; i++)
        {
            if (selectedWords[i])
            {
                // Incrementa a pontuação cumulativa da palavra
                currentRoundWordData[i].cumulativePoints += 1;
                Debug.Log($"Palavra '{currentRound.words[i].wordText}' incrementada para {currentRoundWordData[i].cumulativePoints} pontos cumulativos");

                // Verifica se é empática para pontuação
                if (currentRound.words[i].isEmpathetic)
                {
                    roundEmpatheticScore++;
                    Debug.Log($"Palavra empática selecionada: {currentRound.words[i].wordText}");
                }
            }
        }

        // Salva os dados atualizados
        SaveRoundWordData(currentRoundIndex);

        // Atualiza a nuvem de palavras com os novos dados
        if (WordCloudDisplay.Instance != null)
        {
            WordCloudDisplay.Instance.LoadRoundData(currentRoundWordData);
        }

        // Adiciona ao score total
        totalEmpatheticScore += roundEmpatheticScore;
        Debug.Log($"Score da rodada: {roundEmpatheticScore}, Score total: {totalEmpatheticScore}");

        // Mostra a segunda imagem e a nuvem de palavras
        StartCoroutine(ShowSecondImageAndWordCloud());
    }

    /// <summary>
    /// Corrotina para mostrar as duas imagens lado a lado e a nuvem de palavras.
    /// </summary>
    private IEnumerator ShowSecondImageAndWordCloud()
    {
        Sprite image1 = GetImage1ForRound(currentRoundIndex);
        Sprite image2 = GetImage2ForRound(currentRoundIndex);

        // Esconde a imagem única
        if (singleImageCanvasGroup != null)
        {
            yield return StartCoroutine(FadeCanvasGroup(singleImageCanvasGroup, 1f, 0f, fadeDuration * 0.5f));
            singleImageDisplay.gameObject.SetActive(false);
        }

        // Prepara as duas imagens lado a lado
        if (imageDisplay1 != null && image1 != null)
        {
            imageDisplay1.sprite = image1;
            imageDisplay1.gameObject.SetActive(true);
        }

        if (imageDisplay2 != null && image2 != null)
        {
            imageDisplay2.sprite = image2;
            imageDisplay2.gameObject.SetActive(true);
        }

        // Fade in das duas imagens simultaneamente
        if (imageDisplay1CanvasGroup != null && imageDisplay2CanvasGroup != null)
        {
            StartCoroutine(FadeCanvasGroup(imageDisplay1CanvasGroup, 0f, 1f, fadeDuration));
            yield return StartCoroutine(FadeCanvasGroup(imageDisplay2CanvasGroup, 0f, 1f, fadeDuration));
        }

        // Muda a descrição para o texto de resultados após as imagens aparecerem
        if (descriptionLocalizedText != null)
        {
            descriptionLocalizedText.SetLocalizationKey("situation_results", "texto1");
        }

        // Exibe mensagem de feedback da Aya
        ShowAyaFeedback();

        // Mostra o resumo da rodada com nuvem de palavras
        ShowRoundSummary();
    }

    /// <summary>
    /// Exibe a mensagem de feedback da Aya.
    /// DEPRECATED: O texto agora é mostrado via descriptionLocalizedText usando situation_results.texto1
    /// </summary>
    private void ShowAyaFeedback()
    {
        // O feedback agora é mostrado via descriptionLocalizedText (situation_results.texto1)
        // Mantido por compatibilidade, mas não faz nada
        /*
        if (feedbackText != null)
        {
            feedbackText.text = "As palavras maiores foram as mais escolhidas por você e por outros participantes, " +
                               "revelando pontos de empatia compartilhados em comum. Já as palavras menores representam " +
                               "escolhas menos frequentes, mas igualmente importantes, pois mostram sua visão única da situação.";
        }
        */
    }

    /// <summary>
    /// Corrotina para avançar para a próxima rodada após um delay.
    /// </summary>
    private IEnumerator AdvanceToNextRound()
    {
        yield return null; // garante execução no próximo frame sem atrasos visíveis

        currentRoundIndex++;
        Debug.Log($"Avançando para rodada {currentRoundIndex}");

        if (currentRoundIndex < gameRounds.Count)
        {
            // Ainda há rodadas, continua o jogo
            Debug.Log($"Iniciando rodada {currentRoundIndex}");

            // Reseta as imagens antes de iniciar a próxima rodada
            ResetImagesForNewRound();

            StartRound(currentRoundIndex);
        }
        else
        {
            // Fim do jogo, vai para a tela de resultado
            Debug.Log("Fim do jogo! Indo para tela de resultados...");
            FinishGame();
        }
    }

    /// <summary>
    /// Finaliza o jogo e vai para a tela de resultado.
    /// </summary>
    private void FinishGame()
    {
        // Procura pela tela de resultado e passa a pontuação
        ScreenResult resultScreen = FindFirstObjectByType<ScreenResult>();
        if (resultScreen != null)
        {
            resultScreen.ShowFinalResults(totalEmpatheticScore);
        }

        // Ativa a tela de resultado
        ScreenManager.SetCallScreen(resultScreenName);
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Carrega os dados de pontuação da rodada do JSON ou cria dados padrão.
    /// </summary>
    /// <param name="roundIndex">Índice da rodada (0-2)</param>
    /// <param name="roundData">Dados da rodada com as palavras</param>
    private void LoadRoundWordData(int roundIndex, RoundData roundData)
    {
        if (WordScorePersistence.Instance == null)
        {
            Debug.LogWarning("[ScreenGame] WordScorePersistence.Instance é nulo. Criando dados padrão.");
            currentRoundWordData = CreateDefaultWordData(roundData);

            // IMPORTANTE: Mesmo sem persistência, carrega os dados na nuvem
            if (WordCloudDisplay.Instance != null)
            {
                WordCloudDisplay.Instance.LoadRoundData(currentRoundWordData);
                Debug.Log($"[ScreenGame] Dados padrão carregados na nuvem: {currentRoundWordData.Count} palavras");
            }
            return;
        }

        // Cria lista com os textos das palavras
        List<string> wordTexts = new List<string>();
        foreach (var word in roundData.words)
        {
            wordTexts.Add(word.wordText);
        }

        // Carrega dados salvos ou cria dados padrão
        currentRoundWordData = WordScorePersistence.Instance.LoadRoundData(roundIndex, wordTexts);

        // Garante que os textos estejam corretos (caso o JSON esteja desatualizado)
        for (int i = 0; i < currentRoundWordData.Count && i < roundData.words.Count; i++)
        {
            if (currentRoundWordData[i].text != roundData.words[i].wordText)
            {
                currentRoundWordData[i].text = roundData.words[i].wordText;
            }
        }

        // Carrega os dados na nuvem de palavras
        if (WordCloudDisplay.Instance != null)
        {
            WordCloudDisplay.Instance.LoadRoundData(currentRoundWordData);
            Debug.Log($"[ScreenGame] Dados da rodada {roundIndex + 1} carregados na nuvem: {currentRoundWordData.Count} palavras");
        }
        else
        {
            Debug.LogError("[ScreenGame] WordCloudDisplay.Instance é nulo! Não foi possível carregar dados na nuvem.");
        }
    }

    /// <summary>
    /// Cria dados padrão para uma rodada.
    /// </summary>
    /// <param name="roundData">Dados da rodada</param>
    /// <returns>Lista de WordData inicializados</returns>
    private List<WordData> CreateDefaultWordData(RoundData roundData)
    {
        List<WordData> wordDataList = new List<WordData>();

        foreach (var word in roundData.words)
        {
            wordDataList.Add(new WordData(word.wordText, 1, 1));
        }

        return wordDataList;
    }

    /// <summary>
    /// Salva os dados de pontuação da rodada no JSON.
    /// </summary>
    /// <param name="roundIndex">Índice da rodada (0-2)</param>
    private void SaveRoundWordData(int roundIndex)
    {
        if (WordScorePersistence.Instance == null)
        {
            Debug.LogWarning("[ScreenGame] WordScorePersistence.Instance é nulo. Não foi possível salvar.");
            return;
        }

        bool success = WordScorePersistence.Instance.SaveRoundData(roundIndex, currentRoundWordData);

        if (success)
        {
            Debug.Log($"[ScreenGame] Dados da rodada {roundIndex + 1} salvos com sucesso");
        }
        else
        {
            Debug.LogError($"[ScreenGame] Falha ao salvar dados da rodada {roundIndex + 1}");
        }
    }

    /// <summary>
    /// Reseta as imagens para iniciar uma nova rodada.
    /// </summary>
    private void ResetImagesForNewRound()
    {
        // Reseta a imagem única
        if (singleImageDisplay != null)
        {
            singleImageDisplay.sprite = null;
            singleImageDisplay.gameObject.SetActive(false);
        }
        if (singleImageCanvasGroup != null)
            singleImageCanvasGroup.alpha = 0f;

        // Reseta a primeira imagem
        if (imageDisplay1 != null)
        {
            imageDisplay1.sprite = null;
            imageDisplay1.gameObject.SetActive(false);
        }
        if (imageDisplay1CanvasGroup != null)
            imageDisplay1CanvasGroup.alpha = 0f;

        // Reseta a segunda imagem
        if (imageDisplay2 != null)
        {
            imageDisplay2.sprite = null;
            imageDisplay2.gameObject.SetActive(false);
        }
        if (imageDisplay2CanvasGroup != null)
            imageDisplay2CanvasGroup.alpha = 0f;

        // Limpa o feedback text
        if (feedbackText != null)
            feedbackText.text = "";
    }

    /// <summary>
    /// Atualiza o texto do botão de confirmação usando LocalizedText.
    /// </summary>
    /// <param name="useConfirm">True para "Confirmar" (botao1), False para "Continuar" (botao2)</param>
    private void UpdateConfirmButtonLabel(bool useConfirm = true)
    {
        string targetText = useConfirm ? "CONFIRMAR" : "CONTINUAR";
        Debug.Log($"[UpdateConfirmButtonLabel] Tentando mudar para: {targetText}");

        if (confirmButtonLocalizedText != null)
        {
            string key = useConfirm ? "botao1" : "botao2";
            Debug.Log($"[UpdateConfirmButtonLabel] Usando LocalizedText com key: situation_results.{key}");
            confirmButtonLocalizedText.SetLocalizationKey("situation_results", key);
        }
        else
        {
            Debug.LogWarning("[UpdateConfirmButtonLabel] confirmButtonLocalizedText é NULL! Usando fallback.");
            // Fallback para o método antigo se não tiver LocalizedText
            string label = useConfirm ? confirmButtonDefaultLabel : confirmButtonContinueLabel;
            if (confirmButtonText != null)
            {
                confirmButtonText.text = label;
                Debug.Log($"[UpdateConfirmButtonLabel] Texto atualizado via confirmButtonText: {label}");
            }
            else if (confirmButton != null)
            {
                TextMeshProUGUI labelTMP = confirmButton.GetComponentInChildren<TextMeshProUGUI>();
                if (labelTMP != null)
                {
                    labelTMP.text = label;
                    Debug.Log($"[UpdateConfirmButtonLabel] Texto atualizado via GetComponentInChildren: {label}");
                }
                else
                {
                    Debug.LogError("[UpdateConfirmButtonLabel] Não encontrou TextMeshProUGUI no botão!");
                }
            }
            else
            {
                Debug.LogError("[UpdateConfirmButtonLabel] confirmButton é NULL!");
            }
        }
    }

    /// <summary>
    /// Exibe o resumo com a nuvem de palavras e as escolhas feitas na rodada.
    /// </summary>
    private void ShowRoundSummary()
    {
        isAwaitingSummaryContinue = true;

        // Mantém as imagens visíveis, só esconde os botões de palavra
        // Esconde todos os botões de palavra
        for (int i = 0; i < currentWordButtons.Count; i++)
        {
            if (currentWordButtons[i] != null)
            {
                currentWordButtons[i].gameObject.SetActive(false);
            }
        }

        if (wordsContainer != null)
        {
            wordsContainer.gameObject.SetActive(false);
        }

        if (wordCloudSummaryCanvasGroup != null)
        {
            wordCloudSummaryCanvasGroup.gameObject.SetActive(true);
            wordCloudSummaryCanvasGroup.alpha = 1f;
            wordCloudSummaryCanvasGroup.interactable = true;
            wordCloudSummaryCanvasGroup.blocksRaycasts = true;
        }

        if (WordCloudDisplay.Instance != null)
        {
            WordCloudDisplay.Instance.ForceUpdateDisplay();
        }

        if (confirmButton != null)
        {
            confirmButton.interactable = true;
            UpdateConfirmButtonLabel(false); // false = "Continuar"
        }
    }

    /// <summary>
    /// Trata o clique no botão enquanto o resumo está visível.
    /// </summary>
    private void HandleSummaryContinue()
    {
        isAwaitingSummaryContinue = false;

        if (wordCloudSummaryCanvasGroup != null)
        {
            wordCloudSummaryCanvasGroup.alpha = 0f;
            wordCloudSummaryCanvasGroup.interactable = false;
            wordCloudSummaryCanvasGroup.blocksRaycasts = false;
            wordCloudSummaryCanvasGroup.gameObject.SetActive(false);
        }

        if (confirmButton != null)
        {
            confirmButton.interactable = false;
            UpdateConfirmButtonLabel(true); // true = "Confirmar"
        }

        // Retorna ao fluxo normal
        StartCoroutine(AdvanceToNextRound());
    }

    /// <summary>
    /// Atualiza a descrição da situação usando LocalizedText para a rodada especificada.
    /// </summary>
    /// <param name="roundIndex">Índice da rodada (0-2)</param>
    private void UpdateSituationDescription(int roundIndex)
    {
        if (descriptionLocalizedText == null)
        {
            Debug.LogWarning("descriptionLocalizedText não está atribuído!");
            return;
        }

        switch (roundIndex)
        {
            case 0:
                descriptionLocalizedText.SetLocalizationKey("situation1", "descricao1");
                break;
            case 1:
                descriptionLocalizedText.SetLocalizationKey("situation2", "descricao1");
                break;
            case 2:
                descriptionLocalizedText.SetLocalizationKey("situation3", "descricao1");
                break;
            default:
                Debug.LogError($"Índice de rodada inválido: {roundIndex}");
                break;
        }
    }

    /// <summary>
    /// Retorna a primeira imagem da rodada especificada.
    /// </summary>
    /// <param name="roundIndex">Índice da rodada (0-2)</param>
    /// <returns>Sprite da primeira imagem da rodada</returns>
    private Sprite GetImage1ForRound(int roundIndex)
    {
        switch (roundIndex)
        {
            case 0: return round1Image1;
            case 1: return round2Image1;
            case 2: return round3Image1;
            default:
                Debug.LogError($"Índice de rodada inválido: {roundIndex}");
                return null;
        }
    }

    /// <summary>
    /// Retorna a segunda imagem da rodada especificada.
    /// </summary>
    /// <param name="roundIndex">Índice da rodada (0-2)</param>
    /// <returns>Sprite da segunda imagem da rodada</returns>
    private Sprite GetImage2ForRound(int roundIndex)
    {
        switch (roundIndex)
        {
            case 0: return round1Image2;
            case 1: return round2Image2;
            case 2: return round3Image2;
            default:
                Debug.LogError($"Índice de rodada inválido: {roundIndex}");
                return null;
        }
    }

    #endregion

    #region Debug Methods

    /// <summary>
    /// Método de debug para verificar o estado atual do jogo.
    /// Pode ser chamado no Inspector ou via código.
    /// </summary>
    [ContextMenu("Debug Game State")]
    public void DebugGameState()
    {
        Debug.Log($"=== ESTADO DO JOGO ===");
        Debug.Log($"Rodada atual: {currentRoundIndex}");
        Debug.Log($"Total de rodadas configuradas: {gameRounds.Count}");
        Debug.Log($"Score total: {totalEmpatheticScore}");
        Debug.Log($"Botões de palavras criados: {currentWordButtons.Count}");
        Debug.Log($"Palavras selecionadas: {selectedWords.Count}");

        for (int i = 0; i < selectedWords.Count; i++)
        {
            if (selectedWords[i])
            {
                Debug.Log($"Palavra {i} selecionada");
            }
        }

        if (gameRounds.Count > currentRoundIndex)
        {
            Debug.Log($"Palavras da rodada atual: {gameRounds[currentRoundIndex].words.Count}");
        }
    }

    /// <summary>
    /// Força a inicialização das rodadas para debug.
    /// </summary>
    [ContextMenu("Force Initialize Rounds")]
    public void ForceInitializeRounds()
    {
        InitializeDefaultRounds();
        Debug.Log($"Rodadas inicializadas! Total: {gameRounds.Count}");
    }

    #endregion

    #region Initialization

    /// <summary>
    /// Inicializa as rodadas com dados padrão conforme especificado.
    /// </summary>
    private void InitializeDefaultRounds()
    {
        gameRounds.Clear();

        // Rodada 1
        RoundData round1 = new RoundData();
        round1.words.AddRange(new WordChoice[]
        {
            new WordChoice { wordText = "Adaptação", isEmpathetic = true },
            new WordChoice { wordText = "Desengajado", isEmpathetic = false },
            new WordChoice { wordText = "Resolução de problema", isEmpathetic = true },
            new WordChoice { wordText = "Falta de profissionalismo", isEmpathetic = false },
            new WordChoice { wordText = "Compromisso", isEmpathetic = true },
            new WordChoice { wordText = "Falta de comunicação", isEmpathetic = false },
            new WordChoice { wordText = "Respeito ao cliente", isEmpathetic = true },
            new WordChoice { wordText = "Negligente", isEmpathetic = false }
        });
        gameRounds.Add(round1);

        // Rodada 2
        RoundData round2 = new RoundData();
        round2.words.AddRange(new WordChoice[]
        {
            new WordChoice { wordText = "Desleixo", isEmpathetic = false },
            new WordChoice { wordText = "Adaptação", isEmpathetic = true },
            new WordChoice { wordText = "Resiliência", isEmpathetic = true },
            new WordChoice { wordText = "Falta de respeito", isEmpathetic = false },
            new WordChoice { wordText = "Amadorismo", isEmpathetic = false },
            new WordChoice { wordText = "Compromisso", isEmpathetic = true },
            new WordChoice { wordText = "Prioridade", isEmpathetic = true },
            new WordChoice { wordText = "Falta de atenção", isEmpathetic = false }
        });
        gameRounds.Add(round2);

        // Rodada 3
        RoundData round3 = new RoundData();
        round3.words.AddRange(new WordChoice[]
        {
            new WordChoice { wordText = "Improdutividade", isEmpathetic = false },
            new WordChoice { wordText = "Distração", isEmpathetic = false },
            new WordChoice { wordText = "Resolução de problemas", isEmpathetic = true },
            new WordChoice { wordText = "Colaboração", isEmpathetic = true },
            new WordChoice { wordText = "Desorganização", isEmpathetic = false },
            new WordChoice { wordText = "Descomprometimento", isEmpathetic = false },
            new WordChoice { wordText = "Estratégia", isEmpathetic = true },
            new WordChoice { wordText = "Parceria", isEmpathetic = true }
        });
        gameRounds.Add(round3);
    }

    #endregion
}