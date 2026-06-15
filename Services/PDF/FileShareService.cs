using Microsoft.Maui.ApplicationModel.DataTransfer;

namespace VetAnesthesiaApp.Services.Pdf;

public class FileShareService : IFileShareService
{
    public Task ShareFileAsync(string filePath, string title)
    {
        return Share.Default.RequestAsync(new ShareFileRequest
        {
            Title = title,
            File = new ShareFile(filePath)
        });
    }
}
