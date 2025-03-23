using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RoleBasedManagement.Data;
using RoleBasedManagement.Models;
using System.Security.Claims;

namespace RoleBasedManagement.Controllers
{
    [Authorize(Roles= "Student")]
    [Route("api/[controller]")]
    [ApiController]
    public class StudentController : ControllerBase
    {
        private readonly AppDBContext _context;
        private readonly IWebHostEnvironment _environment;
        private const int MaxFileSize = 10 * 1024 * 1024; // 10MB
        private readonly string[] AllowedFileTypes = { ".pdf", ".doc", ".docx", ".txt" };

        public StudentController(AppDBContext context, IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        [HttpGet("assignments")]
        public async Task<IActionResult> GetAssignments([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            var studentIdClaim = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier);
            if(studentIdClaim == null)
            {
                return Unauthorized(new { message = "User information is missing in the token" });
            }

            var assignments = await _context.Assignments
                .Where(a => a.DueDate >= DateTime.UtcNow)
                .OrderBy(a => a.DueDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var total = await _context.Assignments
                .Where(a => a.DueDate >= DateTime.UtcNow)
                .CountAsync();

            return Ok(new { 
                assignments,
                total,
                page,
                pageSize,
                totalPages = (int)Math.Ceiling(total / (double)pageSize)
            });
        }

        [HttpGet("my-submissions")]
        public async Task<IActionResult> GetMySubmissions([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            var studentIdClaim = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier);
            if(studentIdClaim == null)
            {
                return Unauthorized(new { message = "User information is missing in the token" });
            }

            var submissions = await _context.Submissions
                .Include(s => s.Assignment)
                .Where(s => s.StudentId.ToString() == studentIdClaim.Value)
                .OrderByDescending(s => s.SubmissionDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var total = await _context.Submissions
                .Where(s => s.StudentId.ToString() == studentIdClaim.Value)
                .CountAsync();

            return Ok(new { 
                submissions,
                total,
                page,
                pageSize,
                totalPages = (int)Math.Ceiling(total / (double)pageSize)
            });
        }

        [HttpPost("submit-assignment/{assignmentId}")]
        public async Task<IActionResult> SubmitAssignment(int assignmentId, [FromForm] IFormFile file)
        {
            if(file == null || file.Length == 0)
            {
                return BadRequest(new { message = "No file was uploaded" });
            }

            if(file.Length > MaxFileSize)
            {
                return BadRequest(new { message = "File size exceeds 10MB limit" });
            }

            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if(!AllowedFileTypes.Contains(extension))
            {
                return BadRequest(new { message = "Invalid file type. Allowed types: PDF, DOC, DOCX, TXT" });
            }

            var studentIdClaim = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier);
            if(studentIdClaim == null)
            {
                return Unauthorized(new { message = "User information is missing in the token" });
            }

            var assignment = await _context.Assignments.FindAsync(assignmentId);
            if(assignment == null)
            {
                return NotFound(new { message = "Assignment not found" });
            }

            if(DateTime.UtcNow > assignment.DueDate)
            {
                return BadRequest(new { message = "Assignment submission deadline has passed" });
            }

            // Create unique filename
            var fileName = $"{Guid.NewGuid()}{extension}";
            var uploadsFolder = Path.Combine(_environment.WebRootPath, "Submissions");
            if(!Directory.Exists(uploadsFolder))
            {
                Directory.CreateDirectory(uploadsFolder);
            }

            var filePath = Path.Combine(uploadsFolder, fileName);
            using(var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            // Create submission record
            var submission = new Submission
            {
                AssignmentId = assignmentId,
                StudentId = int.Parse(studentIdClaim.Value),
                SubmissionDate = DateTime.UtcNow,
                FilePath = fileName,
                OriginalFileName = file.FileName
            };

            _context.Submissions.Add(submission);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Assignment submitted successfully", submission });
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
                .Where(s => s.StudentId.ToString() == studentIdClaim.Value && !string.IsNullOrEmpty(s.Grade))
                .OrderByDescending(s => s.GradedDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var total = await _context.Submissions
                .Where(s => s.StudentId.ToString() == studentIdClaim.Value && !string.IsNullOrEmpty(s.Grade))
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
