public class Submission
{
    public int Id { get; set; }
    public int AssignmentId { get; set; }
    public string StudentId { get; set; } = string.Empty; // Initialize with default value
    public DateTime SubmissionDate { get; set; }
    public string Content { get; set; } = string.Empty; // Initialize with default value
    public string Grade { get; set; } = string.Empty; // Initialize with default value
    public DateTime? GradedDate { get; set; }
    public string GradedBy { get; set; } = string.Empty; // Initialize with default value
    public string Comments { get; set; } = string.Empty; // Initialize with default value
    public Assignment Assignment { get; set; } = new Assignment(); // Initialize with default value
}
