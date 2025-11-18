using UnityEngine;
using TMPro;
using _4._NFC_Firjan.Scripts.Server;

namespace FIRJAN.Utilities
{
    /// <summary>
    /// Overlay de debug para exibir informações do NFC em qualquer tela.
    /// Pressione F1 (configurável) para mostrar/esconder.
    /// </summary>
    public class NFCDebugOverlay : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private KeyCode toggleKey = KeyCode.F1;
        [SerializeField] private KeyCode simulateNFCKey = KeyCode.N;
        [SerializeField] private KeyCode restartReaderKey = KeyCode.R;
        [SerializeField] private KeyCode testReaderKey = KeyCode.T;
        [SerializeField] private KeyCode simulatePostKey = KeyCode.P;

        private bool isMonitoringEvents = false;
        private float monitoringStartTime = 0f;
        private int eventsDetected = 0;
        private System.Collections.Generic.List<string> testLogs = new System.Collections.Generic.List<string>();
        private const int MAX_LOGS = 15;

        [Header("UI References")]
        [SerializeField] private CanvasGroup debugCanvasGroup;
        [SerializeField] private TextMeshProUGUI debugText;

        private bool isVisible = false;

        private void Start()
        {
            // Começa invisível mas ativo
            if (debugCanvasGroup != null)
            {
                debugCanvasGroup.alpha = 0f;
                debugCanvasGroup.interactable = false;
                debugCanvasGroup.blocksRaycasts = false;
            }
        }

        private void Update()
        {
            // Toggle do painel
            if (Input.GetKeyDown(toggleKey))
            {
                ToggleDebugPanel();
            }

            // Simula NFC (apenas em editor ou development build)


            if (Input.GetKeyDown(simulateNFCKey))
            {
                SimulateNFC();
            }

            if (Input.GetKeyDown(restartReaderKey))
            {
                RestartNFCReader();
            }

            if (Input.GetKeyDown(testReaderKey))
            {
                TestReaderEvents();
            }

            if (Input.GetKeyDown(simulatePostKey))
            {
                SimulateCompletePost();
            }

            // Verifica timeout do teste
            if (isMonitoringEvents && Time.time - monitoringStartTime > 10f)
            {
                StopMonitoringEvents();
            }

            // Atualiza info sempre que estiver visível OU durante teste
            if (isVisible || isMonitoringEvents)
            {
                UpdateDebugInfo();
            }
        }

        private void ToggleDebugPanel()
        {
            if (debugCanvasGroup != null)
            {
                isVisible = !isVisible;
                debugCanvasGroup.alpha = isVisible ? 1f : 0f;
                debugCanvasGroup.interactable = isVisible;
                debugCanvasGroup.blocksRaycasts = isVisible;

                if (isVisible)
                {
                    UpdateDebugInfo();
                }
            }
        }
        private void UpdateDebugInfo()
        {
            if (debugText == null) return;

            string info = "=== NFC DEBUG INFO ===\n\n";

            // Verifica NFCGameService
            if (NFCGameService.Instance != null)
            {
                info += "<color=#00FF00>✓ NFCGameService: ATIVO</color>\n";

                string lastNfcId = NFCGameService.Instance.GetLastNfcId();
                if (!string.IsNullOrEmpty(lastNfcId))
                {
                    info += "<color=#FFFF00>Último NFC:</color> " + lastNfcId + "\n";
                }
                else
                {
                    info += "<color=#808080>Nenhum NFC lido ainda</color>\n";
                }
            }
            else
            {
                info += "<color=#FF0000>✗ NFCGameService: INATIVO</color>\n";
            }

            info += "\n";

            // Verifica NFCReceiver e status do leitor de hardware
            try
            {
                var nfcReceiverType = System.Type.GetType("_4._NFC_Firjan.Scripts.NFC.NFCReceiver, Assembly-CSharp");
                if (nfcReceiverType != null)
                {
                    var nfcReceiver = FindFirstObjectByType(nfcReceiverType);
                    if (nfcReceiver != null)
                    {
                        info += "<color=#00FF00>✓ NFCReceiver: ENCONTRADO</color>\n";

                        // Verifica status do leitor através da DLL Lando
                        var cardReaderField = nfcReceiverType.GetField("_cardReader",
                            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

                        if (cardReaderField != null)
                        {
                            var cardReader = cardReaderField.GetValue(nfcReceiver);
                            if (cardReader != null)
                            {
                                // Tenta verificar se tem leitores conectados via Lando
                                var cardReaderType = cardReader.GetType();
                                var readersProperty = cardReaderType.GetProperty("Readers");

                                if (readersProperty != null)
                                {
                                    var readers = readersProperty.GetValue(cardReader);
                                    if (readers != null)
                                    {
                                        var readersList = readers as System.Collections.ICollection;
                                        if (readersList != null && readersList.Count > 0)
                                        {
                                            info += "<color=#00FF00>✓ Leitor NFC Hardware: CONECTADO (" + readersList.Count + ")</color>\n";

                                            // Lista os leitores encontrados
                                            int index = 1;
                                            foreach (var reader in readersList)
                                            {
                                                if (reader != null)
                                                {
                                                    info += "<color=#FFFF00>  [" + index + "] " + reader.ToString() + "</color>\n";
                                                    index++;
                                                }
                                            }

                                            info += "<color=#00FF00>Status: PRONTO PARA LER CARTÕES</color>\n";
                                        }
                                        else
                                        {
                                            info += "<color=#FF0000>✗ Leitor NFC Hardware: DESCONECTADO</color>\n";
                                            info += "<color=#808080>  Nenhum leitor USB detectado</color>\n";
                                            info += "<color=#FFA500>  Verifique: USB conectado? Drivers instalados?</color>\n";
                                        }
                                    }
                                    else
                                    {
                                        info += "<color=#FF0000>✗ Leitor NFC Hardware: DESCONECTADO</color>\n";
                                    }
                                }
                                else
                                {
                                    info += "<color=#FFA500>⚠ Não foi possível verificar hardware</color>\n";
                                }
                            }
                        }
                    }
                    else
                    {
                        info += "<color=#FF0000>✗ NFCReceiver: NÃO ENCONTRADO</color>\n";
                    }
                }
            }
            catch (System.Exception e)
            {
                info += "<color=#FFA500>⚠ Erro: " + e.Message + "</color>\n";
            }

            info += "\n";

            // Mostra status do teste se estiver ativo
            if (isMonitoringEvents)
            {
                float timeLeft = 10f - (Time.time - monitoringStartTime);
                info += "<color=#FFFF00>=== TESTE ATIVO ===</color>\n";
                info += "<color=#FFFF00>Tempo: " + timeLeft.ToString("F1") + "s | Eventos: " + eventsDetected + "</color>\n\n";

                // Mostra histórico de logs
                info += "<color=#808080>--- Histórico ---</color>\n";
                foreach (var log in testLogs)
                {
                    info += log + "\n";
                }
                info += "\n";
            }
            else if (testLogs.Count > 0)
            {
                // Mostra resultado final após o teste
                info += "<color=#808080>=== Último Teste ===</color>\n";
                foreach (var log in testLogs)
                {
                    info += log + "\n";
                }
                info += "\n";
            }

            info += "<color=#00FFFF>[" + toggleKey + "]</color> Esconder\n";
            info += "<color=#00FFFF>[" + testReaderKey + "]</color> Testar Leitor (10s)\n";
            info += "<color=#00FFFF>[" + restartReaderKey + "]</color> Reiniciar Leitor\n";
            info += "<color=#00FFFF>[" + simulatePostKey + "]</color> POST Real (tela final)\n";
            info += "<color=#00FFFF>[" + simulateNFCKey + "]</color> NFC Fake";

            debugText.text = info;
        }

        private void SimulateNFC()
        {
            testLogs.Clear();
            AddTestLog("<color=#00FFFF>=== SIMULAR NFC (FAKE) ===</color>");

            try
            {
                // Encontra o NFCReceiver
                var nfcReceiverType = System.Type.GetType("_4._NFC_Firjan.Scripts.NFC.NFCReceiver, Assembly-CSharp");
                if (nfcReceiverType != null)
                {
                    var nfcReceiver = FindFirstObjectByType(nfcReceiverType);
                    if (nfcReceiver != null)
                    {
                        // Pega o evento OnNFCConnected
                        var onNFCConnectedField = nfcReceiverType.GetField("OnNFCConnected");
                        if (onNFCConnectedField != null)
                        {
                            var onNFCConnectedEvent = onNFCConnectedField.GetValue(nfcReceiver) as UnityEngine.Events.UnityEvent<string, string>;
                            if (onNFCConnectedEvent != null)
                            {
                                // Simula um ID de NFC fake e nome de leitor fake
                                string fakeNfcId = "DEBUG_NFC_" + System.DateTime.Now.Ticks;
                                string fakeReaderName = "DEBUG_READER";

                                AddTestLog("Invocando evento fake...");

                                // Invoca o evento como se o hardware tivesse detectado um cartão
                                onNFCConnectedEvent.Invoke(fakeNfcId, fakeReaderName);

                                AddTestLog("<color=#00FF00>✓ Evento invocado!</color>");
                                AddTestLog("<color=#FFFF00>ID: " + fakeNfcId + "</color>");
                            }
                            else
                            {
                                AddTestLog("<color=#FF0000>✗ Evento é nulo!</color>");
                            }
                        }
                        else
                        {
                            AddTestLog("<color=#FF0000>✗ Campo não encontrado!</color>");
                        }
                    }
                    else
                    {
                        AddTestLog("<color=#FF0000>✗ NFCReceiver não encontrado!</color>");
                        AddTestLog("<color=#808080>Adicione o prefab NFC na cena</color>");
                    }
                }
                else
                {
                    AddTestLog("<color=#FF0000>✗ Tipo NFCReceiver não encontrado!</color>");
                }
            }
            catch (System.Exception e)
            {
                AddTestLog("<color=#FF0000>✗ ERRO: " + e.Message + "</color>");
            }
        }
        private void RestartNFCReader()
        {
            testLogs.Clear();
            AddTestLog("<color=#00FFFF>=== REINICIAR LEITOR ===</color>");

            try
            {
                var nfcReceiverType = System.Type.GetType("_4._NFC_Firjan.Scripts.NFC.NFCReceiver, Assembly-CSharp");
                if (nfcReceiverType != null)
                {
                    var nfcReceiver = FindFirstObjectByType(nfcReceiverType);
                    if (nfcReceiver != null)
                    {
                        var cardReaderField = nfcReceiverType.GetField("_cardReader",
                            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

                        if (cardReaderField != null)
                        {
                            var cardReader = cardReaderField.GetValue(nfcReceiver);
                            if (cardReader != null)
                            {
                                var cardReaderType = cardReader.GetType();

                                // Para o monitoramento
                                AddTestLog("Parando monitoramento...");
                                var stopWatchMethod = cardReaderType.GetMethod("StopWatch");
                                if (stopWatchMethod != null)
                                {
                                    stopWatchMethod.Invoke(cardReader, null);
                                    AddTestLog("<color=#FFFF00>StopWatch executado</color>");
                                }

                                // Aguarda um momento
                                System.Threading.Thread.Sleep(500);

                                // Reinicia o monitoramento
                                AddTestLog("Reiniciando...");
                                var startWatchMethod = cardReaderType.GetMethod("StartWatch");
                                if (startWatchMethod != null)
                                {
                                    startWatchMethod.Invoke(cardReader, null);
                                    AddTestLog("<color=#00FF00>✓ Leitor reiniciado!</color>");
                                    AddTestLog("<color=#00FFFF>Passe um cartão para testar</color>");
                                }
                                else
                                {
                                    AddTestLog("<color=#FF0000>✗ StartWatch não encontrado</color>");
                                }
                            }
                            else
                            {
                                AddTestLog("<color=#FF0000>✗ _cardReader é nulo</color>");
                            }
                        }
                        else
                        {
                            AddTestLog("<color=#FF0000>✗ Campo _cardReader não encontrado</color>");
                        }
                    }
                    else
                    {
                        AddTestLog("<color=#FF0000>✗ NFCReceiver não encontrado</color>");
                    }
                }
                else
                {
                    AddTestLog("<color=#FF0000>✗ Tipo NFCReceiver não encontrado</color>");
                }
            }
            catch (System.Exception e)
            {
                AddTestLog("<color=#FF0000>✗ ERRO: " + e.Message + "</color>");
            }
        }
        private void AddTestLog(string message)
        {
            testLogs.Add(message);
            if (testLogs.Count > MAX_LOGS)
            {
                testLogs.RemoveAt(0);
            }
            // Força mostrar o painel se não estiver visível
            if (!isVisible && debugCanvasGroup != null)
            {
                isVisible = true;
                debugCanvasGroup.alpha = 1f;
                debugCanvasGroup.interactable = true;
                debugCanvasGroup.blocksRaycasts = true;
            }
        }

        private void TestReaderEvents()
        {
            testLogs.Clear();
            AddTestLog("<color=#00FFFF>=== INICIANDO TESTE ===</color>");
            AddTestLog("Monitorando eventos por 10s...");

            try
            {
                var nfcReceiverType = System.Type.GetType("_4._NFC_Firjan.Scripts.NFC.NFCReceiver, Assembly-CSharp");
                if (nfcReceiverType != null)
                {
                    var nfcReceiver = FindFirstObjectByType(nfcReceiverType);
                    if (nfcReceiver != null)
                    {
                        // Reseta contadores
                        eventsDetected = 0;
                        isMonitoringEvents = true;
                        monitoringStartTime = Time.time;

                        // Conecta aos eventos do NFCReceiver
                        var onNFCConnectedField = nfcReceiverType.GetField("OnNFCConnected");
                        var onNFCDisconnectedField = nfcReceiverType.GetField("OnNFCDisconnected");
                        var onNFCReaderConnectedField = nfcReceiverType.GetField("OnNFCReaderConnected");
                        var onNFCReaderDisconnectedField = nfcReceiverType.GetField("OnNFCReaderDisconected");

                        if (onNFCConnectedField != null)
                        {
                            var evt = onNFCConnectedField.GetValue(nfcReceiver) as UnityEngine.Events.UnityEvent<string, string>;
                            if (evt != null)
                            {
                                evt.AddListener((id, reader) =>
                                {
                                    eventsDetected++;
                                    AddTestLog("<color=#00FF00>✓ Cartão Conectado!</color>");
                                    AddTestLog("<color=#FFFF00>  ID: " + id + "</color>");
                                });
                            }
                        }

                        if (onNFCDisconnectedField != null)
                        {
                            var evt = onNFCDisconnectedField.GetValue(nfcReceiver) as UnityEngine.Events.UnityEvent;
                            if (evt != null)
                            {
                                evt.AddListener(() =>
                                {
                                    eventsDetected++;
                                    AddTestLog("<color=#00FF00>✓ Cartão Desconectado</color>");
                                });
                            }
                        }

                        if (onNFCReaderConnectedField != null)
                        {
                            var evt = onNFCReaderConnectedField.GetValue(nfcReceiver) as UnityEngine.Events.UnityEvent<string>;
                            if (evt != null)
                            {
                                evt.AddListener((readerName) =>
                                {
                                    eventsDetected++;
                                    AddTestLog("<color=#00FF00>✓ Leitor Conectado!</color>");
                                    AddTestLog("<color=#FFFF00>  Nome: " + readerName + "</color>");
                                });
                            }
                        }

                        if (onNFCReaderDisconnectedField != null)
                        {
                            var evt = onNFCReaderDisconnectedField.GetValue(nfcReceiver) as UnityEngine.Events.UnityEvent;
                            if (evt != null)
                            {
                                evt.AddListener(() =>
                                {
                                    eventsDetected++;
                                    AddTestLog("<color=#FFA500>! Leitor Desconectado</color>");
                                });
                            }
                        }

                        AddTestLog("<color=#808080>Aguardando eventos...</color>");
                    }
                    else
                    {
                        AddTestLog("<color=#FF0000>✗ NFCReceiver não encontrado!</color>");
                    }
                }
            }
            catch (System.Exception e)
            {
                AddTestLog("<color=#FF0000>✗ ERRO: " + e.Message + "</color>");
                isMonitoringEvents = false;
            }
        }

        private void SimulateCompletePost()
        {
            StartCoroutine(SimulateCompletePostCoroutine());
        }

        private System.Collections.IEnumerator SimulateCompletePostCoroutine()
        {
            testLogs.Clear();
            AddTestLog("<color=#00FFFF>=== SIMULAR POST COMPLETO ===</color>");

            // Verifica se UnityMainThreadDispatcher existe
            if (!_4._NFC_Firjan.Scripts.UnityMainThreadDispatcher.Exists())
            {
                AddTestLog("<color=#FFA500>⚠ UnityMainThreadDispatcher não encontrado</color>");
                AddTestLog("<color=#808080>Continuando sem ele...</color>");
            }

            // Usa o card específico 38-E4-DB-3C
            string fakeNfcId = "38-E4-DB-3C";
            AddTestLog("<color=#FFFF00>NFC ID: " + fakeNfcId + "</color>");

            // Pega o ServerComunication
            var serverCommunication = FindFirstObjectByType<_4._NFC_Firjan.Scripts.Server.ServerComunication>();
            if (serverCommunication == null)
            {
                AddTestLog("<color=#FF0000>✗ ServerComunication não encontrado</color>");
                yield break;
            }

            AddTestLog("<color=#00FF00>✓ ServerComunication encontrado</color>");
            AddTestLog("<color=#808080>  IP: " + serverCommunication.Ip + "</color>");
            AddTestLog("<color=#808080>  Port: " + serverCommunication.Port + "</color>");

            // Cria GameModel com pontos aleatórios
            var gameData = new GameModel
            {
                nfcId = fakeNfcId,
                gameId = 5,
                skill1 = UnityEngine.Random.Range(50, 100), // Empathy
                skill2 = UnityEngine.Random.Range(50, 100), // ActiveListening
                skill3 = UnityEngine.Random.Range(50, 100)  // SelfAwareness
            };

            AddTestLog("<color=#FFFF00>Pontos gerados (aleatórios):</color>");
            AddTestLog("<color=#FFFF00>  Empathy: " + gameData.skill1 + "</color>");
            AddTestLog("<color=#FFFF00>  ActiveListening: " + gameData.skill2 + "</color>");
            AddTestLog("<color=#FFFF00>  SelfAwareness: " + gameData.skill3 + "</color>");

            AddTestLog("<color=#00FFFF>Enviando POST ao servidor...</color>");

            System.Threading.Tasks.Task<System.Net.HttpStatusCode> postTask = null;

            try
            {
                // Envia o POST usando o método do ServerComunication
                postTask = serverCommunication.UpdateNfcInfoFromGame(gameData);
                AddTestLog("<color=#00FFFF>Task iniciada, aguardando...</color>");
            }
            catch (System.Exception e)
            {
                AddTestLog("<color=#FF0000>✗ ERRO ao iniciar POST:</color>");
                AddTestLog("<color=#FF0000>" + e.Message + "</color>");
                yield break;
            }

            // Aguarda o Task com timeout de 10 segundos
            float timeout = 10f;
            float elapsed = 0f;

            while (!postTask.IsCompleted && elapsed < timeout)
            {
                elapsed += Time.deltaTime;

                // Mostra progresso a cada segundo
                if (Mathf.FloorToInt(elapsed) != Mathf.FloorToInt(elapsed - Time.deltaTime))
                {
                    AddTestLog("<color=#808080>Aguardando... " + Mathf.FloorToInt(elapsed) + "s</color>");
                }

                yield return null;
            }

            // Verifica se deu timeout
            if (!postTask.IsCompleted)
            {
                AddTestLog("<color=#FF0000>✗✗✗ TIMEOUT! ✗✗✗</color>");
                AddTestLog("<color=#FFA500>Servidor não respondeu em " + timeout + "s</color>");
                AddTestLog("<color=#808080>Verifique:</color>");
                AddTestLog("<color=#808080>1. Servidor está rodando?</color>");
                AddTestLog("<color=#808080>2. IP/Port corretos?</color>");
                AddTestLog("<color=#808080>3. Firewall bloqueando?</color>");
                yield break;
            }

            // Verifica se o Task teve erro
            if (postTask.IsFaulted)
            {
                AddTestLog("<color=#FF0000>✗ ERRO durante o POST:</color>");
                if (postTask.Exception != null)
                {
                    var innerException = postTask.Exception.InnerException ?? postTask.Exception;
                    AddTestLog("<color=#FF0000>" + innerException.GetType().Name + ": " + innerException.Message + "</color>");

                    if (innerException is System.Net.Http.HttpRequestException)
                    {
                        AddTestLog("<color=#FFA500>Problema de conexão com o servidor</color>");
                        AddTestLog("<color=#808080>IP: " + serverCommunication.Ip + ":" + serverCommunication.Port + "</color>");
                    }
                    else if (innerException is System.Threading.Tasks.TaskCanceledException)
                    {
                        AddTestLog("<color=#FFA500>Timeout do HttpClient (8s)</color>");
                    }
                }
                yield break;
            }

            AddTestLog("<color=#00FF00>✓ Resposta recebida!</color>");

            // Processa o resultado na main thread usando UnityMainThreadDispatcher
            if (_4._NFC_Firjan.Scripts.UnityMainThreadDispatcher.Exists())
            {
                _4._NFC_Firjan.Scripts.UnityMainThreadDispatcher.Instance().Enqueue(() =>
                {
                    ProcessPostResult(postTask, fakeNfcId);
                });
            }
            else
            {
                // Fallback: processa diretamente (já estamos na main thread via corrotina)
                ProcessPostResult(postTask, fakeNfcId);
            }
        }

        private void ProcessPostResult(System.Threading.Tasks.Task<System.Net.HttpStatusCode> postTask, string fakeNfcId)
        {
            // Verifica se o Task foi cancelado (timeout)
            if (postTask.IsCanceled)
            {
                AddTestLog("<color=#FFA500>⚠ Task cancelado (timeout do HttpClient - 8s)</color>");
                AddTestLog("<color=#808080>Servidor não respondeu a tempo</color>");
                return;
            }

            // Verifica se houve erro
            if (postTask.IsFaulted)
            {
                var innerEx = postTask.Exception?.InnerException ?? postTask.Exception;
                AddTestLog("<color=#FF0000>✗ ERRO: " + innerEx?.Message + "</color>");
                return;
            }

            // Verifica se completou com sucesso antes de acessar Result
            if (!postTask.IsCompletedSuccessfully)
            {
                AddTestLog("<color=#FF0000>✗ Task não completou com sucesso</color>");
                return;
            }

            var statusCode = postTask.Result;
            AddTestLog("Resposta recebida: " + statusCode);

            if (statusCode == System.Net.HttpStatusCode.OK)
            {
                AddTestLog("<color=#00FF00>✓✓✓ POST SUCCESS ✓✓✓</color>");

                // Atualiza o lastReadNfcId no NFCGameService
                if (NFCGameService.Instance != null)
                {
                    var nfcServiceType = NFCGameService.Instance.GetType();
                    var lastReadNfcIdField = nfcServiceType.GetField("lastReadNfcId",
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    if (lastReadNfcIdField != null)
                    {
                        lastReadNfcIdField.SetValue(NFCGameService.Instance, fakeNfcId);
                    }
                }

                // Chama OnPostSuccess no ScreenResult
                var screenResult = FindFirstObjectByType<ScreenResult>();
                if (screenResult != null)
                {
                    AddTestLog("<color=#00FFFF>Chamando OnPostSuccess...</color>");
                    screenResult.OnPostSuccess();
                    AddTestLog("<color=#00FF00>✓ Finalizado com sucesso!</color>");
                }
            }
            else
            {
                AddTestLog("<color=#FF0000>✗✗✗ POST FAILED ✗✗✗</color>");
                AddTestLog("<color=#FFA500>Status: " + statusCode + "</color>");
            }
        }

        private void StopMonitoringEvents()
        {
            if (!isMonitoringEvents) return;

            isMonitoringEvents = false;

            if (eventsDetected > 0)
            {
                AddTestLog("<color=#00FF00>=== SUCESSO! ===</color>");
                AddTestLog("<color=#00FF00>Hardware funcionando!</color>");
                AddTestLog("<color=#00FF00>Total: " + eventsDetected + " evento(s)</color>");
            }
            else
            {
                AddTestLog("<color=#FF0000>=== FALHA! ===</color>");
                AddTestLog("<color=#FF0000>Nenhum evento em 10s</color>");
                AddTestLog("<color=#FFA500>Causas possíveis:</color>");
                AddTestLog("<color=#808080>1. USB desconectado</color>");
                AddTestLog("<color=#808080>2. Drivers não instalados</color>");
                AddTestLog("<color=#808080>3. Leitor travado (tecla R)</color>");
            }
        }
    }
}
