namespace VetAnesthesiaApp.Models;

public sealed record SessionAlert(VetAlertLevel Level, string Message);

public enum VetAlertLevel
{
    Info = 0,
    Warning = 1,
    Critical = 2
}
