===========================================
  JOGO DA EMPATIA - GERENCIAMENTO DE DADOS
  Sistema de Pontuação Acumulativa
===========================================

📍 LOCALIZAÇÃO DOS ARQUIVOS
-------------------------------------------
Esta pasta contém os dados acumulados de TODOS os jogadores.

No Editor Unity:
  Assets/StreamingAssets/WordScores/

No Build (jogo compilado):
  [Pasta do Jogo]/FIRJAN_JogoDaEmpatia_Data/StreamingAssets/WordScores/

Exemplo Windows:
  C:\Program Files\MeuJogo\FIRJAN_JogoDaEmpatia_Data\StreamingAssets\WordScores\

Exemplo Mac:
  /Applications/MeuJogo.app/Contents/Resources/Data/StreamingAssets/WordScores/


📊 ARQUIVOS DE DADOS
-------------------------------------------
- round1_scores.json  → Pontuações da Rodada 1
- round2_scores.json  → Pontuações da Rodada 2
- round3_scores.json  → Pontuações da Rodada 3


🎮 COMO FUNCIONA DURANTE O EVENTO
-------------------------------------------
1. JOGADOR 1 joga e escolhe palavras
   → As pontuações aumentam nos JSONs

2. JOGADOR 2 joga logo depois
   → As pontuações se SOMAM às do Jogador 1

3. JOGADOR 3, 4, 5... jogam
   → Cada vez mais as palavras populares ficam maiores!

4. A nuvem de palavras mostra o que TODOS escolheram coletivamente


🔄 RESETAR DADOS PARA NOVO EVENTO
-------------------------------------------
OPÇÃO 1 - Deletar arquivos:
  1. Feche o jogo
  2. Delete os 3 arquivos JSON desta pasta
  3. Ao iniciar o jogo, novos arquivos serão criados com pontuação 1

OPÇÃO 2 - Editar manualmente:
  1. Abra qualquer arquivo JSON em um editor de texto
  2. Mude todos os "cumulativePoints" para 1
  3. Salve o arquivo

Exemplo de arquivo resetado:
{
  "words": [
    {
      "text": "Adaptação",
      "points": 1,
      "cumulativePoints": 1   ← Todos em 1 = reset
    },
    ...
  ]
}


💾 BACKUP DOS DADOS DO EVENTO
-------------------------------------------
Para guardar os resultados de um evento:
  1. Copie os 3 arquivos JSON
  2. Cole em outra pasta com nome do evento
     Exemplo: "Resultados_Evento_FIRJAN_15_Jan_2025"
  3. Agora pode resetar para o próximo evento!


📈 VISUALIZAR DADOS ACUMULADOS
-------------------------------------------
Abra qualquer arquivo JSON com:
- Notepad/Bloco de Notas
- Visual Studio Code
- Qualquer editor de texto

Você verá:
{
  "words": [
    {
      "text": "Empatia",
      "points": 1,
      "cumulativePoints": 47   ← 47 jogadores escolheram essa palavra!
    },
    {
      "text": "Negligente",
      "points": 1,
      "cumulativePoints": 3    ← Apenas 3 escolheram
    }
  ]
}


🎯 EXEMPLO DE USO EM EVENTO DE 3 DIAS
-------------------------------------------
DIA 1 (Segunda):
  - 50 pessoas jogam
  - Palavras acumulam de 1 até ~50

DIA 2 (Terça):
  - Mais 80 pessoas jogam
  - Palavras continuam acumulando de ~50 até ~130

DIA 3 (Quarta):
  - Mais 100 pessoas jogam
  - Total acumulado: ~230 jogadas!
  - A nuvem mostra padrões de TODOS os 230 participantes

Após o evento:
  - Faça backup dos JSONs
  - Resete para o próximo evento


⚙️ CONFIGURAÇÃO NO UNITY
-------------------------------------------
No GameObject "WordScorePersistence":
  ✅ Use StreamingAssets = TRUE  (para eventos)
  ❌ Use StreamingAssets = FALSE (salvaria em pasta escondida)


🐛 RESOLUÇÃO DE PROBLEMAS
-------------------------------------------
PROBLEMA: Nuvem de palavras vazia
SOLUÇÃO: Verifique se os 3 arquivos JSON existem nesta pasta

PROBLEMA: Dados não estão sendo salvos
SOLUÇÃO: Certifique-se que o jogo tem permissão de escrita nesta pasta

PROBLEMA: Quero começar do zero
SOLUÇÃO: Delete os 3 arquivos JSON e reinicie o jogo


📞 SUPORTE
-------------------------------------------
Em caso de dúvidas, consulte a documentação técnica
ou entre em contato com a equipe de desenvolvimento.


✨ DICA PROFISSIONAL
-------------------------------------------
Sempre faça backup dos JSONs após eventos importantes!
Você pode usar esses dados para análises posteriores
e entender melhor os padrões de empatia dos participantes.

===========================================
