namespace FfmpegApi2.Models;


public class SRTEntry
{
    public int SequenceNumber { get; set; }
    public double StartTime { get; set; }
    public double EndTime { get; set; }
    public string Text { get; set; }
}