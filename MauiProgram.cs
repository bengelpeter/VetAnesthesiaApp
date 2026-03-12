using CommunityToolkit.Maui;
using CommunityToolkit.Maui.Media;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MudBlazor.Services;
using VetAnesthesiaApp.Services.Buckets;
using VetAnesthesiaApp.Services.Data;
using VetAnesthesiaApp.Services.Speech;
using VetAnesthesiaApp.Services.Voice;

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

            builder.Services.AddSingleton<ITextToNumberParser, SpokenNumberParser>();

            builder.Services.AddScoped<IVoiceParserService, VoiceParserService>();
            builder.Services.AddScoped<IVoiceCommandApplicationService, VoiceCommandApplicationService>();

            builder.Services.AddScoped<IBucketService, BucketService>();

            builder.Services.AddSingleton<IAnesthesiaRepository, SqliteAnesthesiaRepository>();
            var app = builder.Build();
            return app;
        }

        private static void InitializeDatabase(MauiApp app)
        {
            try
            {
                using var scope = app.Services.CreateScope();
                var repo = scope.ServiceProvider.GetRequiredService<IAnesthesiaRepository>();
                repo.InitializeAsync().GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {

            }
          
        }
    }
}