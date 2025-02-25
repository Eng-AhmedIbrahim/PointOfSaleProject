using POS.Reports.Models;
using POS.Reports.ReportsMakerServices;
using QuestPDF.Fluent;

namespace POS.API.Controllers;

[Route("api/[controller]")]
[ApiController]
public class ReportController : ControllerBase
{
    private readonly IWebHostEnvironment _hostEnvironment;
    private readonly string _reportsFolder;

    public ReportController(IWebHostEnvironment hostEnvironment)
    {
        _hostEnvironment = hostEnvironment;
        _reportsFolder = Path.Combine(_hostEnvironment.ContentRootPath, "Reports");
        Directory.CreateDirectory(_reportsFolder);
    }

    [HttpGet("GenerateReceipt")]
    public async Task<IActionResult> GenerateReceiptReport()
    {
        var receipt = FakeDataGenerator.GenerateFakeReceipt(20);
        var document = await Task.Run(() => new ReceiptDocument(receipt));

        var timestamp = DateTimeOffset.Now.ToString("yyyy-MM-dd_hh-mm-ss_tt");
        var outputPath = Path.Combine(_reportsFolder, $"{timestamp}-receipt.pdf");

        // Save the PDF
        document.GeneratePdf(outputPath);

        return Ok($"Report generated successfully: {outputPath}");
    }
}