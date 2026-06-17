using CommunityToolkit.Maui;
using CommunityToolkit.Maui.Media;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MudBlazor.Services;
using VetAnesthesiaApp.Services.Alerts;
using VetAnesthesiaApp.Services.Buckets;
using VetAnesthesiaApp.Services.Data;
using VetAnesthesiaApp.Services.Pdf;
using QuestPDF.Infrastructure;
using VetAnesthesiaApp.Services.Speech;
using VetAnesthesiaApp.Services.Voice;
using VetAnesthesiaApp.Services.Workflow;
namespace VetAnesthesiaApp
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();

            builder
                .UseMauiApp<App>()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                })
                .UseMauiCommunityToolkit();

            builder.Services.AddSingleton<ISpeechToText>(SpeechToText.Default);
            builder.Services.AddSingleton<IVetSpeechService, VetSpeechService>();

            builder.Services.AddMauiBlazorWebView();
            builder.Services.AddMudServices();

#if DEBUG
            builder.Services.AddBlazorWebViewDeveloperTools();
            builder.Logging.AddDebug();
#endif
#if ANDROID
            builder.Services.AddScoped<IPdfExportService, AndroidPdfExportService>();
#elif WINDOWS
            QuestPDF.Settings.License = LicenseType.Community;
            builder.Services.AddScoped<IPdfExportService, PdfExportService>();
#else
            builder.Services.AddScoped<IPdfExportService, UnsupportedPdfExportService>();
#endif
            builder.Services.AddScoped<IFileShareService, FileShareService>();
            builder.Services.AddScoped<IPdfSessionExportCoordinator, PdfSessionExportCoordinator>();
            builder.Services.AddScoped<ISessionAlertEvaluator, SessionAlertEvaluator>();
            builder.Services.AddScoped<IChartConfigurationService, ChartConfigurationService>();
            builder.Services.AddScoped<ISessionCompletionEvaluator, SessionCompletionEvaluator>();
            builder.Services.AddScoped<ISessionHandoffSummaryService, SessionHandoffSummaryService>();
            builder.Services.AddScoped<ISessionStructuredExportService, SessionStructuredExportService>();
            builder.Services.AddScoped<IWorkflowTemplateService, WorkflowTemplateService>();
            builder.Services.AddScoped<IWorkflowTelemetryService, WorkflowTelemetryService>();
            builder.Services.AddScoped<IClipboardService, ClipboardService>();
            builder.Services.AddSingleton<ITextToNumberParser, SpokenNumberParser>();

            builder.Services.AddScoped<IVoiceParserService, VoiceParserService>();
            builder.Services.AddScoped<IVoiceCommandApplicationService, VoiceCommandApplicationService>();

            builder.Services.AddScoped<IBucketService, BucketService>();

            builder.Services.AddSingleton<IAnesthesiaRepository, SqliteAnesthesiaRepository>();
            return builder.Build();
        }
    }
}
