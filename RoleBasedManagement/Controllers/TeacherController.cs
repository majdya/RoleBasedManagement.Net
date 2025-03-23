using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RoleBasedManagement.Data;
using RoleBasedManagement.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace RoleBasedManagement.Controllers
{
    [Authorize(Roles ="teacher")]
    [Route("api/[controller]")]
    [ApiController]
    public class TeacherController : ControllerBase
    {
        private readonly AppDBContext _context;

        public TeacherController(AppDBContext context)
        {
            _context = context;
        }

        // Get all assignments created by the teacher
        [HttpGet("my-assignments")]
        public async Task<IActionResult> GetMyAssignments([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            var createdByClaim = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier);
            if(createdByClaim == null)
            {
                return Unauthorized(new { message = "User information is missing in the token" });
            }

            var assignments = await _context.Assignments
                .Where(a => a.CreatedBy == createdByClaim.Value)
                .OrderByDescending(a => a.CreatedDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var total = await _context.Assignments
                .Where(a => a.CreatedBy == createdByClaim.Value)
                .CountAsync();

            return Ok(new { 
                assignments,
                total,
                page,
                pageSize,
                totalPages = (int)Math.Ceiling(total / (double)pageSize)
            });
        }

        [HttpPost("assignments")]
        public async Task<IActionResult> CreateAssignment([FromBody] CreateAssignmentDTO? assignmentDTO)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new { 
                    message = "Validation failed", 
                    errors = ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage)
                });
            }

            if(assignmentDTO == null)
            {
                return BadRequest(new { message = "Assignment data is required" });
            }

            var createdByClaim = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier);
            if(createdByClaim == null)
            {
                return Unauthorized(new { message = "User information is missing in the token" });
            }

            var assignment = new Assignment
            {
                Title = assignmentDTO.Title,
                Description = assignmentDTO.Description,
                DueDate = assignmentDTO.DueDate,
                CreatedDate = DateTime.UtcNow,
                CreatedBy = createdByClaim.Value
            };

            _context.Assignments.Add(assignment);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Assignment created", assignment });
        }

        // Edit Assignments
        [HttpPut("assignments/{id}")]
        public async Task<IActionResult> EditAssignment(int id, [FromBody] CreateAssignmentDTO updatedAssignmentDTO)
        {
            if(updatedAssignmentDTO == null)
            {
                return BadRequest(new { message = "Invalid assignment data" });
            }

            var assignment = await _context.Assignments.FindAsync(id);
            if(assignment == null)
            {
                return NotFound(new { message = "Assignment not found" });
            }

            var createdByClaim = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier);
            if(createdByClaim == null || assignment.CreatedBy != createdByClaim.Value)
            {
                return Unauthorized(new { message = "You are not authorized to edit this assignment" });
            }

            // Only update the allowed fields
            assignment.Title = updatedAssignmentDTO.Title;
            assignment.Description = updatedAssignmentDTO.Description;
            assignment.DueDate = updatedAssignmentDTO.DueDate;
            // CreatedDate and CreatedBy remain unchanged

            _context.Assignments.Update(assignment);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Assignment updated", assignment });
        }

        // Delete Assignments
        [HttpDelete("assignments/{id}")]
        public async Task<IActionResult> DeleteAssignment(int id)
        {
            var assignment = await _context.Assignments.FindAsync(id);
            if(assignment == null)
            {
                return NotFound(new { message = "Assignment not found" });
            }

            var createdByClaim = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier);
            if(createdByClaim == null || assignment.CreatedBy != createdByClaim.Value)
            {
                return Unauthorized(new { message = "You are not authorized to delete this assignment" });
            }

            _context.Assignments.Remove(assignment);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Assignment deleted" });
        }

        // Grade Assignments
        [HttpPut("submissions/{id}/grade")]
        public async Task<IActionResult> GradeAssignment(int id, [FromBody] GradeSubmissionRequest request)
        {
            if(request == null || string.IsNullOrEmpty(request.Grade) || request.Grade.Length > 3)
            {
                return BadRequest(new { message = "Invalid grade format" });
            }

            var submission = await _context.Submissions
                .Include(s => s.Assignment)
                .FirstOrDefaultAsync(s => s.Id == id);

            if(submission == null)
            {
                return NotFound(new { message = "Submission not found" });
            }

            var createdByClaim = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier);
            if(createdByClaim == null || submission.Assignment.CreatedBy != createdByClaim.Value)
            {
                return Unauthorized(new { message = "You are not authorized to grade this submission" });
            }

            submission.Grade = request.Grade;
            submission.GradedDate = DateTime.UtcNow;
            submission.GradedBy = createdByClaim.Value;
            submission.Comments = request.Comments;

            _context.Submissions.Update(submission);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Submission graded", submission });
        }

        // View Student Submissions for an assignment
        [HttpGet("assignments/{assignmentId}/submissions")]
        public async Task<IActionResult> ViewStudentSubmissions(int assignmentId, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            var assignment = await _context.Assignments.FindAsync(assignmentId);
            if(assignment == null)
            {
                return NotFound(new { message = "Assignment not found" });
            }

            var createdByClaim = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier);
            if(createdByClaim == null || assignment.CreatedBy != createdByClaim.Value)
            {
                return Unauthorized(new { message = "You are not authorized to view these submissions" });
            }

            var submissions = await _context.Submissions
                .Include(s => s.Student)
                .Where(s => s.AssignmentId == assignmentId)
                .OrderByDescending(s => s.SubmissionDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var total = await _context.Submissions
                .Where(s => s.AssignmentId == assignmentId)
                .CountAsync();

            return Ok(new { 
                submissions,
                total,
                page,
                pageSize,
                totalPages = (int)Math.Ceiling(total / (double)pageSize)
            });
        }
    }

    public class GradeSubmissionRequest
    {
        public required string Grade { get; set; }
        public required string Comments { get; set; }
    }
}
