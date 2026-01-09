using JiraClone.Application.DTOs.Sprints;
using JiraClone.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace JiraClone.API.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public class SprintsController : ControllerBase
{
    private readonly ISprintService _sprintService;
    private readonly ILogger<SprintsController> _logger;

    public SprintsController(ISprintService sprintService, ILogger<SprintsController> logger)
    {
        _sprintService = sprintService;
        _logger = logger;
    }

    /// <summary>
    /// Get all sprints with optional project filter
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<SprintDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll([FromQuery] ulong? projectId = null)
    {
        try
        {
            var sprints = await _sprintService.GetAllSprintsAsync(projectId);
            return Ok(sprints);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting sprints");
            return StatusCode(500, new { message = "An error occurred while retrieving sprints" });
        }
    }

    /// <summary>
    /// Get sprint lookup information including assignee, project, and current active sprint
    /// </summary>
    /// <param name="assigneeName">Optional assignee name (full name or email)</param>
    /// <param name="projectName">Optional project name</param>
    /// <returns>Sprint lookup response with assigneeId, projectId, and current sprint if found</returns>
    [HttpGet("lookup")]
    [ProducesResponseType(typeof(SprintLookupResponseDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSprintLookup([FromQuery] string? assigneeName = null, [FromQuery] string? projectName = null)
    {
        try
        {
            var result = await _sprintService.GetSprintLookupAsync(assigneeName, projectName);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting sprint lookup with assigneeName: {AssigneeName}, projectName: {ProjectName}", assigneeName, projectName);
            return StatusCode(500, new { message = "An error occurred while retrieving sprint lookup information" });
        }
    }

    /// <summary>
    /// Get sprint by ID
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(SprintDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(ulong id)
    {
        try
        {
            var sprint = await _sprintService.GetSprintByIdAsync(id);

            if (sprint == null)
            {
                return NotFound(new { message = "Sprint not found" });
            }

            return Ok(sprint);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting sprint {SprintId}", id);
            return StatusCode(500, new { message = "An error occurred while retrieving the sprint" });
        }
    }

    /// <summary>
    /// Create a new sprint
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(SprintDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateSprintDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var userId = GetCurrentUserId();
            var sprint = await _sprintService.CreateSprintAsync(dto, userId);

            if (sprint == null)
            {
                return BadRequest(new { message = "Failed to create sprint. Check that project exists and dates are valid." });
            }

            return CreatedAtAction(nameof(GetById), new { id = sprint.Id }, sprint);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating sprint");
            return StatusCode(500, new { message = "An error occurred while creating the sprint" });
        }
    }

    /// <summary>
    /// Update a sprint
    /// </summary>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(SprintDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Update(ulong id, [FromBody] UpdateSprintDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var userId = GetCurrentUserId();
            var sprint = await _sprintService.UpdateSprintAsync(id, dto, userId);

            if (sprint == null)
            {
                return NotFound(new { message = "Sprint not found or dates are invalid" });
            }

            return Ok(sprint);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating sprint {SprintId}", id);
            return StatusCode(500, new { message = "An error occurred while updating the sprint" });
        }
    }

    /// <summary>
    /// Delete a sprint
    /// </summary>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(ulong id)
    {
        try
        {
            var userId = GetCurrentUserId();
            var result = await _sprintService.DeleteSprintAsync(id, userId);

            if (!result)
            {
                return NotFound(new { message = "Sprint not found" });
            }

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting sprint {SprintId}", id);
            return StatusCode(500, new { message = "An error occurred while deleting the sprint" });
        }
    }

    /// <summary>
    /// Start a sprint (change status from Planned to Active)
    /// </summary>
    [HttpPost("{id}/start")]
    [ProducesResponseType(typeof(SprintDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> StartSprint(ulong id)
    {
        try
        {
            var userId = GetCurrentUserId();

            // Get existing sprint first
            var existingSprint = await _sprintService.GetSprintByIdAsync(id);
            if (existingSprint == null)
            {
                return NotFound(new { message = "Sprint not found" });
            }

            // Create update DTO with existing values, only changing status and start date
            var dto = new UpdateSprintDto
            {
                Name = existingSprint.Name,
                Goal = existingSprint.Goal,
                StartDate = DateTime.UtcNow,
                EndDate = existingSprint.EndDate ?? DateTime.UtcNow.AddDays(14), // Default 2 weeks if not set
                Status = Domain.Enums.SprintStatus.Active
            };

            var sprint = await _sprintService.UpdateSprintAsync(id, dto, userId);

            if (sprint == null)
            {
                return NotFound(new { message = "Sprint not found or invalid dates" });
            }

            return Ok(sprint);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting sprint {SprintId}", id);
            return StatusCode(500, new { message = "An error occurred while starting the sprint" });
        }
    }

    /// <summary>
    /// Complete a sprint (change status from Active to Completed)
    /// </summary>
    [HttpPost("{id}/complete")]
    [ProducesResponseType(typeof(SprintDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CompleteSprint(ulong id)
    {
        try
        {
            var userId = GetCurrentUserId();

            // Get existing sprint first
            var existingSprint = await _sprintService.GetSprintByIdAsync(id);
            if (existingSprint == null)
            {
                return NotFound(new { message = "Sprint not found" });
            }

            // Create update DTO with existing values, only changing status and end date
            var dto = new UpdateSprintDto
            {
                Name = existingSprint.Name,
                Goal = existingSprint.Goal,
                StartDate = existingSprint.StartDate ?? DateTime.UtcNow.AddDays(-14), // Use existing or default
                EndDate = DateTime.UtcNow,
                Status = Domain.Enums.SprintStatus.Completed
            };

            var sprint = await _sprintService.UpdateSprintAsync(id, dto, userId);

            if (sprint == null)
            {
                return NotFound(new { message = "Sprint not found or invalid dates" });
            }

            return Ok(sprint);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error completing sprint {SprintId}", id);
            return StatusCode(500, new { message = "An error occurred while completing the sprint" });
        }
    }

    private ulong GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return ulong.Parse(userIdClaim ?? "0");
    }
}
