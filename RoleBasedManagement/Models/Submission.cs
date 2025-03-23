namespace RoleBasedManagement.Models
{
    public class Submission
    {
        public int Id { get; set; }
        public int AssignmentId { get; set; }
        public int StudentId { get; set; }
        public DateTime SubmissionDate { get; set; }
        public string FilePath { get; set; } = string.Empty;
        public string OriginalFileName { get; set; } = string.Empty;
        public string Grade { get; set; } = string.Empty;
        public DateTime? GradedDate { get; set; }
        public string GradedBy { get; set; } = string.Empty;
        public string Comments { get; set; } = string.Empty;

        // Navigation properties
        public Assignment Assignment { get; set; } = null!;
        public Student Student { get; set; } = null!;
    }
}
