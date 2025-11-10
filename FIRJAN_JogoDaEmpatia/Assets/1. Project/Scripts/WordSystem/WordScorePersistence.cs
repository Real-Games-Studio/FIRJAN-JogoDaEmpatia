using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

/// <summary>
/// Gerenciador de persistência para as pontuações das palavras.
/// Salva e carrega dados JSON para cada rodada separadamente.
///
/// IMPORTANTE PARA EVENTOS:
/// - Os JSONs ficam em StreamingAssets (fácil acesso e edição)
/// - Pontuações acumulam entre TODOS os jogadores do evento
/// - Para resetar: delete os 3 arquivos JSON ou edite manualmente
/// - Caminho: [Pasta do Jogo]/FIRJAN_JogoDaEmpatia_Data/StreamingAssets/WordScores/
/// </summary>
public class WordScorePersistence : MonoBehaviour
{
    private const string FOLDER_NAME = "WordScores";
    private const string ROUND1_FILENAME = "round1_scores.json";
    private const string ROUND2_FILENAME = "round2_scores.json";
    private const string ROUND3_FILENAME = "round3_scores.json";

    public static WordScorePersistence Instance { get; private set; }

    [Header("Save Location")]
    [Tooltip("Usar StreamingAssets (para eventos) ou PersistentDataPath (padrão Unity)?")]
    public bool useStreamingAssets = true;

    [Header("Debug Settings")]
    [Tooltip("Mostrar logs detalhados de debug?")]
    public bool showDebugLogs = true;

    private string saveFolder = "";

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeSaveFolder();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Inicializa a pasta onde os JSONs serão salvos.
    /// </summary>
    private void InitializeSaveFolder()
    {
        if (useStreamingAssets)
        {
            // StreamingAssets - fácil de acessar e editar
            saveFolder = Path.Combine(Application.streamingAssetsPath, FOLDER_NAME);
        }
        else
        {
            // PersistentDataPath - padrão Unity (pasta escondida)
            saveFolder = Application.persistentDataPath;
        }

        // Cria a pasta se não existir
        if (!Directory.Exists(saveFolder))
        {
            try
            {
                Directory.CreateDirectory(saveFolder);
                Debug.Log($"[WordScorePersistence] Pasta criada: {saveFolder}");
            }
            catch (Exception e)
            {
                Debug.LogError($"[WordScorePersistence] Erro ao criar pasta: {e.Message}");
            }
        }

        if (showDebugLogs)
        {
            Debug.Log($"[WordScorePersistence] Salvando em: {saveFolder}");
        }
    }

    /// <summary>
    /// Retorna o caminho completo do arquivo JSON para uma rodada específica.
    /// </summary>
    private string GetFilePath(int roundIndex)
    {
        string filename;
        switch (roundIndex)
        {
            case 0:
                filename = ROUND1_FILENAME;
                break;
            case 1:
                filename = ROUND2_FILENAME;
                break;
            case 2:
                filename = ROUND3_FILENAME;
                break;
            default:
                Debug.LogError($"[WordScorePersistence] Índice de rodada inválido: {roundIndex}");
                return null;
        }

        return Path.Combine(saveFolder, filename);
    }

    /// <summary>
    /// Carrega os dados de pontuação de uma rodada específica.
    /// Se o arquivo não existir, retorna dados inicializados com cumulativePoints = 1.
    /// </summary>
    /// <param name="roundIndex">Índice da rodada (0-2)</param>
    /// <param name="defaultWords">Lista de palavras padrão caso o arquivo não exista</param>
    /// <returns>Lista de WordData com pontuações carregadas</returns>
    public List<WordData> LoadRoundData(int roundIndex, List<string> defaultWords)
    {
        string filePath = GetFilePath(roundIndex);
        if (string.IsNullOrEmpty(filePath))
        {
            return CreateDefaultWordData(defaultWords);
        }

        if (File.Exists(filePath))
        {
            try
            {
                string json = File.ReadAllText(filePath);
                RoundScoreData data = JsonUtility.FromJson<RoundScoreData>(json);

                if (data != null && data.words != null && data.words.Count > 0)
                {
                    if (showDebugLogs)
                    {
                        Debug.Log($"[WordScorePersistence] Dados carregados da rodada {roundIndex + 1}: {data.words.Count} palavras");
                        foreach (var word in data.words)
                        {
                            Debug.Log($"  - '{word.text}': points={word.points}, cumulativePoints={word.cumulativePoints}");
                        }
                    }

                    return data.words;
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[WordScorePersistence] Erro ao carregar dados da rodada {roundIndex + 1}: {e.Message}");
            }
        }
        else
        {
            if (showDebugLogs)
            {
                Debug.Log($"[WordScorePersistence] Arquivo não encontrado para rodada {roundIndex + 1}. Criando dados padrão.");
            }
        }

        // Se não conseguiu carregar ou arquivo não existe, cria dados padrão
        return CreateDefaultWordData(defaultWords);
    }

    /// <summary>
    /// Salva os dados de pontuação de uma rodada específica.
    /// </summary>
    /// <param name="roundIndex">Índice da rodada (0-2)</param>
    /// <param name="words">Lista de WordData para salvar</param>
    /// <returns>True se salvou com sucesso, False caso contrário</returns>
    public bool SaveRoundData(int roundIndex, List<WordData> words)
    {
        string filePath = GetFilePath(roundIndex);
        if (string.IsNullOrEmpty(filePath))
        {
            return false;
        }

        try
        {
            RoundScoreData data = new RoundScoreData();
            data.words = words;

            string json = JsonUtility.ToJson(data, true);
            File.WriteAllText(filePath, json);

            if (showDebugLogs)
            {
                Debug.Log($"[WordScorePersistence] Dados salvos para rodada {roundIndex + 1}: {words.Count} palavras");
                Debug.Log($"[WordScorePersistence] Caminho: {filePath}");
                foreach (var word in words)
                {
                    Debug.Log($"  - '{word.text}': points={word.points}, cumulativePoints={word.cumulativePoints}");
                }
            }

            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"[WordScorePersistence] Erro ao salvar dados da rodada {roundIndex + 1}: {e.Message}");
            return false;
        }
    }

    /// <summary>
    /// Cria dados padrão para palavras que ainda não têm pontuação salva.
    /// </summary>
    /// <param name="wordTexts">Lista de textos das palavras</param>
    /// <returns>Lista de WordData com pontuações inicializadas em 1</returns>
    private List<WordData> CreateDefaultWordData(List<string> wordTexts)
    {
        List<WordData> wordDataList = new List<WordData>();

        foreach (string wordText in wordTexts)
        {
            wordDataList.Add(new WordData(wordText, 1, 1));
        }

        if (showDebugLogs)
        {
            Debug.Log($"[WordScorePersistence] Criados dados padrão para {wordDataList.Count} palavras");
        }

        return wordDataList;
    }

    /// <summary>
    /// Atualiza a pontuação cumulativa de uma palavra específica.
    /// </summary>
    /// <param name="roundIndex">Índice da rodada (0-2)</param>
    /// <param name="wordText">Texto da palavra</param>
    /// <param name="pointsToAdd">Pontos a adicionar (padrão: 1)</param>
    /// <returns>True se atualizou com sucesso, False caso contrário</returns>
    public bool UpdateWordScore(int roundIndex, string wordText, int pointsToAdd = 1)
    {
        List<WordData> words = LoadRoundData(roundIndex, new List<string> { wordText });

        WordData targetWord = words.Find(w => w.text == wordText);
        if (targetWord != null)
        {
            targetWord.cumulativePoints += pointsToAdd;
            return SaveRoundData(roundIndex, words);
        }
        else
        {
            Debug.LogWarning($"[WordScorePersistence] Palavra '{wordText}' não encontrada na rodada {roundIndex + 1}");
            return false;
        }
    }

    /// <summary>
    /// Reseta todos os dados salvos (útil para debug/testes).
    /// </summary>
    [ContextMenu("Reset All Saved Data")]
    public void ResetAllData()
    {
        for (int i = 0; i < 3; i++)
        {
            string filePath = GetFilePath(i);
            if (!string.IsNullOrEmpty(filePath) && File.Exists(filePath))
            {
                try
                {
                    File.Delete(filePath);
                    Debug.Log($"[WordScorePersistence] Dados da rodada {i + 1} deletados");
                }
                catch (Exception e)
                {
                    Debug.LogError($"[WordScorePersistence] Erro ao deletar dados da rodada {i + 1}: {e.Message}");
                }
            }
        }

        Debug.Log("[WordScorePersistence] Todos os dados foram resetados");
    }

    /// <summary>
    /// Exibe o caminho onde os arquivos JSON são salvos.
    /// </summary>
    [ContextMenu("Show Save Location")]
    public void ShowSaveLocation()
    {
        Debug.Log($"[WordScorePersistence] Caminho de salvamento: {Application.persistentDataPath}");

        for (int i = 0; i < 3; i++)
        {
            string filePath = GetFilePath(i);
            bool exists = File.Exists(filePath);
            Debug.Log($"  - Rodada {i + 1}: {filePath} (Existe: {exists})");
        }
    }

    /// <summary>
    /// Debug: Carrega e exibe todos os dados salvos.
    /// </summary>
    [ContextMenu("Debug All Saved Data")]
    public void DebugAllSavedData()
    {
        Debug.Log("=== DADOS SALVOS ===");

        for (int i = 0; i < 3; i++)
        {
            string filePath = GetFilePath(i);
            if (File.Exists(filePath))
            {
                try
                {
                    string json = File.ReadAllText(filePath);
                    Debug.Log($"\n--- Rodada {i + 1} ---");
                    Debug.Log(json);
                }
                catch (Exception e)
                {
                    Debug.LogError($"[WordScorePersistence] Erro ao ler rodada {i + 1}: {e.Message}");
                }
            }
            else
            {
                Debug.Log($"\n--- Rodada {i + 1} ---");
                Debug.Log("(Arquivo não existe)");
            }
        }

        Debug.Log("====================");
    }
}
