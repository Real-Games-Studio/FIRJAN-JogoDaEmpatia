// CLEANED: single NFCGameService implementation
using System;
using System.Net;
using System.Threading.Tasks;
using UnityEngine;
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

    private NFCReceiver nfcReceiver;
    private ServerComunication serverCommunication;
    private string lastReadNfcId;
    private GameModel pendingGameData;
    private bool isInitialized = false;
    private bool shouldNotifySuccess = false;

    #endregion

    #region Unity Callbacks

    private void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
            InitializeNFCService();
        }
        else if (_instance != this)
        {
            Destroy(gameObject);
        }
    }

    private void Update()
    {
        // Executa NotifyPostSuccess na main thread
        if (shouldNotifySuccess)
        {
            shouldNotifySuccess = false;
            NotifyPostSuccess();
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
            LogNFC("AVISO: ServerComunication não encontrado na cena.");
            LogNFC("Criando componente ServerComunication...");
            serverCommunication = gameObject.AddComponent<ServerComunication>();
            LoadServerConfiguration();
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
                    serverCommunication.Ip = config.serverIP;
                    serverCommunication.Port = int.Parse(config.serverPort);
                    LogNFC($"Configuração carregada: {config.serverIP}:{config.serverPort}");
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

    private async void OnNFCCardRead(string nfcId, string readerName)
    {
        LogNFC("===========================================");
        LogNFC("=== EVENTO OnNFCCardRead DISPARADO ===");
        LogNFC("===========================================");
        LogNFC($"Timestamp: {DateTime.Now:HH:mm:ss.fff}");
        LogNFC($"Cartão ID: {nfcId}");
        LogNFC($"Leitor: {readerName}");

        lastReadNfcId = nfcId;
        LogNFC($"lastReadNfcId armazenado: {lastReadNfcId}");

        LogNFC($"pendingGameData é null? {pendingGameData == null}");

        if (pendingGameData != null)
        {
            LogNFC("✓ DADOS PENDENTES ENCONTRADOS!");
            LogNFC($"Empathy (skill1): {pendingGameData.skill1}");
            LogNFC($"ActiveListening (skill2): {pendingGameData.skill2}");
            LogNFC($"SelfAwareness (skill3): {pendingGameData.skill3}");

            pendingGameData.nfcId = nfcId;
            LogNFC($"nfcId atribuído: {pendingGameData.nfcId}");

            LogNFC("Chamando ServerComunication.UpdateNfcInfoFromGame...");
            await SendGameDataToServer(pendingGameData);

            pendingGameData = null;
            LogNFC("Dados pendentes limpos após envio");
        }
        else
        {
            LogNFC("⚠ ⚠ ⚠ AVISO: Cartão lido mas NÃO HÁ dados do jogo pendentes!");
            LogNFC("Certifique-se de que SubmitGameResult foi chamado ANTES de passar o cartão.");
        }

        LogNFC("=== FIM OnNFCCardRead ===");
    }

    private void OnNFCCardRemoved()
    {
        LogNFC("===========================================");
        LogNFC("=== CARTÃO NFC REMOVIDO ===");
        LogNFC("===========================================");
        LogNFC($"Timestamp: {DateTime.Now:HH:mm:ss.fff}");
    }

    #endregion

    #region Public Methods

    public void SubmitGameResult(int empathy, int activeListening, int selfAwareness)
    {
        LogNFC("=== SubmitGameResult CHAMADO ===");
        LogNFC($"Timestamp: {DateTime.Now:HH:mm:ss.fff}");
        LogNFC($"Parâmetros recebidos:");
        LogNFC($"  - Empathy: {empathy}");
        LogNFC($"  - ActiveListening: {activeListening}");
        LogNFC($"  - SelfAwareness: {selfAwareness}");

        if (!isInitialized)
        {
            LogNFC("✗ ERRO: Serviço NFC não inicializado!");
            return;
        }

        LogNFC("Criando GameModel...");
        pendingGameData = new GameModel
        {
            nfcId = "", // Será preenchido quando o cartão for lido
            gameId = 5,
            skill1 = empathy,
            skill2 = activeListening,
            skill3 = selfAwareness
        };

        LogNFC("✓ ✓ ✓ Dados armazenados com SUCESSO!");
        LogNFC($"pendingGameData.skill1 (Empathy): {pendingGameData.skill1}");
        LogNFC($"pendingGameData.skill2 (ActiveListening): {pendingGameData.skill2}");
        LogNFC($"pendingGameData.skill3 (SelfAwareness): {pendingGameData.skill3}");
        LogNFC($"pendingGameData.gameId: {pendingGameData.gameId}");
        LogNFC("Aguardando leitura do cartão NFC...");
        LogNFC("=== FIM SubmitGameResult ===");
    }

    public string GetLastNfcId() => lastReadNfcId;

    #endregion

    #region Private Methods

    private async Task SendGameDataToServer(GameModel gameData)
    {
        LogNFC("=== SendGameDataToServer CHAMADO ===");

        if (gameData == null)
        {
            LogNFC("ERRO: gameData é null");
            return;
        }

        if (string.IsNullOrEmpty(gameData.nfcId))
        {
            LogNFC($"ERRO: nfcId está vazio.");
            return;
        }

        LogNFC($"Dados válidos! Enviando POST para NFC ID: {gameData.nfcId}");
        LogNFC($"URL: http://{serverCommunication.Ip}:{serverCommunication.Port}/users/{gameData.nfcId}");
        LogNFC($"Payload: {gameData.ToString()}");

        try
        {
            HttpStatusCode statusCode = await serverCommunication.UpdateNfcInfoFromGame(gameData);

            LogNFC($"Resposta do servidor: {statusCode}");

            if (statusCode == HttpStatusCode.OK)
            {
                LogNFC("✓ ✓ ✓ POST SUCCESS ✓ ✓ ✓");
                LogNFC("Agendando NotifyPostSuccess() para executar na main thread...");
                shouldNotifySuccess = true;
            }
            else
            {
                LogNFC($"✗ ✗ ✗ POST FAILED ✗ ✗ ✗");
                LogNFC($"Status Code: {statusCode}");
            }
        }
        catch (Exception ex)
        {
            LogNFC($"✗ ERRO na requisição: {ex.Message}");
            LogNFC($"Stack trace: {ex.StackTrace}");
        }

        LogNFC("=== SendGameDataToServer FINALIZADO ===");
    }

    #endregion

    #region Helper Methods

    private void LogNFC(string message)
    {
        if (enableDebugLogs) Debug.Log($"[NFC] {message}");
    }

    private void NotifyPostSuccess()
    {
        LogNFC("=== NotifyPostSuccess CHAMADO ===");

        var screen = FindFirstObjectByType<ScreenResult>();
        if (screen != null)
        {
            LogNFC("✓ ScreenResult encontrado! Chamando OnPostSuccess()...");
            screen.OnPostSuccess();
            LogNFC("✓ OnPostSuccess() chamado com sucesso");
        }
        else
        {
            LogNFC("✗ ERRO: ScreenResult não encontrado na cena!");
            LogNFC("Certifique-se de que a ScreenResult existe e está ativa na hierarquia");
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
    public string serverIP = "192.168.0.212";
    public string serverPort = "8080";
}
