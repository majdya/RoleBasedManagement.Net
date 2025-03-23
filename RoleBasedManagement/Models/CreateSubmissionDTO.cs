using System.ComponentModel.DataAnnotations;

namespace RoleBasedManagement.Models
{
    public class CreateSubmissionDTO
    {
        [Required(ErrorMessage = "Submission content is required")]
        public string Content { get; set; } = string.Empty;
    }
} 