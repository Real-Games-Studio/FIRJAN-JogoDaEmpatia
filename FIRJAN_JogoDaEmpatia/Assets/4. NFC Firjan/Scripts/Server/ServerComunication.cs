using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;

namespace _4._NFC_Firjan.Scripts.Server
{
	public class ServerComunication : MonoBehaviour
	{
		/// <summary>
		/// Precisa ser colocado para receber do Json em cada aplicação.
		/// </summary>
		public string Ip;

		/// <summary>
		/// Precisa ser colocado para receber do Json em cada aplicação.
		/// </summary>
		public int Port;

		private HttpClient _client;

		private void Awake()
		{
			_client = new HttpClient();
		}

		private string GetFullEndGameUrl(string nfcId)
		{
			return $"http://{Ip}:{Port}/users/{nfcId}/endgame";
		}
		
		private string GetFullNfcUrl(string nfcId)
		{
			return $"http://{Ip}:{Port}/users/{nfcId}";
		}

		/// <summary>
		/// Informação deve ser enviada após o jogo para atualizar a pontuação do jogador, aconselho colocar o id do jogo por Json
		/// </summary>
		/// <param name=">gameInfo"><see cref="GameModel"/> Pontuação de cada jogo funciona diferente, olhar no <see href="https://docs.google.com/document/d/14COKL4PcHkT3_J_TiCc79gAZNwbT6pKFmMdV3G9mY0Q/edit?usp=sharing">documento</see></param>
		/// <returns>Codigo de status do update ao server <see cref="HttpStatusCode"/></returns>
		public async Task<HttpStatusCode> UpdateNfcInfoFromGame(GameModel gameInfo)
		{
			var url = GetFullNfcUrl(gameInfo.nfcId);
			var request = new HttpRequestMessage(HttpMethod.Post, url);
			var content = new StringContent(gameInfo.ToString());
			request.Content = content;
			Debug.Log($"Sending to {url}:{request.Content?.ReadAsStringAsync().Result}");
			var response = _client.SendAsync(request).Result;
			return response.StatusCode;
		}

		/// <summary>
		/// Usado para pegar as informações atuais do nfc
		/// </summary>
		/// <param name="nfcId">Nome enviado pelo <see cref="NFC.NFCReceiver"/></param>
		/// <returns><see cref="EndGameResponseModel"></see></returns>
		public async Task<EndGameResponseModel> GetNfcInfo(string nfcId)
		{
			var url = GetFullNfcUrl(nfcId);
			var request = new HttpRequestMessage(HttpMethod.Get, url);
			Debug.Log($"Sending to {url}");
			var response = _client.SendAsync(request).Result;
			Debug.Log($"Response code is {response.StatusCode}");
			if (response.StatusCode == HttpStatusCode.OK)
			{
				return JsonConvert.DeserializeObject<EndGameResponseModel>(await response.Content.ReadAsStringAsync());
			}
			else
			{
				return null;
			}
		}

		/// <summary>
		/// Usado para enviar o nome do usuario para o Banco de dados e avisar que já passou pela última experiência
		/// </summary>
		/// <param name="endGameRequestModel"></param>
		/// <param name="nfcId"></param>
		/// <returns></returns>
		public async Task<EndGameResponseModel> PostNameForEndGameInfo(EndGameRequestModel endGameRequestModel,
			string nfcId)
		{
			var url = GetFullEndGameUrl(nfcId);
			var request = new HttpRequestMessage(HttpMethod.Post, url);
			var content = new StringContent(endGameRequestModel.ToString());
			request.Content = content;

			Debug.Log($"Sending to {url}:{request.Content?.ReadAsStringAsync().Result}");
			var response = _client.SendAsync(request).Result;
			Debug.Log($"Response code is {response.StatusCode}");
			if (response.StatusCode == HttpStatusCode.OK)
			{
				return Newtonsoft.Json.JsonConvert.DeserializeObject<EndGameResponseModel>(response.Content.ReadAsStringAsync().Result);
			}
			else
			{
				Debug.Log($"Response code is {response.StatusCode}");
				return null;
			}

		}

	}
}