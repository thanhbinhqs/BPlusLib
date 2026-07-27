# Media

Reads basic media metadata from common audio file formats (MP3, WAV) using pure file-header parsing. No external dependencies or COM interop required.

## Classes

### MediaInfo
Represents metadata about a media file, including audio/video codec info, duration, bitrate, sample rate, and ID3 tag fields.

| Property | Returns | Description |
|----------|---------|-------------|
| FilePath | string | The full path to the media file |
| Duration | TimeSpan? | The duration of the media, if known |
| Bitrate | int? | The bitrate in bits per second, if known |
| SampleRate | int? | The audio sample rate in Hz, if known |
| VideoWidth | int? | The video width in pixels, if known |
| VideoHeight | int? | The video height in pixels, if known |
| VideoCodec | string? | The video codec name, if known |
| AudioCodec | string? | The audio codec name, if known |
| Title | string? | Title from metadata tags |
| Artist | string? | Artist from metadata tags |
| Album | string? | Album from metadata tags |
| TrackNumber | int? | Track number, if present |
| Year | int? | Release year, if present |
| Genre | string? | Genre string, if present |
| Comment | string? | Comment field, if present |

### MediaInfoReader
Reads basic media metadata from common file formats using pure file-header parsing. Thread-safe (all methods are stateless).

Supported formats:
- **MP3** — ID3v1 and ID3v2 tag parsing, Xing/Info header for bitrate and duration estimation
- **WAV** — RIFF header parsing for sample rate, bit depth, channels, and duration

| Method | Returns | Description |
|--------|---------|-------------|
| Read(string filePath) | MediaInfo? | Reads media metadata from the specified file |
| IsAudioFile(string extension) | bool | Determines whether the extension is a known audio file type |
| IsVideoFile(string extension) | bool | Determines whether the extension is a known video file type |

## Usage

```csharp
using BPlusLib.Foundation.Media;

// Read metadata from an MP3 file
var info = MediaInfoReader.Read(@"C:\Music\song.mp3");
if (info != null)
{
    Console.WriteLine($"Title: {info.Title}");
    Console.WriteLine($"Artist: {info.Artist}");
    Console.WriteLine($"Duration: {info.Duration}");
    Console.WriteLine($"Bitrate: {info.Bitrate} bps");
    Console.WriteLine($"Album: {info.Album}");
}

// Read metadata from a WAV file
var wavInfo = MediaInfoReader.Read(@"C:\Audio\recording.wav");
Console.WriteLine($"Sample Rate: {wavInfo?.SampleRate} Hz");

// Check file types
bool isAudio = MediaInfoReader.IsAudioFile(".mp3");  // true
bool isVideo = MediaInfoReader.IsVideoFile(".mkv");  // true
```

## Dependencies
- No external NuGet packages required
- Pure managed C# file-header parsing
- Cross-platform
