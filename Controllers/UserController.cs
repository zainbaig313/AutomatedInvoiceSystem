using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/users")]
public class UserController : ControllerBase
{
    private readonly IUserService _userService;

    public UserController(IUserService userService) => _userService = userService;

    // ── Auth ──────────────────────────────────────────────────────────────────

    /// <summary>POST api/users/login</summary>
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginRequestDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Email) || string.IsNullOrWhiteSpace(dto.Password))
            return BadRequest(new { message = "Email and password are required." });

        var response = await _userService.LoginAsync(dto);
        if (response == null)
            return Unauthorized(new { message = "Invalid email or password." });

        return Ok(response);
    }

    // ── User management ───────────────────────────────────────────────────────

    /// <summary>POST api/users — Register a new accountant / owner.</summary>
    [HttpPost]
    [AllowAnonymous] // first-time setup; restrict this in production
    public async Task<IActionResult> Create([FromBody] CreateUserDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Name) ||
            string.IsNullOrWhiteSpace(dto.Email) ||
            string.IsNullOrWhiteSpace(dto.Password))
        {
            return BadRequest(new { message = "Name, email, and password are required." });
        }

        var user = await _userService.CreateAsync(dto);
        return CreatedAtAction(nameof(GetMe), user);
    }

    /// <summary>GET api/users/me — Returns the profile of the logged-in user.</summary>
    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> GetMe()
    {
        var idClaim = User.FindFirstValue(ClaimTypes.NameIdentifier)
                   ?? User.FindFirstValue("sub");

        if (!int.TryParse(idClaim, out var userId))
            return Unauthorized();

        var user = await _userService.GetByIdAsync(userId);
        return user == null ? NotFound() : Ok(user);
    }

    /// <summary>
    /// PUT api/users/{id} — Update name, bank, and digital wallet.
    /// Password changes are not supported here on purpose (use a separate flow).
    /// </summary>
    [HttpPut("{id:int}")]
    [Authorize]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateUserDto dto)
    {
        var updated = await _userService.UpdateAsync(id, dto);
        return updated == null ? NotFound() : Ok(updated);
    }
}
