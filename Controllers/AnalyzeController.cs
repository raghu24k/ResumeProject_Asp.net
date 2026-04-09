using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ResumeProject.Data;
using ResumeProject.Services;
using System.Security.Claims;
using System.Text;

namespace ResumeProject.Controllers
{
    [Authorize(AuthenticationSchemes = "CookieAuth")]
    public class AnalyzeController : Controller
    {
        private readonly GroqService _groqService;
        private readonly ApplicationDbContext _context;

        public AnalyzeController(GroqService groqService, ApplicationDbContext context)
        {
            _groqService = groqService;
            _context = context;
        }

        private int GetUserId() =>
            int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        // ─── POST: Analyze uploaded PDF text ──────────────────────────────────
        // The PDF text is extracted on the client side using pdf.js,
        // then sent here as plain text for Groq analysis.
        [HttpPost]
        public async Task<IActionResult> AnalyzePdfText([FromBody] AnalyzePdfRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request?.ResumeText))
                    return BadRequest(new { error = "No resume text provided." });

                // ✅ VALIDATE the PDF is actually a resume first
                var validation = await _groqService.ValidateResumeContent(request.ResumeText);
                
                if (!validation.IsValid)
                {
                    return Ok(new { success = false, validationFailed = true, validation });
                }

                // ✅ If validation passes, analyze the resume
                var result = await _groqService.AnalyzeResumeText(request.ResumeText);
                return Json(new { success = true, analysis = result, validation });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = $"Analysis failed: {ex.Message}" });
            }
        }

        // ─── POST: Analyze resume being built (by resume ID) ─────────────────
        [HttpPost]
        public async Task<IActionResult> AnalyzeResume([FromBody] AnalyzeResumeRequest request)
        {
            try
            {
                int userId = GetUserId();
                var resume = await _context.Resumes
                    .Include(r => r.WorkExperiences)
                    .Include(r => r.Educations)
                    .Include(r => r.Skills)
                    .Include(r => r.Projects)
                    .Include(r => r.Certifications)
                    .FirstOrDefaultAsync(r => r.Id == request.ResumeId && r.UserId == userId);

                if (resume == null)
                    return NotFound(new { error = "Resume not found." });

                // Build a text representation of the resume
                var sb = new StringBuilder();

                // Personal Details
                var name = $"{resume.FirstName} {resume.LastName}".Trim();
                if (!string.IsNullOrEmpty(name)) sb.AppendLine($"Name: {name}");
                if (!string.IsNullOrEmpty(resume.Email)) sb.AppendLine($"Email: {resume.Email}");
                if (!string.IsNullOrEmpty(resume.Phone)) sb.AppendLine($"Phone: {resume.Phone}");
                if (!string.IsNullOrEmpty(resume.Location)) sb.AppendLine($"Location: {resume.Location}");
                if (!string.IsNullOrEmpty(resume.LinkedIn)) sb.AppendLine($"LinkedIn: {resume.LinkedIn}");
                if (!string.IsNullOrEmpty(resume.Portfolio)) sb.AppendLine($"Portfolio: {resume.Portfolio}");
                sb.AppendLine();

                // Professional Summary
                if (!string.IsNullOrEmpty(resume.ProfessionalSummary))
                {
                    sb.AppendLine("PROFESSIONAL SUMMARY:");
                    sb.AppendLine(resume.ProfessionalSummary);
                    sb.AppendLine();
                }

                // Work Experience
                if (resume.WorkExperiences.Any())
                {
                    sb.AppendLine("WORK EXPERIENCE:");
                    foreach (var exp in resume.WorkExperiences.OrderByDescending(e => e.StartDate))
                    {
                        sb.AppendLine($"- {exp.JobTitle} at {exp.Company}");
                        if (!string.IsNullOrEmpty(exp.Location)) sb.Append($"  Location: {exp.Location}");
                        var dates = $"  {exp.StartDate?.ToString("MMM yyyy")} - {(exp.IsCurrent ? "Present" : exp.EndDate?.ToString("MMM yyyy"))}";
                        sb.AppendLine(dates);
                        if (!string.IsNullOrEmpty(exp.Description)) sb.AppendLine($"  {exp.Description}");
                    }
                    sb.AppendLine();
                }

                // Education
                if (resume.Educations.Any())
                {
                    sb.AppendLine("EDUCATION:");
                    foreach (var edu in resume.Educations.OrderByDescending(e => e.EndDate ?? e.StartDate))
                    {
                        sb.AppendLine($"- {edu.Degree} at {edu.Institution}");
                        if (!string.IsNullOrEmpty(edu.Location)) sb.AppendLine($"  Field: {edu.Location}");
                        var dates = $"  {edu.StartDate?.ToString("MMM yyyy")} - {edu.EndDate?.ToString("MMM yyyy")}";
                        sb.AppendLine(dates);
                        if (!string.IsNullOrEmpty(edu.GradeGPA)) sb.AppendLine($"  GPA: {edu.GradeGPA}");
                        if (!string.IsNullOrEmpty(edu.Description)) sb.AppendLine($"  {edu.Description}");
                    }
                    sb.AppendLine();
                }

                // Skills
                if (resume.Skills.Any())
                {
                    sb.AppendLine("SKILLS:");
                    foreach (var s in resume.Skills)
                    {
                        sb.AppendLine($"- {s.Name} ({s.Level})");
                    }
                    sb.AppendLine();
                }

                // Projects
                if (resume.Projects.Any())
                {
                    sb.AppendLine("PROJECTS:");
                    foreach (var p in resume.Projects.OrderByDescending(p => p.StartDate))
                    {
                        sb.AppendLine($"- {p.Title}");
                        if (!string.IsNullOrEmpty(p.TechStack)) sb.AppendLine($"  Tech: {p.TechStack}");
                        if (!string.IsNullOrEmpty(p.ProjectUrl)) sb.AppendLine($"  URL: {p.ProjectUrl}");
                        if (!string.IsNullOrEmpty(p.Description)) sb.AppendLine($"  {p.Description}");
                    }
                    sb.AppendLine();
                }

                // Certifications
                if (resume.Certifications.Any())
                {
                    sb.AppendLine("CERTIFICATIONS:");
                    foreach (var c in resume.Certifications)
                    {
                        sb.AppendLine($"- {c.CertificationName} by {c.IssuingOrganization} ({c.DateEarned})");
                        if (!string.IsNullOrEmpty(c.CredentialLink)) sb.AppendLine($"  Link: {c.CredentialLink}");
                    }
                }

                var resumeText = sb.ToString();

                if (resumeText.Trim().Length < 20)
                    return BadRequest(new { error = "Your resume is too empty to analyze. Please add more details first." });

                var result = await _groqService.AnalyzeResumeText(resumeText);
                return Json(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = $"Analysis failed: {ex.Message}" });
            }
        }
    }

    public class AnalyzePdfRequest
    {
        public string ResumeText { get; set; } = "";
    }

    public class AnalyzeResumeRequest
    {
        public int ResumeId { get; set; }
    }
}