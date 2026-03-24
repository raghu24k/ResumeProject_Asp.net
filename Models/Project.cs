using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ResumeProject.Models
{
    public class Project
    {
        public int Id { get; set; }

        [ForeignKey("Resume")]
        public int ResumeId { get; set; }
        public Resume? Resume { get; set; }

        [MaxLength(200)]
        public string? Title { get; set; }
        [MaxLength(200)]
        public string? TechStack { get; set; }
        [MaxLength(500)]
        public string? ProjectUrl { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string? Description { get; set; }
    }
}