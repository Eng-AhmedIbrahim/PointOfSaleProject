using POS.Reports.Models;
using POS.Reports.ReportsMakerServices;
using QuestPDF.Fluent;

namespace POS.API.Controllers;

[Route("api/[controller]")]
[ApiController]
public class ReportController : ControllerBase
{
    private readonly IWebHostEnvironment _hostEnvironment;
    private readonly string _reportPath;

    public ReportController(IWebHostEnvironment hostEnvironment)
    {
        _hostEnvironment = hostEnvironment;
        _reportPath = Path.Combine(_hostEnvironment.WebRootPath, "Reports");
    }

    [HttpGet("GenerateReceipt")]
    public async Task<IActionResult> GenerateReceiptReport()
    {
        var receipt = FakeDataGenerator.GenerateFakeReceipt(5);
        var document = await Task.Run(() => new ReceiptDocument(receipt));

        var outputPath = Path.Combine(_reportPath, $"{DateTimeOffset.Now}-receipt.pdf");
        document.GeneratePdf(outputPath);
        return Ok("Report generated successfully");
    }
}