using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using VetAnesthesiaApp.Models;
using VetAnesthesiaApp.Services.Data;

namespace VetAnesthesiaApp.Services.Pdf;

public class PdfExportService : IPdfExportService
{
    private readonly IAnesthesiaRepository _repository;

    public PdfExportService(IAnesthesiaRepository repository)
    {
        _repository = repository;
    }

    public async Task<string> ExportSessionPdfAsync(Guid sessionId)
    {
        await _repository.InitializeAsync();

        var session = await _repository.GetSessionAsync(sessionId)
            ?? throw new InvalidOperationException("Session not found.");

        var animal = await _repository.GetAnimalAsync(session.AnimalId)
            ?? throw new InvalidOperationException("Animal not found.");

        var buckets = await _repository.GetBucketsAsync(sessionId);
        var noteEntries = buckets
            .Where(x => !string.IsNullOrWhiteSpace(x.Notes))
            .ToList();

        var fields = new List<(string Label, Func<AnesthesiaBucket, string> Selector)>
        {
            ("ISO", b => FormatDecimal(b.IsoPercent)),
            ("O2", b => FormatDecimal(b.OxygenFlowRate)),
            ("ETCO2", b => FormatDecimal(b.Etco2)),
            ("SpO2", b => b.Spo2?.ToString() ?? "-"),
            ("Temp", b => FormatDecimal(b.Temperature)),
            ("HR", b => b.HeartRate?.ToString() ?? "-"),
            ("RR", b => b.RespiratoryRate?.ToString() ?? "-"),
            ("SYS", b => b.SystolicBp?.ToString() ?? "-"),
            ("DIA", b => b.DiastolicBp?.ToString() ?? "-"),
            ("MAP", b => b.Map?.ToString() ?? "-")
        };

        var fileName = $"anesthesia-session-{sessionId}.pdf";
        var filePath = Path.Combine(FileSystem.CacheDirectory, fileName);

        Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(20);
                page.DefaultTextStyle(x => x.FontSize(9));

                page.Header().Column(column =>
                {
                    column.Spacing(4);
                    column.Item().Text("Vet Anesthesia Record").FontSize(18).Bold();
                    column.Item().Text(
                        $"Animal: {animal.Name}    Owner: {animal.OwnerName ?? "-"}    Species: {animal.Species}");
                    column.Item().Text(
                        $"Weight: {FormatDecimal(animal.Weight)}    Procedure: {session.Procedure ?? "-"}    Start: {session.SessionStartTime:g}");
                });

                page.Content().Column(column =>
                {
                    column.Spacing(10);
                    column.Item().Text("Recorded Vitals").Bold();

                    column.Item().ScaleToFit().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.ConstantColumn(70);

                            foreach (var _ in buckets)
                                columns.ConstantColumn(58);
                        });

                        table.Header(header =>
                        {
                            header.Cell().Element(HeaderCellStyle).Text("Field").Bold();

                            foreach (var bucket in buckets)
                                header.Cell().Element(HeaderCellStyle).Text(bucket.BucketStartTime.ToString("HH:mm"));
                        });

                        foreach (var field in fields)
                        {
                            table.Cell().Element(LabelCellStyle).Text(field.Label).Bold();

                            foreach (var bucket in buckets)
                                table.Cell().Element(ValueCellStyle).Text(field.Selector(bucket));
                        }
                    });

                    if (noteEntries.Count > 0)
                    {
                        column.Item().PaddingTop(8).Text("Procedure Notes").Bold();

                        foreach (var note in noteEntries)
                        {
                            column.Item().Border(1).BorderColor(QuestPDF.Helpers.Colors.Grey.Lighten2).Padding(6).Column(noteColumn =>
                            {
                                noteColumn.Spacing(2);
                                noteColumn.Item().Text(note.BucketStartTime.ToString("hh:mm tt")).SemiBold();
                                noteColumn.Item().Text(note.Notes ?? string.Empty);
                            });
                        }
                    }
                });

                page.Footer().AlignRight().Text($"Generated {DateTime.Now:g}");
            });
        }).GeneratePdf(filePath);

        return filePath;
    }

    private static string FormatDecimal(decimal? value)
    {
        if (!value.HasValue)
            return "-";

        return value.Value % 1 == 0
            ? value.Value.ToString("0")
            : value.Value.ToString("0.##");
    }

    private static QuestPDF.Infrastructure.IContainer HeaderCellStyle(QuestPDF.Infrastructure.IContainer container)
    {
        return container
            .Border(1)
            .BorderColor(QuestPDF.Helpers.Colors.Grey.Lighten2)
            .Background(QuestPDF.Helpers.Colors.Grey.Lighten3)
            .PaddingVertical(4)
            .PaddingHorizontal(2)
            .AlignCenter()
            .AlignMiddle();
    }

    private static QuestPDF.Infrastructure.IContainer LabelCellStyle(QuestPDF.Infrastructure.IContainer container)
    {
        return container
            .Border(1)
            .BorderColor(QuestPDF.Helpers.Colors.Grey.Lighten2)
            .Background(QuestPDF.Helpers.Colors.Grey.Lighten4)
            .PaddingVertical(4)
            .PaddingHorizontal(2)
            .AlignCenter()
            .AlignMiddle();
    }

    private static QuestPDF.Infrastructure.IContainer ValueCellStyle(QuestPDF.Infrastructure.IContainer container)
    {
        return container
            .Border(1)
            .BorderColor(QuestPDF.Helpers.Colors.Grey.Lighten2)
            .PaddingVertical(4)
            .PaddingHorizontal(2)
            .AlignCenter()
            .AlignMiddle();
    }
}
