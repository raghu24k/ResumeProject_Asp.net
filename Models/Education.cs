using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ResumeProject.Models
{
    public class Education
    {
        public int Id { get; set; }

        [ForeignKey("Resume")]
        public int ResumeId { get; set; }
        public Resume? Resume { get; set; }

        [MaxLength(200)]
        public string? Degree { get; set; }
        [MaxLength(200)]
        public string? Institution { get; set; }
        [MaxLength(200)]
        public string? Location { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        [MaxLength(10)]
        public string? GradeGPA { get; set; }
        public string? Description { get; set; }
    }
}