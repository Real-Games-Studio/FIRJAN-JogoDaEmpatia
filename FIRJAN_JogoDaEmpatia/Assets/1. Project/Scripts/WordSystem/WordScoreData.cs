using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Estrutura de dados para uma palavra individual com pontuação.
/// </summary>
[Serializable]
public class WordData
{
    [Tooltip("Texto da palavra")]
    public string text;

    [Tooltip("Pontos da sessão atual (reseta a cada rodada)")]
    public int points;

    [Tooltip("Pontos acumulados de todas as sessões (persistente)")]
    public int cumulativePoints;

    public WordData()
    {
        text = "";
        points = 1;
        cumulativePoints = 1;
    }

    public WordData(string wordText)
    {
        text = wordText;
        points = 1;
        cumulativePoints = 1;
    }

    public WordData(string wordText, int sessionPoints, int cumulative)
    {
        text = wordText;
        points = sessionPoints;
        cumulativePoints = cumulative;
    }
}

/// <summary>
/// Estrutura de dados para todas as palavras de uma rodada específica.
/// </summary>
[Serializable]
public class RoundWordData
{
    [Tooltip("Número da rodada (1, 2 ou 3)")]
    public int roundNumber;

    [Tooltip("Lista de palavras desta rodada com suas pontuações")]
    public List<WordData> words;

    public RoundWordData()
    {
        roundNumber = 1;
        words = new List<WordData>();
    }

    public RoundWordData(int round)
    {
        roundNumber = round;
        words = new List<WordData>();
    }
}

/// <summary>
/// Estrutura de dados para o jogo completo (todas as 3 rodadas).
/// </summary>
[Serializable]
public class GameWordScores
{
    [Tooltip("Lista de dados de todas as rodadas")]
    public List<RoundWordData> rounds;

    public GameWordScores()
    {
        rounds = new List<RoundWordData>();
    }
}

/// <summary>
/// Estrutura simplificada para salvar/carregar JSON (compatível com o formato fornecido).
/// </summary>
[Serializable]
public class RoundScoreData
{
    public List<WordData> words;

    public RoundScoreData()
    {
        words = new List<WordData>();
    }
}
