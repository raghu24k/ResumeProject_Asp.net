using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ResumeProject.Data;
using ResumeProject.Models;
using System.Security.Claims;

namespace ResumeProject.Controllers
{
    [Authorize(AuthenticationSchemes = "CookieAuth")]
    public class ResumeController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ResumeController(ApplicationDbContext context)
        {
            _context = context;
        }

        private int GetUserId() =>
            int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        // DASHBOARD (My Resumes)
        public async Task<IActionResult> Dashboard()
        {
            int userId = GetUserId();
            var resumes = await _context.Resumes
                .Where(r => r.UserId == userId)
                .OrderByDescending(r => r.UpdatedAt)
                .ToListAsync();
            ViewBag.UserName = User.FindFirstValue(ClaimTypes.Name);
            return View(resumes);
        }

        // ANALYZE PDF (AI Scorer) 

        public IActionResult Analyze()
        {
            ViewBag.UserName = User.FindFirstValue(ClaimTypes.Name);
            return View();
        }

        // POST: Create new resume
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateResume()
        {
            int userId = GetUserId();
            var resume = new Resume
            {
                UserId = userId,
                Template = "Professional",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _context.Resumes.Add(resume);
            await _context.SaveChangesAsync();
            return RedirectToAction("PersonalDetails", new { id = resume.Id });
        }

        // POST: Delete resume
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteResume(int id)
        {
            int userId = GetUserId();
            var resume = await _context.Resumes.FirstOrDefaultAsync(r => r.Id == id && r.UserId == userId);
            if (resume != null)
            {
                _context.Resumes.Remove(resume);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Dashboard));
        }

        // Helper: Get resume with all includes, verify ownership
        private async Task<Resume?> GetResume(int id)
        {
            int userId = GetUserId();
            return await _context.Resumes
                .Include(r => r.WorkExperiences)
                .Include(r => r.Educations)
                .Include(r => r.Skills)
                .Include(r => r.Projects)
                .Include(r => r.Certifications)
                .FirstOrDefaultAsync(r => r.Id == id && r.UserId == userId);
        }

        // ─── PERSONAL DETAILS ────────────────────────────────────────────────

        public async Task<IActionResult> PersonalDetails(int? id)
        {
            int userId = GetUserId();
            Resume? resume;
            if (id.HasValue)
                resume = await GetResume(id.Value);
            else
                resume = await _context.Resumes
                    .Include(r => r.WorkExperiences)
                    .Include(r => r.Educations)
                    .Include(r => r.Skills)
                    .Include(r => r.Projects)
                    .FirstOrDefaultAsync(r => r.UserId == userId);

            if (resume == null)
            {
                resume = new Resume { UserId = userId, Template = "Professional" };
                _context.Resumes.Add(resume);
                await _context.SaveChangesAsync();
            }
            ViewBag.UserName = User.FindFirstValue(ClaimTypes.Name);
            return View(resume);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> PersonalDetails(Resume model)
        {
            int userId = GetUserId();
            var resume = await _context.Resumes.FirstOrDefaultAsync(r => r.Id == model.Id && r.UserId == userId);
            if (resume == null) return NotFound();

            resume.FirstName = model.FirstName;
            resume.LastName = model.LastName;
            resume.Email = model.Email;
            resume.Phone = model.Phone;
            resume.Location = model.Location;
            resume.LinkedIn = model.LinkedIn;
            resume.Portfolio = model.Portfolio;
            resume.ProfessionalSummary = model.ProfessionalSummary;
            resume.Template = model.Template;
            resume.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            TempData["Success"] = "Personal details saved!";
            return RedirectToAction(nameof(PersonalDetails), new { id = resume.Id });
        }

        // ─── TEMPLATE ────────────────────────────────────────────────────────

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> SetTemplate(int resumeId, string template)
        {
            int userId = GetUserId();
            var resume = await _context.Resumes.FirstOrDefaultAsync(r => r.Id == resumeId && r.UserId == userId);
            if (resume != null)
            {
                resume.Template = template;
                resume.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(PersonalDetails), new { id = resumeId });
        }

        // ─── DOWNLOAD RESUME (PDF) ────────────────────────────────────────────

        public async Task<IActionResult> DownloadView(int id)
        {
            var resume = await GetResume(id);
            if (resume == null) return RedirectToAction(nameof(Dashboard));
            return View("DownloadResume", resume);
        }

        // ─── EXPERIENCE ──────────────────────────────────────────────────────

        public async Task<IActionResult> Experience(int? id)
        {
            int userId = GetUserId();
            Resume? resume;
            if (id.HasValue)
                resume = await GetResume(id.Value);
            else
                resume = await _context.Resumes
                    .Include(r => r.WorkExperiences)
                    .Include(r => r.Educations)
                    .Include(r => r.Skills)
                    .Include(r => r.Projects)
                    .FirstOrDefaultAsync(r => r.UserId == userId);

            if (resume == null) return RedirectToAction(nameof(Dashboard));
            ViewBag.UserName = User.FindFirstValue(ClaimTypes.Name);
            return View(resume);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> AddExperience(WorkExperience model, int resumeId)
        {
            int userId = GetUserId();
            var resume = await _context.Resumes.FirstOrDefaultAsync(r => r.Id == resumeId && r.UserId == userId);
            if (resume == null) return NotFound();
            // Skip if no meaningful data was entered
            if (string.IsNullOrWhiteSpace(model.Company) && string.IsNullOrWhiteSpace(model.JobTitle))
                return RedirectToAction(nameof(Experience), new { id = resumeId });
            model.ResumeId = resume.Id;
            _context.WorkExperiences.Add(model);
            resume.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            TempData["Success"] = "Experience saved!";
            return RedirectToAction(nameof(Experience), new { id = resumeId });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteExperience(int id, int resumeId)
        {
            int userId = GetUserId();
            var exp = await _context.WorkExperiences
                .Include(w => w.Resume)
                .FirstOrDefaultAsync(w => w.Id == id && w.Resume!.UserId == userId);
            if (exp != null) { _context.WorkExperiences.Remove(exp); await _context.SaveChangesAsync(); }
            return RedirectToAction(nameof(Experience), new { id = resumeId });
        }

        // ─── EDUCATION ───────────────────────────────────────────────────────

        public async Task<IActionResult> Education(int? id)
        {
            int userId = GetUserId();
            Resume? resume;
            if (id.HasValue)
                resume = await GetResume(id.Value);
            else
                resume = await _context.Resumes
                    .Include(r => r.WorkExperiences)
                    .Include(r => r.Educations)
                    .Include(r => r.Skills)
                    .Include(r => r.Projects)
                    .FirstOrDefaultAsync(r => r.UserId == userId);

            if (resume == null) return RedirectToAction(nameof(Dashboard));
            ViewBag.UserName = User.FindFirstValue(ClaimTypes.Name);
            return View(resume);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> AddEducation(Education model, int resumeId)
        {
            int userId = GetUserId();
            var resume = await _context.Resumes.FirstOrDefaultAsync(r => r.Id == resumeId && r.UserId == userId);
            if (resume == null) return NotFound();
            // Skip if no meaningful data was entered
            if (string.IsNullOrWhiteSpace(model.Institution) && string.IsNullOrWhiteSpace(model.Degree))
                return RedirectToAction(nameof(Education), new { id = resumeId });
            model.ResumeId = resume.Id;
            _context.Educations.Add(model);
            resume.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            TempData["Success"] = "Education saved!";
            return RedirectToAction(nameof(Education), new { id = resumeId });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteEducation(int id, int resumeId)
        {
            int userId = GetUserId();
            var edu = await _context.Educations
                .Include(e => e.Resume)
                .FirstOrDefaultAsync(e => e.Id == id && e.Resume!.UserId == userId);
            if (edu != null) { _context.Educations.Remove(edu); await _context.SaveChangesAsync(); }
            return RedirectToAction(nameof(Education), new { id = resumeId });
        }

        // ─── SKILLS ──────────────────────────────────────────────────────────

        public async Task<IActionResult> Skills(int? id)
        {
            int userId = GetUserId();
            Resume? resume;
            if (id.HasValue)
                resume = await GetResume(id.Value);
            else
                resume = await _context.Resumes
                    .Include(r => r.WorkExperiences)
                    .Include(r => r.Educations)
                    .Include(r => r.Skills)
                    .Include(r => r.Projects)
                    .FirstOrDefaultAsync(r => r.UserId == userId);

            if (resume == null) return RedirectToAction(nameof(Dashboard));
            ViewBag.UserName = User.FindFirstValue(ClaimTypes.Name);
            return View(resume);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> AddSkill(Skill model, int resumeId)
        {
            int userId = GetUserId();
            var resume = await _context.Resumes.FirstOrDefaultAsync(r => r.Id == resumeId && r.UserId == userId);
            if (resume == null) return NotFound();
            // Skip if no meaningful data was entered
            if (string.IsNullOrWhiteSpace(model.Name))
                return RedirectToAction(nameof(Skills), new { id = resumeId });
            model.ResumeId = resume.Id;
            _context.Skills.Add(model);
            resume.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            TempData["Success"] = "Skill saved!";
            return RedirectToAction(nameof(Skills), new { id = resumeId });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteSkill(int id, int resumeId)
        {
            int userId = GetUserId();
            var skill = await _context.Skills
                .Include(s => s.Resume)
                .FirstOrDefaultAsync(s => s.Id == id && s.Resume!.UserId == userId);
            if (skill != null) { _context.Skills.Remove(skill); await _context.SaveChangesAsync(); }
            return RedirectToAction(nameof(Skills), new { id = resumeId });
        }

        // ─── PROJECTS ────────────────────────────────────────────────────────

        public async Task<IActionResult> Projects(int? id)
        {
            int userId = GetUserId();
            Resume? resume;
            if (id.HasValue)
                resume = await GetResume(id.Value);
            else
                resume = await _context.Resumes
                    .Include(r => r.WorkExperiences)
                    .Include(r => r.Educations)
                    .Include(r => r.Skills)
                    .Include(r => r.Projects)
                    .FirstOrDefaultAsync(r => r.UserId == userId);

            if (resume == null) return RedirectToAction(nameof(Dashboard));
            ViewBag.UserName = User.FindFirstValue(ClaimTypes.Name);
            return View(resume);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> AddProject(Project model, int resumeId)
        {
            int userId = GetUserId();
            var resume = await _context.Resumes.FirstOrDefaultAsync(r => r.Id == resumeId && r.UserId == userId);
            if (resume == null) return NotFound();
            // Skip if no meaningful data was entered
            if (string.IsNullOrWhiteSpace(model.Title))
                return RedirectToAction(nameof(Projects), new { id = resumeId });
            model.ResumeId = resume.Id;
            _context.Projects.Add(model);
            resume.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            TempData["Success"] = "Project saved!";
            return RedirectToAction(nameof(Projects), new { id = resumeId });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteProject(int id, int resumeId)
        {
            int userId = GetUserId();
            var proj = await _context.Projects
                .Include(p => p.Resume)
                .FirstOrDefaultAsync(p => p.Id == id && p.Resume!.UserId == userId);
            if (proj != null) { _context.Projects.Remove(proj); await _context.SaveChangesAsync(); }
            return RedirectToAction(nameof(Projects), new { id = resumeId });
        }

        // ─── CERTIFICATIONS ──────────────────────────────────────────────────

        public async Task<IActionResult> Certifications(int? id)
        {
            int userId = GetUserId();
            Resume? resume;
            if (id.HasValue)
                resume = await GetResume(id.Value);
            else
                resume = await _context.Resumes
                    .Include(r => r.WorkExperiences)
                    .Include(r => r.Educations)
                    .Include(r => r.Skills)
                    .Include(r => r.Projects)
                    .Include(r => r.Certifications)
                    .FirstOrDefaultAsync(r => r.UserId == userId);

            if (resume == null) return RedirectToAction(nameof(Dashboard));
            ViewBag.UserName = User.FindFirstValue(ClaimTypes.Name);
            return View(resume);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> AddCertification(Certification model, int resumeId)
        {
            int userId = GetUserId();
            var resume = await _context.Resumes.FirstOrDefaultAsync(r => r.Id == resumeId && r.UserId == userId);
            if (resume == null) return NotFound();
            // Skip if no meaningful data was entered
            if (string.IsNullOrWhiteSpace(model.CertificationName))
                return RedirectToAction(nameof(Certifications), new { id = resumeId });
            model.ResumeId = resume.Id;
            _context.Certifications.Add(model);
            resume.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            TempData["Success"] = "Certification saved!";
            return RedirectToAction(nameof(Certifications), new { id = resumeId });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteCertification(int id, int resumeId)
        {
            int userId = GetUserId();
            var cert = await _context.Certifications
                .Include(c => c.Resume)
                .FirstOrDefaultAsync(c => c.Id == id && c.Resume!.UserId == userId);
            if (cert != null) { _context.Certifications.Remove(cert); await _context.SaveChangesAsync(); }
            return RedirectToAction(nameof(Certifications), new { id = resumeId });
        }
    }
}