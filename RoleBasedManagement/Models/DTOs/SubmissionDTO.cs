namespace RoleBasedManagement.Models.DTOs
{
    public class SubmissionDTO
    {
        public int Id { get; set; }
        public string Content { get; set; }
        public DateTime SubmissionDate { get; set; }
        public string Grade { get; set; }
        public AssignmentDTO Assignment { get; set; } // Nested assignment information
    }


}
