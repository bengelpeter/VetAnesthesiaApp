namespace VetAnesthesiaApp.Services.Voice;

public interface ITextToNumberParser
{
    decimal? Parse(string valueText);
}