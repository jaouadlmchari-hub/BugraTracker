using BugTracker.Application.Interfaces.Services;
using System.IO;

namespace BugTracker.Infrastructure.Services;

public class FileValidationService : IFileValidationService
{
    private const long MaxFileSize = 25 * 1024 * 1024; // 25 MB
    private const long MaxMp4Size = 10 * 1024 * 1024;  // 10 MB

    private static readonly Dictionary<string, string[]> AllowedMimeTypes = new()
    {
        [".jpg"] = new[] { "image/jpeg" },
        [".jpeg"] = new[] { "image/jpeg" },
        [".png"] = new[] { "image/png" },
        [".gif"] = new[] { "image/gif" },
        [".webp"] = new[] { "image/webp" },
        [".svg"] = new[] { "image/svg+xml" },

        [".pdf"] = new[] { "application/pdf" },

        [".doc"] = new[] { "application/msword" },
        [".docx"] = new[]
        {
            "application/vnd.openxmlformats-officedocument.wordprocessingml.document"
        },

        [".xls"] = new[] { "application/vnd.ms-excel" },
        [".xlsx"] = new[]
        {
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
        },

        [".zip"] = new[] { "application/zip" },

        [".txt"] = new[] { "text/plain" },
        [".log"] = new[] { "text/plain" },
        [".json"] = new[] { "application/json", "text/json" },
        [".xml"] = new[] { "application/xml", "text/xml" },

        [".mp4"] = new[] { "video/mp4" }
    };

    private static readonly HashSet<string> ForbiddenExtensions =new(StringComparer.OrdinalIgnoreCase)
        {
            ".exe",
            ".sh",
            ".bat",
            ".cmd",
            ".ps1",
            ".dll",
            ".com",
            ".msi",
            ".scr",
            ".vbs",
            ".js"
        };

    public async Task ValidateAsync(Stream fileStream,string fileName,string contentType)
    {
        // 1. Vérifier le fichier
        if (fileStream == null)
            throw new InvalidDataException("Aucun fichier fourni.");

        if (string.IsNullOrWhiteSpace(fileName))
            throw new InvalidDataException("Nom du fichier obligatoire.");

        // 2. Vérifier la taille
        if (fileStream.Length > MaxFileSize)
        {
            throw new InvalidDataException(
                "FILE_TOO_LARGE");
        }

        // 3. Récupérer l'extension
        var extension = Path.GetExtension(fileName);

        if (string.IsNullOrWhiteSpace(extension))
        {
            throw new InvalidDataException(
                "FILE_TYPE_NOT_ALLOWED");
        }

        extension = extension.ToLowerInvariant();

        // 4. Refuser les extensions dangereuses
        if (ForbiddenExtensions.Contains(extension))
        {
            throw new InvalidDataException(
                "FILE_TYPE_NOT_ALLOWED");
        }

        // 5. Vérifier que l'extension est autorisée
        if (!AllowedMimeTypes.TryGetValue(
                extension,
                out var allowedMimeTypes))
        {
            throw new InvalidDataException(
                "FILE_TYPE_NOT_ALLOWED");
        }

        // 6. Vérifier le MIME type
        if (!allowedMimeTypes.Contains(
                contentType,
                StringComparer.OrdinalIgnoreCase))
        {
            throw new InvalidDataException(
                "FILE_TYPE_NOT_ALLOWED");
        }

        // 7. MP4 : maximum 10 MB
        if (extension == ".mp4" &&
            fileStream.Length > MaxMp4Size)
        {
            throw new InvalidDataException(
                "FILE_TOO_LARGE");
        }

        // 8. Vérifier les Magic Bytes
        if (fileStream.CanSeek)
            fileStream.Position = 0;

        var isValid = await ValidateMagicBytesAsync(
            fileStream,
            extension);

        if (!isValid)
        {
            throw new InvalidDataException(
                "FILE_TYPE_NOT_ALLOWED");
        }

        // 9. Revenir au début du fichier
        if (fileStream.CanSeek)
            fileStream.Position = 0;
    }

    private static async Task<bool> ValidateMagicBytesAsync(Stream stream,string extension)
    {
        var buffer = new byte[16];

        var bytesRead = await stream.ReadAsync(buffer);

        if (bytesRead == 0)
            return false;

        return extension switch
        {
            ".jpg" or ".jpeg" =>
                IsJpeg(buffer, bytesRead),

            ".png" =>
                IsPng(buffer, bytesRead),

            ".gif" =>
                IsGif(buffer, bytesRead),

            ".webp" =>
                IsWebp(buffer, bytesRead),

            ".pdf" =>
                IsPdf(buffer, bytesRead),

            ".zip" =>
                IsZip(buffer, bytesRead),

            ".mp4" =>
                IsMp4(buffer, bytesRead),

            ".svg" =>
                IsSvg(buffer, bytesRead),

            ".txt" or ".log" or ".json" or ".xml" =>
                IsTextFile(buffer, bytesRead),

            ".doc" =>
                IsOleCompoundFile(buffer, bytesRead),

            ".docx" or ".xlsx" =>
                IsZip(buffer, bytesRead),

            _ => false
        };
    }

    private static bool IsJpeg(byte[] buffer, int length)
    {
        return length >= 3 &&
               buffer[0] == 0xFF &&
               buffer[1] == 0xD8 &&
               buffer[2] == 0xFF;
    }

    private static bool IsPng(byte[] buffer, int length)
    {
        return length >= 8 &&
               buffer[0] == 0x89 &&
               buffer[1] == 0x50 &&
               buffer[2] == 0x4E &&
               buffer[3] == 0x47 &&
               buffer[4] == 0x0D &&
               buffer[5] == 0x0A &&
               buffer[6] == 0x1A &&
               buffer[7] == 0x0A;
    }

    private static bool IsGif(byte[] buffer, int length)
    {
        return length >= 6 &&
               buffer[0] == 'G' &&
               buffer[1] == 'I' &&
               buffer[2] == 'F' &&
               buffer[3] == '8' &&
               (buffer[4] == '7' || buffer[4] == '9') &&
               buffer[5] == 'a';
    }

    private static bool IsWebp(byte[] buffer, int length)
    {
        return length >= 12 &&
               buffer[0] == 'R' &&
               buffer[1] == 'I' &&
               buffer[2] == 'F' &&
               buffer[3] == 'F' &&
               buffer[8] == 'W' &&
               buffer[9] == 'E' &&
               buffer[10] == 'B' &&
               buffer[11] == 'P';
    }

    private static bool IsPdf(byte[] buffer, int length)
    {
        return length >= 5 &&
               buffer[0] == '%' &&
               buffer[1] == 'P' &&
               buffer[2] == 'D' &&
               buffer[3] == 'F' &&
               buffer[4] == '-';
    }

    private static bool IsZip(byte[] buffer, int length)
    {
        return length >= 4 &&
               buffer[0] == 0x50 &&
               buffer[1] == 0x4B &&
               buffer[2] == 0x03 &&
               buffer[3] == 0x04;
    }

    private static bool IsMp4(byte[] buffer, int length)
    {
        // MP4 possède généralement "ftyp"
        // aux octets 4 à 7.
        return length >= 8 &&
               buffer[4] == 'f' &&
               buffer[5] == 't' &&
               buffer[6] == 'y' &&
               buffer[7] == 'p';
    }

    private static bool IsSvg(byte[] buffer, int length)
    {
        var text = System.Text.Encoding.UTF8
            .GetString(buffer, 0, length)
            .TrimStart();

        return text.StartsWith("<svg",
            StringComparison.OrdinalIgnoreCase) ||
               text.StartsWith("<?xml",
            StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsTextFile(byte[] buffer, int length)
    {
        for (int i = 0; i < length; i++)
        {
            byte b = buffer[i];

            if (b == 0)
                return false;
        }

        return true;
    }

    private static bool IsOleCompoundFile(byte[] buffer,int length)
    {
        return length >= 8 &&
               buffer[0] == 0xD0 &&
               buffer[1] == 0xCF &&
               buffer[2] == 0x11 &&
               buffer[3] == 0xE0 &&
               buffer[4] == 0xA1 &&
               buffer[5] == 0xB1 &&
               buffer[6] == 0x1A &&
               buffer[7] == 0xE1;
    }
}