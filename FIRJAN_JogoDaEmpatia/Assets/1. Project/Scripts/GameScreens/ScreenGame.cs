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
/// COMPORTAMENTO DAS RODADAS (4 FASES):
/// FASE 1 - Introdução:
///   - Mostra primeira imagem com descrição da situação
///   - Botão "Avançar" para prosseguir
///
/// FASE 2 - Primeira Escolha:
///   - Mantém primeira imagem e texto de descrição visíveis
///   - Mostra botões de palavras para seleção
///   - Botão "Confirmar" (habilitado apenas se pelo menos 1 palavra selecionada)
///
/// FASE 3 - Revisão com Contexto Completo:
///   - Mostra duas imagens lado a lado
///   - Mantém texto de descrição visível
///   - Permite re-seleção das palavras
///   - Botão "Continuar" (sempre habilitado)
///
/// FASE 4 - Resultados:
///   - Esconde imagens e texto de descrição
///   - Mostra botões selecionados + nuvem de palavras + texto explicativo
///   - Botão "Avançar" para próximo round ou tela final
///
/// SITUAÇÕES DAS RODADAS:
/// Situação 1: Em uma reunião online importante com um cliente, Marcos não liga a câmera.
/// Situação 2: Funcionário chega atrasado e de moletom numa reunião super importante com clientes.
/// Situação 3: Funcionária toma café com outro colaborador enquanto o resto da equipe trabalha sem parar.
///
/// CONFIGURAÇÃO DAS IMAGENS:
/// - Arraste as 6 sprites nos campos: round1Image1, round1Image2, round2Image1, round2Image2, round3Image1, round3Image2
/// - UI References:
///   * imagesHorizontalLayoutGroup: RectTransform do HorizontalLayoutGroup pai (para force rebuild)
///   * imageDisplay1GO/imageDisplay2GO: GameObjects dentro do HorizontalLayoutGroup
///   * imageDisplay1/imageDisplay2: Componentes Image dos GameObjects
///   * imageDisplay1CanvasGroup/imageDisplay2CanvasGroup: CanvasGroups para fade in/out
///   * descriptionText: TextMeshProUGUI com AutoSizeOptions (65 para 1 imagem, 35 para 2 imagens)
/// - Efeito de fade controlado por fadeDuration (padrão 1 segundo)
/// - Layout é automaticamente reconstruído quando imagens são ativadas/desativadas
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

    [Header("Round Images - PT (Portuguese)")]
    [Tooltip("Imagem 1 da Rodada 1 - PT")]
    public Sprite round1Image1PT;
    [Tooltip("Imagem 2 da Rodada 1 - PT")]
    public Sprite round1Image2PT;
    [Tooltip("Imagem 1 da Rodada 2 - PT")]
    public Sprite round2Image1PT;
    [Tooltip("Imagem 2 da Rodada 2 - PT")]
    public Sprite round2Image2PT;
    [Tooltip("Imagem 1 da Rodada 3 - PT")]
    public Sprite round3Image1PT;
    [Tooltip("Imagem 2 da Rodada 3 - PT")]
    public Sprite round3Image2PT;

    [Header("Round Images - EN (English)")]
    [Tooltip("Imagem 1 da Rodada 1 - EN")]
    public Sprite round1Image1EN;
    [Tooltip("Imagem 2 da Rodada 1 - EN")]
    public Sprite round1Image2EN;
    [Tooltip("Imagem 1 da Rodada 2 - EN")]
    public Sprite round2Image1EN;
    [Tooltip("Imagem 2 da Rodada 2 - EN")]
    public Sprite round2Image2EN;
    [Tooltip("Imagem 1 da Rodada 3 - EN")]
    public Sprite round3Image1EN;
    [Tooltip("Imagem 2 da Rodada 3 - EN")]
    public Sprite round3Image2EN;

    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI headerText;
    [SerializeField] private Image singleImageDisplay; // DEPRECATED: Não é mais usado no novo fluxo
    [SerializeField] private CanvasGroup singleImageCanvasGroup; // DEPRECATED: Não é mais usado no novo fluxo
    [SerializeField] private RectTransform imagesHorizontalLayoutGroup; // Transform do HorizontalLayoutGroup que contém as imagens
    [SerializeField] private GameObject imageDisplay1GO; // GameObject da primeira imagem (dentro do HorizontalLayoutGroup)
    [SerializeField] private Image imageDisplay1;
    [SerializeField] private CanvasGroup imageDisplay1CanvasGroup;
    [SerializeField] private GameObject imageDisplay2GO; // GameObject da segunda imagem (dentro do HorizontalLayoutGroup)
    [SerializeField] private Image imageDisplay2;
    [SerializeField] private CanvasGroup imageDisplay2CanvasGroup;
    [SerializeField] private TextMeshProUGUI descriptionText;
    [SerializeField] private CanvasGroup descriptionCanvasGroup; // CanvasGroup para fade in/out da descrição
    [SerializeField] private Transform wordsContainer;
    [SerializeField] private WordButtonAnimator wordButtonAnimator; // Componente para animar os botões
    [SerializeField] private CanvasFadeIn wordsContainerFadeIn; // Componente para fazer fade in do container de palavras
    [SerializeField] private Button wordButtonPrefab;
    [SerializeField] private Button confirmButton;
    [SerializeField] private Button skipWaitButton; // DEPRECATED: Não é mais usado no novo fluxo de 4 fases
    [SerializeField] private TextMeshProUGUI feedbackText;
    [Header("Word Cloud Summary References")]
    [SerializeField] private CanvasGroup inRoundCanvasGroup;
    [SerializeField] private CanvasGroup wordCloudSummaryCanvasGroup;
    [SerializeField] private Transform selectedWordsContainer; // Container para mostrar apenas palavras selecionadas
    [SerializeField] private TextMeshProUGUI confirmButtonText;
    [SerializeField] private string confirmButtonDefaultLabel = "Confirmar";
    [SerializeField] private string confirmButtonContinueLabel = "Continuar";

    [Header("Screen Settings")]
    [SerializeField] private string resultScreenName = "results";
    [SerializeField] private float imageTransitionDelay = 10f; // DEPRECATED: Não é mais usado no novo fluxo
    [SerializeField] private float fadeDuration = 1f;
    [SerializeField] private string questionDescription = "Qual sua opinião sobre isso?"; // DEPRECATED

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
    [SerializeField] private LocalizedText summaryLocalizedText; // Texto explicativo da nuvem de palavras
    [SerializeField] private LocalizedText confirmButtonLocalizedText;

    #endregion

    #region Private Fields

    /// <summary>
    /// Enum para controlar as fases do jogo.
    /// </summary>
    private enum GamePhase
    {
        Phase1_Introduction,    // Fase 1: Imagem + texto + botão "Avançar"
        Phase2_FirstChoice,     // Fase 2: Primeira imagem + botões de escolha + botão "Confirmar"
        Phase3_ReviewChoice,    // Fase 3: Duas imagens + botões (re-seleção) + botão "Continuar"
        Phase4_Results          // Fase 4: Resultados (nuvem de palavras + texto explicativo)
    }

    private int currentRoundIndex = 0;
    private int totalEmpatheticScore = 0;
    private List<Button> currentWordButtons = new List<Button>();
    private List<bool> selectedWords = new List<bool>();
    private List<string> currentRoundSelectedWords = new List<string>(); // Palavras selecionadas na rodada atual
    private GamePhase currentPhase = GamePhase.Phase1_Introduction;
    private List<WordData> currentRoundWordData = new List<WordData>(); // Dados da rodada atual

    #endregion

    #region Unity Callbacks

    private void Start()
    {
        // Garante inscrição no evento de idioma após LanguageManager estar pronto
        StartCoroutine(EnsureLanguageManagerSubscription());
    }

    public override void OnEnable()
    {
        base.OnEnable();

        // Subscreve ao evento de mudança de idioma
        if (LanguageManager.Instance != null)
        {
            Debug.Log("[Problema2][OnEnable] Subscribing to OnLanguageChanged event");
            LanguageManager.Instance.OnLanguageChanged += OnLanguageChanged;
            Debug.Log("[Problema2][OnEnable] Subscribed successfully to OnLanguageChanged");
        }
        else
        {
            Debug.LogWarning("[Problema2][OnEnable] LanguageManager.Instance is NULL! Cannot subscribe to OnLanguageChanged");
        }

        // Reinicia o jogo quando a tela é ativada
        // Usa corrotina para garantir que WordCloudDisplay.Instance já foi inicializado
        StartCoroutine(ResetGameDelayed());
    }

    public override void OnDisable()
    {
        base.OnDisable();

        // Desinscreve do evento de mudança de idioma
        if (LanguageManager.Instance != null)
        {
            Debug.Log("[Problema2][OnDisable] Unsubscribing from OnLanguageChanged event");
            LanguageManager.Instance.OnLanguageChanged -= OnLanguageChanged;
            Debug.Log("[Problema2][OnDisable] Unsubscribed from OnLanguageChanged");
        }
    }

    /// <summary>
    /// Corrotina que aguarda o LanguageManager estar pronto e se inscreve no evento.
    /// </summary>
    private IEnumerator EnsureLanguageManagerSubscription()
    {
        Debug.Log("[Problema2][EnsureLanguageManagerSubscription] Waiting for LanguageManager...");

        // Aguarda até que LanguageManager esteja disponível
        while (LanguageManager.Instance == null)
        {
            yield return null;
        }

        Debug.Log("[Problema2][EnsureLanguageManagerSubscription] LanguageManager found! Subscribing to OnLanguageChanged");

        // Remove primeiro para evitar duplicação
        LanguageManager.Instance.OnLanguageChanged -= OnLanguageChanged;
        // Inscreve no evento
        LanguageManager.Instance.OnLanguageChanged += OnLanguageChanged;

        Debug.Log("[Problema2][EnsureLanguageManagerSubscription] Successfully subscribed to OnLanguageChanged");
    }

    /// <summary>
    /// Chamado quando o idioma é alterado. Atualiza as imagens da rodada atual.
    /// </summary>
    private void OnLanguageChanged()
    {
        Debug.Log($"[Problema2][OnLanguageChanged] ===== EVENTO DISPARADO ===== Current phase: {currentPhase}, Current round: {currentRoundIndex}");
        Debug.Log($"[Problema2][OnLanguageChanged] New language: {(LanguageManager.Instance != null ? LanguageManager.Instance.CurrentLanguage.ToString() : "NULL")}");

        // Só atualiza se estiver em uma fase que mostra imagens
        if (currentPhase == GamePhase.Phase1_Introduction ||
            currentPhase == GamePhase.Phase2_FirstChoice ||
            currentPhase == GamePhase.Phase3_ReviewChoice)
        {
            Debug.Log("[Problema2][OnLanguageChanged] Phase matches, calling RefreshCurrentRoundImages");
            RefreshCurrentRoundImages();
        }
        else
        {
            Debug.Log($"[Problema2][OnLanguageChanged] Phase does NOT match (current: {currentPhase}), skipping image refresh");
        }
    }

    /// <summary>
    /// Atualiza as imagens da rodada atual baseado no idioma.
    /// </summary>
    private void RefreshCurrentRoundImages()
    {
        Debug.Log($"[Problema2][RefreshCurrentRoundImages] ===== STARTING REFRESH ===== Round: {currentRoundIndex}");
        Debug.Log($"[Problema2][RefreshCurrentRoundImages] Current language: {(LanguageManager.Instance != null ? LanguageManager.Instance.CurrentLanguage.ToString() : "NULL")}");

        Sprite image1 = GetImage1ForRound(currentRoundIndex);
        Sprite image2 = GetImage2ForRound(currentRoundIndex);

        Debug.Log($"[Problema2][RefreshCurrentRoundImages] Retrieved sprites - Image1: {(image1 != null ? image1.name : "NULL")}, Image2: {(image2 != null ? image2.name : "NULL")}");

        // Atualiza a primeira imagem se estiver ativa
        if (imageDisplay1 != null && imageDisplay1GO != null && imageDisplay1GO.activeSelf)
        {
            Debug.Log($"[Problema2][RefreshCurrentRoundImages] BEFORE update - Image1 current sprite: {(imageDisplay1.sprite != null ? imageDisplay1.sprite.name : "NULL")}");
            imageDisplay1.sprite = image1;
            Debug.Log($"[Problema2][RefreshCurrentRoundImages] AFTER update - Image1 new sprite: {(imageDisplay1.sprite != null ? imageDisplay1.sprite.name : "NULL")}");
        }
        else
        {
            Debug.Log($"[Problema2][RefreshCurrentRoundImages] Image1 NOT updated - Display1: {(imageDisplay1 != null)}, GO1: {(imageDisplay1GO != null)}, Active: {(imageDisplay1GO != null && imageDisplay1GO.activeSelf)}");
        }

        // Atualiza a segunda imagem se estiver ativa
        if (imageDisplay2 != null && imageDisplay2GO != null && imageDisplay2GO.activeSelf)
        {
            Debug.Log($"[Problema2][RefreshCurrentRoundImages] BEFORE update - Image2 current sprite: {(imageDisplay2.sprite != null ? imageDisplay2.sprite.name : "NULL")}");
            imageDisplay2.sprite = image2;
            Debug.Log($"[Problema2][RefreshCurrentRoundImages] AFTER update - Image2 new sprite: {(imageDisplay2.sprite != null ? imageDisplay2.sprite.name : "NULL")}");
        }
        else
        {
            Debug.Log($"[Problema2][RefreshCurrentRoundImages] Image2 NOT updated - Display2: {(imageDisplay2 != null)}, GO2: {(imageDisplay2GO != null)}, Active: {(imageDisplay2GO != null && imageDisplay2GO.activeSelf)}");
        }

        Debug.Log($"[Problema2][RefreshCurrentRoundImages] ===== REFRESH COMPLETED =====");
    }

    /// <summary>
    /// Versão com delay do ResetGame para garantir que WordCloudDisplay.Instance está pronto.
    /// </summary>
    private IEnumerator ResetGameDelayed()
    {
        // Aguarda até que WordCloudDisplay.Instance esteja disponível
        int maxAttempts = 10;
        int attempts = 0;
        while (WordCloudDisplay.Instance == null && attempts < maxAttempts)
        {
            yield return null; // Aguarda um frame
            attempts++;
        }

        if (WordCloudDisplay.Instance == null)
        {
            Debug.LogError("[ScreenGame] WordCloudDisplay.Instance ainda é nulo após aguardar! Verifique se o GameObject com WordCloudDisplay está na cena.");
        }

        // IMPORTANTE: Garante que estamos inscritos no evento de mudança de idioma
        // (pode ser que OnEnable tenha executado antes do LanguageManager estar pronto)
        if (LanguageManager.Instance != null)
        {
            Debug.Log("[Problema2][ResetGameDelayed] RE-Subscribing to OnLanguageChanged (safety check)");
            // Remove primeiro para evitar subscrição dupla
            LanguageManager.Instance.OnLanguageChanged -= OnLanguageChanged;
            LanguageManager.Instance.OnLanguageChanged += OnLanguageChanged;
            Debug.Log("[Problema2][ResetGameDelayed] Successfully RE-subscribed to OnLanguageChanged");
        }
        else
        {
            Debug.LogWarning("[Problema2][ResetGameDelayed] LanguageManager.Instance is STILL NULL!");
        }

        // Agora pode chamar ResetGame com segurança
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
        currentPhase = GamePhase.Phase1_Introduction;

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
        if (imageDisplay1GO != null)
        {
            imageDisplay1GO.SetActive(false);
        }
        if (imageDisplay1 != null)
        {
            imageDisplay1.sprite = null;
        }
        if (imageDisplay1CanvasGroup != null)
            imageDisplay1CanvasGroup.alpha = 0f;

        if (imageDisplay2GO != null)
        {
            imageDisplay2GO.SetActive(false);
        }
        if (imageDisplay2 != null)
        {
            imageDisplay2.sprite = null;
        }
        if (imageDisplay2CanvasGroup != null)
            imageDisplay2CanvasGroup.alpha = 0f;

        // Reseta o CanvasGroup da descrição
        if (descriptionCanvasGroup != null)
            descriptionCanvasGroup.alpha = 0f;

        // Esconde o botão de pular
        if (skipWaitButton != null)
            skipWaitButton.gameObject.SetActive(false);

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
        currentPhase = GamePhase.Phase1_Introduction;

        // Limpa botões selecionados do round anterior (se houver)
        ClearSelectedWordsButtons();

        // Carrega os dados salvos da rodada ou cria dados padrão
        LoadRoundWordData(roundIndex, currentRound);

        // Esconde as palavras e botão de confirmar inicialmente
        if (wordsContainer != null)
            wordsContainer.gameObject.SetActive(false);
        if (confirmButton != null)
        {
            confirmButton.gameObject.SetActive(false);
            confirmButton.interactable = false;
        }

        // Esconde o botão de pular (não é mais usado)
        if (skipWaitButton != null)
            skipWaitButton.gameObject.SetActive(false);

        // Limpa o texto de feedback
        if (feedbackText != null)
            feedbackText.text = "";

        // Reseta o header para "Situação X" ANTES de iniciar a sequência
        // (necessário porque o round anterior pode ter mudado para "Resultados")
        if (localizedTextHeader != null)
        {
            var roundNumber = roundIndex + 1;
            localizedTextHeader.SetLocalizationKey("common", "situationTitle");
            localizedTextHeader.SetFormatVariables(roundNumber.ToString());
            Debug.Log($"[ScreenGame] Header resetado para: Situação {roundNumber}");
        }

        // Inicia a Fase 1: Introdução
        StartCoroutine(Phase1_ShowIntroduction(roundIndex, currentRound));
    }

    /// <summary>
    /// FASE 1: Mostra primeira imagem + texto de introdução + botão "Avançar".
    /// O usuário deve clicar em "Avançar" para ir para a Fase 2.
    /// </summary>
    private IEnumerator Phase1_ShowIntroduction(int roundIndex, RoundData roundData)
    {
        // Aguarda até que a tela esteja ativa
        while (!IsOn())
        {
            yield return null;
        }

        Sprite image1 = GetImage1ForRound(roundIndex);

        // Desativa o texto explicativo da nuvem de palavras
        if (summaryLocalizedText != null)
        {
            summaryLocalizedText.gameObject.SetActive(false);
        }

        // Ativa apenas a primeira imagem (segunda imagem fica desativada)
        if (imageDisplay1GO != null)
        {
            imageDisplay1GO.SetActive(true);
        }
        if (imageDisplay2GO != null)
        {
            imageDisplay2GO.SetActive(false);
        }

        // Força o rebuild do layout após ativar/desativar GameObjects
        ForceRebuildLayout(imagesHorizontalLayoutGroup);

        // Define a sprite da primeira imagem
        if (imageDisplay1 != null && image1 != null)
        {
            imageDisplay1.sprite = image1;
        }

        // Font size é sempre 35 (removido ajuste dinâmico)
        if (descriptionText != null)
        {
            descriptionText.fontSizeMax = 35f;
        }

        // Atualiza o header com o número da situação
        if (headerText != null)
        {
            var roundNumber = roundIndex + 1;
            headerText.text = $"Situação {roundNumber}";
            localizedTextHeader.SetFormatVariables(roundNumber.ToString());
        }

        // Atualiza a descrição da situação
        UpdateSituationDescription(roundIndex);

        // Fade in da primeira imagem E do texto de descrição simultaneamente
        if (imageDisplay1CanvasGroup != null && descriptionCanvasGroup != null)
        {
            StartCoroutine(FadeCanvasGroup(descriptionCanvasGroup, 0f, 1f, fadeDuration));
            yield return StartCoroutine(FadeCanvasGroup(imageDisplay1CanvasGroup, 0f, 1f, fadeDuration));
        }
        else if (imageDisplay1CanvasGroup != null)
        {
            yield return StartCoroutine(FadeCanvasGroup(imageDisplay1CanvasGroup, 0f, 1f, fadeDuration));
        }

        // Mostra o botão "Avançar" (sempre habilitado na Fase 1)
        if (confirmButton != null)
        {
            confirmButton.gameObject.SetActive(true);
            confirmButton.interactable = true;
            // TODO: Criar key para "Avançar" - por enquanto usa "botao2" (Continuar)
            UpdateConfirmButtonLabel(false); // false = "Continuar" (temporário, deveria ser "Avançar")
            Debug.Log("[Phase1] Botão 'Avançar' ativado");
        }
    }

    /// <summary>
    /// FASE 2: Troca o texto de descrição para a pergunta da Fase 2, mantém primeira imagem, mostra botões de escolha.
    /// Botão "Confirmar" aparece mas só habilita quando pelo menos 1 palavra for selecionada.
    /// </summary>
    private IEnumerator Phase2_ShowFirstChoice(int roundIndex, RoundData roundData)
    {
        currentPhase = GamePhase.Phase2_FirstChoice;
        Debug.Log("[Phase2] Iniciando fase de primeira escolha");

        // Atualiza a descrição para a pergunta da Fase 2
        if (descriptionLocalizedText != null)
        {
            Debug.Log("[Phase2] BEFORE SetLocalizationKey - current text: " + (descriptionText != null ? descriptionText.text : "NULL"));
            descriptionLocalizedText.SetLocalizationKey("common", "phase2Description");
            Debug.Log("[Phase2] AFTER SetLocalizationKey - new text should be: " + (descriptionText != null ? descriptionText.text : "NULL"));
        }
        else
        {
            Debug.LogError("[Phase2] descriptionLocalizedText is NULL!");
        }

        // Mantém a primeira imagem visível

        // Cria os botões de palavras
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

        // Mostra o botão "Confirmar" (desabilitado até que uma palavra seja selecionada)
        if (confirmButton != null)
        {
            confirmButton.gameObject.SetActive(true);
            confirmButton.interactable = false;
            UpdateConfirmButtonLabel(true); // true = "Confirmar"
            Debug.Log("[Phase2] Botão 'Confirmar' ativado (mas desabilitado até seleção)");
        }
    }

    /// <summary>
    /// FASE 3: Ativa segunda imagem, mostra duas imagens lado a lado, troca texto para pergunta da Fase 3 e permite re-seleção.
    /// Botão "Continuar" sempre habilitado (pois já houve confirmação anterior).
    /// </summary>
    private IEnumerator Phase3_ShowReviewChoice(int roundIndex, RoundData roundData)
    {
        currentPhase = GamePhase.Phase3_ReviewChoice;
        Debug.Log("[Phase3] Iniciando fase de revisão de escolha");

        Sprite image2 = GetImage2ForRound(roundIndex);

        // Atualiza a descrição para a pergunta da Fase 3
        if (descriptionLocalizedText != null)
        {
            Debug.Log("[Phase3] BEFORE SetLocalizationKey - current text: " + (descriptionText != null ? descriptionText.text : "NULL"));
            descriptionLocalizedText.SetLocalizationKey("common", "phase3Description");
            Debug.Log("[Phase3] AFTER SetLocalizationKey - new text should be: " + (descriptionText != null ? descriptionText.text : "NULL"));
        }
        else
        {
            Debug.LogError("[Phase3] descriptionLocalizedText is NULL!");
        }

        // Ativa a segunda imagem (primeira já está ativa desde a Fase 1)
        if (imageDisplay2GO != null)
        {
            imageDisplay2GO.SetActive(true);
        }

        // Força o rebuild do layout após ativar a segunda imagem
        ForceRebuildLayout(imagesHorizontalLayoutGroup);

        // Define a sprite da segunda imagem
        if (imageDisplay2 != null && image2 != null)
        {
            imageDisplay2.sprite = image2;
        }

        // Fade in da segunda imagem (primeira já está visível)
        if (imageDisplay2CanvasGroup != null)
        {
            yield return StartCoroutine(FadeCanvasGroup(imageDisplay2CanvasGroup, 0f, 1f, fadeDuration));
        }

        // Os botões de palavras já estão criados, apenas garantimos que estão visíveis e interativos
        if (wordsContainer != null)
        {
            wordsContainer.gameObject.SetActive(true);
        }

        // Atualiza o botão para "Continuar" (sempre habilitado pois já teve confirmação)
        if (confirmButton != null)
        {
            confirmButton.interactable = true;
            UpdateConfirmButtonLabel(false); // false = "Continuar"
            Debug.Log("[Phase3] Botão 'Continuar' ativado");
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
    /// Força a atualização do layout de um Transform e seus filhos.
    /// Útil quando GameObjects são ativados/desativados em um LayoutGroup.
    /// </summary>
    /// <param name="transform">Transform pai que contém o LayoutGroup</param>
    private void ForceRebuildLayout(Transform transform)
    {
        if (transform == null) return;

        // Força o rebuild imediato do layout
        UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(transform as RectTransform);

        Debug.Log($"[ForceRebuildLayout] Layout reconstruído para: {transform.name}");
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

        // Aguarda o fade-in da tela e o Grid Layout terminar de calcular
        yield return new WaitForSeconds(0.3f);

        // Aguarda mais alguns frames para o Grid Layout estabilizar
        yield return null;
        yield return null;

        if (wordButtonAnimator != null)
        {
            // Reseta qualquer animação anterior antes de iniciar nova
            wordButtonAnimator.ResetButtons();
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
                colors.highlightedColor = Color.green;
                colors.pressedColor = Color.green;
                colors.selectedColor = Color.green;
            }
            else
            {
                colors.normalColor = Color.white;
                colors.highlightedColor = Color.white;
                colors.pressedColor = Color.white;
                colors.selectedColor = Color.white;
            }

            button.colors = colors;

            // Atualiza o estado do botão de confirmar
            UpdateConfirmButtonState();
        }
    }

    /// <summary>
    /// Atualiza o estado do botão de confirmar baseado nas seleções e na fase atual.
    /// Fase 1: Sempre habilitado (botão "Avançar")
    /// Fase 2: Só habilita se pelo menos 1 palavra foi selecionada (botão "Confirmar")
    /// Fase 3: Sempre habilitado (botão "Continuar", permite re-seleção)
    /// Fase 4: Sempre habilitado (botão "Avançar" para próximo round)
    /// </summary>
    private void UpdateConfirmButtonState()
    {
        if (confirmButton == null)
            return;

        switch (currentPhase)
        {
            case GamePhase.Phase1_Introduction:
                confirmButton.interactable = true; // Sempre habilitado
                break;

            case GamePhase.Phase2_FirstChoice:
                bool hasSelection = HasAnyWordSelected();
                confirmButton.interactable = hasSelection;
                Debug.Log($"[UpdateConfirmButtonState Phase2] Botão {(hasSelection ? "habilitado" : "desabilitado")}");
                break;

            case GamePhase.Phase3_ReviewChoice:
                confirmButton.interactable = true; // Sempre habilitado (já teve confirmação)
                break;

            case GamePhase.Phase4_Results:
                confirmButton.interactable = true; // Sempre habilitado
                break;
        }
    }

    /// <summary>
    /// Verifica se pelo menos uma palavra foi selecionada.
    /// </summary>
    /// <returns>True se houver pelo menos 1 palavra selecionada</returns>
    private bool HasAnyWordSelected()
    {
        foreach (bool selected in selectedWords)
        {
            if (selected)
                return true;
        }
        return false;
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
    /// Método público chamado pelo botão principal. Controla as 4 fases do jogo.
    /// Fase 1: Botão "Avançar" -> vai para Fase 2
    /// Fase 2: Botão "Confirmar" -> vai para Fase 3 (requer pelo menos 1 palavra selecionada)
    /// Fase 3: Botão "Continuar" -> processa escolhas e vai para Fase 4
    /// Fase 4: Botão "Avançar" -> vai para próximo round ou tela de resultados
    /// </summary>
    public void OnConfirmButtonPressed()
    {
        Debug.Log($"[OnConfirmButtonPressed] Fase atual: {currentPhase}");

        switch (currentPhase)
        {
            case GamePhase.Phase1_Introduction:
                // Fase 1: Avançar para mostrar os botões de escolha
                StartCoroutine(Phase2_ShowFirstChoice(currentRoundIndex, gameRounds[currentRoundIndex]));
                break;

            case GamePhase.Phase2_FirstChoice:
                // Fase 2: Confirmar escolha e mostrar segunda imagem
                if (HasAnyWordSelected())
                {
                    StartCoroutine(Phase3_ShowReviewChoice(currentRoundIndex, gameRounds[currentRoundIndex]));
                }
                else
                {
                    Debug.LogWarning("[Phase2] Nenhuma palavra selecionada!");
                }
                break;

            case GamePhase.Phase3_ReviewChoice:
                // Fase 3: Continuar para processar escolhas e mostrar resultados
                ProcessChoicesAndShowResults();
                break;

            case GamePhase.Phase4_Results:
                // Fase 4: Avançar para próximo round ou tela final
                HandleResultsContinue();
                break;
        }
    }

    /// <summary>
    /// Processa as escolhas da rodada atual e mostra os resultados.
    /// </summary>
    private void ProcessChoicesAndShowResults()
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

        // Limpa a lista de palavras selecionadas da rodada anterior
        currentRoundSelectedWords.Clear();

        // Atualiza os dados das palavras selecionadas
        for (int i = 0; i < selectedWords.Count && i < currentRound.words.Count; i++)
        {
            if (selectedWords[i])
            {
                string selectedWordText = currentRound.words[i].wordText;

                // Adiciona à lista de palavras selecionadas da rodada
                currentRoundSelectedWords.Add(selectedWordText);

                // IMPORTANTE: Procura a palavra pelo TEXTO, não pelo índice!
                // Porque currentRoundWordData pode estar em ordem diferente dos botões
                WordData wordData = currentRoundWordData.Find(w => w.text == selectedWordText);

                if (wordData != null)
                {
                    // Incrementa a pontuação cumulativa da palavra
                    wordData.cumulativePoints += 1;
                    Debug.Log($"Palavra '{selectedWordText}' incrementada para {wordData.cumulativePoints} pontos cumulativos");
                }
                else
                {
                    Debug.LogError($"[ScreenGame] Palavra '{selectedWordText}' não encontrada em currentRoundWordData!");
                }

                // Verifica se é empática para pontuação
                if (currentRound.words[i].isEmpathetic)
                {
                    roundEmpatheticScore++;
                    Debug.Log($"Palavra empática selecionada: {selectedWordText}");
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

        // Mostra a nuvem de palavras (esconde imagens)
        StartCoroutine(ShowWordCloudSummary());
    }

    /// <summary>
    /// Corrotina para esconder as imagens e o texto de descrição, e mostrar a nuvem de palavras.
    /// FASE 4: Esconde imagens + texto de descrição, mostra nuvem de palavras + texto explicativo
    /// </summary>
    private IEnumerator ShowWordCloudSummary()
    {
        // Esconde o texto de descrição com fade out
        if (descriptionCanvasGroup != null)
        {
            StartCoroutine(FadeCanvasGroup(descriptionCanvasGroup, 1f, 0f, fadeDuration * 0.5f));
        }

        // Esconde as duas imagens com fade out
        if (imageDisplay1CanvasGroup != null && imageDisplay2CanvasGroup != null)
        {
            StartCoroutine(FadeCanvasGroup(imageDisplay1CanvasGroup, 1f, 0f, fadeDuration * 0.5f));
            yield return StartCoroutine(FadeCanvasGroup(imageDisplay2CanvasGroup, 1f, 0f, fadeDuration * 0.5f));

            // Desativa os GameObjects após o fade out
            if (imageDisplay1GO != null)
                imageDisplay1GO.SetActive(false);
            if (imageDisplay2GO != null)
                imageDisplay2GO.SetActive(false);
        }

        // Muda o header para "Resultados"
        if (localizedTextHeader != null)
        {
            localizedTextHeader.SetLocalizationKey("situation_results", "titulo1");
        }

        // Ativa e configura o texto explicativo da nuvem de palavras
        if (summaryLocalizedText != null)
        {
            summaryLocalizedText.gameObject.SetActive(true);
            summaryLocalizedText.SetLocalizationKey("situation_results", "texto1");
        }

        // Instancia os botões das palavras selecionadas no container de resultados
        InstantiateSelectedWordsButtons();

        // Mostra o resumo da rodada com nuvem de palavras
        ShowRoundSummary();
    }

    /// <summary>
    /// Instancia botões apenas das palavras selecionadas no container de resultados.
    /// </summary>
    private void InstantiateSelectedWordsButtons()
    {
        if (selectedWordsContainer == null)
        {
            Debug.LogWarning("[ScreenGame] selectedWordsContainer não está configurado!");
            return;
        }

        if (wordButtonPrefab == null)
        {
            Debug.LogWarning("[ScreenGame] wordButtonPrefab não está configurado!");
            return;
        }

        // Limpa botões anteriores do container de resultados
        foreach (Transform child in selectedWordsContainer)
        {
            Destroy(child.gameObject);
        }

        // Determina qual section usar baseado na rodada atual
        string section = $"situation{currentRoundIndex + 1}";

        Debug.Log($"[ScreenGame] Instanciando {currentRoundSelectedWords.Count} palavras selecionadas no container de resultados");

        // Instancia apenas as palavras selecionadas
        for (int i = 0; i < currentRoundSelectedWords.Count; i++)
        {
            string selectedWord = currentRoundSelectedWords[i];

            Button wordButton = Instantiate(wordButtonPrefab, selectedWordsContainer);

            // Remove o comportamento de clique (é apenas visual)
            wordButton.interactable = false;

            // Configura o texto do botão
            LocalizedText localizedText = wordButton.GetComponentInChildren<LocalizedText>();
            if (localizedText != null)
            {
                // Encontra o índice da palavra na lista original para pegar a key correta
                int originalIndex = gameRounds[currentRoundIndex].words.FindIndex(w => w.wordText == selectedWord);
                if (originalIndex >= 0)
                {
                    string key = $"opcao{originalIndex + 1}";
                    localizedText.SetLocalizationKey(section, key);
                    Debug.Log($"[ScreenGame] Botão criado: {selectedWord} (key: {section}.{key})");
                }
            }
            else
            {
                // Fallback: usa o texto diretamente
                TextMeshProUGUI buttonText = wordButton.GetComponentInChildren<TextMeshProUGUI>();
                if (buttonText != null)
                {
                    buttonText.text = selectedWord;
                }
            }
        }

        Debug.Log($"[ScreenGame] {currentRoundSelectedWords.Count} botões de palavras selecionadas instanciados!");

        // Anima o container e os botões
        StartCoroutine(AnimateSelectedWordsContainer());
    }

    /// <summary>
    /// Anima o container de palavras selecionadas e seus botões.
    /// Primeiro faz o fade in do container (CanvasFadeIn), depois anima os botões (WordButtonAnimator).
    /// </summary>
    private IEnumerator AnimateSelectedWordsContainer()
    {
        // Primeiro faz o fade in do container se houver CanvasFadeIn
        CanvasFadeIn containerFadeIn = selectedWordsContainer.GetComponent<CanvasFadeIn>();
        if (containerFadeIn != null)
        {
            Debug.Log($"[ScreenGame] Fazendo fade in do selectedWordsContainer");
            containerFadeIn.DoFadeIn();
        }

        // Aguarda alguns frames para o fade in e o Grid Layout calcular as posições
        yield return null;
        yield return null;
        yield return new WaitForSeconds(0.2f);

        // Depois anima os botões se houver WordButtonAnimator
        WordButtonAnimator selectedWordsAnimator = selectedWordsContainer.GetComponent<WordButtonAnimator>();
        if (selectedWordsAnimator != null)
        {
            Debug.Log($"[ScreenGame] Animando botões selecionados com WordButtonAnimator");
            selectedWordsAnimator.ResetButtons();
            selectedWordsAnimator.AnimateButtons();
        }
    }

    /// <summary>
    /// Limpa os botões de palavras selecionadas do container de resultados.
    /// </summary>
    private void ClearSelectedWordsButtons()
    {
        if (selectedWordsContainer == null) return;

        Debug.Log("[ScreenGame] Limpando botões de palavras selecionadas do round anterior");

        // Destrói todos os filhos do container
        foreach (Transform child in selectedWordsContainer)
        {
            Destroy(child.gameObject);
        }

        // Reseta o alpha do CanvasGroup se houver
        CanvasGroup containerCanvasGroup = selectedWordsContainer.GetComponent<CanvasGroup>();
        if (containerCanvasGroup != null)
        {
            containerCanvasGroup.alpha = 0f;
        }
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

        // IMPORTANTE: NÃO modificar a ordem/textos dos dados carregados!
        // A ordem será gerenciada automaticamente pela ordenação por cumulativePoints

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
        // Reseta a primeira imagem
        if (imageDisplay1GO != null)
        {
            imageDisplay1GO.SetActive(false);
        }
        if (imageDisplay1 != null)
        {
            imageDisplay1.sprite = null;
        }
        if (imageDisplay1CanvasGroup != null)
            imageDisplay1CanvasGroup.alpha = 0f;

        // Reseta a segunda imagem
        if (imageDisplay2GO != null)
        {
            imageDisplay2GO.SetActive(false);
        }
        if (imageDisplay2 != null)
        {
            imageDisplay2.sprite = null;
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
        currentPhase = GamePhase.Phase4_Results;
        Debug.Log("[Phase4] Mostrando resultados");

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
    /// Trata o clique no botão enquanto os resultados estão visíveis (Fase 4).
    /// Avança para o próximo round ou vai para a tela final.
    /// </summary>
    private void HandleResultsContinue()
    {
        Debug.Log("[Phase4] Avançando para próximo round");

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
        }

        // Avança para o próximo round
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

        Debug.Log($"[UpdateSituationDescription] BEFORE - Round {roundIndex}, current text: " + (descriptionText != null ? descriptionText.text : "NULL"));

        switch (roundIndex)
        {
            case 0:
                descriptionLocalizedText.SetLocalizationKey("situation1", "descricao1");
                Debug.Log("[UpdateSituationDescription] Set to situation1.descricao1");
                break;
            case 1:
                descriptionLocalizedText.SetLocalizationKey("situation2", "descricao1");
                Debug.Log("[UpdateSituationDescription] Set to situation2.descricao1");
                break;
            case 2:
                descriptionLocalizedText.SetLocalizationKey("situation3", "descricao1");
                Debug.Log("[UpdateSituationDescription] Set to situation3.descricao1");
                break;
            default:
                Debug.LogError($"Índice de rodada inválido: {roundIndex}");
                break;
        }

        Debug.Log($"[UpdateSituationDescription] AFTER - new text: " + (descriptionText != null ? descriptionText.text : "NULL"));
    }

    /// <summary>
    /// Retorna a primeira imagem da rodada especificada baseada no idioma atual.
    /// </summary>
    /// <param name="roundIndex">Índice da rodada (0-2)</param>
    /// <returns>Sprite da primeira imagem da rodada</returns>
    private Sprite GetImage1ForRound(int roundIndex)
    {
        // Se LanguageManager for null, assume Português como padrão
        bool isPortuguese = LanguageManager.Instance == null ||
                           LanguageManager.Instance.CurrentLanguage == Language.Portuguese;

        Debug.Log($"[Problema2][GetImage1ForRound] Round {roundIndex}, Language: {(LanguageManager.Instance != null ? LanguageManager.Instance.CurrentLanguage.ToString() : "NULL (default PT)")}, isPortuguese: {isPortuguese}");

        Sprite selectedSprite = null;
        switch (roundIndex)
        {
            case 0:
                selectedSprite = isPortuguese ? round1Image1PT : round1Image1EN;
                Debug.Log($"[Problema2][GetImage1ForRound] Round 0 - PT sprite: {(round1Image1PT != null ? round1Image1PT.name : "NULL")}, EN sprite: {(round1Image1EN != null ? round1Image1EN.name : "NULL")}");
                Debug.Log($"[Problema2][GetImage1ForRound] Round 0 - Selected: {(isPortuguese ? "PT" : "EN")}, Returning sprite: {(selectedSprite != null ? selectedSprite.name : "NULL")}");
                return selectedSprite;
            case 1:
                selectedSprite = isPortuguese ? round2Image1PT : round2Image1EN;
                Debug.Log($"[Problema2][GetImage1ForRound] Round 1 - PT sprite: {(round2Image1PT != null ? round2Image1PT.name : "NULL")}, EN sprite: {(round2Image1EN != null ? round2Image1EN.name : "NULL")}");
                Debug.Log($"[Problema2][GetImage1ForRound] Round 1 - Selected: {(isPortuguese ? "PT" : "EN")}, Returning sprite: {(selectedSprite != null ? selectedSprite.name : "NULL")}");
                return selectedSprite;
            case 2:
                selectedSprite = isPortuguese ? round3Image1PT : round3Image1EN;
                Debug.Log($"[Problema2][GetImage1ForRound] Round 2 - PT sprite: {(round3Image1PT != null ? round3Image1PT.name : "NULL")}, EN sprite: {(round3Image1EN != null ? round3Image1EN.name : "NULL")}");
                Debug.Log($"[Problema2][GetImage1ForRound] Round 2 - Selected: {(isPortuguese ? "PT" : "EN")}, Returning sprite: {(selectedSprite != null ? selectedSprite.name : "NULL")}");
                return selectedSprite;
            default:
                Debug.LogError($"[Problema2][GetImage1ForRound] Índice de rodada inválido: {roundIndex}");
                return null;
        }
    }

    /// <summary>
    /// Retorna a segunda imagem da rodada especificada baseada no idioma atual.
    /// </summary>
    /// <param name="roundIndex">Índice da rodada (0-2)</param>
    /// <returns>Sprite da segunda imagem da rodada</returns>
    private Sprite GetImage2ForRound(int roundIndex)
    {
        // Se LanguageManager for null, assume Português como padrão
        bool isPortuguese = LanguageManager.Instance == null ||
                           LanguageManager.Instance.CurrentLanguage == Language.Portuguese;

        Debug.Log($"[Problema2][GetImage2ForRound] Round {roundIndex}, Language: {(LanguageManager.Instance != null ? LanguageManager.Instance.CurrentLanguage.ToString() : "NULL (default PT)")}, isPortuguese: {isPortuguese}");

        Sprite selectedSprite = null;
        switch (roundIndex)
        {
            case 0:
                selectedSprite = isPortuguese ? round1Image2PT : round1Image2EN;
                Debug.Log($"[Problema2][GetImage2ForRound] Round 0 - PT sprite: {(round1Image2PT != null ? round1Image2PT.name : "NULL")}, EN sprite: {(round1Image2EN != null ? round1Image2EN.name : "NULL")}");
                Debug.Log($"[Problema2][GetImage2ForRound] Round 0 - Selected: {(isPortuguese ? "PT" : "EN")}, Returning sprite: {(selectedSprite != null ? selectedSprite.name : "NULL")}");
                return selectedSprite;
            case 1:
                selectedSprite = isPortuguese ? round2Image2PT : round2Image2EN;
                Debug.Log($"[Problema2][GetImage2ForRound] Round 1 - PT sprite: {(round2Image2PT != null ? round2Image2PT.name : "NULL")}, EN sprite: {(round2Image2EN != null ? round2Image2EN.name : "NULL")}");
                Debug.Log($"[Problema2][GetImage2ForRound] Round 1 - Selected: {(isPortuguese ? "PT" : "EN")}, Returning sprite: {(selectedSprite != null ? selectedSprite.name : "NULL")}");
                return selectedSprite;
            case 2:
                selectedSprite = isPortuguese ? round3Image2PT : round3Image2EN;
                Debug.Log($"[Problema2][GetImage2ForRound] Round 2 - PT sprite: {(round3Image2PT != null ? round3Image2PT.name : "NULL")}, EN sprite: {(round3Image2EN != null ? round3Image2EN.name : "NULL")}");
                Debug.Log($"[Problema2][GetImage2ForRound] Round 2 - Selected: {(isPortuguese ? "PT" : "EN")}, Returning sprite: {(selectedSprite != null ? selectedSprite.name : "NULL")}");
                return selectedSprite;
            default:
                Debug.LogError($"[Problema2][GetImage2ForRound] Índice de rodada inválido: {roundIndex}");
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

        // Rodada 1 - ORDEM CORRESPONDE AO JSON (opcao1PT até opcao8PT)
        RoundData round1 = new RoundData();
        round1.words.AddRange(new WordChoice[]
        {
            new WordChoice { wordText = "Adaptação", isEmpathetic = true },                   // opcao1PT
            new WordChoice { wordText = "Resolução de problema", isEmpathetic = true },       // opcao2PT
            new WordChoice { wordText = "Compromisso", isEmpathetic = true },                 // opcao3PT
            new WordChoice { wordText = "Respeito ao cliente", isEmpathetic = true },         // opcao4PT
            new WordChoice { wordText = "Desengajado", isEmpathetic = false },                // opcao5PT
            new WordChoice { wordText = "Falta de profissionalismo", isEmpathetic = false },  // opcao6PT
            new WordChoice { wordText = "Falta de comunicação", isEmpathetic = false },       // opcao7PT
            new WordChoice { wordText = "Negligente", isEmpathetic = false }                  // opcao8PT
        });
        gameRounds.Add(round1);

        // Rodada 2 - ORDEM CORRESPONDE AO JSON (opcao1PT até opcao8PT)
        RoundData round2 = new RoundData();
        round2.words.AddRange(new WordChoice[]
        {
            new WordChoice { wordText = "Desleixo", isEmpathetic = false },          // opcao1PT
            new WordChoice { wordText = "Resiliência", isEmpathetic = true },        // opcao2PT
            new WordChoice { wordText = "Amadorismo", isEmpathetic = false },        // opcao3PT
            new WordChoice { wordText = "Prioridade", isEmpathetic = true },         // opcao4PT
            new WordChoice { wordText = "Adaptação", isEmpathetic = true },          // opcao5PT
            new WordChoice { wordText = "Falta de respeito", isEmpathetic = false }, // opcao6PT
            new WordChoice { wordText = "Compromisso", isEmpathetic = true },        // opcao7PT
            new WordChoice { wordText = "Falta de atenção", isEmpathetic = false }   // opcao8PT
        });
        gameRounds.Add(round2);

        // Rodada 3 - ORDEM CORRESPONDE AO JSON (opcao1PT até opcao8PT)
        RoundData round3 = new RoundData();
        round3.words.AddRange(new WordChoice[]
        {
            new WordChoice { wordText = "Improdutividade", isEmpathetic = false },      // opcao1PT
            new WordChoice { wordText = "Resolução de problemas", isEmpathetic = true }, // opcao2PT
            new WordChoice { wordText = "Desorganização", isEmpathetic = false },        // opcao3PT
            new WordChoice { wordText = "Estratégia", isEmpathetic = true },             // opcao4PT
            new WordChoice { wordText = "Distração", isEmpathetic = false },             // opcao5PT
            new WordChoice { wordText = "Colaboração", isEmpathetic = true },            // opcao6PT
            new WordChoice { wordText = "Descomprometimento", isEmpathetic = false },    // opcao7PT
            new WordChoice { wordText = "Parceria", isEmpathetic = true }                // opcao8PT
        });
        gameRounds.Add(round3);
    }

    #endregion
}