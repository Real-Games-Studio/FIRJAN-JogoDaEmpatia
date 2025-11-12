// CLEANED: single NFCGameService implementation
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using System;
using _4._NFC_Firjan.Scripts.Server;
using _4._NFC_Firjan.Scripts.NFC;

/// <summary>
/// Serviço principal do sistema NFC para o Jogo da Empatia.
/// Implementa singleton pattern e integração automática com NFCReceiver e ServerComunication.
/// Gerencia dados pendentes e envio automático quando cartão NFC é aproximado.
/// </summary>
public class NFCGameService : MonoBehaviour
{
    #region Singleton

    private static NFCGameService _instance;
    public static NFCGameService Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindFirstObjectByType<NFCGameService>();
                if (_instance == null)
                {
                    GameObject go = new GameObject("NFCGameService");
                    _instance = go.AddComponent<NFCGameService>();
                    DontDestroyOnLoad(go);
                }
            }
            return _instance;
        }
    }

    #endregion

    #region Fields

    [Header("NFC Configuration")]
    [SerializeField] private bool enableDebugLogs = true;

    [Header("Server Configuration")]
    [SerializeField] private string serverIP = "127.0.0.1";
    [SerializeField] private string serverPort = "3000";

    private NFCReceiver nfcReceiver;
    private ServerComunication serverCommunication;
    private string lastReadNfcId;
    private EmpathyGameModel pendingGameData;
    private bool isInitialized = false;

    // Queue para executar ações na thread principal
    private System.Collections.Generic.Queue<System.Action> mainThreadActions = new System.Collections.Generic.Queue<System.Action>();
    private readonly object lockObject = new object();

    #endregion

    #region Unity Callbacks

    private void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeNFCService();
        }
        else if (_instance != this)
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        LoadServerConfiguration();
    }

    private void Update()
    {
        // Executa ações pendentes na thread principal
        lock (lockObject)
        {
            while (mainThreadActions.Count > 0)
            {
                var action = mainThreadActions.Dequeue();
                action?.Invoke();
            }
        }
    }

    #endregion

    #region Initialization

    private void InitializeNFCService()
    {
        LogNFC("Inicializando serviço NFC do Jogo da Empatia...");

        nfcReceiver = FindFirstObjectByType<NFCReceiver>();
        serverCommunication = FindFirstObjectByType<ServerComunication>();

        if (nfcReceiver == null)
        {
            LogNFC("ERRO: NFCReceiver não encontrado! Certifique-se de que o prefab NFC está na cena.");
            return;
        }

        if (serverCommunication == null)
        {
            LogNFC("ERRO: ServerComunication não encontrado! Certifique-se de que o prefab Server está na cena.");
            return;
        }

        SetupNFCEvents();
        isInitialized = true;
        LogNFC("Serviço NFC inicializado com sucesso!");
    }

    private void SetupNFCEvents()
    {
        if (nfcReceiver != null)
        {
            nfcReceiver.OnNFCConnected.RemoveListener(OnNFCCardRead);
            nfcReceiver.OnNFCDisconnected.RemoveListener(OnNFCCardRemoved);

            nfcReceiver.OnNFCConnected.AddListener(OnNFCCardRead);
            nfcReceiver.OnNFCDisconnected.AddListener(OnNFCCardRemoved);

            LogNFC("Eventos NFC configurados.");
        }
    }

    private void LoadServerConfiguration()
    {
        string configPath = System.IO.Path.Combine(Application.streamingAssetsPath, "serverconfig.json");
        if (System.IO.File.Exists(configPath))
        {
            try
            {
                string configJson = System.IO.File.ReadAllText(configPath);
                ServerConfig config = JsonUtility.FromJson<ServerConfig>(configJson);
                if (config != null && !string.IsNullOrEmpty(config.serverIP))
                {
                    serverIP = config.serverIP;
                    serverPort = config.serverPort;
                    LogNFC($"Configuração carregada: {serverIP}:{serverPort}");
                }
            }
            catch (Exception ex)
            {
                LogNFC($"Erro ao carregar configuração: {ex.Message}");
            }
        }
        else
        {
            LogNFC($"Arquivo de configuração não encontrado: {configPath}");
        }
    }

    #endregion

    #region NFC Events

    private void OnNFCCardRead(string nfcId, string readerName)
    {
        // Garante que tudo seja executado na thread principal
        RunOnMainThread(() =>
        {
            LogNFC("=== CARTÃO NFC LIDO ===");
            LogNFC($"Cartão: {nfcId} | Leitor: {readerName} | {DateTime.Now:HH:mm:ss.fff}");

            lastReadNfcId = nfcId;

            if (pendingGameData != null)
            {
                LogNFC($"Dados pendentes encontrados! Associando ao NFC ID: {nfcId}");
                LogNFC($"Dados antes: {pendingGameData}");

                pendingGameData.nfcId = nfcId;

                LogNFC($"Dados depois: {pendingGameData}");
                LogNFC("Chamando SendGameDataToServer...");

                SendGameDataToServer(pendingGameData);

                pendingGameData = null;
                LogNFC("Dados pendentes limpos após envio");
            }
            else
            {
                LogNFC("⚠ AVISO: Cartão lido mas NÃO HÁ dados do jogo pendentes!");
                LogNFC("Certifique-se de que SubmitGameResult foi chamado ANTES de passar o cartão.");
            }
        });
    }

    private void OnNFCCardRemoved()
    {
        LogNFC("Cartão removido");
    }

    #endregion

    #region Public Methods

    public void SubmitGameResult(int empathy, int activeListening, int selfAwareness)
    {
        LogNFC("=== SubmitGameResult CHAMADO ===");
        LogNFC($"Parâmetros recebidos - Empathy: {empathy}, ActiveListening: {activeListening}, SelfAwareness: {selfAwareness}");

        if (!isInitialized)
        {
            LogNFC("ERRO: Serviço NFC não inicializado!");
            return;
        }

        pendingGameData = new EmpathyGameModel("", empathy, activeListening, selfAwareness);
        LogNFC($"✓ Dados armazenados com sucesso! Aguardando leitura do cartão NFC...");
        LogNFC($"Dados pendentes: {pendingGameData}");
    }

    public string GetLastNfcId() => lastReadNfcId;

    #endregion

    #region Private Methods

    private void SendGameDataToServer(EmpathyGameModel gameData)
    {
        LogNFC("=== SendGameDataToServer CHAMADO ===");

        if (gameData == null)
        {
            LogNFC("ERRO: gameData é null");
            return;
        }

        if (string.IsNullOrEmpty(gameData.nfcId))
        {
            LogNFC($"ERRO: nfcId está vazio. gameData: {gameData}");
            return;
        }

        LogNFC($"Dados válidos! Iniciando POST para NFC ID: {gameData.nfcId}");
        StartCoroutine(SendPostRequest(gameData));
    }

    private IEnumerator SendPostRequest(EmpathyGameModel gameData)
    {
        LogNFC("=== COROUTINE SendPostRequest INICIADA ===");

        string url = $"http://{serverIP}:{serverPort}/users/{gameData.nfcId}";
        string jsonData = gameData.ToJson();
        LogNFC($"POST -> {url}");
        LogNFC($"Payload: {jsonData}");

        using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
        {
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonData);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            LogNFC("Enviando requisição HTTP...");
            yield return request.SendWebRequest();
            LogNFC("Requisição HTTP concluída!");

            if (request.result == UnityWebRequest.Result.Success)
            {
                LogNFC($"✓ POST SUCCESS: {request.downloadHandler.text}");
                NotifyPostSuccess();
            }
            else
            {
                LogNFC($"✗ POST FAILED: {request.error}");
                LogNFC($"Response Code: {request.responseCode}");
                LogNFC($"URL tentada: {url}");
            }
        }

        LogNFC("=== COROUTINE SendPostRequest FINALIZADA ===");
    }

    #endregion

    #region Helper Methods

    private void LogNFC(string message)
    {
        if (enableDebugLogs) Debug.Log($"[NFC] {message}");
    }

    private void NotifyPostSuccess()
    {
        // Garante que a notificação seja feita na thread principal
        RunOnMainThread(() =>
        {
            var screen = FindFirstObjectByType<ScreenResult>();
            if (screen != null)
            {
                LogNFC("Notificando ScreenResult sobre sucesso do POST");
                screen.OnPostSuccess();
            }
            else
            {
                LogNFC("ScreenResult não encontrado para notificar sucesso do POST");
            }
        });
    }

    /// <summary>
    /// Enfileira uma ação para ser executada na thread principal do Unity.
    /// </summary>
    private void RunOnMainThread(System.Action action)
    {
        lock (lockObject)
        {
            mainThreadActions.Enqueue(action);
        }
    }

    #endregion
}

/// <summary>
/// Estrutura para carregar configuração do servidor.
/// </summary>
[System.Serializable]
public class ServerConfig
{
    public string serverIP = "127.0.0.1";
    public string serverPort = "3000";
}