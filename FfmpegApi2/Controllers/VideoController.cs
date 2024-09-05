using FfmpegApi2.Services;
using Microsoft.AspNetCore.Mvc;

namespace FfmpegApi2.Controllers
{
    [ApiController]
    //[DisableRequestSizeLimit]
    [Route("api/[controller]")]
    public class VideoController : ControllerBase
    {
        private readonly ILogger<VideoController> _logger;
        private readonly FileService _fileService;
        private readonly VideoProcessingService _videoProcessingService;

        public VideoController(ILogger<VideoController> logger, FileService fileService, VideoProcessingService videoProcessingService)
        {
            _logger = logger;
            _fileService = fileService;
            _videoProcessingService = videoProcessingService;
        }

        //[RequestFormLimits(ValueLengthLimit = int.MaxValue, MultipartBodyLengthLimit = int.MaxValue)]
        [HttpPost("upload")]
        public async Task<IActionResult> UploadVideo(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest("No file uploaded.");
            }

            _fileService.ClearUploadDirectory();

            var videoFilePath = await _fileService.SaveFileAsync(file);
            var subtitledVideoPath = await _videoProcessingService.ProcessVideoAsync(videoFilePath);

            return Ok(new { SubtitledVideoPath = subtitledVideoPath });
        }
    }
}
