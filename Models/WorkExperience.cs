using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ResumeProject.Models
{
    public class WorkExperience
    {
        public int Id { get; set; }

        [ForeignKey("Resume")]
        public int ResumeId { get; set; }
        public Resume? Resume { get; set; }

        [MaxLength(200)]
        public string? JobTitle { get; set; }
        [MaxLength(200)]
        public string? Company { get; set; }
        [MaxLength(200)]
        public string? Location { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public bool IsCurrent { get; set; }
        public string? Description { get; set; }
    }
}