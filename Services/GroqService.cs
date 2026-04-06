using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ResumeProject.Services
{
    public class GroqService
    {
        private readonly string _apiKey;
        private readonly string _model;
        private readonly HttpClient _httpClient;
        private const string GroqApiUrl = "https://api.groq.com/openai/v1/chat/completions";

        public GroqService(IConfiguration configuration)
        {
            _apiKey = configuration["Groq:ApiKey"] ?? throw new Exception("Groq:ApiKey not configured");
            _model = configuration["Groq:Model"] ?? "llama-3.3-70b-versatile";
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
            _httpClient.Timeout = TimeSpan.FromSeconds(60);
        }

        public async Task<ResumeAnalysisResult> AnalyzeResumeText(string resumeText)
        {
            var systemPrompt = @"You are an expert ATS (Applicant Tracking System) resume analyzer and career coach. 
Analyze the given resume text and return a JSON response with EXACTLY this structure (no markdown, no extra text, ONLY valid JSON):
{
  ""atsScore"": <number 0-100>,
  ""summary"": ""<2-3 sentence overall assessment>"",
  ""strengths"": [""<strength 1>"", ""<strength 2>"", ""<strength 3>"", ""<strength 4>"", ""<strength 5>""],
  ""improvements"": [""<improvement 1>"", ""<improvement 2>"", ""<improvement 3>"", ""<improvement 4>"", ""<improvement 5>""],
  ""keywordSuggestions"": [""<keyword 1>"", ""<keyword 2>"", ""<keyword 3>"", ""<keyword 4>"", ""<keyword 5>""],
  ""formattingTips"": [""<tip 1>"", ""<tip 2>"", ""<tip 3>""]
}

Scoring criteria:
- Contact info completeness (10 pts)
- Professional summary quality (15 pts)
- Work experience with action verbs and metrics (25 pts)
- Education section (10 pts)
- Skills relevance and variety (15 pts)
- Projects with descriptions (15 pts)
- Overall formatting and ATS-friendliness (10 pts)

Be specific, actionable, and honest. If sections are missing, mention that clearly.
IMPORTANT: Return ONLY the JSON object. No markdown formatting, no code blocks, no additional text.";

            var userPrompt = $"Analyze this resume for ATS compatibility:\n\n{resumeText}";

            var requestBody = new
            {
                model = _model,
                messages = new[]
                {
                    new { role = "system", content = systemPrompt },
                    new { role = "user", content = userPrompt }
                },
                temperature = 0.3,
                max_tokens = 1500
            };

            var jsonContent = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(GroqApiUrl, content);
            var responseBody = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception($"Groq API error: {response.StatusCode} - {responseBody}");
            }

            // Parse the Groq response
            using var doc = JsonDocument.Parse(responseBody);
            var messageContent = doc.RootElement
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString();

            if (string.IsNullOrWhiteSpace(messageContent))
                throw new Exception("Empty response from Groq API");

            // Clean the response - remove markdown code blocks if present
            messageContent = messageContent.Trim();
            if (messageContent.StartsWith("```json"))
                messageContent = messageContent.Substring(7);
            else if (messageContent.StartsWith("```"))
                messageContent = messageContent.Substring(3);
            if (messageContent.EndsWith("```"))
                messageContent = messageContent.Substring(0, messageContent.Length - 3);
            messageContent = messageContent.Trim();

            var result = JsonSerializer.Deserialize<ResumeAnalysisResult>(messageContent, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            return result ?? throw new Exception("Failed to parse AI response");
        }
    }

    public class ResumeAnalysisResult
    {
        [JsonPropertyName("atsScore")]
        public int AtsScore { get; set; }

        [JsonPropertyName("summary")]
        public string Summary { get; set; } = "";

        [JsonPropertyName("strengths")]
        public List<string> Strengths { get; set; } = new();

        [JsonPropertyName("improvements")]
        public List<string> Improvements { get; set; } = new();

        [JsonPropertyName("keywordSuggestions")]
        public List<string> KeywordSuggestions { get; set; } = new();

        [JsonPropertyName("formattingTips")]
        public List<string> FormattingTips { get; set; } = new();
    }
}
