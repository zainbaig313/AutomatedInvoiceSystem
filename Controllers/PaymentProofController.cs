using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/payment-proofs")]
[Authorize]
public class PaymentProofController : AuthorizedControllerBase
{
    private readonly IPaymentProofService _proofService;

    public PaymentProofController(IPaymentProofService proofService)
        => _proofService = proofService;

    /// <summary>
    /// GET api/payment-proofs/invoice/{invoiceId}
    /// Only returns proofs for invoices that belong to the logged-in user.
    /// Returns an empty list (not 403) if the invoice belongs to someone else.
    /// </summary>
    [HttpGet("invoice/{invoiceId:int}")]
    public async Task<IActionResult> GetByInvoice(int invoiceId)
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized();

        var proofs = await _proofService.GetByInvoiceAsync(userId.Value, invoiceId);
        return Ok(proofs);
    }

    /// <summary>
    /// PATCH api/payment-proofs/{id}/review
    /// Approve or reject a proof — only works if the invoice belongs to the logged-in user.
    /// Body: { "approved": true } or { "approved": false, "rejectReason": "Blurry image" }
    /// </summary>
    [HttpPatch("{id:int}/review")]
    public async Task<IActionResult> Review(int id, [FromBody] ReviewProofDto dto)
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized();

        if (!dto.Approved && string.IsNullOrWhiteSpace(dto.RejectReason))
            return BadRequest(new { message = "A reject reason is required when rejecting a proof." });

        var result = await _proofService.ReviewAsync(userId.Value, id, dto);
        return result == null ? NotFound() : Ok(result);
    }
}
