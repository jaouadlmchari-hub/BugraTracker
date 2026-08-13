using Amazon.S3;
using Amazon.S3.Model;
using BugTracker.Application.Interfaces.Services;
using BugTracker.Infrastructure.Configurations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace BugTracker.Infrastructure.Services;

public class S3FileStorageService : IFileStorageService
{
    private readonly IAmazonS3 _s3Client;
    private readonly string _bucketName;

    public S3FileStorageService(IAmazonS3 s3Client, IOptions<S3Settings> options)
    {
        _s3Client = s3Client;

        _bucketName = options.Value.BucketName;
    }

    public async Task UploadAsync(Stream fileStream,string storageFileName, string contentType)
    {
        var request = new PutObjectRequest
        {
            BucketName = _bucketName,
            Key = storageFileName,
            InputStream = fileStream,
            ContentType = contentType
        };

        await _s3Client.PutObjectAsync(request);
    }

    public Task<string> GenerateDownloadUrlAsync(string storageFileName,TimeSpan expiration)
    {
        var request = new GetPreSignedUrlRequest
        {
            BucketName = _bucketName,
            Key = storageFileName,
            Expires = DateTime.UtcNow.Add(expiration)
        };

        var url = _s3Client.GetPreSignedURL(request);

        return Task.FromResult(url);
    }

    public async Task DeleteAsync(string storageFileName)
    {
        var request = new DeleteObjectRequest
        {
            BucketName = _bucketName,
            Key = storageFileName
        };

        await _s3Client.DeleteObjectAsync(request);
    }
}