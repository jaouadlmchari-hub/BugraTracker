
namespace BugTracker.Application.Interfaces.Services
{
    public interface IFileStorageService
    {
        Task UploadAsync(Stream fileStream,string storageFileName, string contentType);

        Task<string> GenerateDownloadUrlAsync(string storageFileName, TimeSpan expiration);

        Task DeleteAsync(string storageFileName);
    }
}
