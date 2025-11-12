using System;

namespace FIRJAN.Utilities
{
    [Serializable]
    public class LanguageData
    {
        public CommonData common;
        public CTAData cta;
        public SituationData situation1;
        public SituationData situation2;
        public SituationData situation3;
        public SituationResultsData situation_results;
        public GameOverData game_over;
        public HeaderData header;
    }

    [Serializable]
    public class CommonData
    {
        public string situationTitlePT;
        public string situationTitleEN;
        public string skipButtonPT;
        public string skipButtonEN;
    }

    [Serializable]
    public class CTAData
    {
        public string titulo1PT;
        public string titulo1EN;
        public string descricao1PT;
        public string descricao1EN;
        public string botao1PT;
        public string botao1EN;
    }

    [Serializable]
    public class SituationData
    {
        public string titulo1PT;
        public string titulo1EN;
        public string subtitulo1PT;
        public string subtitulo1EN;
        public string falaPersonagemPT;
        public string falaPersonagemEN;
        public string falaPersonagem1PT;
        public string falaPersonagem1EN;
        public string descricao1PT;
        public string descricao1EN;
        public string opcao1PT;
        public string opcao1EN;
        public string opcao2PT;
        public string opcao2EN;
        public string opcao3PT;
        public string opcao3EN;
        public string opcao4PT;
        public string opcao4EN;
        public string opcao5PT;
        public string opcao5EN;
        public string opcao6PT;
        public string opcao6EN;
        public string opcao7PT;
        public string opcao7EN;
        public string opcao8PT;
        public string opcao8EN;
        public string botao1PT;
        public string botao1EN;
    }

    [Serializable]
    public class SituationResultsData
    {
        public string titulo1PT;
        public string titulo1EN;
        public string texto1PT;
        public string texto1EN;
        public string palavra1PT;
        public string palavra1EN;
        public string palavra2PT;
        public string palavra2EN;
        public string palavra3PT;
        public string palavra3EN;
        public string palavra4PT;
        public string palavra4EN;
        public string palavra5PT;
        public string palavra5EN;
        public string palavra6PT;
        public string palavra6EN;
        public string palavra7PT;
        public string palavra7EN;
        public string palavra8PT;
        public string palavra8EN;
        public string botao1PT;
        public string botao1EN;
        public string botao2PT;
        public string botao2EN;
    }

    [Serializable]
    public class GameOverData
    {
        public string titulo1PT;
        public string titulo1EN;
        public string titulo2PT;
        public string titulo2EN;
        public string texto1PT;
        public string texto1EN;
        public string texto2PT;
        public string texto2EN;
        public string texto3PT;
        public string texto3EN;
        public string texto4PT;
        public string texto4EN;
        public string botaoFinalizarPT;
        public string botaoFinalizarEN;
        public string indicador1PT;
        public string indicador1EN;
        public string indicador2PT;
        public string indicador2EN;
        public string indicador3PT;
        public string indicador3EN;
    }

    [Serializable]
    public class HeaderData
    {
        public string titulo1PT;
        public string titulo1EN;
        public string closeTituloPT;
        public string closeTituloEN;
        public string closeYesPT;
        public string closeYesEN;
        public string closeNoPT;
        public string closeNoEN;
    }
}
