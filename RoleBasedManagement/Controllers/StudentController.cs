using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RoleBasedManagement.Data;
using RoleBasedManagement.Models;
using System.Security.Claims;

namespace RoleBasedManagement.Controllers
{
    [Authorize(Roles = "student")]
    [Route("api/[controller]")]
    [ApiController]
    public class StudentController : ControllerBase
    {
        private readonly AppDBContext _context;

        public StudentController(AppDBContext context)
        {
            _context = context;
        }

        // Get all available assignments
        [HttpGet("assignments")]
        public async Task<IActionResult> GetAssignments([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            var assignments = await _context.Assignments
                .OrderByDescending(a => a.CreatedDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var total = await _context.Assignments.CountAsync();

            return Ok(new { 
                assignments,
                total,
                page,
                pageSize,
                totalPages = (int)Math.Ceiling(total / (double)pageSize)
            });
        }

        // Get a specific assignment
        [HttpGet("assignments/{id}")]
        public async Task<IActionResult> GetAssignment(int id)
        {
            var assignment = await _context.Assignments.FindAsync(id);
            if (assignment == null)
            {
                return NotFound(new { message = "Assignment not found" });
            }

            return Ok(assignment);
        }

        // Submit an assignment
        [HttpPost("assignments/{assignmentId}/submit")]
        public async Task<IActionResult> SubmitAssignment(int assignmentId, [FromBody] CreateSubmissionDTO submissionDTO)
        {
            if (submissionDTO == null)
            {
                return BadRequest(new { message = "Invalid submission data" });
            }

            var studentIdClaim = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier);
            if (studentIdClaim == null)
            {
                return Unauthorized(new { message = "User information is missing in the token" });
            }

            // Check if assignment exists
            var assignment = await _context.Assignments.FindAsync(assignmentId);
            if (assignment == null)
            {
                return NotFound(new { message = "Assignment not found" });
            }

            // Check if assignment is past due date
            if (DateTime.UtcNow > assignment.DueDate)
            {
                return BadRequest(new { message = "Assignment submission deadline has passed" });
            }

            // Check if student has already submitted
            var existingSubmission = await _context.Submissions
                .FirstOrDefaultAsync(s => s.AssignmentId == assignmentId && s.StudentId == studentIdClaim.Value);

            if (existingSubmission != null)
            {
                return BadRequest(new { message = "You have already submitted this assignment" });
            }

            var submission = new Submission
            {
                AssignmentId = assignmentId,
                StudentId = studentIdClaim.Value,
                Content = submissionDTO.Content,
                SubmissionDate = DateTime.UtcNow
            };

            _context.Submissions.Add(submission);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Submission created", submission, assignment });
        }

        // Get student's submissions
        [HttpGet("my-submissions")]
        public async Task<IActionResult> GetMySubmissions([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            var studentIdClaim = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier);
            if (studentIdClaim == null)
            {
                return Unauthorized(new { message = "User information is missing in the token" });
            }

            var submissions = await _context.Submissions
                .Include(s => s.Assignment)
                .Where(s => s.StudentId == studentIdClaim.Value)
                .OrderByDescending(s => s.SubmissionDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var total = await _context.Submissions
                .Where(s => s.StudentId == studentIdClaim.Value)
                .CountAsync();

            // Fetch available assignments that have not been submitted
            var availableAssignments = await _context.Assignments
                .Where(a => !a.Submissions.Any(s => s.StudentId == studentIdClaim.Value))
                .ToListAsync();

            return Ok(new { 
                submissions,
                total,
                page,
                pageSize,
                totalPages = (int)Math.Ceiling(total / (double)pageSize),
                availableAssignments // Include available assignments in the response
            });
        }

        [HttpGet("grades")]
        public async Task<IActionResult> GetGrades([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            var studentIdClaim = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier);
            if(studentIdClaim == null)
            {
                return Unauthorized(new { message = "User information is missing in the token" });
            }

            var gradedSubmissions = await _context.Submissions
                .Include(s => s.Assignment)
                .Where(s => s.StudentId == studentIdClaim.Value && !string.IsNullOrEmpty(s.Grade))
                .OrderByDescending(s => s.GradedDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var total = await _context.Submissions
                .Where(s => s.StudentId == studentIdClaim.Value && !string.IsNullOrEmpty(s.Grade))
                .CountAsync();

            return Ok(new { 
                grades = gradedSubmissions,
                total,
                page,
                pageSize,
                totalPages = (int)Math.Ceiling(total / (double)pageSize)
            });
        }
    }
}
