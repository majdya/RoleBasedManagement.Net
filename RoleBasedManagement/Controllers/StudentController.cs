using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RoleBasedManagement.Data;
using RoleBasedManagement.Models;
using RoleBasedManagement.Models.DTOs;
using System.Security.Claims;

namespace RoleBasedManagement.Controllers
{

    [Authorize(Roles = "student")]
    [Route("api/[controller]")]
    [ApiController]
    public class StudentController : ControllerBase
    {
        

        private readonly ILogger<StudentController> _logger;
        private readonly AppDBContext _context;

        private IActionResult ErrorResponse(string message, int statusCode)
        {
            return StatusCode(statusCode, new { message });
        }

        public StudentController(AppDBContext context, ILogger<StudentController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // Get all available assignments
        [HttpGet("assignments")]
        public async Task<IActionResult> GetAssignments([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            var studentIdClaim = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier);
            if(studentIdClaim == null)
            {
                return Unauthorized(new { message = "User information is missing in the token" });
            }

            var studentId = studentIdClaim.Value;

            var assignments = await _context.Assignments
                .OrderByDescending(a => a.CreatedDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(a => new
                {
                    a.Id,
                    a.Title,
                    a.Description,
                    a.DueDate,
                    a.CreatedDate,
                    Status = _context.Submissions
                        .Where(s => s.AssignmentId == a.Id && s.StudentId == studentId)
                        .Select(s => !string.IsNullOrEmpty(s.Grade) ? "graded" : "submitted")
                        .FirstOrDefault() ?? "pending"
                })
                .ToListAsync();

            var total = await _context.Assignments.CountAsync();

            return Ok(new
            {
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
            if(submissionDTO == null)
            {
                _logger.LogWarning("Assignment with ID {AssignmentId} not found", assignmentId);
                return ErrorResponse("Invalid submission data", StatusCodes.Status400BadRequest);
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

            // Check if a submission already exists for the same assignment and student
            var existingSubmission = await _context.Submissions
                .FirstOrDefaultAsync(s => s.AssignmentId == assignmentId && s.StudentId == studentIdClaim.Value);
            if (existingSubmission != null)
            {
                return Conflict(new { message = "Submission already exists for this assignment" });
            }

            // Proceed to create a new submission
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
            if(studentIdClaim == null)
            {
                return ErrorResponse("User information is missing in the token", StatusCodes.Status401Unauthorized);
            }

            var submissions = await _context.Submissions
                .Include(s => s.Assignment)
                .Where(s => s.StudentId == studentIdClaim.Value)
                .OrderByDescending(s => s.SubmissionDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(s => new SubmissionDTO
                {
                    Id = s.Id,
                    Content = s.Content,
                    SubmissionDate = s.SubmissionDate,
                    Grade = s.Grade,
                    Assignment = new AssignmentDTO
                    {
                        Id = s.Assignment.Id,
                        Title = s.Assignment.Title,
                        Description = s.Assignment.Description,
                        DueDate = s.Assignment.DueDate,
                        CreatedDate = s.Assignment.CreatedDate
                    }
                })
                .ToListAsync();

            var total = await _context.Submissions
                .Where(s => s.StudentId == studentIdClaim.Value)
                .CountAsync();

            return Ok(new
            {
                submissions,
                total,
                page,
                pageSize,
                totalPages = (int)Math.Ceiling(total / (double)pageSize)
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
