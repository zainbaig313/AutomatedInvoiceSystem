using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/clients")]
[Authorize]
public class ClientController : AuthorizedControllerBase
{
    private readonly IClientService _clientService;

    public ClientController(IClientService clientService) => _clientService = clientService;

    /// <summary>GET api/clients — Only returns clients added by the logged-in user.</summary>
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized();

        return Ok(await _clientService.GetAllAsync(userId.Value));
    }

    /// <summary>GET api/clients/{id}</summary>
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized();

        var client = await _clientService.GetByIdAsync(userId.Value, id);
        return client == null ? NotFound() : Ok(client);
    }

    /// <summary>POST api/clients — Add a single client.</summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateClientDto dto)
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized();

        if (string.IsNullOrWhiteSpace(dto.Name) ||
            string.IsNullOrWhiteSpace(dto.Email) ||
            string.IsNullOrWhiteSpace(dto.Phone))
        {
            return BadRequest(new { message = "Name, email, and phone are required." });
        }

        try
        {
            var client = await _clientService.CreateAsync(userId.Value, dto);
            return CreatedAtAction(nameof(GetById), new { id = client.Id }, client);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>PUT api/clients/{id}</summary>
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateClientDto dto)
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized();

        try
        {
            var updated = await _clientService.UpdateAsync(userId.Value, id, dto);
            return updated == null ? NotFound() : Ok(updated);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>DELETE api/clients/{id}</summary>
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized();

        try
        {
            var deleted = await _clientService.DeleteAsync(userId.Value, id);
            return deleted ? NoContent() : NotFound();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>POST api/clients/import — Bulk import from CSV or Excel (form-data key: "file").</summary>
    [HttpPost("import")]
    public async Task<IActionResult> BulkImport(IFormFile file)
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized();

        if (file == null || file.Length == 0)
            return BadRequest(new { message = "Please upload a CSV or Excel file." });

        try
        {
            await using var stream = file.OpenReadStream();
            var imported = await _clientService.BulkImportAsync(userId.Value, stream, file.FileName);
            return Ok(new
            {
                message = $"{imported.Count} client(s) imported successfully.",
                clients = imported
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
