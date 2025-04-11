using RoleBasedManagement.Models;

public class Assignment
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty; // Initialize with default value
    public string Description { get; set; } = string.Empty; // Initialize with default value
    public DateTime DueDate { get; set; }
    public DateTime CreatedDate { get; set; }
    public string CreatedBy { get; set; } = string.Empty; // Initialize with default value
    public ICollection<Submission> Submissions { get; set; } = new List<Submission>(); // Initialize with empty collection
}
