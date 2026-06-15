namespace VetAnesthesiaApp.Services.Pdf;

public interface IFileShareService
{
    Task ShareFileAsync(string filePath, string title);
}
