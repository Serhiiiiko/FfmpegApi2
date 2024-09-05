using System.Globalization;
using System.Net.WebSockets;
using System.Text;
using FfmpegApi2.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace FfmpegApi2.Services
{
    public class TranscriptionService
    {
        private readonly string _voskServerUrl;
        private List<SRTEntry> srtEntries = new List<SRTEntry>();
        private StringBuilder accumulatedJson = new StringBuilder();

        public TranscriptionService(string voskServerUrl)
        {
            _voskServerUrl = voskServerUrl;
        }

        public async Task<string> TranscribeAudioAsync(string audioFilePath)
        {
            using (var ws = new ClientWebSocket())
            {
                await ws.ConnectAsync(new Uri(_voskServerUrl), CancellationToken.None);

                await using (var fsSource = new FileStream(audioFilePath, FileMode.Open, FileAccess.Read))
                {
                    var data = new byte[8000];
                    while (true)
                    {
                        var count = await fsSource.ReadAsync(data, 0, 8000);
                        if (count == 0)
                            break;

                        await ws.SendAsync(new ArraySegment<byte>(data, 0, count), WebSocketMessageType.Binary, true, CancellationToken.None);
                        await ReceiveResult(ws);
                    }
                }

                await SendFinalData(ws);
                await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "OK", CancellationToken.None);

                string srtFilePath = GenerateSRTFile(Path.Combine(Path.GetDirectoryName(audioFilePath), Path.GetFileNameWithoutExtension(audioFilePath) + ".srt"));
                return srtFilePath;
            }
        }

        private async Task ReceiveResult(ClientWebSocket ws)
        {
            var buffer = new byte[4096];
            var result = await ws.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
            var receivedString = Encoding.UTF8.GetString(buffer, 0, result.Count).Trim();

            if (string.IsNullOrEmpty(receivedString))
            {
                Console.WriteLine("Received an empty JSON string. Ignoring this input.");
                return;
            }

            accumulatedJson.Append(receivedString);

            if (IsJsonComplete(accumulatedJson.ToString()))
            {
                try
                {
                    ParseAndStoreNoPhrase(accumulatedJson.ToString());
                }
                catch (JsonReaderException ex)
                {
                    Console.WriteLine($"JSON Parsing Error: {ex.Message}");
                }
                finally
                {
                    accumulatedJson.Clear();
                }
            }
        }

        private bool IsJsonComplete(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return false;
            }

            json = json.Trim();
            if (json.StartsWith("{") && json.EndsWith("}"))
            {
                try
                {
                    JObject.Parse(json);
                    return true;
                }
                catch (JsonReaderException)
                {
                    return false;
                }
            }
            return false;
        }

        private void ParseAndStorePhrase(string receivedString)
        {
            try
            {
                var json = JObject.Parse(receivedString);

                if (json["result"] != null)
                {
                    StringBuilder phrase = new StringBuilder();
                    double? phraseStartTime = null;
                    double? phraseEndTime = null;

                    foreach (var word in json["result"])
                    {
                        var text = word["word"].ToString();
                        var startTime = (double)word["start"];
                        var endTime = (double)word["end"];

                        // Initialize phrase start time
                        if (phrase.Length == 0)
                        {
                            phraseStartTime = startTime;
                        }

                        phrase.Append(text + " ");
                        phraseEndTime = endTime;

                        // Check for logical phrase boundaries
                        if (text.EndsWith(".") || text.EndsWith("?") || text.EndsWith("!") || text.EndsWith(",") || text.EndsWith(";"))
                        {
                            // Check for sentence-ending punctuation
                            AddPhraseEntry(phrase, phraseStartTime.Value, phraseEndTime.Value);
                            phrase.Clear();
                            phraseStartTime = null;
                            phraseEndTime = null;
                        }
                        else if (phrase.Length > 150) // Optionally limit length to avoid excessively long phrases
                        {
                            // Handle long phrases without punctuation
                            AddPhraseEntry(phrase, phraseStartTime.Value, phraseEndTime.Value);
                            phrase.Clear();
                            phraseStartTime = null;
                            phraseEndTime = null;
                        }
                    }

                    // Add the last phrase if any remaining
                    if (phrase.Length > 0)
                    {
                        AddPhraseEntry(phrase, phraseStartTime.Value, phraseEndTime.Value);
                    }
                }
            }
            catch (JsonReaderException ex)
            {
                Console.WriteLine($"JSON Parsing Error: {ex.Message}");
            }
        }

        private void AddPhraseEntry(StringBuilder phrase, double startTime, double endTime)
        {
            srtEntries.Add(new SRTEntry
            {
                SequenceNumber = srtEntries.Count + 1,
                StartTime = startTime,
                EndTime = endTime,
                Text = phrase.ToString().Trim()
            });
        }

        private void ParseAndStoreNoPhrase(string receivedString)
        {
            try
            {
                var json = JObject.Parse(receivedString);

                if (json["result"] != null)
                {
                    foreach (var word in json["result"])
                    {
                        var text = word["word"].ToString().ToLower();
                        var startTime = (double)word["start"];
                        var endTime = (double)word["end"];
                        var duration = endTime - startTime;

                        // Skip placeholder words and very short words
                        if (IsPlaceholderWord(text) )
                        {
                            continue;
                        }

                        srtEntries.Add(new SRTEntry
                        {
                            SequenceNumber = srtEntries.Count + 1,
                            StartTime = startTime,
                            EndTime = endTime,
                            Text = text
                        });
                    }
                }
            }
            catch (JsonReaderException ex)
            {
                Console.WriteLine($"JSON Parsing Error: {ex.Message}");
            }
        }

        private bool IsPlaceholderWord(string word)
        {
            // Add more words if needed, based on what Vosk commonly outputs during silence
            var placeholders = new HashSet<string> { "the", "uh", "um", "a", "an", "yeah" };
            return placeholders.Contains(word);
        }

        private async Task SendFinalData(ClientWebSocket ws)
        {
            var eof = Encoding.UTF8.GetBytes("{\"eof\" : 1}");
            await ws.SendAsync(new ArraySegment<byte>(eof), WebSocketMessageType.Text, true, CancellationToken.None);
        }

        private string GenerateSRTFile(string outputPath)
        {
            using (StreamWriter sw = new StreamWriter(outputPath))
            {
                foreach (var entry in srtEntries)
                {
                    sw.WriteLine(entry.SequenceNumber);
                    sw.WriteLine($"{ConvertToSRTTimeFormat(entry.StartTime)} --> {ConvertToSRTTimeFormat(entry.EndTime)}");
                    sw.WriteLine(entry.Text);
                    sw.WriteLine();
                }
            }
            Console.WriteLine("SRT file generated successfully.");
            return outputPath;
        }

        private string ConvertToSRTTimeFormat(double timeInSeconds)
        {
            TimeSpan time = TimeSpan.FromSeconds(timeInSeconds);
            return time.ToString(@"hh\:mm\:ss\,fff", CultureInfo.InvariantCulture);
        }
    }
}
