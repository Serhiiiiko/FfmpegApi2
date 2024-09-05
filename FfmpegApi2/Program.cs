using FfmpegApi2.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.WebHost.ConfigureKestrel(options => options.Limits.MaxRequestBodySize = long.MaxValue); // Allows for even larger files

// Register services
builder.Services.AddSingleton<FileService>();
builder.Services.AddSingleton<FFmpegService>();
builder.Services.AddSingleton<TranscriptionService>(sp => new TranscriptionService("ws://vosk-en:2700"));
builder.Services.AddSingleton<VideoProcessingService>();

builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseWebSockets();
app.UseAuthorization();
app.MapControllers();
app.Run();
