// File: Controllers/FaqBotController.cs
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Linq;
using System.ComponentModel.DataAnnotations;

namespace Doceity.Controllers
{
    // Modello per ricevere la domanda dal frontend via JSON
    public class FaqBotRequestModel
    {
        [Required(ErrorMessage = "La domanda è obbligatoria.")]
        [StringLength(500, ErrorMessage = "La domanda non può superare i 500 caratteri.")]
        public string Question { get; set; }
    }

    public class FaqBotController : Controller
    {
        private readonly IConfiguration _configuration;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<FaqBotController> _logger;

        // Contesto specifico per Doceity (mantenuto in italiano per ora)
        // Se vuoi supportare meglio le domande in altre lingue, potresti
        // fornire questo contesto anche in inglese o renderlo più neutro.
        private const string DoceityPlatformContext =
            "Doceity è una piattaforma web italiana per video consulenze e corsi live. " +
            "Gli utenti si registrano come 'Utente Standard' o 'Esperto'. " +
            "Gli esperti devono essere approvati dall'amministratore. " +
            "Gli utenti cercano esperti, richiedono consulenze private (proponendo data/ora o scegliendo dalla disponibilità) e si iscrivono a corsi live. " +
            "Gli esperti definiscono servizi, disponibilità, creano corsi, gestiscono richieste (accetta/rifiuta). " +
            "Include gestione utenti, calendario esperti, sessioni video. " +
            "L'obiettivo è facilitare l'incontro tra domanda e offerta di competenze specifiche tramite video interazioni.";

        public FaqBotController(IConfiguration configuration, IHttpClientFactory httpClientFactory, ILogger<FaqBotController> logger)
        {
            _configuration = configuration;
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AskGemini([FromBody] FaqBotRequestModel model)
        {
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Richiesta AskGemini non valida: {ModelStateErrors}", ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage)));
                return BadRequest(ModelState);
            }

            string apiKey = _configuration["Gemini:ApiKey"];
            if (string.IsNullOrEmpty(apiKey))
            {
                _logger.LogError("Chiave API Gemini (Gemini:ApiKey) non trovata nella configurazione.");
                return StatusCode(500, new { message = "Error configuring the server: cannot contact the assistant." }); // Messaggio errore in inglese
            }

            // --- MODIFICA CHIAVE: Prompt Multilingua ---
            // Istruzioni in inglese per maggiore robustezza con l'LLM
            string prompt = $"You are a friendly and helpful AI virtual assistant for the 'Doceity' web platform. " +
                            $"Your primary task is to answer user questions about using the Doceity platform, based on the provided context. " +
                            $"**CRITICAL INSTRUCTION: You MUST detect the language of the user's question below and ALWAYS respond in that SAME language.** " +
                            $"Be polite and professional. " +
                            $"Here is the context about the Doceity platform (use it only if the question is relevant, the context is in Italian): '{DoceityPlatformContext}' " +
                            $"--- Specific user question: \"{model.Question}\" --- " +
                            $"If the question is unclear, ask for clarification *in the original language of the question*. If the question is completely unrelated to Doceity or the provided context, gently explain, *in the original language of the question*, that you can only assist with Doceity-related inquiries. Do not invent features not described in the context.";
            // --- FINE MODIFICA PROMPT ---

            try
            {
                var modelNameToUse = "gemini-1.5-flash-latest";
                _logger.LogInformation("Using Gemini model: {ModelName}", modelNameToUse);

                var endpointUrl = $"https://generativelanguage.googleapis.com/v1beta/models/{modelNameToUse}:generateContent?key={apiKey}";

                var requestPayload = new
                {
                    contents = new[] { new { parts = new[] { new { text = prompt } } } },
                    generationConfig = new { temperature = 0.6 }, // Manteniamo un po' di coerenza
                    safetySettings = new[] {
                       new { category = "HARM_CATEGORY_HARASSMENT", threshold = "BLOCK_MEDIUM_AND_ABOVE" },
                       new { category = "HARM_CATEGORY_HATE_SPEECH", threshold = "BLOCK_MEDIUM_AND_ABOVE" },
                       new { category = "HARM_CATEGORY_SEXUALLY_EXPLICIT", threshold = "BLOCK_MEDIUM_AND_ABOVE" },
                       new { category = "HARM_CATEGORY_DANGEROUS_CONTENT", threshold = "BLOCK_MEDIUM_AND_ABOVE" }
                    }
                };

                var jsonPayload = JsonSerializer.Serialize(requestPayload, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
                var httpContent = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

                _logger.LogInformation("Sending request to Gemini API endpoint: {EndpointUrl}", $"https://generativelanguage.googleapis.com/v1beta/models/{modelNameToUse}:generateContent");
                _logger.LogDebug("Payload sent to Gemini: {Payload}", jsonPayload);

                var httpClient = _httpClientFactory.CreateClient("GeminiApiClient");
                var response = await httpClient.PostAsync(endpointUrl, httpContent);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Error from Gemini API. StatusCode: {StatusCode}. Response: {ErrorContent}", response.StatusCode, errorContent);
                    return StatusCode((int)response.StatusCode, new { message = "Error communicating with the AI assistant." }); // Messaggio errore in inglese
                }

                var responseContent = await response.Content.ReadAsStringAsync();
                _logger.LogInformation("Successfully received response from Gemini.");
                _logger.LogDebug("Gemini response content: {ResponseContent}", responseContent);

                // --- MODIFICA: Messaggio di Fallback Generico ---
                string botAnswer = "Sorry, I couldn't process a valid response. Please try rephrasing your question."; // Fallback in inglese
                // --- FINE MODIFICA FALLBACK ---

                try
                {
                    using var jsonDoc = JsonDocument.Parse(responseContent);
                    var candidate = jsonDoc.RootElement.TryGetProperty("candidates", out var candidates) && candidates.ValueKind == JsonValueKind.Array && candidates.GetArrayLength() > 0
                                    ? candidates[0] : (JsonElement?)null;

                    if (candidate.HasValue && candidate.Value.TryGetProperty("content", out var content) && content.ValueKind == JsonValueKind.Object)
                    {
                        if (content.TryGetProperty("parts", out var parts) && parts.ValueKind == JsonValueKind.Array && parts.GetArrayLength() > 0)
                        {
                            if (parts[0].TryGetProperty("text", out var textElement) && textElement.ValueKind == JsonValueKind.String)
                            {
                                botAnswer = textElement.GetString() ?? botAnswer;
                                _logger.LogInformation("Extracted answer from Gemini.");
                            }
                            else { _logger.LogWarning("Field 'text' not found or not a string in the first part of Gemini response."); }
                        }
                        else { _logger.LogWarning("Field 'parts' not found or not an array in Gemini response."); }
                    }
                    else
                    {
                        _logger.LogWarning("No valid candidate found or 'content' structure not compliant in Gemini response.");
                        if (jsonDoc.RootElement.TryGetProperty("promptFeedback", out var promptFeedback) &&
                            promptFeedback.TryGetProperty("blockReason", out var blockReason))
                        {
                            var reason = blockReason.GetString();
                            _logger.LogWarning("Gemini response was blocked. Reason: {BlockReason}", reason);
                            // Risposta neutra per il blocco
                            botAnswer = $"I cannot answer this question as it violates safety guidelines (Reason: {reason}). Please try rephrasing.";
                        }
                    }
                }
                catch (JsonException jsonEx)
                {
                    _logger.LogError(jsonEx, "Error parsing JSON response from Gemini. Content: {ResponseContent}", responseContent);
                    // Usa il messaggio di fallback definito sopra
                }

                return Ok(new { answer = botAnswer }); // La risposta sarà nella lingua rilevata da Gemini
            }
            catch (HttpRequestException httpEx)
            {
                _logger.LogError(httpEx, "Network error during Gemini API call.");
                return StatusCode(503, new { message = "Unable to reach the AI assistant at the moment. Please try again later." }); // Service Unavailable
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected generic error in AskGemini action.");
                return StatusCode(500, new { message = "An unexpected internal error occurred." });
            }
        }
    }
}