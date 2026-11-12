using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/invoices")]
[Authorize]
public class InvoiceController : AuthorizedControllerBase
{
    private readonly IInvoiceService _invoiceService;

    public InvoiceController(IInvoiceService invoiceService) => _invoiceService = invoiceService;

    /// <summary>GET api/invoices — Only returns invoices created by the logged-in user.</summary>
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized();

        return Ok(await _invoiceService.GetAllAsync(userId.Value));
    }

    /// <summary>GET api/invoices/{id}</summary>
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized();

        var invoice = await _invoiceService.GetByIdAsync(userId.Value, id);
        return invoice == null ? NotFound() : Ok(invoice);
    }

    /// <summary>POST api/invoices — Create invoice (auto-sends email + PDF).</summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateInvoiceDto dto)
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized();

        var invoice = await _invoiceService.CreateAsync(userId.Value, dto);
        return CreatedAtAction(nameof(GetById), new { id = invoice.Id }, invoice);
    }

    /// <summary>PATCH api/invoices/{id}/mark-paid</summary>
    [HttpPatch("{id:int}/mark-paid")]
    public async Task<IActionResult> MarkAsPaid(int id)
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized();

        var success = await _invoiceService.MarkAsPaidAsync(userId.Value, id);
        return success ? Ok(new { message = "Invoice marked as paid." }) : NotFound();
    }

    /// <summary>DELETE api/invoices/{id}</summary>
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized();

        var deleted = await _invoiceService.DeleteAsync(userId.Value, id);
        return deleted ? NoContent() : NotFound();
    }

    /// <summary>GET api/invoices/{id}/pdf — Download invoice PDF.</summary>
    [HttpGet("{id:int}/pdf")]
    public async Task<IActionResult> DownloadPdf(int id)
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized();

        try
        {
            var pdfBytes = await _invoiceService.GeneratePdfAsync(userId.Value, id);
            return File(pdfBytes, "application/pdf", $"invoice-{id}.pdf");
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    /// <summary>GET api/invoices/report?year=2024&amp;month=11 — Monthly Excel report.</summary>
    [HttpGet("report")]
    public async Task<IActionResult> MonthlyReport([FromQuery] int year, [FromQuery] int month)
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized();

        if (year < 2000 || month is < 1 or > 12)
            return BadRequest(new { message = "Provide a valid year and month (1-12)." });

        var excelBytes = await _invoiceService.ExportMonthlyReportAsync(userId.Value, year, month);
        return File(excelBytes,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            $"InvoiceReport-{year}-{month:D2}.xlsx");
    }
}
