namespace FfmpegApi2.Services;

public class VideoProcessingService
{
    private readonly FFmpegService _ffmpegService;
    private readonly TranscriptionService _transcriptionService;
    private readonly FileService _fileService;

    public VideoProcessingService(FFmpegService ffmpegService, TranscriptionService transcriptionService, FileService fileService)
    {
        _ffmpegService = ffmpegService;
        _transcriptionService = transcriptionService;
        _fileService = fileService;
    }

    public async Task<string> ProcessVideoAsync(string videoFilePath)
    {
        var extractAudioTask = ExtractAudioAsync(videoFilePath);

        var formatVideoTask = FormatVideoAsync(videoFilePath);

        var audioFilePath = await extractAudioTask;
        var transcribeTask = TranscribeAudioAsync(audioFilePath);

        var formatedVideoPath = await formatVideoTask;
        var srtFilePath = await transcribeTask;

        var subtitledVideoPath = await OverlaySubtitlesAsync(formatedVideoPath, srtFilePath);

        return subtitledVideoPath;
    }

    private async Task<string> ExtractAudioAsync(string videoFilePath)
    {
        var audioFilePath = _fileService.GenerateFilePath(Path.GetFileNameWithoutExtension(videoFilePath), ".wav");
        var ffmpegExtractAudioCmd = $"ffmpeg -i \"{videoFilePath}\" -ac 1 -ar 8000 -c:a pcm_s16le \"{audioFilePath}\"";
        await _ffmpegService.ExecuteFFmpegCommandAsync(ffmpegExtractAudioCmd);
        return audioFilePath;
    }

    private async Task<string> FormatVideoAsync(string videoFilePath)
    {
        var formatedVideoPath = _fileService.GenerateFilePath(Path.GetFileNameWithoutExtension(videoFilePath), "_formated.mp4");
        var formatedOverlayCmd = $"ffmpeg -i \"{videoFilePath}\" -vf 'split[original][copy]; [copy]scale=1080:1920:force_original_aspect_ratio=increase,crop=1080:1920,boxblur=luma_radius=20:luma_power=1.5[blurred]; [original]scale=iw*1.83:ih*1.83[original_scaled]; [blurred][original_scaled]overlay=(main_w-overlay_w)/2:(main_h-overlay_h)/2' \"{formatedVideoPath}\"";
        await _ffmpegService.ExecuteFFmpegCommandAsync(formatedOverlayCmd);
        return formatedVideoPath;
    }

    private async Task<string> OverlaySubtitlesAsync(string videoFilePath, string srtFilePath)
    {
        var subtitledVideoPath = _fileService.GenerateFilePath(Path.GetFileNameWithoutExtension(videoFilePath), "_subtitled.mp4");
        var ffmpegOverlayCmd = $"ffmpeg -i \"{videoFilePath}\" -vf \"subtitles={srtFilePath}:force_style=\\'FontName=Verdana,MarginV=65,FontWeight=Bold,Fontsize=24\\'\" \"{subtitledVideoPath}\"";
        await _ffmpegService.ExecuteFFmpegCommandAsync(ffmpegOverlayCmd);
        return subtitledVideoPath;
    }

    private async Task<string> TranscribeAudioAsync(string audioFilePath)
    {
        return await _transcriptionService.TranscribeAudioAsync(audioFilePath);
    }
}


//var formatedVideoPath = _fileService.GenerateFilePath(Path.GetFileNameWithoutExtension(videoFilePath), "_formated.mp4");
//var formatedOverlayCmd = $"ffmpeg -i \"{videoFilePath}\" -vf 'split[original][copy]; [copy]scale=1080:1920:force_original_aspect_ratio=increase,crop=1080:1920,boxblur=luma_radius=20:luma_power=1.5[blurred]; [original]scale=iw*2:ih*2[original_scaled]; [blurred][original_scaled]overlay=(main_w-overlay_w)/2:(main_h-overlay_h)/2' \"{formatedVideoPath}\"";
//var formatedVideoTask = _ffmpegService.ExecuteFFmpegCommandAsync(formatedOverlayCmd);