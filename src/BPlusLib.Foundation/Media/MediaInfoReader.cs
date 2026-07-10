// <copyright file="MediaInfoReader.cs" company="BPlusLib">
// Copyright (c) BPlusLib. All rights reserved.
// Licensed under the MIT license.
// </copyright>

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace BPlusLib.Foundation.Media
{
    /// <summary>
    /// Represents metadata about a media file, including audio/video codec
    /// info, duration, bitrate, sample rate, and ID3 tag fields.
    /// </summary>
    public sealed class MediaInfo
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MediaInfo"/> class.
        /// </summary>
        /// <param name="filePath">The path of the media file.</param>
        internal MediaInfo(string filePath)
        {
            FilePath = filePath ?? throw new ArgumentNullException(nameof(filePath));
        }

        /// <summary>Gets the full path to the media file.</summary>
        public string FilePath { get; }

        /// <summary>Gets the duration of the media, if known.</summary>
        public TimeSpan? Duration { get; internal set; }

        /// <summary>Gets the bitrate in bits per second, if known.</summary>
        public int? Bitrate { get; internal set; }

        /// <summary>Gets the audio sample rate in Hz, if known.</summary>
        public int? SampleRate { get; internal set; }

        /// <summary>Gets the video width in pixels, if known.</summary>
        public int? VideoWidth { get; internal set; }

        /// <summary>Gets the video height in pixels, if known.</summary>
        public int? VideoHeight { get; internal set; }

        /// <summary>Gets the video codec name, if known.</summary>
        public string? VideoCodec { get; internal set; }

        /// <summary>Gets the audio codec name, if known.</summary>
        public string? AudioCodec { get; internal set; }

        /// <summary>Gets the title from metadata tags, if present.</summary>
        public string? Title { get; internal set; }

        /// <summary>Gets the artist from metadata tags, if present.</summary>
        public string? Artist { get; internal set; }

        /// <summary>Gets the album from metadata tags, if present.</summary>
        public string? Album { get; internal set; }

        /// <summary>Gets the track number, if present.</summary>
        public int? TrackNumber { get; internal set; }

        /// <summary>Gets the release year, if present.</summary>
        public int? Year { get; internal set; }

        /// <summary>Gets the genre string, if present.</summary>
        public string? Genre { get; internal set; }

        /// <summary>Gets the comment field, if present.</summary>
        public string? Comment { get; internal set; }
    }

    /// <summary>
    /// Reads basic media metadata from common file formats (MP3, WAV) using
    /// pure file-header parsing. No external dependencies or COM interop required.
    /// Thread-safe (all methods are stateless).
    /// </summary>
    /// <remarks>
    /// <para>
    /// Supported formats:
    /// <list type="bullet">
    ///   <item><description>MP3 — ID3v1 and ID3v2 tag parsing, Xing/Info header for bitrate and duration estimation.</description></item>
    ///   <item><description>WAV — RIFF header parsing for sample rate, bit depth, channels, and duration.</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// For video formats or unrecognized audio formats, <see cref="Read(string)"/> returns a
    /// <see cref="MediaInfo"/> with only <see cref="MediaInfo.FilePath"/> populated.
    /// </para>
    /// </remarks>
    public static class MediaInfoReader
    {
        // -----------------------------------------------------------------
        // Constants
        // -----------------------------------------------------------------

        private const int Id3v1TagSize = 128;
        private const int Id3v1HeaderOffset = -128;
        private const string Id3v1Signature = "TAG";
        private const string Id3v2Signature = "ID3";
        private const string RiffSignature = "RIFF";
        private const string WaveSignature = "WAVE";
        private const string RiffFmtChunk = "fmt ";
        private const string RiffDataChunk = "data";

        private static readonly HashSet<string> AudioExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".mp3", ".wav", ".flac", ".ogg", ".m4a", ".wma",
        };

        private static readonly HashSet<string> VideoExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".mp4", ".avi", ".mkv", ".mov", ".wmv", ".flv",
        };

        /// <summary>
        /// A mapping from ID3v1 genre byte values to human-readable genre names.
        /// This covers the standard (ID3v1) genres (0–79) plus common Winamp
        /// extensions (80–147).
        /// </summary>
        private static readonly string[] Id3v1Genres =
        {
            "Blues", "Classic Rock", "Country", "Dance", "Disco", "Funk",
            "Grunge", "Hip-Hop", "Jazz", "Metal", "New Age", "Oldies",
            "Other", "Pop", "R&B", "Rap", "Reggae", "Rock", "Techno",
            "Industrial", "Alternative", "Ska", "Death Metal", "Pranks",
            "Soundtrack", "Euro-Techno", "Ambient", "Trip-Hop", "Vocal",
            "Jazz+Funk", "Fusion", "Trance", "Classical", "Instrumental",
            "Acid", "House", "Game", "Sound Clip", "Gospel", "Noise",
            "AlternRock", "Bass", "Soul", "Punk", "Space", "Meditative",
            "Instrumental Pop", "Instrumental Rock", "Ethnic", "Gothic",
            "Darkwave", "Techno-Industrial", "Electronic", "Pop-Folk",
            "Eurodance", "Dream", "Southern Rock", "Comedy", "Cult",
            "Gangsta", "Top 40", "Christian Rap", "Pop/Funk", "Jungle",
            "Native American", "Cabaret", "New Wave", "Psychadelic",
            "Rave", "Showtunes", "Trailer", "Lo-Fi", "Tribal",
            "Acid Punk", "Acid Jazz", "Polka", "Retro", "Musical",
            "Rock & Roll", "Hard Rock",
            // Winamp extended genres (80–147)
            "Folk", "Folk-Rock", "National Folk", "Swing", "Fast Fusion",
            "Bebob", "Latin", "Revival", "Celtic", "Bluegrass",
            "Avantgarde", "Gothic Rock", "Progressive Rock",
            "Psychedelic Rock", "Symphonic Rock", "Slow Rock", "Big Band",
            "Chorus", "Easy Listening", "Acoustic", "Humour", "Speech",
            "Chanson", "Opera", "Chamber Music", "Sonata", "Symphony",
            "Booty Bass", "Primus", "Porn Groove", "Satire", "Slow Jam",
            "Club", "Tango", "Samba", "Folklore", "Ballad",
            "Power Ballad", "Rhythmic Soul", "Freestyle", "Duet",
            "Punk Rock", "Drum Solo", "A Cappella", "Euro-House",
            "Dance Hall", "Goa", "Drum & Bass", "Club-House",
            "Hardcore", "Terror", "Indie", "BritPop", "Negerpunk",
            "Polsk Punk", "Beat", "Christian Gangsta Rap", "Heavy Metal",
            "Black Metal", "Crossover", "Contemporary Christian",
            "Christian Rock", "Merengue", "Salsa", "Thrash Metal",
            "Anime", "JPop", "Synthpop",
        };

        // -----------------------------------------------------------------
        // Public API
        // -----------------------------------------------------------------

        /// <summary>
        /// Reads media metadata from the specified file.
        /// </summary>
        /// <param name="filePath">The path to the media file.</param>
        /// <returns>
        /// A <see cref="MediaInfo"/> instance with populated fields, or
        /// <see langword="null"/> if the file could not be read.
        /// </returns>
        public static MediaInfo? Read(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
                return null;

            if (!File.Exists(filePath))
                return null;

            try
            {
                string extension = Path.GetExtension(filePath);

                if (string.IsNullOrEmpty(extension))
                    return null;

                MediaInfo info = new MediaInfo(Path.GetFullPath(filePath));

                if (string.Equals(extension, ".mp3", StringComparison.OrdinalIgnoreCase))
                {
                    ParseMp3(filePath, info);
                }
                else if (string.Equals(extension, ".wav", StringComparison.OrdinalIgnoreCase))
                {
                    ParseWav(filePath, info);
                }
                else
                {
                    // For unsupported formats, just return the file path info.
                }

                return info;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Determines whether the specified file extension corresponds to a
        /// known audio file type.
        /// </summary>
        /// <param name="extension">The file extension (e.g. ".mp3").</param>
        /// <returns>
        /// <see langword="true"/> if the extension is a recognized audio format;
        /// otherwise <see langword="false"/>.
        /// </returns>
        public static bool IsAudioFile(string extension)
        {
            if (string.IsNullOrEmpty(extension))
                return false;

            string ext = extension.TrimStart('.');
            return AudioExtensions.Contains("." + ext);
        }

        /// <summary>
        /// Determines whether the specified file extension corresponds to a
        /// known video file type.
        /// </summary>
        /// <param name="extension">The file extension (e.g. ".mp4").</param>
        /// <returns>
        /// <see langword="true"/> if the extension is a recognized video format;
        /// otherwise <see langword="false"/>.
        /// </returns>
        public static bool IsVideoFile(string extension)
        {
            if (string.IsNullOrEmpty(extension))
                return false;

            string ext = extension.TrimStart('.');
            return VideoExtensions.Contains("." + ext);
        }

        // -----------------------------------------------------------------
        // MP3 Parsing
        // -----------------------------------------------------------------

        /// <summary>
        /// Parses an MP3 file, populating <paramref name="info"/> with ID3
        /// tags and Xing/Info header data.
        /// </summary>
        private static void ParseMp3(string filePath, MediaInfo info)
        {
            byte[] fileBytes = File.ReadAllBytes(filePath);
            if (fileBytes.Length < 4)
                return;

            // Try ID3v2 (at start of file) first — it contains richer data
            // than ID3v1 and may override v1 fields.
            int id3v2Size = TryParseId3v2(fileBytes, info);

            // Try ID3v1 (128 bytes at end of file)
            if (fileBytes.Length >= Id3v1TagSize)
            {
                TryParseId3v1(fileBytes, info);
            }

            // Try to find a Xing/Info header for bitrate and duration estimation.
            // Start scanning after the ID3v2 tag (if present), looking for
            // a valid MPEG sync word (0xFFE0+).
            int scanStart = (id3v2Size > 0) ? id3v2Size : 0;
            TryParseXingHeader(fileBytes, scanStart, filePath, info);

            // If we have a bitrate but no duration, estimate from file size.
            if (info.Bitrate.HasValue && !info.Duration.HasValue && info.Bitrate.Value > 0)
            {
                long audioDataSize = fileBytes.Length - id3v2Size - Id3v1TagSize;
                if (audioDataSize > 0)
                {
                    // Duration (seconds) = (file_size_in_bits) / bitrate
                    double totalBits = audioDataSize * 8L;
                    double durationSec = totalBits / info.Bitrate.Value;
                    if (durationSec > 0 && durationSec < 86400) // Sanity: < 24 hours
                    {
                        info.Duration = TimeSpan.FromSeconds(durationSec);
                    }
                }
            }
        }

        /// <summary>
        /// Parses an ID3v2 tag from the beginning of the file data.
        /// Returns the total size of the ID3v2 tag (header + extended header + frames),
        /// or 0 if no valid ID3v2 tag was found.
        /// </summary>
        private static int TryParseId3v2(byte[] data, MediaInfo info)
        {
            if (data.Length < 10)
                return 0;

            // ID3v2 header: 3 bytes signature, 1 byte version major, 1 byte version minor,
            // 1 byte flags, 4 bytes size (synchsafe integer)
            if (data[0] != (byte)'I' || data[1] != (byte)'D' || data[2] != (byte)'3')
                return 0;

            int tagSize = ReadSynchSafeInt(data, 6);
            if (tagSize <= 0 || data.Length < tagSize + 10)
                return 0;

            // Total size is 10-byte header + tag size
            int totalSize = tagSize + 10;
            int pos = 10; // Start after the header

            // Skip extended header if present (flag byte 0x40)
            bool hasExtendedHeader = (data[5] & 0x40) != 0;
            if (hasExtendedHeader)
            {
                if (pos + 4 > data.Length)
                    return totalSize;

                int extHeaderSize = ReadSynchSafeInt(data, pos);
                if (extHeaderSize < 4)
                    return totalSize;

                pos += extHeaderSize + 4; // +4 because size field itself is 4 bytes
            }

            // Parse frames
            while (pos + 10 <= data.Length)
            {
                // Frame header: 4 bytes frame ID, 4 bytes size, 2 bytes flags
                string frameId = Encoding.ASCII.GetString(data, pos, 4);

                // A frame ID is ASCII alphanumeric. If we hit garbage (e.g. zeros), stop.
                if (!IsAsciiAlphanumeric(frameId))
                    break;

                int frameSize = ReadBigEndian32(data, pos + 4);
                if (frameSize <= 0 || pos + 10 + frameSize > data.Length)
                    break;

                // Skip frame flags
                pos += 10;

                string? value = DecodeId3v2FrameData(data, pos, frameSize);

                if (!string.IsNullOrEmpty(value))
                {
                    StoreId3Frame(frameId, value!, info);
                }

                pos += frameSize;
            }

            return totalSize;
        }

        /// <summary>
        /// Decodes an ID3v2 frame payload to a string, handling text encoding
        /// (Latin-1 or UTF-16).
        /// </summary>
        private static string? DecodeId3v2FrameData(byte[] data, int offset, int size)
        {
            if (size < 1)
                return null;

            // First byte is the encoding byte
            byte encodingByte = data[offset];
            int contentOffset = offset + 1;
            int contentSize = size - 1;

            if (contentSize <= 0)
                return null;

            // Trim null terminators and trailing whitespace
            int actualLength = contentSize;
            while (actualLength > 0 && data[contentOffset + actualLength - 1] == 0)
                actualLength--;

            if (actualLength <= 0)
                return null;

            try
            {
                switch (encodingByte)
                {
                    case 0: // ISO-8859-1 (Latin-1)
                        return Latin1Encoding.GetString(data, contentOffset, actualLength).Trim();

                    case 1: // UTF-16 with BOM
                    case 2: // UTF-16BE without BOM
                        return DecodeUtf16(data, contentOffset, actualLength).Trim();

                    case 3: // UTF-8
                        return Encoding.UTF8.GetString(data, contentOffset, actualLength).Trim();

                    default:
                        return Latin1Encoding.GetString(data, contentOffset, actualLength).Trim();
                }
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Decodes a UTF-16 encoded byte sequence, respecting BOM if present.
        /// </summary>
        private static string DecodeUtf16(byte[] data, int offset, int length)
        {
            if (length < 2)
                return string.Empty;

            // Check for BOM
            if (data[offset] == 0xFF && data[offset + 1] == 0xFE)
            {
                // Little-endian UTF-16
                return Encoding.Unicode.GetString(data, offset + 2, length - 2);
            }

            if (data[offset] == 0xFE && data[offset + 1] == 0xFF)
            {
                // Big-endian UTF-16
                return Encoding.BigEndianUnicode.GetString(data, offset + 2, length - 2);
            }

            // No BOM: assume UTF-16 little-endian (common default)
            return Encoding.Unicode.GetString(data, offset, length);
        }

        /// <summary>
        /// Stores a parsed ID3v2 frame value into the appropriate
        /// <see cref="MediaInfo"/> property based on the frame ID.
        /// </summary>
        private static void StoreId3Frame(string frameId, string value, MediaInfo info)
        {
            switch (frameId)
            {
                case "TIT2": // Title/song name
                case "TT2":  // ID3v2.2
                    if (string.IsNullOrEmpty(info.Title))
                        info.Title = value;
                    break;

                case "TPE1": // Lead performer(s)
                case "TP1":  // ID3v2.2
                    if (string.IsNullOrEmpty(info.Artist))
                        info.Artist = value;
                    break;

                case "TALB": // Album
                case "TAL":  // ID3v2.2
                    if (string.IsNullOrEmpty(info.Album))
                        info.Album = value;
                    break;

                case "TRCK": // Track number
                case "TRK":  // ID3v2.2
                    if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int track))
                    {
                        info.TrackNumber = track;
                    }
                    else
                    {
                        // Handle "N/M" format
                        int slash = value.IndexOf('/');
                        if (slash > 0 && int.TryParse(value.Substring(0, slash), NumberStyles.Integer, CultureInfo.InvariantCulture, out track))
                        {
                            info.TrackNumber = track;
                        }
                    }

                    break;

                case "TYER": // Year
                case "TDRC": // Recording date (ID3v2.4)
                    if (value.Length >= 4 &&
                        int.TryParse(value.Substring(0, 4), NumberStyles.Integer, CultureInfo.InvariantCulture, out int year))
                    {
                        info.Year = year;
                    }

                    break;

                case "TCON": // Genre
                case "TCO":  // ID3v2.2
                    info.Genre = value;
                    break;

                case "COMM": // Comment
                    if (string.IsNullOrEmpty(info.Comment))
                        info.Comment = value;
                    break;

                case "TBPM": // Beats per minute
                case "TCOM": // Composer
                case "TEXT": // Lyricist
                case "TPUB": // Publisher
                    // Stored for completeness but not currently surfaced in properties
                    break;
            }
        }

        /// <summary>
        /// Parses an ID3v1 tag (128 bytes at the end of the file).
        /// </summary>
        private static void TryParseId3v1(byte[] data, MediaInfo info)
        {
            int tagStart = data.Length - Id3v1TagSize;
            if (tagStart < 0)
                return;

            // Check signature "TAG"
            if (data[tagStart] != (byte)'T' ||
                data[tagStart + 1] != (byte)'A' ||
                data[tagStart + 2] != (byte)'G')
            {
                return;
            }

            // ID3v1 fields are fixed-length, null-terminated or space-padded.
            // Use only if the ID3v2 parser didn't already populate the field (ID3v2 takes priority).

            // Title: 30 bytes at offset +3
            if (string.IsNullOrEmpty(info.Title))
            {
                info.Title = ReadId3v1String(data, tagStart + 3, 30);
            }

            // Artist: 30 bytes at offset +33
            if (string.IsNullOrEmpty(info.Artist))
            {
                info.Artist = ReadId3v1String(data, tagStart + 33, 30);
            }

            // Album: 30 bytes at offset +63
            if (string.IsNullOrEmpty(info.Album))
            {
                info.Album = ReadId3v1String(data, tagStart + 63, 30);
            }

            // Year: 4 bytes at offset +93
            if (!info.Year.HasValue)
            {
                string yearStr = ReadId3v1String(data, tagStart + 93, 4);
                if (yearStr.Length == 4 &&
                    int.TryParse(yearStr, NumberStyles.Integer, CultureInfo.InvariantCulture, out int year))
                {
                    info.Year = year;
                }
            }

            // Comment: 30 bytes at offset +97 (or 28 if track number present)
            // ID3v1.1: if byte 125 is 0 and byte 126 is non-zero, then
            //   bytes 97-124 = comment (28 bytes), byte 125 = 0, byte 126 = track number
            int commentOffset = tagStart + 97;
            int commentLength = 30;

            if (data[tagStart + 125] == 0 && data[tagStart + 126] != 0)
            {
                // ID3v1.1: track number present
                commentLength = 28;

                if (!info.TrackNumber.HasValue)
                {
                    info.TrackNumber = data[tagStart + 126];
                }
            }

            if (string.IsNullOrEmpty(info.Comment))
            {
                info.Comment = ReadId3v1String(data, commentOffset, commentLength);
            }

            // Genre: 1 byte at offset +127
            byte genreByte = data[tagStart + 127];
            if (string.IsNullOrEmpty(info.Genre) && genreByte < Id3v1Genres.Length)
            {
                info.Genre = Id3v1Genres[genreByte];
            }
        }

        /// <summary>
        /// Attempts to locate and parse a Xing/Info header in the MP3 stream.
        /// The Xing header contains bitrate, sample rate, and frame count info
        /// used to compute accurate duration.
        /// </summary>
        private static void TryParseXingHeader(byte[] data, int startOffset, string filePath, MediaInfo info)
        {
            // Scan for a valid MPEG sync word (11 bits of 1s: 0xFFE0+)
            // then look for "Xing" or "Info" at offset 36 (MPEG1) or 21 (MPEG2).
            int searchLimit = Math.Min(startOffset + 4096, data.Length - 4);
            int pos = startOffset;

            while (pos < searchLimit)
            {
                // Look for 0xFF 0xE? sync
                if (data[pos] == 0xFF && (data[pos + 1] & 0xE0) == 0xE0)
                {
                    int header = (data[pos] << 24) | (data[pos + 1] << 16) |
                                 (data[pos + 2] << 8) | data[pos + 3];

                    // Extract bitrate index (bits 16-19)
                    int bitrateIndex = (header >> 12) & 0x0F;
                    int sampleRateIndex = (header >> 10) & 0x03;
                    int padding = (header >> 9) & 0x01;
                    int version = (header >> 19) & 0x03; // 11=MPEG1, 10=MPEG2, 01=reserved, 00=MPEG2.5
                    int layer = (header >> 17) & 0x03;   // 11=L1, 10=L2, 01=L3

                    if (bitrateIndex < 1 || bitrateIndex > 14 || sampleRateIndex > 2)
                    {
                        pos++;
                        continue;
                    }

                    // Determine side information size
                    int sideInfoSize;
                    int xingOffset;

                    if (version == 3) // MPEG1
                    {
                        sideInfoSize = (layer == 1) ? 32 : 17;
                        xingOffset = 36;
                    }
                    else // MPEG2 or MPEG2.5
                    {
                        sideInfoSize = (layer == 1) ? 17 : 9;
                        xingOffset = 21;
                    }

                    int xingPos = pos + xingOffset;
                    if (xingPos + 8 > data.Length)
                    {
                        pos++;
                        continue;
                    }

                    // Look for "Xing" or "Info" marker
                    if ((data[xingPos] == (byte)'X' && data[xingPos + 1] == (byte)'i' &&
                         data[xingPos + 2] == (byte)'n' && data[xingPos + 3] == (byte)'g') ||
                        (data[xingPos] == (byte)'I' && data[xingPos + 1] == (byte)'n' &&
                         data[xingPos + 2] == (byte)'f' && data[xingPos + 3] == (byte)'o'))
                    {
                        // Found Xing/Info header
                        int flags = ReadBigEndian32(data, xingPos + 4);

                        int frameCount = 0;
                        int byteCount = 0;

                        if ((flags & 0x0001) != 0) // Frames field present
                        {
                            frameCount = ReadBigEndian32(data, xingPos + 8);
                        }

                        if ((flags & 0x0002) != 0) // Bytes field present
                        {
                            byteCount = ReadBigEndian32(data, xingPos + 12);
                        }

                        // Get bitrate from the MPEG header table
                        int bitrate = GetMpegBitrate(version, layer, bitrateIndex);
                        int sampleRate = GetMpegSampleRate(version, sampleRateIndex);

                        if (bitrate > 0)
                        {
                            info.Bitrate = bitrate * 1000; // Convert to bps
                        }

                        if (sampleRate > 0)
                        {
                            info.SampleRate = sampleRate;
                        }

                        // Compute duration from frame count (more accurate)
                        if (frameCount > 0 && sampleRate > 0)
                        {
                            // For MPEG Layer III, each frame has 1152 samples (MPEG1) or 576 samples (MPEG2)
                            int samplesPerFrame = (version == 3) ? 1152 : 576;
                            double durationSec = (double)(frameCount * samplesPerFrame) / sampleRate;

                            if (durationSec > 0 && durationSec < 86400)
                            {
                                info.Duration = TimeSpan.FromSeconds(durationSec);
                            }
                        }

                        // If we got a duration from frame count, we're done.
                        // Otherwise, we may fall back to the file-size based estimate.
                        return;
                    }

                    pos++;
                }
                else
                {
                    pos++;
                }
            }
        }

        /// <summary>
        /// Returns the bitrate (in kbps) for the given MPEG version, layer,
        /// and bitrate index.
        /// </summary>
        private static int GetMpegBitrate(int version, int layer, int index)
        {
            // Bitrate tables in kbps
            // version: 3=MPEG1, 2=MPEG2, 0=MPEG2.5
            // layer: 3=L1, 2=L2, 1=L3

            if (index < 1 || index > 14)
                return 0;

            if (version == 3) // MPEG1
            {
                if (layer == 3) // Layer I
                {
                    return new[] { 32, 64, 96, 128, 160, 192, 224, 256, 288, 320, 352, 384, 416, 448 }[index - 1];
                }

                if (layer == 2) // Layer II
                {
                    return new[] { 32, 48, 56, 64, 80, 96, 112, 128, 160, 192, 224, 256, 320, 384 }[index - 1];
                }

                if (layer == 1) // Layer III
                {
                    return new[] { 32, 40, 48, 56, 64, 80, 96, 112, 128, 160, 192, 224, 256, 320 }[index - 1];
                }
            }
            else // MPEG2 or MPEG2.5
            {
                if (layer == 3) // Layer I
                {
                    return new[] { 32, 48, 56, 64, 80, 96, 112, 128, 144, 160, 176, 192, 224, 256 }[index - 1];
                }

                if (layer == 2) // Layer II
                {
                    return new[] { 8, 16, 24, 32, 40, 48, 56, 64, 80, 96, 112, 128, 144, 160 }[index - 1];
                }

                if (layer == 1) // Layer III
                {
                    return new[] { 8, 16, 24, 32, 40, 48, 56, 64, 80, 96, 112, 128, 144, 160 }[index - 1];
                }
            }

            return 0;
        }

        /// <summary>
        /// Returns the sample rate (in Hz) for the given MPEG version and
        /// sample rate index.
        /// </summary>
        private static int GetMpegSampleRate(int version, int index)
        {
            // index: 0=44100, 1=48000, 2=32000 (for MPEG1)
            if (index > 2)
                return 0;

            if (version == 3) // MPEG1
            {
                return new[] { 44100, 48000, 32000 }[index];
            }

            if (version == 2) // MPEG2
            {
                return new[] { 22050, 24000, 16000 }[index];
            }

            if (version == 0) // MPEG2.5
            {
                return new[] { 11025, 12000, 8000 }[index];
            }

            return 0;
        }

        // -----------------------------------------------------------------
        // WAV / RIFF Parsing
        // -----------------------------------------------------------------

        /// <summary>
        /// Parses a WAV file's RIFF header to extract sample rate, bit depth,
        /// channels, and duration.
        /// </summary>
        private static void ParseWav(string filePath, MediaInfo info)
        {
            using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            int fileSize = (int)stream.Length;

            if (fileSize < 44) // Minimum valid WAV header size
                return;

            // Read RIFF header
            byte[] header = new byte[12];
            if (stream.Read(header, 0, 12) != 12)
                return;

            // Verify RIFF and WAVE signatures
            if (Encoding.ASCII.GetString(header, 0, 4) != RiffSignature ||
                Encoding.ASCII.GetString(header, 8, 4) != WaveSignature)
            {
                return;
            }

            // Parse chunks until we find "fmt " and "data"
            int fmtSize = 0;
            int audioFormat = 0;
            int numChannels = 0;
            int sampleRate = 0;
            int byteRate = 0;
            int blockAlign = 0;
            int bitsPerSample = 0;
            long dataSize = 0;

            while (stream.Position < stream.Length - 8)
            {
                byte[] chunkHeader = new byte[8];
                if (stream.Read(chunkHeader, 0, 8) != 8)
                    break;

                string chunkId = Encoding.ASCII.GetString(chunkHeader, 0, 4);
                int chunkSize = BitConverter.ToInt32(chunkHeader, 4);

                if (chunkSize < 0 || stream.Position + chunkSize > stream.Length)
                    break;

                if (chunkId == RiffFmtChunk)
                {
                    // fmt chunk: format tag (2), channels (2), sample rate (4), byte rate (4),
                    //           block align (2), bits per sample (2)
                    byte[] fmtData = new byte[Math.Min(chunkSize, 16)];
                    int bytesRead = stream.Read(fmtData, 0, fmtData.Length);
                    if (bytesRead >= 16)
                    {
                        audioFormat = BitConverter.ToUInt16(fmtData, 0);
                        numChannels = BitConverter.ToUInt16(fmtData, 2);
                        sampleRate = BitConverter.ToInt32(fmtData, 4);
                        byteRate = BitConverter.ToInt32(fmtData, 8);
                        blockAlign = BitConverter.ToUInt16(fmtData, 12);
                        bitsPerSample = BitConverter.ToUInt16(fmtData, 14);
                        fmtSize = chunkSize;
                    }
                    else
                    {
                        // Skip remaining chunk data if we couldn't parse it
                        stream.Seek(chunkSize - bytesRead, SeekOrigin.Current);
                    }
                }
                else if (chunkId == RiffDataChunk)
                {
                    dataSize = chunkSize;
                    // Don't read the data — just record its size and break.
                    break;
                }
                else
                {
                    // Skip this chunk
                    stream.Seek(chunkSize, SeekOrigin.Current);
                }
            }

            // Populate MediaInfo from parsed header data
            if (sampleRate > 0)
            {
                info.SampleRate = sampleRate;
                info.AudioCodec = audioFormat switch
                {
                    1 => "PCM",
                    3 => "IEEE Float",
                    6 => "ALAW",
                    7 => "Mu-Law",
                    0xFFFE => "WMA", // WAVE_FORMAT_EXTENSIBLE
                    _ => $"Format-{audioFormat}",
                };
            }

            if (bitsPerSample > 0)
            {
                info.Bitrate = sampleRate * numChannels * bitsPerSample;
            }

            if (dataSize > 0 && sampleRate > 0 && numChannels > 0 && bitsPerSample > 0)
            {
                long bytesPerSecond = sampleRate * (long)numChannels * (bitsPerSample / 8L);
                if (bytesPerSecond > 0)
                {
                    double durationSec = (double)dataSize / bytesPerSecond;
                    if (durationSec > 0 && durationSec < 86400)
                    {
                        info.Duration = TimeSpan.FromSeconds(durationSec);
                    }
                }
            }
        }

        // -----------------------------------------------------------------
        // Private helpers
        // -----------------------------------------------------------------

        /// <summary>
        /// Gets the ISO-8859-1 (Latin-1) encoding.
        /// On .NET Framework this is not available as <c>Encoding.Latin1</c>
        /// so we create it explicitly.
        /// </summary>
        private static Encoding Latin1Encoding =>
#if NET472
            Encoding.GetEncoding(28591); // ISO-8859-1 code page
#else
            Encoding.Latin1;
#endif

        // -----------------------------------------------------------------
        // Binary Helpers
        // -----------------------------------------------------------------

        /// <summary>
        /// Reads a 4-byte synchsafe integer (used in ID3v2 headers).
        /// Each byte uses only 7 bits (MSB is always 0).
        /// </summary>
        private static int ReadSynchSafeInt(byte[] data, int offset)
        {
            return (data[offset] << 21) |
                   (data[offset + 1] << 14) |
                   (data[offset + 2] << 7) |
                   data[offset + 3];
        }

        /// <summary>
        /// Reads a big-endian 32-bit integer from the specified offset.
        /// </summary>
        private static int ReadBigEndian32(byte[] data, int offset)
        {
            return (data[offset] << 24) |
                   (data[offset + 1] << 16) |
                   (data[offset + 2] << 8) |
                   data[offset + 3];
        }

        /// <summary>
        /// Reads a fixed-length string from the data buffer, trimming null
        /// characters and trailing whitespace.
        /// </summary>
        private static string ReadId3v1String(byte[] data, int offset, int length)
        {
            // Find the null terminator or end of the field
            int end = offset;
            int maxEnd = offset + length;
            while (end < maxEnd && data[end] != 0)
                end++;

            if (end == offset)
                return string.Empty;

            // Strip trailing whitespace as well
            int trimEnd = end;
            while (trimEnd > offset && (data[trimEnd - 1] == ' ' || data[trimEnd - 1] == '\0' || data[trimEnd - 1] == '\r' || data[trimEnd - 1] == '\n'))
                trimEnd--;

            if (trimEnd == offset)
                return string.Empty;

            return Latin1Encoding.GetString(data, offset, trimEnd - offset);
        }

        /// <summary>
        /// Returns <see langword="true"/> if the 4-character string consists
        /// of ASCII alphanumeric characters (used to identify ID3v2 frame IDs).
        /// </summary>
        private static bool IsAsciiAlphanumeric(string s)
        {
            if (s.Length != 4)
                return false;

            for (int i = 0; i < 4; i++)
            {
                char c = s[i];
                if (!((c >= 'A' && c <= 'Z') || (c >= '0' && c <= '9')))
                    return false;
            }

            return true;
        }
    }
}
