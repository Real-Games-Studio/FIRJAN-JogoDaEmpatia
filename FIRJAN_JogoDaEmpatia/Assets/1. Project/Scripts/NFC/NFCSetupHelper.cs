using _4._NFC_Firjan.Scripts.NFC;
using _4._NFC_Firjan.Scripts.Server;
using UnityEngine;

/// <summary>
/// Script auxiliar para configuração automática do sistema NFC na cena.
/// Adicione este componente em um GameObject na cena para configurar automaticamente
/// todos os componentes necessários do sistema NFC.
/// </summary>
public class NFCSetupHelper : MonoBehaviour
{
    [Header("Setup Configuration")]
    [SerializeField] private bool autoSetupOnStart = true;
    [SerializeField] private bool enableDebugLogs = true;

    [Header("Prefabs Required (Optional - Auto Search)")]
    [SerializeField] private GameObject nfcPrefab;
    [SerializeField] private GameObject serverPrefab;

    private void Start()
    {
        if (autoSetupOnStart)
        {
            SetupNFCSystem();
        }
    }

    /// <summary>
    /// Configura automaticamente o sistema NFC na cena.
    /// </summary>
    [ContextMenu("Setup NFC System")]
    public void SetupNFCSystem()
    {
        Log("=== INICIANDO CONFIGURAÇÃO DO SISTEMA NFC ===");

        // 1. Verifica se NFCGameService existe
        SetupNFCGameService();

        // 2. Verifica se os prefabs NFC e Server estão na cena
        SetupNFCComponents();

        // 3. Verifica arquivo de configuração
        CheckServerConfiguration();

        Log("=== CONFIGURAÇÃO NFC CONCLUÍDA ===");
    }

    /// <summary>
    /// Configura o NFCGameService.
    /// </summary>
    private void SetupNFCGameService()
    {
        NFCGameService nfcService = FindFirstObjectByType<NFCGameService>();

        if (nfcService == null)
        {
            GameObject nfcServiceGO = new GameObject("NFCGameService");
            nfcService = nfcServiceGO.AddComponent<NFCGameService>();
            Log("NFCGameService criado automaticamente.");
        }
        else
        {
            Log("NFCGameService já existe na cena.");
        }
    }

    /// <summary>
    /// Configura os componentes NFC necessários.
    /// </summary>
    private void SetupNFCComponents()
    {
        // Verifica NFCReceiver
        NFCReceiver nfcReceiver = FindFirstObjectByType<NFCReceiver>();
        if (nfcReceiver == null)
        {
            LogWarning("NFCReceiver não encontrado! Adicione o prefab [Adicionar na Cena] NFC.prefab na cena.");
        }
        else
        {
            Log("NFCReceiver encontrado na cena.");
        }

        // Verifica ServerComunication
        ServerComunication serverCom = FindFirstObjectByType<ServerComunication>();
        if (serverCom == null)
        {
            LogWarning("ServerComunication não encontrado! Adicione o prefab [Adicionar na Cena] Server.prefab na cena.");
        }
        else
        {
            Log("ServerComunication encontrado na cena.");
        }
    }

    /// <summary>
    /// Verifica se o arquivo de configuração existe.
    /// </summary>
    private void CheckServerConfiguration()
    {
        string configPath = System.IO.Path.Combine(Application.streamingAssetsPath, "serverconfig.json");

        if (System.IO.File.Exists(configPath))
        {
            Log("Arquivo serverconfig.json encontrado.");
            try
            {
                string configContent = System.IO.File.ReadAllText(configPath);
                Log($"Configuração carregada: {configContent}");
            }
            catch (System.Exception ex)
            {
                LogWarning($"Erro ao ler configuração: {ex.Message}");
            }
        }
        else
        {
            LogWarning($"Arquivo serverconfig.json não encontrado em: {configPath}");
        }
    }

    /// <summary>
    /// Testa o sistema NFC com dados simulados.
    /// </summary>
    [ContextMenu("Test NFC System")]
    public void TestNFCSystem()
    {
        Log("=== TESTANDO SISTEMA NFC ===");

        if (NFCGameService.Instance != null)
        {
            // Testa com pontuações médias
            NFCGameService.Instance.SubmitGameResult(8, 6, 4);
            Log("Teste de envio realizado com sucesso!");
        }
        else
        {
            LogWarning("NFCGameService não disponível para teste.");
        }
    }

    /// <summary>
    /// Limpa os dados NFC armazenados.
    /// </summary>
    [ContextMenu("Clear NFC Data")]
    public void ClearNFCData()
    {
        if (NFCGameService.Instance != null)
        {
            // NFCGameService.Instance.ClearNfcData();
            Log("Dados NFC limpos.");
        }
    }

    /// <summary>
    /// Mostra informações de debug do sistema NFC.
    /// </summary>
    [ContextMenu("Debug NFC System")]
    public void DebugNFCSystem()
    {
        if (NFCGameService.Instance != null)
        {
            // NFCGameService.Instance.DebugServiceState();
        }
    }

    /// <summary>
    /// Mostra o último NFC ID lido.
    /// </summary>
    [ContextMenu("Show Last NFC ID")]
    public void ShowLastNfcId()
    {
        if (NFCGameService.Instance != null)
        {
            string lastId = NFCGameService.Instance.GetLastNfcId();
            Log($"Último NFC ID lido: {lastId ?? "Nenhum"}");
        }
    }

    private void Log(string message)
    {
        if (enableDebugLogs)
        {
            Debug.Log($"[NFC Setup] {message}");
        }
    }

    private void LogWarning(string message)
    {
        if (enableDebugLogs)
        {
            Debug.LogWarning($"[NFC Setup] {message}");
        }
    }
}