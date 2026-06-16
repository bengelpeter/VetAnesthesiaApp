using Android.Graphics.Pdf;
using VetAnesthesiaApp.Models;
using VetAnesthesiaApp.Services.Data;
using VetAnesthesiaApp.Services.Workflow;
using AndroidCanvas = Android.Graphics.Canvas;
using AndroidColor = Android.Graphics.Color;
using AndroidPaint = Android.Graphics.Paint;
using AndroidRect = Android.Graphics.Rect;

namespace VetAnesthesiaApp.Services.Pdf;

public class AndroidPdfExportService : IPdfExportService
{
    private const int PageWidth = 1200;
    private const int PageHeight = 850;
    private const int Margin = 40;
    private const int HeaderHeight = 140;
    private const int RowHeight = 42;
    private const int LabelColumnWidth = 120;
    private const int BucketColumnWidth = 120;
    private const int BucketsPerPage = 8;

    private readonly IAnesthesiaRepository _repository;
    private readonly IChartConfigurationService _chartConfigurationService;

    public AndroidPdfExportService(IAnesthesiaRepository repository, IChartConfigurationService chartConfigurationService)
    {
        _repository = repository;
        _chartConfigurationService = chartConfigurationService;
    }

    public async Task<string> ExportSessionPdfAsync(Guid sessionId)
    {
        await _repository.InitializeAsync();

        var session = await _repository.GetSessionAsync(sessionId)
            ?? throw new InvalidOperationException("Session not found.");

        var animal = await _repository.GetAnimalAsync(session.AnimalId)
            ?? throw new InvalidOperationException("Animal not found.");

        var settings = await _repository.GetClinicSettingsAsync();
        var buckets = (await _repository.GetBucketsAsync(sessionId))
            .OrderBy(x => x.BucketStartTime)
            .ToList();

        if (buckets.Count == 0)
        {
            throw new InvalidOperationException("No anesthesia buckets are available to export.");
        }

        var notes = buckets
            .Where(x => !string.IsNullOrWhiteSpace(x.Notes))
            .Select(x => $"{x.BucketStartTime:hh:mm tt}  {x.Notes}")
            .ToList();

        var fields = _chartConfigurationService.GetConfiguredFields(settings)
            .Select(field => (field.Label, Selector: new Func<AnesthesiaBucket, string>(bucket => FormatFieldValue(field.ValueSelector(bucket), field.IsWholeNumber))))
            .ToList();

        Directory.CreateDirectory(FileSystem.CacheDirectory);

        var fileName = $"anesthesia-session-{sessionId}.pdf";
        var filePath = Path.Combine(FileSystem.CacheDirectory, fileName);

        using var document = new PdfDocument();

        var bucketPages = buckets
            .Chunk(BucketsPerPage)
            .ToList();

        var pageNumber = 1;

        foreach (var bucketPage in bucketPages)
        {
            using var page = document.StartPage(new PdfDocument.PageInfo.Builder(PageWidth, PageHeight, pageNumber).Create())
                ?? throw new InvalidOperationException("Android PDF page creation failed.");
            DrawBucketPage(page.Canvas!, animal, session, settings.ClinicName, GetPdfTitle(settings), bucketPage, fields, pageNumber, bucketPages.Count);
            document.FinishPage(page);
            pageNumber++;
        }

        if (notes.Count > 0)
        {
            foreach (var notePage in notes.Chunk(18))
            {
                using var page = document.StartPage(new PdfDocument.PageInfo.Builder(PageWidth, PageHeight, pageNumber).Create())
                    ?? throw new InvalidOperationException("Android PDF notes page creation failed.");
                DrawNotesPage(page.Canvas!, animal, session, settings.ClinicName, GetPdfTitle(settings), notePage, pageNumber);
                document.FinishPage(page);
                pageNumber++;
            }
        }

        await using var stream = File.Create(filePath);
        document.WriteTo(stream);

        return filePath;
    }

    private static void DrawBucketPage(
        AndroidCanvas canvas,
        Animal animal,
        AnesthesiaSession session,
        string? clinicName,
        string pdfTitle,
        IReadOnlyList<AnesthesiaBucket> buckets,
        IReadOnlyList<(string Label, Func<AnesthesiaBucket, string> Selector)> fields,
        int pageNumber,
        int totalPages)
    {
        canvas.DrawColor(AndroidColor.White);

        using var titlePaint = new AndroidPaint
        {
            Color = AndroidColor.Black,
            AntiAlias = true,
            TextSize = 28f,
            FakeBoldText = true
        };

        using var bodyPaint = new AndroidPaint
        {
            Color = AndroidColor.Black,
            AntiAlias = true,
            TextSize = 16f
        };

        using var headerFillPaint = new AndroidPaint
        {
            Color = AndroidColor.Rgb(230, 236, 245)
        };

        using var labelFillPaint = new AndroidPaint
        {
            Color = AndroidColor.Rgb(242, 244, 248)
        };

        using var borderPaint = new AndroidPaint
        {
            Color = AndroidColor.Rgb(170, 176, 186),
            StrokeWidth = 1f
        };
        borderPaint.SetStyle(AndroidPaint.Style.Stroke);

        var y = Margin;

        canvas.DrawText(pdfTitle, Margin, y, titlePaint);
        y += 32;

        if (!string.IsNullOrWhiteSpace(clinicName))
        {
            canvas.DrawText($"Clinic: {clinicName.Trim()}", Margin, y, bodyPaint);
            y += 24;
        }

        canvas.DrawText($"Animal: {animal.Name}", Margin, y, bodyPaint);
        canvas.DrawText($"Species: {DisplayText(animal.Species)}", Margin + 310, y, bodyPaint);
        canvas.DrawText($"Owner: {DisplayText(animal.OwnerName)}", Margin + 620, y, bodyPaint);
        y += 24;

        canvas.DrawText($"Procedure: {DisplayText(session.Procedure)}", Margin, y, bodyPaint);
        canvas.DrawText($"Weight: {FormatDecimal(animal.Weight)}", Margin + 310, y, bodyPaint);
        canvas.DrawText($"Start: {session.SessionStartTime:g}", Margin + 620, y, bodyPaint);
        y += 24;

        canvas.DrawText($"Page {pageNumber} of {totalPages}", Margin, y, bodyPaint);
        y = Margin + HeaderHeight;

        var tableX = Margin;
        var currentX = tableX;

        var headerRect = new AndroidRect(currentX, y, currentX + LabelColumnWidth, y + RowHeight);
        canvas.DrawRect(headerRect, headerFillPaint);
        canvas.DrawRect(headerRect, borderPaint);
        DrawCenteredText(canvas, "Field", headerRect, bodyPaint, true);
        currentX += LabelColumnWidth;

        foreach (var bucket in buckets)
        {
            var bucketRect = new AndroidRect(currentX, y, currentX + BucketColumnWidth, y + RowHeight);
            canvas.DrawRect(bucketRect, headerFillPaint);
            canvas.DrawRect(bucketRect, borderPaint);
            DrawCenteredText(canvas, bucket.BucketStartTime.ToString("HH:mm"), bucketRect, bodyPaint, true);
            currentX += BucketColumnWidth;
        }

        y += RowHeight;

        foreach (var field in fields)
        {
            currentX = tableX;

            var labelRect = new AndroidRect(currentX, y, currentX + LabelColumnWidth, y + RowHeight);
            canvas.DrawRect(labelRect, labelFillPaint);
            canvas.DrawRect(labelRect, borderPaint);
            DrawCenteredText(canvas, field.Label, labelRect, bodyPaint, true);
            currentX += LabelColumnWidth;

            foreach (var bucket in buckets)
            {
                var valueRect = new AndroidRect(currentX, y, currentX + BucketColumnWidth, y + RowHeight);
                canvas.DrawRect(valueRect, borderPaint);
                DrawCenteredText(canvas, field.Selector(bucket), valueRect, bodyPaint, false);
                currentX += BucketColumnWidth;
            }

            y += RowHeight;
        }
    }

    private static void DrawNotesPage(
        AndroidCanvas canvas,
        Animal animal,
        AnesthesiaSession session,
        string? clinicName,
        string pdfTitle,
        IReadOnlyList<string> notes,
        int pageNumber)
    {
        canvas.DrawColor(AndroidColor.White);

        using var titlePaint = new AndroidPaint
        {
            Color = AndroidColor.Black,
            AntiAlias = true,
            TextSize = 28f,
            FakeBoldText = true
        };

        using var bodyPaint = new AndroidPaint
        {
            Color = AndroidColor.Black,
            AntiAlias = true,
            TextSize = 16f
        };

        var y = Margin;
        canvas.DrawText(pdfTitle, Margin, y, titlePaint);
        y += 32;

        if (!string.IsNullOrWhiteSpace(clinicName))
        {
            canvas.DrawText($"Clinic: {clinicName.Trim()}", Margin, y, bodyPaint);
            y += 24;
        }

        canvas.DrawText($"Animal: {animal.Name}", Margin, y, bodyPaint);
        canvas.DrawText($"Procedure: {DisplayText(session.Procedure)}", Margin + 320, y, bodyPaint);
        canvas.DrawText($"Page {pageNumber}", Margin + 850, y, bodyPaint);
        y += 40;
        canvas.DrawText("Procedure Notes", Margin, y, titlePaint);
        y += 34;

        foreach (var note in notes)
        {
            var lines = WrapText(note, 120);

            foreach (var line in lines)
            {
                canvas.DrawText(line, Margin, y, bodyPaint);
                y += 22;
            }

            y += 10;
        }
    }

    private static void DrawCenteredText(AndroidCanvas canvas, string text, AndroidRect bounds, AndroidPaint paint, bool bold)
    {
        var previousFakeBold = paint.FakeBoldText;
        paint.FakeBoldText = bold;

        var displayText = DisplayText(text);
        var measuredWidth = paint.MeasureText(displayText);
        var baseline = bounds.CenterY() - ((paint.Descent() + paint.Ascent()) / 2);
        var x = (float)(bounds.Left + Math.Max(8d, (bounds.Width() - measuredWidth) / 2f));

        canvas.DrawText(displayText, x, baseline, paint);
        paint.FakeBoldText = previousFakeBold;
    }

    private static string FormatDecimal(decimal? value)
    {
        if (!value.HasValue)
        {
            return "-";
        }

        return value.Value % 1 == 0
            ? value.Value.ToString("0")
            : value.Value.ToString("0.##");
    }

    private static string FormatFieldValue(decimal? value, bool isWholeNumber)
    {
        if (!value.HasValue)
            return "-";

        return isWholeNumber
            ? ((int)value.Value).ToString()
            : FormatDecimal(value);
    }

    private static string DisplayText(string? value) =>
        string.IsNullOrWhiteSpace(value) ? "-" : value;

    private static List<string> WrapText(string value, int maxCharacters)
    {
        var words = value.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var lines = new List<string>();
        var currentLine = string.Empty;

        foreach (var word in words)
        {
            var candidate = string.IsNullOrEmpty(currentLine)
                ? word
                : $"{currentLine} {word}";

            if (candidate.Length <= maxCharacters)
            {
                currentLine = candidate;
                continue;
            }

            if (!string.IsNullOrEmpty(currentLine))
            {
                lines.Add(currentLine);
            }

            currentLine = word;
        }

        if (!string.IsNullOrEmpty(currentLine))
        {
            lines.Add(currentLine);
        }

        return lines.Count == 0 ? new List<string> { "-" } : lines;
    }

    private static string GetPdfTitle(ClinicSettings settings) =>
        string.IsNullOrWhiteSpace(settings.PdfDocumentTitle)
            ? "Anesthesia Record"
            : settings.PdfDocumentTitle.Trim();
}
