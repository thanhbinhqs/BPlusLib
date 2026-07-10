// <copyright file="StreamExtensions.cs" company="BPlusLib">
// Copyright (c) BPlusLib. All rights reserved.
// </copyright>

namespace BPlusLib.Foundation.Extensions
{
    using System;
    using System.IO;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Provides extension methods for <see cref="Stream"/> to simplify common
    /// I/O operations such as reading, writing, copying, and draining.
    /// </summary>
    public static class StreamExtensions
    {
        private const int DefaultBufferSize = 81920;

        /// <summary>
        /// Reads all bytes from the stream into a byte array.
        /// The stream is read from its current position to the end.
        /// </summary>
        /// <param name="stream">The stream to read from.</param>
        /// <returns>
        /// A byte array containing the entire contents of the stream.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="stream"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="IOException">
        /// An I/O error occurs.
        /// </exception>
        public static byte[] ReadAllBytes(this Stream stream)
        {
            if (stream == null)
            {
                throw new ArgumentNullException(nameof(stream));
            }

            if (stream.CanSeek)
            {
                // If we know the length and it's reasonable, allocate exactly.
                long length = stream.Length - stream.Position;
                if (length > 0 && length <= int.MaxValue)
                {
                    var buffer = new byte[(int)length];
                    int offset = 0;
                    int remaining = buffer.Length;
                    while (remaining > 0)
                    {
                        int read = stream.Read(buffer, offset, remaining);
                        if (read == 0)
                        {
                            break;
                        }

                        offset += read;
                        remaining -= read;
                    }

                    // If the stream ended earlier than expected, trim.
                    if (offset < buffer.Length)
                    {
                        Array.Resize(ref buffer, offset);
                    }

                    return buffer;
                }
            }

            // Fallback: read in chunks.
            using (var ms = new MemoryStream())
            {
                stream.CopyTo(ms, DefaultBufferSize);
                return ms.ToArray();
            }
        }

        /// <summary>
        /// Reads all text from the stream, optionally using the specified
        /// <paramref name="encoding"/>. When no encoding is supplied, the
        /// method attempts to detect the encoding from the byte order mark
        /// (BOM); if no BOM is present, UTF-8 is assumed.
        /// </summary>
        /// <param name="stream">The stream to read from.</param>
        /// <param name="encoding">
        /// The encoding to use. If <see langword="null"/>, automatic BOM
        /// detection is attempted, defaulting to UTF-8.
        /// </param>
        /// <returns>
        /// A string containing the entire content of the stream.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="stream"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="IOException">
        /// An I/O error occurs.
        /// </exception>
        public static string ReadAllText(this Stream stream, Encoding? encoding = null)
        {
            if (stream == null)
            {
                throw new ArgumentNullException(nameof(stream));
            }

            if (encoding != null)
            {
                using (var reader = new StreamReader(stream, encoding, detectEncodingFromByteOrderMarks: false))
                {
                    return reader.ReadToEnd();
                }
            }

            // Auto-detect encoding from BOM; default to UTF-8.
            using (var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true))
            {
                return reader.ReadToEnd();
            }
        }

        /// <summary>
        /// Asynchronously copies the source stream to the destination stream,
        /// optionally reporting progress and supporting cancellation.
        /// </summary>
        /// <param name="source">The stream to copy from.</param>
        /// <param name="destination">The stream to copy to.</param>
        /// <param name="progress">
        /// An <see cref="IProgress{T}"/> that receives the total number of
        /// bytes copied so far. May be <see langword="null"/>.
        /// </param>
        /// <param name="ct">A cancellation token.</param>
        /// <param name="bufferSize">
        /// The size of the buffer in bytes. Defaults to 81,920 (80 KB).
        /// </param>
        /// <returns>A task that represents the asynchronous copy operation.</returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="source"/> or <paramref name="destination"/> is
        /// <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <paramref name="bufferSize"/> is zero or negative.
        /// </exception>
        /// <exception cref="OperationCanceledException">
        /// The operation was cancelled via <paramref name="ct"/>.
        /// </exception>
        public static async Task CopyToAsync(
            this Stream source,
            Stream destination,
            IProgress<long>? progress = null,
            CancellationToken ct = default,
            int bufferSize = DefaultBufferSize)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            if (destination == null)
            {
                throw new ArgumentNullException(nameof(destination));
            }

            if (bufferSize <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(bufferSize), bufferSize, "Buffer size must be positive.");
            }

            if (progress == null)
            {
                // No progress reporting — use the built-in CopyToAsync.
                await source.CopyToAsync(destination, bufferSize, ct).ConfigureAwait(false);
                return;
            }

            var buffer = new byte[bufferSize];
            long totalBytesRead = 0;
            int bytesRead;

            while ((bytesRead = await source.ReadAsync(buffer, 0, buffer.Length, ct).ConfigureAwait(false)) != 0)
            {
                await destination.WriteAsync(buffer, 0, bytesRead, ct).ConfigureAwait(false);
                totalBytesRead += bytesRead;
                progress.Report(totalBytesRead);
            }
        }

        /// <summary>
        /// Reads and discards all remaining data from the stream.
        /// </summary>
        /// <param name="stream">The stream to drain.</param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="stream"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="IOException">
        /// An I/O error occurs.
        /// </exception>
        public static void Drain(this Stream stream)
        {
            if (stream == null)
            {
                throw new ArgumentNullException(nameof(stream));
            }

            var buffer = new byte[DefaultBufferSize];
            while (stream.Read(buffer, 0, buffer.Length) > 0)
            {
                // Discard.
            }
        }

        /// <summary>
        /// Writes the specified string to the stream using the given encoding
        /// (or UTF-8 by default).
        /// </summary>
        /// <param name="stream">The stream to write to.</param>
        /// <param name="text">The string to write.</param>
        /// <param name="encoding">
        /// The encoding to use. If <see langword="null"/>, UTF-8 is used.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="stream"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="IOException">
        /// An I/O error occurs.
        /// </exception>
        public static void WriteText(this Stream stream, string text, Encoding? encoding = null)
        {
            if (stream == null)
            {
                throw new ArgumentNullException(nameof(stream));
            }

            encoding ??= Encoding.UTF8;

            byte[] bytes = encoding.GetBytes(text ?? string.Empty);
            stream.Write(bytes, 0, bytes.Length);
        }

        /// <summary>
        /// Reads exactly <paramref name="count"/> bytes from the stream,
        /// throwing if fewer bytes are available before end-of-stream.
        /// </summary>
        /// <param name="stream">The stream to read from.</param>
        /// <param name="count">The exact number of bytes to read.</param>
        /// <returns>
        /// A byte array of exactly <paramref name="count"/> bytes.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="stream"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <paramref name="count"/> is negative.
        /// </exception>
        /// <exception cref="EndOfStreamException">
        /// The stream ends before <paramref name="count"/> bytes could be read.
        /// </exception>
        /// <exception cref="IOException">
        /// An I/O error occurs.
        /// </exception>
        public static byte[] ReadExact(this Stream stream, int count)
        {
            if (stream == null)
            {
                throw new ArgumentNullException(nameof(stream));
            }

            if (count < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(count), count, "count must be non-negative.");
            }

            if (count == 0)
            {
                return Array.Empty<byte>();
            }

            var buffer = new byte[count];
            int offset = 0;
            int remaining = count;

            while (remaining > 0)
            {
                int read = stream.Read(buffer, offset, remaining);
                if (read == 0)
                {
                    throw new EndOfStreamException(
                        $"Stream ended before {count} bytes could be read; only {offset} bytes were read.");
                }

                offset += read;
                remaining -= read;
            }

            return buffer;
        }

        /// <summary>
        /// Attempts to read up to <paramref name="count"/> bytes from the stream
        /// into the buffer, returning the actual number of bytes read without
        /// throwing on end-of-stream.
        /// </summary>
        /// <param name="stream">The stream to read from.</param>
        /// <param name="buffer">The buffer to write into.</param>
        /// <param name="offset">The byte offset in <paramref name="buffer"/> at which to begin writing.</param>
        /// <param name="count">The maximum number of bytes to read.</param>
        /// <returns>
        /// The total number of bytes read into <paramref name="buffer"/>.
        /// This can be less than <paramref name="count"/> if the end of the
        /// stream was reached, or 0 if the stream is at end-of-stream.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="stream"/> or <paramref name="buffer"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <paramref name="offset"/> or <paramref name="count"/> is negative,
        /// or the combined <paramref name="offset"/> and <paramref name="count"/>
        /// exceed the buffer length.
        /// </exception>
        /// <exception cref="IOException">
        /// An I/O error occurs.
        /// </exception>
        public static int TryRead(this Stream stream, byte[] buffer, int offset, int count)
        {
            if (stream == null)
            {
                throw new ArgumentNullException(nameof(stream));
            }

            if (buffer == null)
            {
                throw new ArgumentNullException(nameof(buffer));
            }

            if (offset < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(offset), offset, "offset must be non-negative.");
            }

            if (count < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(count), count, "count must be non-negative.");
            }

            if (offset + count > buffer.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(count), "offset + count exceeds buffer length.");
            }

            return stream.Read(buffer, offset, count);
        }
    }
}
