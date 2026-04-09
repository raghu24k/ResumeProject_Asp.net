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

        // ─── VALIDATE if the PDF text is actually a resume ─────────────────────
        public async Task<ResumeValidationResult> ValidateResumeContent(string extractedText)
        {
            // Step 1: Basic content checks
            var basicValidation = PerformBasicValidation(extractedText);
            if (!basicValidation.IsValid)
                return basicValidation;

            // Step 2: AI-powered resume detection
            return await PerformAIValidation(extractedText);
        }

        private ResumeValidationResult PerformBasicValidation(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return new ResumeValidationResult
                {
                    IsValid = false,
                    Reason = "❌ The uploaded file appears to be empty or unreadable.",
                    Suggestions = new[] { "Make sure you've uploaded a valid PDF file", "Try re-scanning or converting your resume to PDF" }
                };

            var textLength = text.Length;
            var wordCount = text.Split(new[] { ' ', '\n', '\r', '\t' }, StringSplitOptions.RemoveEmptyEntries).Length;

            if (textLength < 200 || wordCount < 50)
                return new ResumeValidationResult
                {
                    IsValid = false,
                    Reason = "❌ The uploaded file is too short to be a valid resume.",
                    Suggestions = new[] { "Resumes typically contain at least 300+ words", "Make sure you've uploaded your complete resume" }
                };

            var lowerText = text.ToLower();

            // Check for common resume keywords
            string[] requiredKeywords = { "experience", "education", "skill", "work", "email", "phone", "linkedin", "project" };
            var foundKeywords = requiredKeywords.Count(k => lowerText.Contains(k));

            if (foundKeywords < 3)
                return new ResumeValidationResult
                {
                    IsValid = false,
                    Reason = "❌ This doesn't appear to be a resume - missing key resume sections.",
                    Suggestions = new[] { "Your resume should include: Work Experience, Education, and Skills sections", "Check that you've uploaded the correct PDF file" }
                };

            // Check for suspicious patterns (non-resume content)
            string[] suspiciousPatterns = { "table of contents", "chapter", "fiction", "novel", "story", "poem", "article", "invoice", "receipt", "contract" };
            var suspiciousCount = suspiciousPatterns.Count(p => lowerText.Contains(p));

            if (suspiciousCount > 2)
                return new ResumeValidationResult
                {
                    IsValid = false,
                    Reason = "❌ This appears to be a document other than a resume (possibly a novel, article, or contract).",
                    Suggestions = new[] { "Please upload your professional resume in PDF format", "Resumes contain your professional background, skills, and experience" }
                };

            return new ResumeValidationResult { IsValid = true, Reason = "Basic validation passed" };
        }

        private async Task<ResumeValidationResult> PerformAIValidation(string extractedText)
        {
            var systemPrompt = @"You are an expert resume validator with 20+ years of HR and ATS experience. 
Your job is to determine if the provided text is a REAL RESUME or NOT.

Return a JSON response with EXACTLY this structure (ONLY valid JSON, no markdown):
{
  ""isResume"": true|false,
  ""confidence"": 0-100,
  ""hasContactInfo"": true|false,
  ""hasExperience"": true|false,
  ""hasEducation"": true|false,
  ""hasSkills"": true|false,
  ""reason"": ""<clear explanation>"",
  ""suggestions"": [""<suggestion 1>"", ""<suggestion 2>""]
}

RULES for determining if it's a resume:
✓ VALID RESUME if it contains:
  - Contact information (name, email, phone, or LinkedIn)
  - At least ONE of: work experience, education, skills, or projects
  - Professional/career-related content
  
✗ NOT A RESUME if it contains:
  - Mostly marketing/sales copy
  - Fiction, stories, articles, or creative writing
  - Legal documents, contracts, or invoices
  - Instructional manuals or technical documentation
  - Books or textbooks
  - Academic papers (unless clearly structured as a resume)
  - Unrelated content like recipes, travel guides, etc.

Be strict but fair. When in doubt, explain your reasoning clearly.";

            var textToAnalyze = extractedText.Length > 2500 ? extractedText.Substring(0, 2500) : extractedText;
            var userPrompt = $"Is this text a resume? Analyze carefully:\n\n{textToAnalyze}";

            var requestBody = new
            {
                model = _model,
                messages = new[]
                {
                    new { role = "system", content = systemPrompt },
                    new { role = "user", content = userPrompt }
                },
                temperature = 0.2,  // Lower temperature for strict validation
                max_tokens = 600
            };

            var jsonContent = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(GroqApiUrl, content);
            var responseBody = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                throw new Exception($"Groq API error: {response.StatusCode} - {responseBody}");

            using var doc = JsonDocument.Parse(responseBody);
            var messageContent = doc.RootElement
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString();

            if (string.IsNullOrWhiteSpace(messageContent))
                throw new Exception("Empty response from resume validator");

            // Clean response
            messageContent = messageContent.Trim();
            if (messageContent.StartsWith("```json"))
                messageContent = messageContent.Substring(7);
            else if (messageContent.StartsWith("```"))
                messageContent = messageContent.Substring(3);
            if (messageContent.EndsWith("```"))
                messageContent = messageContent.Substring(0, messageContent.Length - 3);
            messageContent = messageContent.Trim();

            var validationData = JsonSerializer.Deserialize<ResumeValidationData>(messageContent, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (validationData == null)
                throw new Exception("Failed to parse validation response");

            if (!validationData.IsResume)
            {
                return new ResumeValidationResult
                {
                    IsValid = false,
                    Reason = $"❌ {validationData.Reason}",
                    Suggestions = validationData.Suggestions?.ToArray() ?? Array.Empty<string>(),
                    Confidence = validationData.Confidence
                };
            }

            // Check what sections are present
            if (!validationData.HasContactInfo || (!validationData.HasExperience && !validationData.HasEducation && !validationData.HasSkills))
            {
                return new ResumeValidationResult
                {
                    IsValid = false,
                    Reason = "❌ Your resume is missing essential sections.",
                    Suggestions = new[] { "Please include contact information (email, phone, or LinkedIn)", "Add at least one of: Work Experience, Education, or Skills" },
                    Confidence = validationData.Confidence
                };
            }

            var sections = new List<string?>
            {
                validationData.HasContactInfo ? "📧 Contact Info" : null,
                validationData.HasExperience ? "💼 Work Experience" : null,
                validationData.HasEducation ? "🎓 Education" : null,
                validationData.HasSkills ? "⚡ Skills" : null
            };

            return new ResumeValidationResult
            {
                IsValid = true,
                Reason = "✅ Valid resume detected",
                Confidence = validationData.Confidence,
                DetectedSections = sections.Where(s => s != null).Select(s => s!).ToList()
            };
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

    public class ResumeValidationResult
    {
        public bool IsValid { get; set; }
        public string Reason { get; set; } = "";
        public string[] Suggestions { get; set; } = Array.Empty<string>();
        public int Confidence { get; set; } = 100;
        public List<string> DetectedSections { get; set; } = new();
    }

    public class ResumeValidationData
    {
        [JsonPropertyName("isResume")]
        public bool IsResume { get; set; }

        [JsonPropertyName("confidence")]
        public int Confidence { get; set; }

        [JsonPropertyName("hasContactInfo")]
        public bool HasContactInfo { get; set; }

        [JsonPropertyName("hasExperience")]
        public bool HasExperience { get; set; }

        [JsonPropertyName("hasEducation")]
        public bool HasEducation { get; set; }

        [JsonPropertyName("hasSkills")]
        public bool HasSkills { get; set; }

        [JsonPropertyName("reason")]
        public string Reason { get; set; } = "";

        [JsonPropertyName("suggestions")]
        public List<string> Suggestions { get; set; } = new();
    }
}
