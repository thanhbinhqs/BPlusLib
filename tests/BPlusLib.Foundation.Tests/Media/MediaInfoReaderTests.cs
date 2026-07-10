// <copyright file="MediaInfoReaderTests.cs" company="BPlusLib.Foundation.Tests">
// Copyright (c) BPlusLib.Foundation.Tests. All rights reserved.
// Licensed under the MIT license.
// </copyright>

using System;
using System.IO;
using System.Text;
using Xunit;
using FluentAssertions;
using BPlusLib.Foundation.Media;

namespace BPlusLib.Foundation.Tests.Media
{
    [Trait("Category", "Media")]
    public sealed class MediaInfoReaderTests : IDisposable
    {
        /// <summary>
        /// Gets Latin-1 encoding in a net472-compatible way.
        /// </summary>
        private static Encoding Latin1Encoding =>
#if NET472
            Encoding.GetEncoding(28591);
#else
            Encoding.Latin1;
#endif

        private readonly string _tempDir;

        public MediaInfoReaderTests()
        {
            _tempDir = Path.Combine(Path.GetTempPath(), "MediaInfoReaderTests_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_tempDir);
        }

        public void Dispose()
        {
            if (Directory.Exists(_tempDir))
            {
                try { Directory.Delete(_tempDir, recursive: true); }
                catch { /* Best-effort cleanup */ }
            }
        }

        private string GetTempPath(string fileName) =>
            Path.Combine(_tempDir, fileName);

        // ── IsAudioFile ────────────────────────────────────────

        [Fact]
        public void IsAudioFile_Mp3_ReturnsTrue()
        {
            MediaInfoReader.IsAudioFile(".mp3").Should().BeTrue();
            MediaInfoReader.IsAudioFile("mp3").Should().BeTrue();
            MediaInfoReader.IsAudioFile(".MP3").Should().BeTrue();
        }

        [Fact]
        public void IsAudioFile_Wav_ReturnsTrue()
        {
            MediaInfoReader.IsAudioFile(".wav").Should().BeTrue();
            MediaInfoReader.IsAudioFile("wav").Should().BeTrue();
        }

        [Fact]
        public void IsAudioFile_Txt_ReturnsFalse()
        {
            MediaInfoReader.IsAudioFile(".txt").Should().BeFalse();
        }

        [Fact]
        public void IsAudioFile_Null_ReturnsFalse()
        {
            MediaInfoReader.IsAudioFile(null!).Should().BeFalse();
        }

        [Fact]
        public void IsAudioFile_Empty_ReturnsFalse()
        {
            MediaInfoReader.IsAudioFile(string.Empty).Should().BeFalse();
        }

        // ── IsVideoFile ────────────────────────────────────────

        [Fact]
        public void IsVideoFile_Mp4_ReturnsTrue()
        {
            MediaInfoReader.IsVideoFile(".mp4").Should().BeTrue();
            MediaInfoReader.IsVideoFile("mp4").Should().BeTrue();
            MediaInfoReader.IsVideoFile(".MP4").Should().BeTrue();
        }

        [Fact]
        public void IsVideoFile_Mov_ReturnsTrue()
        {
            MediaInfoReader.IsVideoFile(".mov").Should().BeTrue();
            MediaInfoReader.IsVideoFile("mov").Should().BeTrue();
        }

        [Fact]
        public void IsVideoFile_Txt_ReturnsFalse()
        {
            MediaInfoReader.IsVideoFile(".txt").Should().BeFalse();
        }

        [Fact]
        public void IsVideoFile_Null_ReturnsFalse()
        {
            MediaInfoReader.IsVideoFile(null!).Should().BeFalse();
        }

        [Fact]
        public void IsVideoFile_Empty_ReturnsFalse()
        {
            MediaInfoReader.IsVideoFile(string.Empty).Should().BeFalse();
        }

        // ── Read - edge cases ──────────────────────────────────

        [Fact]
        public void Read_NonExistentFile_ReturnsNull()
        {
            string path = GetTempPath("nonexistent.mp3");
            MediaInfo? info = MediaInfoReader.Read(path);
            info.Should().BeNull();
        }

        [Fact]
        public void Read_NullPath_ReturnsNull()
        {
            MediaInfo? info = MediaInfoReader.Read(null!);
            info.Should().BeNull();
        }

        [Fact]
        public void Read_EmptyPath_ReturnsNull()
        {
            MediaInfo? info = MediaInfoReader.Read(string.Empty);
            info.Should().BeNull();
        }

        [Fact]
        public void Read_EmptyFile_ReturnsNotNull()
        {
            // An empty file with .mp3 extension returns a MediaInfo object
            // (just the FilePath populated) rather than null.
            string path = GetTempPath("empty.mp3");
            File.WriteAllBytes(path, Array.Empty<byte>());
            MediaInfo? info = MediaInfoReader.Read(path);
            info.Should().NotBeNull();
            info!.FilePath.Should().Be(Path.GetFullPath(path));
            info.Duration.Should().BeNull();
            info.Title.Should().BeNull();
            info.Artist.Should().BeNull();
        }

        [Fact]
        public void Read_TooShortFile_ReturnsInfo()
        {
            // A file with only 3 bytes is too short for any header.
            string path = GetTempPath("short.mp3");
            File.WriteAllBytes(path, new byte[] { 0xFF, 0xFB, 0x90 });
            MediaInfo? info = MediaInfoReader.Read(path);
            info.Should().NotBeNull();
            info!.FilePath.Should().Be(Path.GetFullPath(path));
        }

        // ── Read - MP3 with ID3v1 tag ──────────────────────────

        [Fact]
        public void Read_Mp3WithId3v1_ReturnsMetadata()
        {
            string path = GetTempPath("id3v1_test.mp3");

            // Create an ID3v1 tag: 128 bytes with "TAG" signature
            byte[] tag = new byte[128];
            // Signature "TAG" at offset 0
            tag[0] = (byte)'T';
            tag[1] = (byte)'A';
            tag[2] = (byte)'G';
            // Title: 30 bytes at offset 3
            WritePaddedString(tag, 3, 30, "Test Title");
            // Artist: 30 bytes at offset 33
            WritePaddedString(tag, 33, 30, "Test Artist");
            // Album: 30 bytes at offset 63
            WritePaddedString(tag, 63, 30, "Test Album");
            // Year: 4 bytes at offset 93
            WritePaddedString(tag, 93, 4, "2024");
            // Comment: 30 bytes at offset 97 (or 28 with track)
            WritePaddedString(tag, 97, 30, "Test Comment");
            // Genre: 1 byte at offset 127
            tag[127] = 10; // "New Age" genre

            // Need at least some audio data before the tag to avoid empty file rejection.
            // Put 4 bytes of MPEG sync to make it look somewhat like an MP3.
            byte[] audioData = new byte[] { 0xFF, 0xFB, 0x90, 0x00 }; // MPEG1 Layer III sync
            byte[] fileBytes = new byte[audioData.Length + tag.Length];
            Buffer.BlockCopy(audioData, 0, fileBytes, 0, audioData.Length);
            Buffer.BlockCopy(tag, 0, fileBytes, audioData.Length, tag.Length);

            File.WriteAllBytes(path, fileBytes);

            MediaInfo? info = MediaInfoReader.Read(path);
            info.Should().NotBeNull();
            info!.Title.Should().Be("Test Title");
            info.Artist.Should().Be("Test Artist");
            info.Album.Should().Be("Test Album");
            info.Year.Should().Be(2024);
            info.Comment.Should().Be("Test Comment");
            info.Genre.Should().Be("New Age");
        }

        // ── Read - MP3 with ID3v2 tag ──────────────────────────

        [Fact]
        public void Read_Mp3WithId3v2_ReturnsMetadata()
        {
            string path = GetTempPath("id3v2_test.mp3");

            // Build an ID3v2.3 header (10 bytes) + frames.
            byte[] tit2Frame = BuildId3v2Frame("TIT2", "ID3v2 Title");
            byte[] tpe1Frame = BuildId3v2Frame("TPE1", "ID3v2 Artist");
            byte[] talbFrame = BuildId3v2Frame("TALB", "ID3v2 Album");

            int framesSize = tit2Frame.Length + tpe1Frame.Length + talbFrame.Length;
            byte[] id3v2Tag = new byte[10 + framesSize];

            // Header
            id3v2Tag[0] = (byte)'I';
            id3v2Tag[1] = (byte)'D';
            id3v2Tag[2] = (byte)'3';
            id3v2Tag[3] = 3; // Major version 2.3
            id3v2Tag[4] = 0; // Minor version
            id3v2Tag[5] = 0; // Flags
            // Synchsafe size (each byte uses only 7 bits)
            WriteSynchSafeInt(id3v2Tag, 6, framesSize);

            // Copy frames after header
            Buffer.BlockCopy(tit2Frame, 0, id3v2Tag, 10, tit2Frame.Length);
            Buffer.BlockCopy(tpe1Frame, 0, id3v2Tag, 10 + tit2Frame.Length, tpe1Frame.Length);
            Buffer.BlockCopy(talbFrame, 0, id3v2Tag, 10 + tit2Frame.Length + tpe1Frame.Length, talbFrame.Length);

            // Write file with ID3v2 tag at start + some audio bytes
            byte[] audioData = new byte[] { 0xFF, 0xFB, 0x90, 0x00 };
            byte[] fileBytes = new byte[id3v2Tag.Length + audioData.Length];
            Buffer.BlockCopy(id3v2Tag, 0, fileBytes, 0, id3v2Tag.Length);
            Buffer.BlockCopy(audioData, 0, fileBytes, id3v2Tag.Length, audioData.Length);

            File.WriteAllBytes(path, fileBytes);

            MediaInfo? info = MediaInfoReader.Read(path);
            info.Should().NotBeNull();
            info!.Title.Should().Be("ID3v2 Title");
            info.Artist.Should().Be("ID3v2 Artist");
            info.Album.Should().Be("ID3v2 Album");
        }

        [Fact]
        public void Read_Mp3WithId3v2_EncodingLatin1_ReturnsMetadata()
        {
            string path = GetTempPath("id3v2_latin1.mp3");

            // Build ID3v2.3 frames with encoding byte 0 (ISO-8859-1 / Latin-1)
            byte[] tit2Frame = BuildId3v2Frame("TIT2", "Latin Title", encodingByte: 0);
            byte[] tpe1Frame = BuildId3v2Frame("TPE1", "Latin Artist", encodingByte: 0);
            byte[] talbFrame = BuildId3v2Frame("TALB", "Latin Album", encodingByte: 0);

            int framesSize = tit2Frame.Length + tpe1Frame.Length + talbFrame.Length;
            byte[] id3v2Tag = new byte[10 + framesSize];

            id3v2Tag[0] = (byte)'I';
            id3v2Tag[1] = (byte)'D';
            id3v2Tag[2] = (byte)'3';
            id3v2Tag[3] = 3;
            id3v2Tag[4] = 0;
            id3v2Tag[5] = 0;
            WriteSynchSafeInt(id3v2Tag, 6, framesSize);

            Buffer.BlockCopy(tit2Frame, 0, id3v2Tag, 10, tit2Frame.Length);
            Buffer.BlockCopy(tpe1Frame, 0, id3v2Tag, 10 + tit2Frame.Length, tpe1Frame.Length);
            Buffer.BlockCopy(talbFrame, 0, id3v2Tag, 10 + tit2Frame.Length + tpe1Frame.Length, talbFrame.Length);

            byte[] audioData = new byte[] { 0xFF, 0xFB, 0x90, 0x00 };
            byte[] fileBytes = new byte[id3v2Tag.Length + audioData.Length];
            Buffer.BlockCopy(id3v2Tag, 0, fileBytes, 0, id3v2Tag.Length);
            Buffer.BlockCopy(audioData, 0, fileBytes, id3v2Tag.Length, audioData.Length);

            File.WriteAllBytes(path, fileBytes);

            MediaInfo? info = MediaInfoReader.Read(path);
            info.Should().NotBeNull();
            info!.Title.Should().Be("Latin Title");
            info.Artist.Should().Be("Latin Artist");
            info.Album.Should().Be("Latin Album");
        }

        // ── Read - WAV file duration ───────────────────────────

        [Fact]
        public void Read_WavFile_ReturnsDuration()
        {
            string path = GetTempPath("test.wav");

            // Create a minimal WAV file: RIFF header + fmt chunk + data chunk.
            // PCM, 16-bit, mono, 44100 Hz, 1 second of silence.
            int sampleRate = 44100;
            short numChannels = 1;
            short bitsPerSample = 16;
            int dataSize = sampleRate * numChannels * (bitsPerSample / 8); // 88200 bytes
            int byteRate = sampleRate * numChannels * (bitsPerSample / 8);
            short blockAlign = (short)(numChannels * (bitsPerSample / 8)); // 2

            // RIFF header (12 bytes) + fmt chunk (24 bytes) + data chunk (8 + dataSize)
            byte[] wav = new byte[44 + dataSize];
            int offset = 0;

            // RIFF header
            WriteAscii(wav, offset, "RIFF"); offset += 4;
            WriteInt32(wav, offset, 36 + dataSize); offset += 4; // File size - 8
            WriteAscii(wav, offset, "WAVE"); offset += 4;

            // fmt chunk
            WriteAscii(wav, offset, "fmt "); offset += 4;
            WriteInt32(wav, offset, 16); offset += 4; // Subchunk size (PCM)
            WriteInt16(wav, offset, 1); offset += 2;  // Audio format (PCM = 1)
            WriteInt16(wav, offset, numChannels); offset += 2;
            WriteInt32(wav, offset, sampleRate); offset += 4;
            WriteInt32(wav, offset, byteRate); offset += 4;
            WriteInt16(wav, offset, blockAlign); offset += 2;
            WriteInt16(wav, offset, bitsPerSample); offset += 2;

            // data chunk
            WriteAscii(wav, offset, "data"); offset += 4;
            WriteInt32(wav, offset, dataSize); offset += 4;

            // Rest is zeroed (silence)

            File.WriteAllBytes(path, wav);

            MediaInfo? info = MediaInfoReader.Read(path);
            info.Should().NotBeNull();
            info!.FilePath.Should().Be(Path.GetFullPath(path));
            info.SampleRate.Should().Be(sampleRate);
            info.AudioCodec.Should().Be("PCM");
            info.Duration.Should().NotBeNull();
            info.Duration!.Value.TotalSeconds.Should().BeApproximately(1.0, 0.01);
        }

        [Fact]
        public void Read_WavFile_WithDifferentFormat_ReturnsAudioCodec()
        {
            string path = GetTempPath("float.wav");

            // IEEE Float WAV: format tag = 3 (IEEE float)
            int sampleRate = 48000;
            short numChannels = 2;
            short bitsPerSample = 32;
            int dataSize = 96000; // 0.5 seconds of stereo 32-bit float at 48kHz
            int byteRate = sampleRate * numChannels * (bitsPerSample / 8);

            byte[] wav = new byte[44 + dataSize];
            int offset = 0;

            WriteAscii(wav, offset, "RIFF"); offset += 4;
            WriteInt32(wav, offset, 36 + dataSize); offset += 4;
            WriteAscii(wav, offset, "WAVE"); offset += 4;

            WriteAscii(wav, offset, "fmt "); offset += 4;
            WriteInt32(wav, offset, 16); offset += 4;
            WriteInt16(wav, offset, 3); offset += 2;  // IEEE float
            WriteInt16(wav, offset, numChannels); offset += 2;
            WriteInt32(wav, offset, sampleRate); offset += 4;
            WriteInt32(wav, offset, byteRate); offset += 4;
            WriteInt16(wav, offset, (short)(numChannels * (bitsPerSample / 8))); offset += 2;
            WriteInt16(wav, offset, bitsPerSample); offset += 2;

            WriteAscii(wav, offset, "data"); offset += 4;
            WriteInt32(wav, offset, dataSize); offset += 4;

            File.WriteAllBytes(path, wav);

            MediaInfo? info = MediaInfoReader.Read(path);
            info.Should().NotBeNull();
            info!.AudioCodec.Should().Be("IEEE Float");
            info.Duration.Should().NotBeNull();
        }

        [Fact]
        public void Read_WavFile_MinimalHeader_ReturnsInfo()
        {
            // A WAV file with just the header and no data chunk should still return info.
            string path = GetTempPath("minimal.wav");

            byte[] wav = new byte[44];
            int offset = 0;

            WriteAscii(wav, offset, "RIFF"); offset += 4;
            WriteInt32(wav, offset, 36); offset += 4;
            WriteAscii(wav, offset, "WAVE"); offset += 4;

            WriteAscii(wav, offset, "fmt "); offset += 4;
            WriteInt32(wav, offset, 16); offset += 4;
            WriteInt16(wav, offset, 1); offset += 2; // PCM
            WriteInt16(wav, offset, 1); offset += 2; // mono
            WriteInt32(wav, offset, 22050); offset += 4; // sample rate
            WriteInt32(wav, offset, 44100); offset += 4; // byte rate = 22050 * 1 * 2
            WriteInt16(wav, offset, 2); offset += 2; // block align
            WriteInt16(wav, offset, 16); offset += 2; // bits per sample

            WriteAscii(wav, offset, "data"); offset += 4;
            WriteInt32(wav, offset, 0); offset += 4; // empty data chunk

            File.WriteAllBytes(path, wav);

            MediaInfo? info = MediaInfoReader.Read(path);
            info.Should().NotBeNull();
            info!.SampleRate.Should().Be(22050);
            info.Duration.Should().BeNull(); // no data, so no duration
        }

        [Fact]
        public void Read_WavFile_WithNonPcmFormat_Succeeds()
        {
            string path = GetTempPath("alaw.wav");

            // A-LAW format tag = 6; create a valid WAV with proper data
            int sampleRate = 8000;
            short numChannels = 1;
            short bitsPerSample = 8;
            int dataSize = sampleRate; // 1 second of mono 8-bit data
            int byteRate = sampleRate * numChannels * (bitsPerSample / 8);

            byte[] wav = new byte[44 + dataSize];
            int offset = 0;
            WriteAscii(wav, offset, "RIFF"); offset += 4;
            WriteInt32(wav, offset, 36 + dataSize); offset += 4; // Correct file size
            WriteAscii(wav, offset, "WAVE"); offset += 4;
            WriteAscii(wav, offset, "fmt "); offset += 4;
            WriteInt32(wav, offset, 16); offset += 4;
            WriteInt16(wav, offset, 6); offset += 2;  // A-LAW
            WriteInt16(wav, offset, numChannels); offset += 2;
            WriteInt32(wav, offset, sampleRate); offset += 4;
            WriteInt32(wav, offset, byteRate); offset += 4;
            WriteInt16(wav, offset, (short)(numChannels * (bitsPerSample / 8))); offset += 2;
            WriteInt16(wav, offset, bitsPerSample); offset += 2;
            WriteAscii(wav, offset, "data"); offset += 4;
            WriteInt32(wav, offset, dataSize); offset += 4;

            File.WriteAllBytes(path, wav);

            MediaInfo? info = MediaInfoReader.Read(path);
            info.Should().NotBeNull();
            info!.AudioCodec.Should().Be("ALAW");
            info.Duration.Should().NotBeNull();
            info.Duration!.Value.TotalSeconds.Should().BeApproximately(1.0, 0.1);
        }

        // ── Helper methods ─────────────────────────────────────

        private static void WritePaddedString(byte[] buffer, int offset, int maxLength, string value)
        {
            byte[] stringBytes = Latin1Encoding.GetBytes(value);
            int copyLen = Math.Min(stringBytes.Length, maxLength);
            Buffer.BlockCopy(stringBytes, 0, buffer, offset, copyLen);
            // Remaining bytes stay zero (already initialized)
        }

        private static byte[] BuildId3v2Frame(string frameId, string value, byte encodingByte = 3)
        {
            // Encoding: 3 = UTF-8, 0 = Latin-1
            Encoding encoding = encodingByte == 0 ? Latin1Encoding : Encoding.UTF8;
            byte[] valueBytes = encoding.GetBytes(value);

            // Frame: ID (4) + size (4, big-endian) + flags (2) + encoding byte (1) + value
            int frameSize = 1 + valueBytes.Length; // 1 for encoding byte
            byte[] frame = new byte[10 + frameSize];

            // Frame ID
            WriteAscii(frame, 0, frameId);
            // Size (big-endian, excludes frame header) — ID3v2 frame sizes are big-endian
            WriteInt32BigEndian(frame, 4, frameSize);
            // Flags (2 bytes, zero)
            frame[8] = 0;
            frame[9] = 0;
            // Encoding byte
            frame[10] = encodingByte;
            // Value
            Buffer.BlockCopy(valueBytes, 0, frame, 11, valueBytes.Length);

            return frame;
        }

        private static void WriteSynchSafeInt(byte[] buffer, int offset, int value)
        {
            buffer[offset] = (byte)((value >> 21) & 0x7F);
            buffer[offset + 1] = (byte)((value >> 14) & 0x7F);
            buffer[offset + 2] = (byte)((value >> 7) & 0x7F);
            buffer[offset + 3] = (byte)(value & 0x7F);
        }

        private static void WriteAscii(byte[] buffer, int offset, string value)
        {
            byte[] asciiBytes = Encoding.ASCII.GetBytes(value);
            Buffer.BlockCopy(asciiBytes, 0, buffer, offset, asciiBytes.Length);
        }

        private static void WriteInt32BigEndian(byte[] buffer, int offset, int value)
        {
            buffer[offset] = (byte)((value >> 24) & 0xFF);
            buffer[offset + 1] = (byte)((value >> 16) & 0xFF);
            buffer[offset + 2] = (byte)((value >> 8) & 0xFF);
            buffer[offset + 3] = (byte)(value & 0xFF);
        }

        private static void WriteInt32(byte[] buffer, int offset, int value)
        {
            buffer[offset] = (byte)(value & 0xFF);
            buffer[offset + 1] = (byte)((value >> 8) & 0xFF);
            buffer[offset + 2] = (byte)((value >> 16) & 0xFF);
            buffer[offset + 3] = (byte)((value >> 24) & 0xFF);
        }

        private static void WriteInt16(byte[] buffer, int offset, short value)
        {
            buffer[offset] = (byte)(value & 0xFF);
            buffer[offset + 1] = (byte)((value >> 8) & 0xFF);
        }
    }
}
