using System.Drawing.Printing;
using System.IO;
using System.Net.Http;
using System.Net.Http.Json;
using System.Diagnostics;
using Microsoft.Extensions.Configuration;
using PdfiumViewer;
using POS.Core.Entities.Kitchen;
using POS.Core.Entities.OrderEntity;
using POS.Core.Services.Contract.PrinterServices;
using BlazorBase.API;
using Serilog;

namespace POS.Desktop.Services;

public class DesktopPrinterService : IPrinterServices
{
    private readonly HttpClient _httpClient;
    private readonly ApiSettings _apiSettings;
    private readonly IConfiguration _configuration;

    public DesktopPrinterService(
        IHttpClientFactory httpClientFactory,
        ApiSettings apiSettings,
        IConfiguration configuration)
    {
        _apiSettings = apiSettings;
        _configuration = configuration;
        _httpClient = httpClientFactory.CreateClient(apiSettings.ApiName!);
    }

    #region Data Methods (Calling API)

    public async Task<KitchenType?> CreatePrinterAsync(KitchenType printer)
    {
        var response = await _httpClient.PostAsJsonAsync("api/Printer/printer", printer);
        return await response.Content.ReadFromJsonAsync<KitchenType>();
    }

    public async Task<List<KitchenType>?> GetKitchenTypesAsync()
    {
        return await _httpClient.GetFromJsonAsync<List<KitchenType>>("api/Printer/kitchens");
    }

    public async Task<KitchenType?> GetKitchenByIdAsync(int kitchenId)
    {
        return await _httpClient.GetFromJsonAsync<KitchenType>($"api/Printer/kitchen/{kitchenId}");
    }

    public async Task<KitchenType?> UpdateKitchenTypesAsync(KitchenType printer)
    {
        var response = await _httpClient.PutAsJsonAsync("api/Printer/printer", printer);
        return await response.Content.ReadFromJsonAsync<KitchenType>();
    }

    public async Task<OrderSetting?> CreateOrderSettingAsync(OrderSetting orderSetting)
    {
        var response = await _httpClient.PostAsJsonAsync("api/Printer/order-setting", orderSetting);
        return await response.Content.ReadFromJsonAsync<OrderSetting>();
    }

    public async Task<OrderSetting?> UpdateOrderSettingAsync(OrderSetting orderSetting)
    {
        var response = await _httpClient.PutAsJsonAsync("api/Printer/order-setting", orderSetting);
        return await response.Content.ReadFromJsonAsync<OrderSetting>();
    }

    public async Task<List<OrderSetting>?> GetOrderSettingsAsync()
    {
        return await _httpClient.GetFromJsonAsync<List<OrderSetting>>("api/Printer/order-settings");
    }

    public async Task<OrderSetting?> GetOrderSettingByIdAsync(int orderSettingId)
    {
        return await _httpClient.GetFromJsonAsync<OrderSetting>($"api/Printer/order-setting/{orderSettingId}");
    }

    #endregion

    #region Printing Methods (Local Library)

    [System.Runtime.Versioning.SupportedOSPlatform("windows")]
    public async Task<List<string>> GetInstalledPrinters()
    {
        return await Task.Run(() =>
        {
            var printers = new List<string>();
            foreach (string printer in PrinterSettings.InstalledPrinters)
            {
                printers.Add(printer);
            }
            return printers;
        });
    }

    [System.Runtime.Versioning.SupportedOSPlatform("windows")]
    public async Task<bool> PrintPdfAsync(string pdfFilePath, string printerName)
    {
        var stopwatch = Stopwatch.StartNew(); // بدء قياس الوقت

        try
        {
            if (!File.Exists(pdfFilePath))
                throw new FileNotFoundException("PDF file not found.", pdfFilePath);

            byte[] pdfBytes = await File.ReadAllBytesAsync(pdfFilePath);
            using (var memoryStream = new MemoryStream(pdfBytes))
            using (var pdfDocument = PdfDocument.Load(memoryStream))
            {
                var printDoc = new PrintDocument
                {
                    PrinterSettings = new PrinterSettings { PrinterName = printerName },
                    // Use StandardPrintController to prevent the "Printing..." dialog box from appearing
                    PrintController = new StandardPrintController()
                };

                // Thermal receipt paper (80mm) is approx 3.15 inches
                // We set the PaperSize to match the PDF if possible, or force a standard receipt width
                printDoc.DefaultPageSettings.Margins = new Margins(0, 0, 0, 0);
                
                // Set high resolution for thermal printers (usually 203 or 300 DPI)
                printDoc.DefaultPageSettings.PrinterResolution = printDoc.PrinterSettings.PrinterResolutions
                    .Cast<PrinterResolution>()
                    .FirstOrDefault(r => r.Kind == PrinterResolutionKind.High) 
                    ?? new PrinterResolution { Kind = PrinterResolutionKind.Custom, X = 300, Y = 300 };

                int currentPage = 0;
                printDoc.PrintPage += (sender, e) =>
                {
                    if (currentPage < pdfDocument.PageCount)
                    {
                        // Optimization for Xprinter: High quality rendering
                        e.Graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                        e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
                        e.Graphics.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;

                        // Use 300 DPI for sharp text on thermal printers
                        // Render the page fitting it into the actual printable area of the printer
                        pdfDocument.Render(currentPage, e.Graphics, 300, 300, e.MarginBounds, true);
                        
                        currentPage++;
                        e.HasMorePages = currentPage < pdfDocument.PageCount;
                    }
                };

                await Task.Run(() => printDoc.Print()); 
            }
        }
        catch (Exception ex)
        {
            Serilog.Log.Error($"Print failed: {ex.Message}");
            return false;
        }
        finally
        {
            stopwatch.Stop(); // إيقاف القياس
            Serilog.Log.Information($"Total time: {stopwatch.Elapsed.TotalSeconds:F2} seconds");
        }

        return true;
    }

    [System.Runtime.Versioning.SupportedOSPlatform("windows")]
    public async Task PrintPdfToMultipleAsync(string pdfFilePath, List<string> printerNames)
    {
        var printTasks = new List<Task>();

        foreach (var printerName in printerNames)
        {
            printTasks.Add(PrintPdfAsync(pdfFilePath, printerName));
        }

        try
        {
            await Task.WhenAll(printTasks);
        }
        catch (Exception ex)
        {
            Serilog.Log.Error($"Error occurred while printing to multiple: {ex.Message}");
            throw;
        }
    }

    public Task PrintPdfFromUrlAsync(string pdfUrl)
    {
        throw new NotImplementedException("PrintPdfFromUrlAsync is not implemented for Desktop yet.");
    }

    #endregion
}
