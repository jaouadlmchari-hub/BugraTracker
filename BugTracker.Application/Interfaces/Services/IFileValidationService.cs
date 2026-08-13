
namespace BugTracker.Application.Interfaces.Services
{
    public interface IFileValidationService
    {
        Task ValidateAsync(Stream file, string fileName, string contentType);
    }
}
