using FfmpegApi2.Models;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace FfmpegApi2.Services;

public class FileService
{
    private readonly string _uploadPath;

    public FileService()
    {
        _uploadPath = Path.Combine(Directory.GetCurrentDirectory(), "uploads");
        if (!Directory.Exists(_uploadPath))
        {
            Directory.CreateDirectory(_uploadPath);
        }
    }


    public void ClearUploadDirectory()
    {
        if (Directory.Exists(_uploadPath))
        {
            var files = Directory.GetFiles(_uploadPath);
            foreach (var file in files)
            {
                System.IO.File.Delete(file);
            }
        }
    }

    public async Task<string> SaveFileAsync(IFormFile file)
    {
        var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(file.FileName).ToLower();

        fileNameWithoutExtension = fileNameWithoutExtension.Replace(" ", "_");

        fileNameWithoutExtension = Regex.Replace(fileNameWithoutExtension, @"[^a-zA-Z0-9_]", "");

        var uniqueName = Guid.NewGuid().ToString();

        var fileExtension = Path.GetExtension(file.FileName).ToLower();

        var uniqueFileName = $"{fileNameWithoutExtension}_{uniqueName}{fileExtension}";

        var filePath = Path.Combine(_uploadPath, uniqueFileName);

        await using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        return filePath;
    }

    public string GenerateFilePath(string fileNameWithoutExtension, string extension)
    {
        return Path.Combine(_uploadPath, fileNameWithoutExtension + extension);
    }
}