using UnityEngine;
using TMPro;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using FIRJAN.Utilities;

[System.Serializable]
public class WordDisplaySlot
{
    [Header("TextMeshPro Reference")]
    public TextMeshProUGUI textComponent;

    [Header("Position Info")]
    public string positionName = "";

    [Header("Runtime Data")]
    public string currentWord = "";
    public int currentPoints = 0;
}

public class WordCloudDisplay : MonoBehaviour
{
    [Header("TextMeshPro Slots")]
    public List<WordDisplaySlot> wordSlots = new List<WordDisplaySlot>();

    [Header("Size Settings")]
    public float minFontSize = 20f;
    public float maxFontSize = 80f;

    [Header("Animation Settings")]
    public float animationDuration = 0.5f;

    // [Header("Color Settings")]
    // public Gradient colorGradient;

    [Header("Auto-Population Settings")]
    [Tooltip("Auto-preencher slots no Start()?")]
    public bool autoPopulateOnStart = true;

    [Tooltip("Mostrar logs detalhados de debug?")]
    public bool showDebugLogs = true;

    private Dictionary<string, int> wordScores = new Dictionary<string, int>();
    private List<WordData> currentRoundWords = new List<WordData>(); // Dados da rodada atual
    public static WordCloudDisplay Instance { get; private set; }

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    void Start()
    {
        // Auto-preencher slots com palavras disponíveis se habilitado
        if (autoPopulateOnStart)
        {
            AutoPopulateWordSlots();
        }

        // NÃO limpa os slots aqui - os dados serão carregados pelo ScreenGame
        // ClearAllSlots(); // REMOVIDO - causava slots vazios

        if (showDebugLogs)
        {
            Debug.Log("[WordCloudDisplay] Inicializado. Aguardando dados da rodada.");
        }
    }

    void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    public void AddWordPoints(string wordText, int points = 1)
    {
        if (string.IsNullOrEmpty(wordText)) return;

        if (wordScores.ContainsKey(wordText))
        {
            wordScores[wordText] += points;
            // Garantir que não fique negativo
            wordScores[wordText] = Mathf.Max(0, wordScores[wordText]);
        }
        else
        {
            // Garantir que valores iniciais negativos virem 0
            wordScores[wordText] = Mathf.Max(0, points);
        }

        if (showDebugLogs)
        {
            Debug.Log($"[WordCloudDisplay] '{wordText}' now has {wordScores[wordText]} points");
        }
        UpdateWordCloudDisplay();
    }

    private void UpdateWordCloudDisplay()
    {
        if (currentRoundWords.Count == 0)
        {
            Debug.LogWarning("[WordCloudDisplay] Nenhuma palavra carregada na rodada atual");
            return;
        }

        Debug.Log($"[WordCloudDisplay] >> Atualizando display com {currentRoundWords.Count} palavras");

        // Atualiza cada slot com a palavra correspondente (sem ordenar!)
        for (int i = 0; i < currentRoundWords.Count && i < wordSlots.Count; i++)
        {
            var wordData = currentRoundWords[i];
            var slot = wordSlots[i];

            Debug.Log($"[WordCloudDisplay] >> Atualizando slot {i}: '{wordData.text}' com {wordData.cumulativePoints} pontos");

            if (slot.textComponent == null)
            {
                Debug.LogError($"[WordCloudDisplay] >> ERRO: Slot {i} tem textComponent NULL!");
            }

            UpdateSlot(slot, wordData.text, wordData.cumulativePoints, currentRoundWords);
        }

        // Limpa slots extras (não deve acontecer se configurado corretamente)
        for (int i = currentRoundWords.Count; i < wordSlots.Count; i++)
        {
            Debug.Log($"[WordCloudDisplay] >> Limpando slot extra {i}");
            ClearSlot(wordSlots[i]);
        }

        Debug.Log($"[WordCloudDisplay] >> Display atualizado!");
    }

    private void UpdateSlot(WordDisplaySlot slot, string wordText, int points, List<WordData> allWords)
    {
        if (slot.textComponent == null) return;

        bool textChanged = slot.currentWord != wordText;
        bool scoreChanged = slot.currentPoints != points;

        if (textChanged || scoreChanged)
        {
            if (textChanged && !string.IsNullOrEmpty(slot.currentWord))
            {
                AnimateSlotChange(slot, wordText, points, allWords);
            }
            else
            {
                SetSlotImmediate(slot, wordText, points, allWords);
            }
        }
    }

    private void AnimateSlotChange(WordDisplaySlot slot, string newWord, int newPoints, List<WordData> allWords)
    {
        if (slot.textComponent == null) return;

        if (showDebugLogs)
        {
            Debug.Log($"[WordCloudDisplay] Animando mudança em '{slot.positionName}': '{slot.currentWord}' -> '{newWord}'");
        }

        slot.textComponent.DOFade(0f, animationDuration * 0.3f)
            .OnComplete(() =>
            {
                SetSlotImmediate(slot, newWord, newPoints, allWords);
                slot.textComponent.DOFade(1f, animationDuration * 0.7f);
            });
    }

    private void SetSlotImmediate(WordDisplaySlot slot, string wordText, int points, List<WordData> allWords)
    {
        if (slot.textComponent == null) return;

        slot.currentWord = wordText;
        slot.currentPoints = points;

        // Tenta usar LocalizedText se disponível
        LocalizedText localizedText = slot.textComponent.GetComponent<LocalizedText>();
        if (localizedText != null)
        {
            // Determina qual palavra é baseado no índice do slot
            int wordIndex = wordSlots.IndexOf(slot);
            if (wordIndex >= 0 && wordIndex < 8)
            {
                string key = $"palavra{wordIndex + 1}";
                localizedText.SetLocalizationKey("situation_results", key);
            }
            else
            {
                // Fallback se não encontrar o índice
                slot.textComponent.text = wordText;
            }
        }
        else
        {
            // Fallback se não tiver LocalizedText
            slot.textComponent.text = wordText;
        }

        ApplyWordStyle(slot, points, allWords);
    }

    private void ApplyWordStyle(WordDisplaySlot slot, int points, List<WordData> allWords)
    {
        if (slot.textComponent == null || allWords.Count == 0) return;

        // Usa cumulativePoints para calcular o tamanho
        int minPoints = allWords.Min(w => w.cumulativePoints);
        int maxPoints = allWords.Max(w => w.cumulativePoints);

        float normalizedScore;
        if (maxPoints > minPoints)
        {
            normalizedScore = (float)(points - minPoints) / (maxPoints - minPoints);
        }
        else
        {
            // Se todas as palavras têm a mesma pontuação, usa tamanho médio
            normalizedScore = 0.5f;
        }

        // Remove o clamp mínimo para permitir palavras muito pequenas
        normalizedScore = Mathf.Clamp01(normalizedScore);

        float fontSize = Mathf.Lerp(minFontSize, maxFontSize, normalizedScore);
        slot.textComponent.fontSize = fontSize;

        // if (colorGradient != null && colorGradient.colorKeys.Length > 0)
        // {
        //     Color color = colorGradient.Evaluate(normalizedScore);
        //     slot.textComponent.color = color;
        // }

        if (showDebugLogs)
        {
            Debug.Log($"[WordCloudDisplay] '{slot.currentWord}' -> {points} pts (min: {minPoints}, max: {maxPoints}), normalized: {normalizedScore:F2}, fontSize: {fontSize:F1}");
        }
    }

    private void ClearSlot(WordDisplaySlot slot)
    {
        if (slot.textComponent == null) return;

        if (!string.IsNullOrEmpty(slot.currentWord))
        {
            slot.currentWord = "";
            slot.currentPoints = 0;
            slot.textComponent.text = "";
        }
    }

    private void ClearAllSlots()
    {
        foreach (var slot in wordSlots)
        {
            ClearSlot(slot);
        }

        wordScores.Clear();
        Debug.Log("[WordCloudDisplay] All slots cleared");
    }

    public void ResetAllScores()
    {
        wordScores.Clear();
        ClearAllSlots();
        Debug.Log("[WordCloudDisplay] All scores reset");
    }

    public int GetWordScore(string wordText)
    {
        return wordScores.ContainsKey(wordText) ? wordScores[wordText] : 0;
    }

    public Dictionary<string, int> GetAllWordScores()
    {
        return new Dictionary<string, int>(wordScores);
    }

    /// <summary>
    /// Auto-preenche os wordSlots com todas as palavras disponíveis no jogo.
    /// </summary>
    private void AutoPopulateWordSlots()
    {
        // Coletar todas as palavras únicas baseadas no jogo
        HashSet<string> allWords = new HashSet<string>();

        // Lista completa de todas as palavras do jogo (baseado no ScreenGame)
        string[] allGameWords = {
            // Rodada 1
            "Adaptação", "Desengajado", "Resolução de problema", "Falta de profissionalismo",
            "Compromisso", "Falta de comunicação", "Respeito ao cliente", "Negligente",
            // Rodada 2
            "Desleixo", "Resiliência", "Falta de respeito", "Amadorismo",
            "Prioridade", "Falta de atenção",
            // Rodada 3
            "Improdutividade", "Distração", "Resolução de problemas", "Colaboração",
            "Desorganização", "Descomprometimento", "Estratégia", "Parceria"
        };

        // Adicionar todas as palavras (removendo duplicatas automaticamente pelo HashSet)
        foreach (string word in allGameWords)
        {
            if (!string.IsNullOrEmpty(word))
            {
                allWords.Add(word);
            }
        }

        // Limpar slots existentes
        wordSlots.Clear();

        // Criar slots para cada palavra única
        foreach (string word in allWords.OrderBy(w => w))
        {
            WordDisplaySlot newSlot = new WordDisplaySlot();
            newSlot.positionName = $"Slot_{word}";
            newSlot.currentWord = "";
            newSlot.currentPoints = 0;
            // textComponent será configurado manualmente no Inspector

            wordSlots.Add(newSlot);
        }

        if (showDebugLogs)
        {
            Debug.Log($"[WordCloudDisplay] Auto-populado {wordSlots.Count} slots com palavras disponíveis");

            // Log das palavras encontradas
            foreach (var slot in wordSlots)
            {
                Debug.Log($"  - Slot criado: {slot.positionName}");
            }
        }
    }

    [ContextMenu("Auto Populate Word Slots")]
    public void ManualAutoPopulate()
    {
        AutoPopulateWordSlots();
        Debug.Log("[WordCloudDisplay] Slots auto-populados manualmente via Context Menu");

#if UNITY_EDITOR
        // Marcar como "dirty" para salvar no Editor
        UnityEditor.EditorUtility.SetDirty(this);
#endif
    }

    /// <summary>
    /// Método público para forçar a auto-população (útil para chamar do Inspector ou outros scripts).
    /// </summary>
    public void ForceAutoPopulate()
    {
        AutoPopulateWordSlots();
        Debug.Log("[WordCloudDisplay] Slots auto-populados via método público");
    }

    [ContextMenu("Debug Word Slots")]
    public void DebugWordSlots()
    {
        Debug.Log("=== WORD CLOUD DEBUG ===");
        Debug.Log($"Total Slots: {wordSlots.Count}");

        for (int i = 0; i < wordSlots.Count; i++)
        {
            var slot = wordSlots[i];
            var hasComponent = slot.textComponent != null;
            Debug.Log($"Slot {i}: '{slot.positionName}' | Component: {hasComponent} | Word: '{slot.currentWord}' | Points: {slot.currentPoints}");
        }

        Debug.Log($"Total Words Tracked: {wordScores.Count}");
        foreach (var word in wordScores.OrderByDescending(w => w.Value))
        {
            Debug.Log($"  '{word.Key}': {word.Value} points");
        }
        Debug.Log("========================");
    }

    [ContextMenu("Test Add Word Points")]
    public void TestAddWordPoints()
    {
        string[] testWords = { "Empatia", "Colaboração", "Respeito", "Comunicação" };

        foreach (string word in testWords)
        {
            AddWordPoints(word, Random.Range(1, 5));
        }
    }

    [ContextMenu("Reset All Scores")]
    public void DebugResetScores()
    {
        ResetAllScores();
    }

    #region Public Utility Methods

    /// <summary>
    /// Carrega os dados de uma rodada e inicializa a nuvem de palavras.
    /// IMPORTANTE: Deve ser chamado no início de cada rodada.
    /// </summary>
    /// <param name="roundWords">Lista de WordData da rodada</param>
    public void LoadRoundData(List<WordData> roundWords)
    {
        if (roundWords == null || roundWords.Count == 0)
        {
            Debug.LogError("[WordCloudDisplay] Tentativa de carregar rodada com lista vazia ou nula");
            return;
        }

        currentRoundWords = new List<WordData>(roundWords);

        // Limpa o sistema antigo de wordScores
        wordScores.Clear();

        Debug.Log($"[WordCloudDisplay] ===== CARREGANDO RODADA =====");
        Debug.Log($"[WordCloudDisplay] Total de palavras: {currentRoundWords.Count}");
        Debug.Log($"[WordCloudDisplay] Total de slots disponíveis: {wordSlots.Count}");

        foreach (var word in currentRoundWords)
        {
            Debug.Log($"  - '{word.text}': cumulativePoints={word.cumulativePoints}");
        }

        // Verifica se há slots suficientes
        if (wordSlots.Count < currentRoundWords.Count)
        {
            Debug.LogError($"[WordCloudDisplay] ERRO: Apenas {wordSlots.Count} slots disponíveis para {currentRoundWords.Count} palavras!");
        }

        // Verifica se os slots têm textComponent
        int slotsWithComponent = 0;
        for (int i = 0; i < wordSlots.Count; i++)
        {
            if (wordSlots[i].textComponent != null)
                slotsWithComponent++;
            else
                Debug.LogError($"[WordCloudDisplay] ERRO: Slot {i} ('{wordSlots[i].positionName}') não tem TextMeshProUGUI!");
        }
        Debug.Log($"[WordCloudDisplay] Slots com TextMeshProUGUI válido: {slotsWithComponent}/{wordSlots.Count}");

        // Atualiza a visualização imediatamente
        UpdateWordCloudDisplay();

        Debug.Log($"[WordCloudDisplay] ===== FIM DO CARREGAMENTO =====");
    }

    /// <summary>
    /// Atualiza a pontuação de uma palavra específica na rodada atual.
    /// </summary>
    /// <param name="wordText">Texto da palavra</param>
    /// <param name="newCumulativePoints">Nova pontuação cumulativa</param>
    public void UpdateWordCumulativePoints(string wordText, int newCumulativePoints)
    {
        WordData word = currentRoundWords.Find(w => w.text == wordText);
        if (word != null)
        {
            word.cumulativePoints = newCumulativePoints;

            if (showDebugLogs)
            {
                Debug.Log($"[WordCloudDisplay] Palavra '{wordText}' atualizada para {newCumulativePoints} pontos cumulativos");
            }
        }
        else
        {
            Debug.LogWarning($"[WordCloudDisplay] Palavra '{wordText}' não encontrada na rodada atual");
        }
    }

    /// <summary>
    /// Retorna os dados atuais da rodada.
    /// </summary>
    public List<WordData> GetCurrentRoundWords()
    {
        return new List<WordData>(currentRoundWords);
    }

    /// <summary>
    /// Obtém as palavras mais pontuadas em ordem decrescente.
    /// </summary>
    /// <param name="topCount">Número de palavras para retornar (padrão: 5)</param>
    /// <returns>Lista das palavras mais pontuadas</returns>
    public List<KeyValuePair<string, int>> GetTopWords(int topCount = 5)
    {
        return wordScores.OrderByDescending(w => w.Value)
                        .Take(topCount)
                        .ToList();
    }

    /// <summary>
    /// Obtém a palavra com maior pontuação.
    /// </summary>
    /// <returns>Palavra com maior pontuação ou null se não há palavras</returns>
    public string GetTopWord()
    {
        if (wordScores.Count == 0) return null;

        return wordScores.OrderByDescending(w => w.Value)
                        .First()
                        .Key;
    }

    /// <summary>
    /// Força a atualização visual da nuvem de palavras.
    /// </summary>
    public void ForceUpdateDisplay()
    {
        UpdateWordCloudDisplay();
    }

    /// <summary>
    /// Obtém o número total de palavras com pontuação.
    /// </summary>
    public int GetTrackedWordsCount()
    {
        return wordScores.Count;
    }

    /// <summary>
    /// Verifica se uma palavra específica está sendo rastreada.
    /// </summary>
    /// <param name="wordText">Texto da palavra</param>
    /// <returns>True se a palavra está sendo rastreada</returns>
    public bool IsWordTracked(string wordText)
    {
        return wordScores.ContainsKey(wordText);
    }

    #endregion
}