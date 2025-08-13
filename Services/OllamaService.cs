using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace OllamaLocalHostIntergration.Services
{
    public class OllamaService
    {
        private readonly HttpClient _httpClient;
        private string _serverAddress;
        private string _model = "codellama";

        public OllamaService(string serverAddress = "http://localhost:11434")
        {
            _serverAddress = serverAddress;
            _httpClient = new HttpClient();
        }

        public void UpdateServerAddress(string address)
        {
            _serverAddress = address;
        }

        public void SetModel(string model)
        {
            _model = model;
        }

        public async Task<List<string>> GetAvailableModelsAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync($"{_serverAddress}/api/tags");
                response.EnsureSuccessStatusCode();
                var jsonResponse = await response.Content.ReadAsStringAsync();
                var tagsResponse = JsonConvert.DeserializeObject<OllamaTagsResponse>(jsonResponse);
                var models = new List<string>();
                if (tagsResponse?.models != null)
                {
                    foreach (var model in tagsResponse.models)
                    {
                        if (!string.IsNullOrEmpty(model.name))
                            models.Add(model.name);
                    }
                }
                return models;
            }
            catch
            {
                return new List<string>();
            }
        }

        public async Task<string> GenerateResponse(string prompt, string context = "")
        {
            try
            {
                var requestBody = new
                {
                    model = _model,
                    prompt = $"Context:\n{context}\n\nQuestion:\n{prompt}",
                    stream = false
                };

                var content = new StringContent(
                    JsonConvert.SerializeObject(requestBody),
                    Encoding.UTF8,
                    "application/json");

                var response = await _httpClient.PostAsync($"{_serverAddress}/api/generate", content);
                response.EnsureSuccessStatusCode();

                var jsonResponse = await response.Content.ReadAsStringAsync();
                var responseObj = JsonConvert.DeserializeObject<OllamaResponse>(jsonResponse);

                return responseObj?.Response ?? "No response generated";
            }
            catch (Exception ex)
            {
                return $"Error: {ex.Message}";
            }
        }
    }

    public class OllamaResponse
    {
        public string Response { get; set; }
    }

    public class OllamaTagsResponse
    {
        public List<OllamaModelTag> models { get; set; }
    }

    public class OllamaModelTag
    {
        public string name { get; set; }
    }
}