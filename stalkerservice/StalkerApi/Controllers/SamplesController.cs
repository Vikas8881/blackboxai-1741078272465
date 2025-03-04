using Microsoft.AspNetCore.Mvc;
using StalkerModels.Models;
using StalkerApi.Repositories;

namespace StalkerApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SamplesController : ControllerBase
{
    private readonly IRepository<Sample> _repository;
    private readonly ILogger<SamplesController> _logger;

    public SamplesController(IRepository<Sample> repository, ILogger<SamplesController> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Sample>>> GetAll()
    {
        try
        {
            var samples = await _repository.GetAllAsync();
            return Ok(samples);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all samples");
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Sample>> GetById(int id)
    {
        try
        {
            var sample = await _repository.GetByIdAsync(id);
            if (sample == null)
            {
                return NotFound();
            }
            return Ok(sample);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting sample with id {Id}", id);
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpPost]
    public async Task<ActionResult<Sample>> Create(Sample sample)
    {
        try
        {
            var created = await _repository.AddAsync(sample);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating sample");
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, Sample sample)
    {
        if (id != sample.Id)
        {
            return BadRequest();
        }

        try
        {
            await _repository.UpdateAsync(sample);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating sample with id {Id}", id);
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            await _repository.DeleteAsync(id);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting sample with id {Id}", id);
            return StatusCode(500, "Internal server error");
        }
    }
}
