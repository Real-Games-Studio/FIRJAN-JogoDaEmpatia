using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Script para gerenciar a tela inicial (CTA - Call to Action) do Jogo da Empatia.
/// Herda de CanvasScreen e fornece funcionalidade para iniciar o jogo.
///
/// TIMER DE RELOAD AUTOMÁTICO (CTA):
/// - A cada 10 minutos (600 segundos), recarrega automaticamente a cena index 0
/// - Este timer é completamente independente do timer de inatividade global do ScreenCanvasController
/// - O timer só conta quando isInCta = true (tela CTA ativa)
/// - Quando isInCta = false, os outros timeouts do jogo funcionam normalmente
/// - Previne acúmulo de memória em sessões longas paradas na tela inicial
/// </summary>
public class ScreenCta : CanvasScreen
{
    [Header("CTA Screen Settings")]
    [SerializeField] private string gameScreenName = "gameplay";

    [Header("Auto Reload Settings")]
    [SerializeField] private float autoReloadInterval = 600f; // 10 minutos (600 segundos)

    private float ctaTimer = 0f;
    private bool isInCta = false;

    public override void OnEnable()
    {
        base.OnEnable();

        // Ativa o modo CTA e reseta o timer
        isInCta = true;
        ctaTimer = 0f;

        // Informa ao ScreenCanvasController que estamos no modo CTA
        ScreenCanvasController.isInCtaMode = true;

        Debug.Log("[ScreenCta] Modo CTA ativado - Timer de 10 minutos iniciado");
    }

    public override void OnDisable()
    {
        base.OnDisable();

        // Desativa o modo CTA (volta aos timeouts normais do jogo)
        isInCta = false;

        // Informa ao ScreenCanvasController que saímos do modo CTA
        ScreenCanvasController.isInCtaMode = false;

        Debug.Log("[ScreenCta] Modo CTA desativado - Timeouts normais do jogo retomados");
    }

    private void Update()
    {
        // Só incrementa o timer se estiver no modo CTA
        if (!isInCta)
            return;

        ctaTimer += Time.deltaTime;

        // A cada 10 minutos, recarrega a cena
        if (ctaTimer >= autoReloadInterval)
        {
            Debug.Log("[ScreenCta] 10 minutos atingidos! Recarregando cena index 0...");
            ReloadScene();
        }
    }

    /// <summary>
    /// Recarrega a cena index 0 para limpar memória e resetar o jogo.
    /// </summary>
    private void ReloadScene()
    {
        SceneManager.LoadScene(0);
    }

    /// <summary>
    /// Método público chamado pelo botão "Começar" na interface.
    /// Desativa a tela CTA atual e ativa a tela do jogo.
    /// </summary>
    public void StartGame()
    {
        // Desativa o modo CTA ANTES de mudar de tela
        isInCta = false;
        ScreenCanvasController.isInCtaMode = false;

        Debug.Log("[ScreenCta] StartGame - Modo CTA desativado manualmente antes de trocar tela");

        // Chama o método do ScreenManager para ativar a tela do jogo
        ScreenManager.SetCallScreen(gameScreenName);
    }
}