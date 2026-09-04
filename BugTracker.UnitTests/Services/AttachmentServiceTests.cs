using BugTracker.Application.DTOs.Attachments;
using BugTracker.Application.Exceptions;
using BugTracker.Application.Interfaces.Persistence;
using BugTracker.Application.Interfaces.Repositories;
using BugTracker.Application.Interfaces.Services;
using BugTracker.Application.Services;
using BugTracker.Domain.Entities;
using BugTracker.Domain.Enums;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace BugTracker.UnitTests.Services
{
    public class AttachmentServiceTests
    {
        private readonly Mock<IUnitOfWork> _unitOfWorkMock;
        private readonly Mock<IAttachmentRepository> _attachmentRepositoryMock;
        private readonly Mock<IIssueRepository> _issueRepositoryMock;
        private readonly Mock<ICurrentUserService> _currentUserServiceMock;
        private readonly Mock<IFileValidationService> _fileValidationServiceMock;
        private readonly Mock<IFileStorageService> _fileStorageServiceMock;
        private readonly Mock<IActivityLogService> _activityLogServiceMock;
        private readonly Mock<ILogger<AttachmentService>> _loggerMock;
        private readonly AttachmentService _sut;

        public AttachmentServiceTests()
        {
            _unitOfWorkMock = new Mock<IUnitOfWork>();
            _attachmentRepositoryMock = new Mock<IAttachmentRepository>();
            _issueRepositoryMock = new Mock<IIssueRepository>();
            _currentUserServiceMock = new Mock<ICurrentUserService>();
            _fileValidationServiceMock = new Mock<IFileValidationService>();
            _fileStorageServiceMock = new Mock<IFileStorageService>();
            _activityLogServiceMock = new Mock<IActivityLogService>();
            _loggerMock = new Mock<ILogger<AttachmentService>>();

            _unitOfWorkMock.SetupGet(u => u.Attachments).Returns(_attachmentRepositoryMock.Object);
            _unitOfWorkMock.SetupGet(u => u.Issues).Returns(_issueRepositoryMock.Object);

            _sut = new AttachmentService(
                _unitOfWorkMock.Object,
                _currentUserServiceMock.Object,
                _fileValidationServiceMock.Object,
                _fileStorageServiceMock.Object,
                _loggerMock.Object,
                _activityLogServiceMock.Object);
        }

        // ============================================================
        // GetByIdAsync
        // ============================================================

        [Fact]
        public async Task GetByIdAsync_WhenAttachmentExists_ShouldReturnAttachmentDto()
        {
            // Arrange
            var attachmentId = Guid.NewGuid();
            var issueId = Guid.NewGuid();
            var uploaderId = Guid.NewGuid();

            var attachment = new Attachment
            {
                Id = attachmentId,
                IssueId = issueId,
                UploaderId = uploaderId,
                Filename = "bug.png",
                StorageKey = "storage-bug.png",
                MimeType = "image/png",
                SizeBytes = 1024
            };

            const string downloadUrl = "http://localhost/download/bug.png";

            _attachmentRepositoryMock
                .Setup(r => r.GetByIdWithDetailsAsync(attachmentId))
                .ReturnsAsync(attachment);

            _fileStorageServiceMock
                .Setup(s => s.GenerateDownloadUrlAsync(
                    attachment.StorageKey,
                    TimeSpan.FromHours(1)))
                .ReturnsAsync(downloadUrl);

            // Act
            var result = await _sut.GetByIdAsync(attachmentId);

            // Assert
            result.Should().NotBeNull();
            result!.Id.Should().Be(attachmentId);
            result.IssueId.Should().Be(issueId);
            result.UploaderId.Should().Be(uploaderId);
            result.Filename.Should().Be("bug.png");
            result.MimeType.Should().Be("image/png");
            result.SizeBytes.Should().Be(1024);

            _attachmentRepositoryMock.Verify(
                r => r.GetByIdWithDetailsAsync(attachmentId),
                Times.Once);

            _fileStorageServiceMock.Verify(
                s => s.GenerateDownloadUrlAsync(
                    attachment.StorageKey,
                    TimeSpan.FromHours(1)),
                Times.Once);
        }

        [Fact]
        public async Task GetByIdAsync_WhenAttachmentDoesNotExist_ShouldReturnNull()
        {
            // Arrange
            var attachmentId = Guid.NewGuid();

            _attachmentRepositoryMock
                .Setup(r => r.GetByIdWithDetailsAsync(attachmentId))
                .ReturnsAsync((Attachment?)null);

            // Act
            var result = await _sut.GetByIdAsync(attachmentId);

            // Assert
            result.Should().BeNull();

            _fileStorageServiceMock.Verify(
                s => s.GenerateDownloadUrlAsync(
                    It.IsAny<string>(),
                    It.IsAny<TimeSpan>()),
                Times.Never);
        }

        // ============================================================
        // GetByIssueAsync
        // ============================================================

        [Fact]
        public async Task GetByIssueAsync_WhenIssueDoesNotExist_ShouldThrowNotFoundException()
        {
            // Arrange
            var issueId = Guid.NewGuid();

            _issueRepositoryMock
                .Setup(r => r.GetByIdAsync(issueId))
                .ReturnsAsync((Issue?)null);

            // Act
            Func<Task> act = () => _sut.GetByIssueAsync(issueId);

            // Assert
            await act.Should().ThrowAsync<NotFoundException>();

            _attachmentRepositoryMock.Verify(
                r => r.GetByIssueIdAsync(It.IsAny<Guid>()),
                Times.Never);
        }

        [Fact]
        public async Task GetByIssueAsync_WhenIssueExists_ShouldReturnAttachmentsWithDownloadUrls()
        {
            // Arrange
            var issueId = Guid.NewGuid();

            var issue = new Issue
            {
                Id = issueId,
                ProjectId = Guid.NewGuid(),
                Title = "Login bug"
            };

            var attachment1 = new Attachment
            {
                Id = Guid.NewGuid(),
                IssueId = issueId,
                UploaderId = Guid.NewGuid(),
                Filename = "bug1.png",
                StorageKey = "storage-1.png",
                MimeType = "image/png",
                SizeBytes = 1000
            };

            var attachment2 = new Attachment
            {
                Id = Guid.NewGuid(),
                IssueId = issueId,
                UploaderId = Guid.NewGuid(),
                Filename = "bug2.pdf",
                StorageKey = "storage-2.pdf",
                MimeType = "application/pdf",
                SizeBytes = 2000
            };

            var attachments = new List<Attachment>
            {
                attachment1,
                attachment2
            };

            _issueRepositoryMock
                .Setup(r => r.GetByIdAsync(issueId))
                .ReturnsAsync(issue);

            _attachmentRepositoryMock
                .Setup(r => r.GetByIssueIdAsync(issueId))
                .ReturnsAsync(attachments);

            _fileStorageServiceMock
                .Setup(s => s.GenerateDownloadUrlAsync(
                    "storage-1.png",
                    TimeSpan.FromHours(1)))
                .ReturnsAsync("url-1");

            _fileStorageServiceMock
                .Setup(s => s.GenerateDownloadUrlAsync(
                    "storage-2.pdf",
                    TimeSpan.FromHours(1)))
                .ReturnsAsync("url-2");

            // Act
            var result = (await _sut.GetByIssueAsync(issueId)).ToList();

            // Assert
            result.Should().HaveCount(2);

            result.Should().Contain(a => a.Filename == "bug1.png");
            result.Should().Contain(a => a.Filename == "bug2.pdf");

            _attachmentRepositoryMock.Verify(
                r => r.GetByIssueIdAsync(issueId),
                Times.Once);

            _fileStorageServiceMock.Verify(
                s => s.GenerateDownloadUrlAsync(
                    It.IsAny<string>(),
                    TimeSpan.FromHours(1)),
                Times.Exactly(2));
        }

        // ============================================================
        // UploadAsync
        // ============================================================

        [Fact]
        public async Task UploadAsync_WhenIssueDoesNotExist_ShouldThrowNotFoundException()
        {
            // Arrange
            var issueId = Guid.NewGuid();

            var dto = new CreateAttachmentDto
            {
                FileContent = new MemoryStream(new byte[] { 1, 2, 3 }),
                FileName = "bug.png",
                ContentType = "image/png"
            };

            _issueRepositoryMock
                .Setup(r => r.GetByIdAsync(issueId))
                .ReturnsAsync((Issue?)null);

            // Act
            Func<Task> act = () => _sut.UploadAsync(issueId, dto);

            // Assert
            await act.Should().ThrowAsync<NotFoundException>();

            _fileValidationServiceMock.Verify(
                v => v.ValidateAsync(
                    It.IsAny<Stream>(),
                    It.IsAny<string>(),
                    It.IsAny<string>()),
                Times.Never);

            _fileStorageServiceMock.Verify(
                s => s.UploadAsync(
                    It.IsAny<Stream>(),
                    It.IsAny<string>(),
                    It.IsAny<string>()),
                Times.Never);

            _attachmentRepositoryMock.Verify(
                r => r.AddAsync(It.IsAny<Attachment>()),
                Times.Never);

            _unitOfWorkMock.Verify(
                u => u.SaveChangesAsync(),
                Times.Never);
        }

        [Fact]
        public async Task UploadAsync_WhenFileContentIsNull_ShouldThrowBusinessRuleException()
        {
            // Arrange
            var issueId = Guid.NewGuid();

            var issue = new Issue
            {
                Id = issueId,
                ProjectId = Guid.NewGuid(),
                Title = "Login bug"
            };

            var dto = new CreateAttachmentDto
            {
                FileContent = null!,
                FileName = "bug.png",
                ContentType = "image/png"
            };

            _issueRepositoryMock
                .Setup(r => r.GetByIdAsync(issueId))
                .ReturnsAsync(issue);

            // Act
            Func<Task> act = () => _sut.UploadAsync(issueId, dto);

            // Assert
            await act.Should()
                .ThrowAsync<BusinessRuleException>()
                .WithMessage("Aucun fichier fourni.");

            _attachmentRepositoryMock.Verify(
                r => r.CountByIssueIdAsync(It.IsAny<Guid>()),
                Times.Never);

            _fileValidationServiceMock.Verify(
                v => v.ValidateAsync(
                    It.IsAny<Stream>(),
                    It.IsAny<string>(),
                    It.IsAny<string>()),
                Times.Never);

            _fileStorageServiceMock.Verify(
                s => s.UploadAsync(
                    It.IsAny<Stream>(),
                    It.IsAny<string>(),
                    It.IsAny<string>()),
                Times.Never);

            _unitOfWorkMock.Verify(
                u => u.SaveChangesAsync(),
                Times.Never);
        }

        [Fact]
        public async Task UploadAsync_WhenIssueAlreadyHas20Attachments_ShouldThrowBusinessRuleException()
        {
            // Arrange
            var issueId = Guid.NewGuid();

            var issue = new Issue
            {
                Id = issueId,
                ProjectId = Guid.NewGuid(),
                Title = "Login bug"
            };

            var dto = new CreateAttachmentDto
            {
                FileContent = new MemoryStream(new byte[] { 1, 2, 3 }),
                FileName = "bug.png",
                ContentType = "image/png"
            };

            _issueRepositoryMock
                .Setup(r => r.GetByIdAsync(issueId))
                .ReturnsAsync(issue);

            _attachmentRepositoryMock
                .Setup(r => r.CountByIssueIdAsync(issueId))
                .ReturnsAsync(20);

            // Act
            Func<Task> act = () => _sut.UploadAsync(issueId, dto);

            // Assert
            await act.Should()
                .ThrowAsync<BusinessRuleException>()
                .WithMessage("MAX_ATTACHMENTS_REACHED");

            _fileValidationServiceMock.Verify(
                v => v.ValidateAsync(
                    It.IsAny<Stream>(),
                    It.IsAny<string>(),
                    It.IsAny<string>()),
                Times.Never);

            _fileStorageServiceMock.Verify(
                s => s.UploadAsync(
                    It.IsAny<Stream>(),
                    It.IsAny<string>(),
                    It.IsAny<string>()),
                Times.Never);

            _attachmentRepositoryMock.Verify(
                r => r.AddAsync(It.IsAny<Attachment>()),
                Times.Never);

            _unitOfWorkMock.Verify(
                u => u.SaveChangesAsync(),
                Times.Never);
        }

        [Fact]
        public async Task UploadAsync_WhenFileValidationFails_ShouldNotUploadOrSaveAttachment()
        {
            // Arrange
            var issueId = Guid.NewGuid();

            var issue = new Issue
            {
                Id = issueId,
                ProjectId = Guid.NewGuid(),
                Title = "Login bug"
            };

            var stream = new MemoryStream(new byte[] { 1, 2, 3 });

            var dto = new CreateAttachmentDto
            {
                FileContent = stream,
                FileName = "malicious.exe",
                ContentType = "application/octet-stream"
            };

            _issueRepositoryMock
                .Setup(r => r.GetByIdAsync(issueId))
                .ReturnsAsync(issue);

            _attachmentRepositoryMock
                .Setup(r => r.CountByIssueIdAsync(issueId))
                .ReturnsAsync(5);

            _fileValidationServiceMock
                .Setup(v => v.ValidateAsync(
                    stream,
                    dto.FileName,
                    dto.ContentType))
                .ThrowsAsync(
                    new BusinessRuleException("Fichier invalide."));

            // Act
            Func<Task> act = () => _sut.UploadAsync(issueId, dto);

            // Assert
            await act.Should().ThrowAsync<BusinessRuleException>();

            _fileStorageServiceMock.Verify(
                s => s.UploadAsync(
                    It.IsAny<Stream>(),
                    It.IsAny<string>(),
                    It.IsAny<string>()),
                Times.Never);

            _attachmentRepositoryMock.Verify(
                r => r.AddAsync(It.IsAny<Attachment>()),
                Times.Never);

            _activityLogServiceMock.Verify(
                a => a.LogAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<Guid>(),
                    It.IsAny<ActivityAction>(),
                    It.IsAny<string?>(),
                    It.IsAny<string?>(),
                    It.IsAny<string?>()),
                Times.Never);

            _unitOfWorkMock.Verify(
                u => u.SaveChangesAsync(),
                Times.Never);
        }

        [Fact]
        public async Task UploadAsync_WhenStorageUploadFails_ShouldNotSaveAttachmentMetadata()
        {
            // Arrange
            var issueId = Guid.NewGuid();

            var issue = new Issue
            {
                Id = issueId,
                ProjectId = Guid.NewGuid(),
                Title = "Login bug"
            };

            var stream = new MemoryStream(new byte[] { 1, 2, 3, 4 });

            var dto = new CreateAttachmentDto
            {
                FileContent = stream,
                FileName = "bug.png",
                ContentType = "image/png"
            };

            _issueRepositoryMock
                .Setup(r => r.GetByIdAsync(issueId))
                .ReturnsAsync(issue);

            _attachmentRepositoryMock
                .Setup(r => r.CountByIssueIdAsync(issueId))
                .ReturnsAsync(2);

            _fileValidationServiceMock
                .Setup(v => v.ValidateAsync(
                    stream,
                    dto.FileName,
                    dto.ContentType))
                .Returns(Task.CompletedTask);

            _fileStorageServiceMock
                .Setup(s => s.UploadAsync(
                    stream,
                    It.IsAny<string>(),
                    dto.ContentType))
                .ThrowsAsync(new Exception("S3 unavailable"));

            // Act
            Func<Task> act = () => _sut.UploadAsync(issueId, dto);

            // Assert
            await act.Should().ThrowAsync<Exception>();

            _attachmentRepositoryMock.Verify(
                r => r.AddAsync(It.IsAny<Attachment>()),
                Times.Never);

            _activityLogServiceMock.Verify(
                a => a.LogAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<Guid>(),
                    It.IsAny<ActivityAction>(),
                    It.IsAny<string?>(),
                    It.IsAny<string?>(),
                    It.IsAny<string?>()),
                Times.Never);

            _unitOfWorkMock.Verify(
                u => u.SaveChangesAsync(),
                Times.Never);
        }

        [Fact]
        public async Task UploadAsync_WhenDataIsValid_ShouldUploadFileCreateAttachmentAndLogActivity()
        {
            // Arrange
            var issueId = Guid.NewGuid();
            var currentUserId = Guid.NewGuid();

            var issue = new Issue
            {
                Id = issueId,
                ProjectId = Guid.NewGuid(),
                Title = "Login bug"
            };

            var fileBytes = new byte[] { 1, 2, 3, 4, 5 };
            var stream = new MemoryStream(fileBytes);

            var dto = new CreateAttachmentDto
            {
                FileContent = stream,
                FileName = "screenshot.png",
                ContentType = "image/png"
            };

            _issueRepositoryMock
                .Setup(r => r.GetByIdAsync(issueId))
                .ReturnsAsync(issue);

            _currentUserServiceMock
                .SetupGet(c => c.UserId)
                .Returns(currentUserId);

            _attachmentRepositoryMock
                .Setup(r => r.CountByIssueIdAsync(issueId))
                .ReturnsAsync(5);

            _fileValidationServiceMock
                .Setup(v => v.ValidateAsync(
                    stream,
                    dto.FileName,
                    dto.ContentType))
                .Callback<Stream, string, string>((file, _, _) =>
                {
                    file.Position = file.Length;
                })
                .Returns(Task.CompletedTask);

            string? capturedStorageKey = null;
            long? positionAtUpload = null;

            _fileStorageServiceMock
                .Setup(s => s.UploadAsync(
                    stream,
                    It.IsAny<string>(),
                    dto.ContentType))
                .Callback<Stream, string, string>((file, key, _) =>
                {
                    positionAtUpload = file.Position;
                    capturedStorageKey = key;
                })
                .Returns(Task.CompletedTask);

            Attachment? createdAttachment = null;

            _attachmentRepositoryMock
                .Setup(r => r.AddAsync(It.IsAny<Attachment>()))
                .Callback<Attachment>(attachment =>
                {
                    createdAttachment = attachment;
                })
                .Returns(Task.CompletedTask);

            _fileStorageServiceMock
                .Setup(s => s.GenerateDownloadUrlAsync(
                    It.IsAny<string>(),
                    TimeSpan.FromHours(1)))
                .ReturnsAsync(
                    "http://localhost/download/screenshot");

            // Act
            var result = await _sut.UploadAsync(issueId, dto);

            // Assert
            createdAttachment.Should().NotBeNull();
            capturedStorageKey.Should().NotBeNullOrWhiteSpace();

            createdAttachment!.IssueId.Should().Be(issueId);
            createdAttachment.UploaderId.Should().Be(currentUserId);
            createdAttachment.Filename.Should().Be(dto.FileName);
            createdAttachment.MimeType.Should().Be(dto.ContentType);
            createdAttachment.SizeBytes.Should().Be(fileBytes.LongLength);
            createdAttachment.StorageKey.Should().Be(capturedStorageKey);

            capturedStorageKey.Should().EndWith(".png");

            Guid.TryParse(
                Path.GetFileNameWithoutExtension(capturedStorageKey),
                out _)
                .Should()
                .BeTrue();

            // ValidateAsync a déplacé Position à la fin.
            // AttachmentService doit la remettre à 0 avant UploadAsync.
            positionAtUpload.Should().Be(0);

            result.IssueId.Should().Be(issueId);
            result.UploaderId.Should().Be(currentUserId);
            result.Filename.Should().Be(dto.FileName);

            _fileValidationServiceMock.Verify(
                v => v.ValidateAsync(
                    stream,
                    dto.FileName,
                    dto.ContentType),
                Times.Once);

            _fileStorageServiceMock.Verify(
                s => s.UploadAsync(
                    stream,
                    capturedStorageKey!,
                    dto.ContentType),
                Times.Once);

            _attachmentRepositoryMock.Verify(
                r => r.AddAsync(It.IsAny<Attachment>()),
                Times.Once);

            _activityLogServiceMock.Verify(
                a => a.LogAsync(
                    issueId,
                    currentUserId,
                    ActivityAction.AttachmentAdded,
                    null,
                    null,
                    null),
                Times.Once);

            _unitOfWorkMock.Verify(
                u => u.SaveChangesAsync(),
                Times.Once);

            _fileStorageServiceMock.Verify(
                s => s.GenerateDownloadUrlAsync(
                    capturedStorageKey!,
                    TimeSpan.FromHours(1)),
                Times.Once);
        }

        // ============================================================
        // GetDownloadUrlAsync
        // ============================================================

        [Fact]
        public async Task GetDownloadUrlAsync_WhenAttachmentDoesNotExist_ShouldThrowNotFoundException()
        {
            // Arrange
            var attachmentId = Guid.NewGuid();

            _attachmentRepositoryMock
                .Setup(r => r.GetByIdAsync(attachmentId))
                .ReturnsAsync((Attachment?)null);

            // Act
            Func<Task> act = () => _sut.GetDownloadUrlAsync(attachmentId);

            // Assert
            await act.Should().ThrowAsync<NotFoundException>();

            _fileStorageServiceMock.Verify(
                s => s.GenerateDownloadUrlAsync(
                    It.IsAny<string>(),
                    It.IsAny<TimeSpan>()),
                Times.Never);
        }

        [Fact]
        public async Task GetDownloadUrlAsync_WhenAttachmentExists_ShouldReturnPresignedUrl()
        {
            // Arrange
            var attachmentId = Guid.NewGuid();

            var attachment = new Attachment
            {
                Id = attachmentId,
                IssueId = Guid.NewGuid(),
                UploaderId = Guid.NewGuid(),
                Filename = "bug.png",
                StorageKey = "storage-key.png",
                MimeType = "image/png",
                SizeBytes = 1024
            };

            const string expectedUrl =
                "http://localhost/presigned-url";

            _attachmentRepositoryMock
                .Setup(r => r.GetByIdAsync(attachmentId))
                .ReturnsAsync(attachment);

            _fileStorageServiceMock
                .Setup(s => s.GenerateDownloadUrlAsync(
                    attachment.StorageKey,
                    TimeSpan.FromHours(1)))
                .ReturnsAsync(expectedUrl);

            // Act
            var result = await _sut.GetDownloadUrlAsync(attachmentId);

            // Assert
            result.Should().Be(expectedUrl);

            _fileStorageServiceMock.Verify(
                s => s.GenerateDownloadUrlAsync(
                    attachment.StorageKey,
                    TimeSpan.FromHours(1)),
                Times.Once);
        }

        // ============================================================
        // DeleteAsync
        // ============================================================

        [Fact]
        public async Task DeleteAsync_WhenAttachmentDoesNotExist_ShouldThrowNotFoundException()
        {
            // Arrange
            var attachmentId = Guid.NewGuid();

            _attachmentRepositoryMock
                .Setup(r => r.GetByIdAsync(attachmentId))
                .ReturnsAsync((Attachment?)null);

            // Act
            Func<Task> act = () => _sut.DeleteAsync(attachmentId);

            // Assert
            await act.Should().ThrowAsync<NotFoundException>();

            _attachmentRepositoryMock.Verify(
                r => r.Delete(It.IsAny<Attachment>()),
                Times.Never);

            _activityLogServiceMock.Verify(
                a => a.LogAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<Guid>(),
                    It.IsAny<ActivityAction>(),
                    It.IsAny<string?>(),
                    It.IsAny<string?>(),
                    It.IsAny<string?>()),
                Times.Never);

            _fileStorageServiceMock.Verify(
                s => s.DeleteAsync(It.IsAny<string>()),
                Times.Never);

            _unitOfWorkMock.Verify(
                u => u.SaveChangesAsync(),
                Times.Never);
        }

        [Fact]
        public async Task DeleteAsync_WhenAttachmentExists_ShouldDeleteMetadataLogActivityAndDeleteStoredFile()
        {
            // Arrange
            var attachmentId = Guid.NewGuid();
            var issueId = Guid.NewGuid();
            var currentUserId = Guid.NewGuid();

            var attachment = new Attachment
            {
                Id = attachmentId,
                IssueId = issueId,
                UploaderId = Guid.NewGuid(),
                Filename = "bug.png",
                StorageKey = "storage-key.png",
                MimeType = "image/png",
                SizeBytes = 1024
            };

            _attachmentRepositoryMock
                .Setup(r => r.GetByIdAsync(attachmentId))
                .ReturnsAsync(attachment);

            _currentUserServiceMock
                .SetupGet(c => c.UserId)
                .Returns(currentUserId);

            _fileStorageServiceMock
                .Setup(s => s.DeleteAsync(attachment.StorageKey))
                .Returns(Task.CompletedTask);

            // Act
            await _sut.DeleteAsync(attachmentId);

            // Assert
            _attachmentRepositoryMock.Verify(
                r => r.Delete(attachment),
                Times.Once);

            _activityLogServiceMock.Verify(
                a => a.LogAsync(
                    issueId,
                    currentUserId,
                    ActivityAction.AttachmentRemoved,
                    "Attachment",
                    attachment.StorageKey,
                    null),
                Times.Once);

            _unitOfWorkMock.Verify(
                u => u.SaveChangesAsync(),
                Times.Once);

            _fileStorageServiceMock.Verify(
                s => s.DeleteAsync(attachment.StorageKey),
                Times.Once);
        }

        [Fact]
        public async Task DeleteAsync_WhenStorageDeletionFails_ShouldNotThrowAfterDatabaseDeletion()
        {
            // Arrange
            var attachmentId = Guid.NewGuid();
            var issueId = Guid.NewGuid();
            var currentUserId = Guid.NewGuid();

            var attachment = new Attachment
            {
                Id = attachmentId,
                IssueId = issueId,
                UploaderId = Guid.NewGuid(),
                Filename = "bug.png",
                StorageKey = "storage-key.png",
                MimeType = "image/png",
                SizeBytes = 1024
            };

            _attachmentRepositoryMock
                .Setup(r => r.GetByIdAsync(attachmentId))
                .ReturnsAsync(attachment);

            _currentUserServiceMock
                .SetupGet(c => c.UserId)
                .Returns(currentUserId);

            _fileStorageServiceMock
                .Setup(s => s.DeleteAsync(attachment.StorageKey))
                .ThrowsAsync(new Exception("S3 unavailable"));

            // Act
            Func<Task> act = () => _sut.DeleteAsync(attachmentId);

            // Assert
            await act.Should().NotThrowAsync();

            _attachmentRepositoryMock.Verify(
                r => r.Delete(attachment),
                Times.Once);

            _activityLogServiceMock.Verify(
                a => a.LogAsync(
                    issueId,
                    currentUserId,
                    ActivityAction.AttachmentRemoved,
                    "Attachment",
                    attachment.StorageKey,
                    null),
                Times.Once);

            _unitOfWorkMock.Verify(
                u => u.SaveChangesAsync(),
                Times.Once);

            _fileStorageServiceMock.Verify(
                s => s.DeleteAsync(attachment.StorageKey),
                Times.Once);
        }
    }
}