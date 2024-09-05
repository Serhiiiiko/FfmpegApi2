using System.Diagnostics;

namespace FfmpegApi2.Services;

public class FFmpegService
{
    private readonly ILogger<FFmpegService> _logger;

    public FFmpegService(ILogger<FFmpegService> logger)
    {
        _logger = logger;
    }

    public async Task ExecuteFFmpegCommandAsync(string command)
    {
        var processInfo = new ProcessStartInfo("bash", $"-c \"{command}\"")
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using (var process = Process.Start(processInfo))
        {
            var output = await process.StandardOutput.ReadToEndAsync();
            var error = await process.StandardError.ReadToEndAsync();
            _logger.LogInformation(output);
            _logger.LogError(error);
            process.WaitForExit();
        }
    }
}