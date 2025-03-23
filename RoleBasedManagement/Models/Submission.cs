namespace RoleBasedManagement.Models
{
    public class Submission
    {
        public int Id { get; set; }
        public int AssignmentId { get; set; }
        public string StudentId { get; set; } = string.Empty;
        public DateTime SubmissionDate { get; set; }
        public string Content { get; set; } = string.Empty;
        public string Grade { get; set; } = string.Empty;
        public DateTime? GradedDate { get; set; }
        public string GradedBy { get; set; } = string.Empty;
        public string Comments { get; set; } = string.Empty;

        // Keep only the Assignment navigation property
        public Assignment Assignment { get; set; } = null!;
    }
}
