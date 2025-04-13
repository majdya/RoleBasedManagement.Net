public class Submission
{
    public int Id { get; set; }
    public string StudentId { get; set; } = string.Empty; // Initialize with default value
    public int AssignmentId { get; set; }
    public string Content { get; set; } = string.Empty; // Initialize with default value
    public DateTime SubmissionDate { get; set; }
    public string? Grade { get; set; } // Nullable if not graded yet
    public DateTime? GradedDate { get; set; }
    public string GradedBy { get; set; } = string.Empty; // Initialize with default value
    public string Comments { get; set; } = string.Empty; // Initialize with default value

    public virtual Assignment? Assignment { get; set; } // Navigation property
}
