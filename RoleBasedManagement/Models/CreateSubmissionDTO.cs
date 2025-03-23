using System.ComponentModel.DataAnnotations;

namespace RoleBasedManagement.Models
{
    public class CreateSubmissionDTO
    {
        [Required(ErrorMessage = "Assignment ID is required")]
        public int AssignmentId { get; set; }

        [Required(ErrorMessage = "Submission content is required")]
        public string Content { get; set; } = string.Empty;
    }
} 