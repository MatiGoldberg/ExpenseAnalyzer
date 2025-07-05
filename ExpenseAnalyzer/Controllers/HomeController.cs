using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using ExpenseAnalyzer.Models;

namespace ExpenseAnalyzer.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;

    public HomeController(ILogger<HomeController> logger)
    {
        _logger = logger;
    }

    public IActionResult Index()
    {
        return View();
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [HttpPost]
    public IActionResult UploadOfx()
    {
        var file = Request.Form.Files.FirstOrDefault();
        _logger.LogInformation("UploadOfx called. File present: {FilePresent}, FileName: {FileName}", file != null, file?.FileName);
        if (file == null || Path.GetExtension(file.FileName).ToLower() != ".ofx")
        {
            _logger.LogWarning("Upload failed: not an OFX file or file missing.");
            TempData["OfxError"] = "Error: not an OFX file.";
            return RedirectToAction("UploadOfx");
        }

        using var reader = new StreamReader(file.OpenReadStream());
        string ofxContent = reader.ReadToEnd();
        _logger.LogInformation("OFX file read. Length: {Length}", ofxContent.Length);
        var parser = new OfxParser();
        bool result = parser.Parse(ofxContent);
        if (!result)
        {
            _logger.LogError("OFX parsing failed: {Error}", parser.GetError());
            TempData["OfxError"] = parser.GetError() ?? "Unknown error.";
            return RedirectToAction("UploadOfx");
        }
        var transactions = parser.GetTransactions();
        _logger.LogInformation("OFX parsing succeeded. Transactions parsed: {Count}", transactions.Count);
        return View("Transactions", transactions);
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
