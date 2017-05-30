// <copyright file="LzeTiffCompression.cs" company="James Jackson-South">
// Copyright (c) James Jackson-South and contributors.
// Licensed under the Apache License, Version 2.0.
// </copyright>

namespace ImageSharp.Formats.Tiff
{
    using System;
    using System.Buffers;
    using System.IO;
    using System.IO.Compression;
    using System.Runtime.CompilerServices;

    /// <summary>
    /// Class to handle cases where TIFF image data is compressed using LZW compression.
    /// </summary>
    internal static class LzwTiffCompression
    {
        /// <summary>
        /// Decompresses image data into the supplied buffer.
        /// </summary>
        /// <param name="stream">The <see cref="Stream"/> to read image data from.</param>
        /// <param name="byteCount">The number of bytes to read from the input stream.</param>
        /// <param name="buffer">The output buffer for uncompressed data.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Decompress(Stream stream, int byteCount, byte[] buffer)
        {
            SubStream subStream = new SubStream(stream, byteCount);
            using (var decoder = new TiffLzwDecoder(subStream))
            {
                decoder.DecodePixels(buffer.Length, 8, buffer);
            }
        }
    }
}
