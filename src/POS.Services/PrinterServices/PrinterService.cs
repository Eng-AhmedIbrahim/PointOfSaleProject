using System.Diagnostics;
using Pos.Repository.Data.DataSeed;

namespace POS.Services.PrinterServices;

public class PrinterService : IPrinterServices
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IConfiguration _configuration;

    public PrinterService(IUnitOfWork unitOfWork
        ,IConfiguration configuration)
    {
        _unitOfWork = unitOfWork;
        _configuration = configuration;
    }

    public async Task<OrderSetting?> CreateOrderSettingAsync(OrderSetting orderSetting)
    {
        if (orderSetting is null) return null;

        try
        {
            await _unitOfWork.Repository<OrderSetting>().AddAsync(orderSetting);
            var result = await _unitOfWork.CompleteAsync();
            if (result <= 0)
                return null;
        }
        catch (Exception ex)
        {
            Log.Error($"Cant Create Printer {ex.Message}");
            return null;
        }
        return orderSetting;
    }

    public async Task<KitchenType?> CreatePrinterAsync(KitchenType printer)
    {
        if (printer is null) return null;

        try
        {
            await _unitOfWork.Repository<KitchenType>().AddAsync(printer);
            var result = await _unitOfWork.CompleteAsync();
            if (result <= 0)
                return null;
        }
        catch (Exception ex)
        {
            Log.Error($"Cant Create Printer {ex.Message}");
            return null;
        }
        return printer;
    }

    public async Task<KitchenType?> GetKitchenByIdAsync(int kitchenId)
    {
        var existingPrinter = await _unitOfWork.Repository<KitchenType>().GetByIdAsync(kitchenId);

        if (existingPrinter is null) return null;

        return existingPrinter;
    }

    public async Task<List<KitchenType>?> GetKitchenTypesAsync(string? deviceName = null)
    {
        var spec = new KitchenTypeSpecs();
        var kitchenTypes = await _unitOfWork.Repository<KitchenType>().GetAllWithSpecificationAsync(spec);

        if (kitchenTypes is null) return null;

        var result = kitchenTypes.ToList();

        if (!string.IsNullOrEmpty(deviceName))
        {
            // Check if ANY printer exists for this device
            var anyPrinterForDevice = result.SelectMany(k => k.KitchenPrinters).Any(p => p.DeviceName == deviceName);

            if (!anyPrinterForDevice)
            {
                // Seed printers for this device
                await SeedPrintersForDeviceAsync(deviceName, result);
                // Refresh data
                kitchenTypes = await _unitOfWork.Repository<KitchenType>().GetAllWithSpecificationAsync(spec);
                result = kitchenTypes.ToList();
            }

            foreach (var kitchen in result)
            {
                if (kitchen.KitchenPrinters != null)
                {
                    kitchen.KitchenPrinters = kitchen.KitchenPrinters
                        .Where(p => p.DeviceName == deviceName)
                        .ToList();
                }
            }
        }

        return result;
    }

    private async Task SeedPrintersForDeviceAsync(string deviceName, List<KitchenType> kitchenTypes)
    {
        try
        {
            // Reading from json seed file
            var seedPrinters = await PosDbContextDataSeed.GetDataFromJsonFile<KitchenPrinters>("kitchenPrinters.json");

            foreach (var kitchen in kitchenTypes)
            {
                // Find matching seed by KitchenTypeId or use first Default seed
                var seed = seedPrinters?.FirstOrDefault(p => p.KitchenTypeId == kitchen.Id) ??
                           seedPrinters?.FirstOrDefault(p => p.DeviceName == "Default");

                var newPrinter = new KitchenPrinters
                {
                    KitchenTypeId = kitchen.Id,
                    DeviceName = deviceName,
                    Copy1 = seed?.Copy1,
                    Copy2 = seed?.Copy2,
                    Copy3 = seed?.Copy3,
                    Copy4 = seed?.Copy4,
                    Copy5 = seed?.Copy5,
                    Copy6 = seed?.Copy6,
                    Copy7 = seed?.Copy7,
                    Copy8 = seed?.Copy8,
                    Copy9 = seed?.Copy9,
                    Copy10 = seed?.Copy10
                };
                await _unitOfWork.Repository<KitchenPrinters>().AddAsync(newPrinter);
            }
            await _unitOfWork.CompleteAsync();
        }
        catch (Exception ex)
        {
            Log.Error(ex, $"Error seeding printers for device {deviceName}");
        }
    }

    public async Task<OrderSetting?> GetOrderSettingByIdAsync(int orderSettingId)
    {
        try
        {
            return await _unitOfWork.Repository<OrderSetting>().GetByIdAsync(orderSettingId);
        }
        catch (Exception ex)
        {
            Log.Error($"Error fetching OrderSetting by ID {orderSettingId}: {ex.Message}");
            return null;
        }
    }

    public async Task<List<OrderSetting>?> GetOrderSettingsAsync(string? deviceName = null)
    {
        try
        {
            if (string.IsNullOrEmpty(deviceName))
            {
                var orderSettings = await _unitOfWork.Repository<OrderSetting>().GetAllAsync();
                return orderSettings?.ToList();
            }

            var spec = new POS.Core.Specifications.OrderSpecs.OrderSettingSpecs(deviceName);
            var settings = await _unitOfWork.Repository<OrderSetting>().GetAllWithSpecificationAsync(spec);

            if (settings == null || !settings.Any())
            {
                // We can use a similar seeding logic here if needed, 
                // but since OrderService.cs already handles this, 
                // maybe we just call a common initialization if possible.
                // For now, let's keep it simple and match what OrderService does.
                
                var defaultSpec = new POS.Core.Specifications.BaseSpecifications<OrderSetting>(x => string.IsNullOrEmpty(x.ComputerName) || x.ComputerName == "Default");
                var defaultSettings = await _unitOfWork.Repository<OrderSetting>().GetAllWithSpecificationAsync(defaultSpec);

                if (defaultSettings != null && defaultSettings.Any())
                {
                    foreach (var setting in defaultSettings)
                    {
                        var newSetting = new OrderSetting
                        {
                            BranchID = setting.BranchID,
                            OrderType = setting.OrderType,
                            OrderStatment = setting.OrderStatment,
                            Service = setting.Service,
                            Tax = setting.Tax,
                            Tips = setting.Tips,
                            JobID = setting.JobID,
                            CustomerReceiptCount = setting.CustomerReceiptCount,
                            FullKitchenReceiptCount = setting.FullKitchenReceiptCount,
                            SeparateReceiptCount = setting.SeparateReceiptCount,
                            ClosingReceiptCount = setting.ClosingReceiptCount,
                            AddServiceToItemPrice = setting.AddServiceToItemPrice,
                            CanCloseWithoutPrint = setting.CanCloseWithoutPrint,
                            DeductCaptainTips = setting.DeductCaptainTips,
                            CaptainTipsAmount = setting.CaptainTipsAmount,
                            ComputerName = deviceName
                        };
                        await _unitOfWork.Repository<OrderSetting>().AddAsync(newSetting);
                    }
                    await _unitOfWork.CompleteAsync();
                    settings = await _unitOfWork.Repository<OrderSetting>().GetAllWithSpecificationAsync(spec);
                }
            }

            return settings?.ToList();
        }
        catch (Exception ex)
        {
            Log.Error($"Error fetching all OrderSettings for device {deviceName}: {ex.Message}");
            return null;
        }
    }


    public async Task<KitchenType?> UpdateKitchenTypesAsync(KitchenType printer)
    {
        if (printer is null) return null;

        try
        {
            var existingPrinter = await _unitOfWork.Repository<KitchenType>().GetByIdAsync(printer.Id);
            if (existingPrinter is null) return null;

            existingPrinter.KitchenName = printer.KitchenName;
            existingPrinter.BranchId = existingPrinter.BranchId;

            _unitOfWork.Repository<KitchenType>().Update(existingPrinter);
            var result = await _unitOfWork.CompleteAsync();

            if (result <= 0) return null;
        }
        catch (Exception ex)
        {
            Log.Error($"Error updating KitchenType: {ex.Message}");
            return null;
        }

        return printer;
    }

    public async Task<OrderSetting?> UpdateOrderSettingAsync(OrderSetting orderSetting)
    {
        if (orderSetting is null) return null;
        try
        {
            var existingOrderSetting = await _unitOfWork.Repository<OrderSetting>().GetByIdAsync(orderSetting.Id);
            if (existingOrderSetting is null) return null;

            existingOrderSetting.BranchID = existingOrderSetting.BranchID;
            existingOrderSetting.OrderType = existingOrderSetting.OrderType;
            existingOrderSetting.OrderStatment = orderSetting.OrderStatment;
            existingOrderSetting.Service = orderSetting.Service;
            existingOrderSetting.Tax = orderSetting.Tax;
            existingOrderSetting.Tips = orderSetting.Tips;
            existingOrderSetting.JobID = orderSetting.JobID;
            existingOrderSetting.CustomerReceiptCount = orderSetting.CustomerReceiptCount;
            existingOrderSetting.FullKitchenReceiptCount = orderSetting.FullKitchenReceiptCount;
            existingOrderSetting.SeparateReceiptCount = orderSetting.SeparateReceiptCount;
            existingOrderSetting.ClosingReceiptCount = orderSetting.ClosingReceiptCount;
            existingOrderSetting.AddServiceToItemPrice = orderSetting.AddServiceToItemPrice;

            _unitOfWork.Repository<OrderSetting>().Update(existingOrderSetting);
            var result = await _unitOfWork.CompleteAsync();

            if (result <= 0) return null;
        }
        catch (Exception ex)
        {
            Log.Error($"Error updating OrderSetting: {ex.Message}");
            return null;
        }

        return orderSetting;
    }

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
    public async Task<bool> PrintPdfAsync(string filePath, string printerName)
    {
        var exePath = _configuration["PrinterPath"];

        if (!File.Exists(filePath))
            throw new FileNotFoundException("PDF file not found.", filePath);

        var psi = new ProcessStartInfo
        {
            FileName = exePath,
            Arguments = $"\"{filePath}\" \"{printerName}\" /s /R0", // إضافة خيارات تحسين
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = false, // تعطيل الإخراج
            RedirectStandardError = false   // تعطيل الأخطاء
        };

        using var process = new Process { StartInfo = psi };

        process.Start();
        await process.WaitForExitAsync(); // انتظار غير متزامن

        if (process.ExitCode != 0)
            throw new Exception($"فشلت الطباعة. الرمز: {process.ExitCode}");

        return true;
    }

    //public async Task<bool> PrintPdfAsync(string pdfFilePath, string printerName)
    //{
    //    var stopwatch = Stopwatch.StartNew(); // بدء قياس الوقت

    //    try
    //    {
    //        byte[] pdfBytes = await File.ReadAllBytesAsync(pdfFilePath);
    //        using (var memoryStream = new MemoryStream(pdfBytes))
    //        using (var pdfDocument = PdfDocument.Load(memoryStream))
    //        {
    //            var printDoc = new PrintDocument
    //            {
    //                PrinterSettings = new PrinterSettings { PrinterName = printerName },
    //                DefaultPageSettings = new PageSettings
    //                {
    //                    // PaperSize = new PaperSize("A6", 413, 583),
    //                    Margins = new Margins(0, 0, 0, 0)
    //                }
    //            };

    //            int currentPage = 0;
    //            printDoc.PrintPage += (sender, e) =>
    //            {
    //                if (currentPage < pdfDocument.PageCount)
    //                {
    //                    pdfDocument.Render(currentPage, e.Graphics, 150, 150, e.PageBounds, true);
    //                    currentPage++;
    //                    e.HasMorePages = currentPage < pdfDocument.PageCount;
    //                }
    //            };

    //            printDoc.Print(); // عملية الطباعة الفعلية (تستغرق وقتًا)
    //        }
    //    }
    //    catch (Exception ex)
    //    {
    //        Console.WriteLine($"Print failed: {ex.Message}");
    //    }
    //    finally
    //    {
    //        stopwatch.Stop(); // إيقاف القياس
    //        Console.WriteLine($"Total time: {stopwatch.Elapsed.TotalSeconds:F2} seconds");
    //    }

    //    return true;
    //}


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
            Console.WriteLine($"Error occurred while printing: {ex.Message}");
            throw;
        }
    }

    public Task PrintPdfFromUrlAsync(string pdfUrl)
    {
        throw new NotImplementedException();
    }
}