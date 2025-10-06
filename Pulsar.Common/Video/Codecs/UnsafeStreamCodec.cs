using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.IO.Compression;
using System.Runtime.CompilerServices;

namespace Pulsar.Common.Video.Codecs
{
    public class UnsafeStreamCodec : IDisposable
    {
        public int Monitor { get; private set; }
        public Resolution Resolution { get; private set; }
        public Size CheckBlock { get; private set; }

        public int ImageQuality
        {
            get { return _imageQuality; }
            private set
            {
                if (_imageQuality == value) return;

                lock (_imageProcessLock)
                {
                    _imageQuality = value;
                }
            }
        }

        private int _imageQuality;
        private const byte CodecVersion = 1;
        private const byte PacketTypeKeyframe = 1;
        private const byte PacketTypeDelta = 2;
        private const byte CompressionFlagNone = 0;
        private const byte CompressionFlagDeflate = 1;

        private const int BlockMetadataIntCount = 6;
        private const int BlockMetadataSize = BlockMetadataIntCount * sizeof(int);
        private const int KeyframeHeaderSize = 1 + 1 + 1 + 1 + (5 * sizeof(int));
        private const int DeltaFrameHeaderSize = 4;

        [Flags]
        private enum BlockFlags
        {
            None = 0,
            Compressed = 1
        }

        private byte[] _encodeBuffer;
        private Bitmap _decodedBitmap;
        private PixelFormat _encodedFormat;
        private int _encodedWidth;
        private int _encodedHeight;
        private readonly object _imageProcessLock = new object();
        private bool _disposed;

        private readonly List<Rectangle> _workingBlocks = new List<Rectangle>();
        private readonly List<Rectangle> _finalUpdates = new List<Rectangle>();

        private readonly byte[] _metadataBuffer = new byte[BlockMetadataSize];
        private readonly byte[] _lengthBuffer = new byte[4];
        private byte[] _deltaBuffer;
        private byte[] _compressedBlockBuffer;
        private byte[] _decodedFrameBuffer;
        private int _decodedStride;
        private PixelFormat _decodedFormat;
        private int _decodedPixelSize;
        private CompressionLevel CompressionLevel =>
            _imageQuality <= 0 ? CompressionLevel.NoCompression :
            _imageQuality < 50 ? CompressionLevel.Fastest :
            CompressionLevel.Optimal;

        /// <summary>
        /// Initialize a new instance of UnsafeStreamCodec class.
        /// </summary>
        /// <param name="imageQuality">The quality to use between 0-100.</param>
        /// <param name="monitor">The monitor used for the images.</param>
        /// <param name="resolution">The resolution of the monitor.</param>
        public UnsafeStreamCodec(int imageQuality, int monitor, Resolution resolution)
        {
            if (imageQuality < 0 || imageQuality > 100)
                throw new ArgumentOutOfRangeException("imageQuality", "Image quality must be between 0 and 100");

            this.ImageQuality = imageQuality;
            this.Monitor = monitor;
            this.Resolution = resolution;
            this.CheckBlock = new Size(50, 1);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed) return;

            if (disposing)
            {
                if (_decodedBitmap != null)
                {
                    _decodedBitmap.Dispose();
                }
            }

            _disposed = true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int GetPixelSize(PixelFormat format)
        {
            switch (format)
            {
                case PixelFormat.Format24bppRgb:
                case PixelFormat.Format32bppRgb:
                    return 3;
                case PixelFormat.Format32bppArgb:
                case PixelFormat.Format32bppPArgb:
                    return 4;
                default:
                    throw new NotSupportedException(string.Format("Pixel format {0} is not supported", format));
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static unsafe byte* GetScan0Pointer(IntPtr scan0)
        {
            if (IntPtr.Size == 8)
            {
                return (byte*)scan0.ToInt64();
            }
            else
            {
                return (byte*)scan0.ToInt32();
            }
        }

        public unsafe void CodeImage(IntPtr scan0, Rectangle scanArea, Size imageSize, PixelFormat format, Stream outStream)
        {
            if (_disposed) throw new ObjectDisposedException("UnsafeStreamCodec");
            if (!outStream.CanWrite) throw new ArgumentException("Stream must be writable", "outStream");

            lock (_imageProcessLock)
            {
                byte* pScan0 = GetScan0Pointer(scan0);
                int pixelSize = GetPixelSize(format);
                int stride = imageSize.Width * pixelSize;
                int rawLength = stride * imageSize.Height;

                if (_encodeBuffer == null)
                {
                    InitializeEncodeBuffer(scan0, imageSize, format, stride, rawLength, outStream);
                    return;
                }

                ValidateImageFormat(format, imageSize);

                long startPosition = outStream.Position;
                outStream.Write(new byte[4], 0, 4);

                ProcessImageBlocks(pScan0, scanArea, stride, pixelSize, outStream);

                long totalLength = outStream.Position - startPosition - 4;
                outStream.Position = startPosition;
                outStream.Write(BitConverter.GetBytes(totalLength), 0, 4);
                outStream.Position = startPosition + 4 + totalLength;
            }
        }

        private unsafe void InitializeEncodeBuffer(IntPtr scan0, Size imageSize, PixelFormat format,
            int stride, int rawLength, Stream outStream)
        {
            _encodedFormat = format;
            _encodedWidth = imageSize.Width;
            _encodedHeight = imageSize.Height;
            _encodeBuffer = new byte[rawLength];
            EnsureDeltaBufferSize(rawLength);

            fixed (byte* ptr = _encodeBuffer)
            {
                NativeMethods.memcpy(new IntPtr(ptr), scan0, (uint)rawLength);
            }

            byte compressionFlag;
            byte[] framePayload = CompressBuffer(_encodeBuffer, rawLength, out compressionFlag);

            byte[] header = new byte[KeyframeHeaderSize];
            header[0] = CodecVersion;
            header[1] = PacketTypeKeyframe;
            header[2] = compressionFlag;
            header[3] = (byte)GetPixelSize(format);
            Array.Copy(BitConverter.GetBytes((int)format), 0, header, 4, 4);
            Array.Copy(BitConverter.GetBytes(_encodedWidth), 0, header, 8, 4);
            Array.Copy(BitConverter.GetBytes(_encodedHeight), 0, header, 12, 4);
            Array.Copy(BitConverter.GetBytes(stride), 0, header, 16, 4);
            Array.Copy(BitConverter.GetBytes(rawLength), 0, header, 20, 4);

            int payloadLength = header.Length + framePayload.Length;
            outStream.Write(BitConverter.GetBytes(payloadLength), 0, 4);
            outStream.Write(header, 0, header.Length);
            outStream.Write(framePayload, 0, framePayload.Length);
        }

        private void ValidateImageFormat(PixelFormat format, Size imageSize)
        {
            if (this._encodedFormat != format)
                throw new InvalidOperationException("PixelFormat differs from previous bitmap");

            if (this._encodedWidth != imageSize.Width || this._encodedHeight != imageSize.Height)
                throw new InvalidOperationException("Bitmap dimensions differ from previous bitmap");
        }

        private unsafe void ProcessImageBlocks(byte* pScan0, Rectangle scanArea, int stride,
            int pixelSize, Stream outStream)
        {
            _workingBlocks.Clear();
            _finalUpdates.Clear();

            fixed (byte* encBuffer = _encodeBuffer)
            {
                DetectChangedRows(encBuffer, pScan0, scanArea, stride, pixelSize);

                DetectChangedColumns(encBuffer, pScan0, scanArea, stride, pixelSize);

                if (_finalUpdates.Count > 0)
                {
                    WriteDeltaFrameHeader(outStream, pixelSize);
                    WriteCompressedBlocks(pScan0, stride, pixelSize, outStream);
                }
            }
        }

        private unsafe void DetectChangedRows(byte* encBuffer, byte* pScan0, Rectangle scanArea,
            int stride, int pixelSize)
        {
            Size blockSize = new Size(scanArea.Width, CheckBlock.Height);
            Size lastSize = new Size(scanArea.Width % CheckBlock.Width, scanArea.Height % CheckBlock.Height);
            int lastY = scanArea.Height - lastSize.Height;

            for (int y = scanArea.Y; y < scanArea.Height; y += blockSize.Height)
            {
                if (y == lastY)
                    blockSize = new Size(scanArea.Width, lastSize.Height);

                Rectangle currentBlock = new Rectangle(scanArea.X, y, scanArea.Width, blockSize.Height);
                int offset = (y * stride) + (scanArea.X * pixelSize);

                if (NativeMethods.memcmp(encBuffer + offset, pScan0 + offset, (uint)stride) != 0)
                {
                    if (_workingBlocks.Count > 0)
                    {
                        int lastIndex = _workingBlocks.Count - 1;
                        Rectangle lastBlock = _workingBlocks[lastIndex];

                        if (lastBlock.Y + lastBlock.Height == currentBlock.Y)
                        {
                            _workingBlocks[lastIndex] = new Rectangle(lastBlock.X, lastBlock.Y,
                                lastBlock.Width, lastBlock.Height + currentBlock.Height);
                            continue;
                        }
                    }

                    _workingBlocks.Add(currentBlock);
                }
            }
        }

        private unsafe void DetectChangedColumns(byte* encBuffer, byte* pScan0, Rectangle scanArea,
            int stride, int pixelSize)
        {
            Size lastSize = new Size(scanArea.Width % CheckBlock.Width, scanArea.Height % CheckBlock.Height);
            int lastX = scanArea.Width - lastSize.Width;

            for (int i = 0; i < _workingBlocks.Count; i++)
            {
                Rectangle block = _workingBlocks[i];
                Size columnSize = new Size(CheckBlock.Width, block.Height);

                for (int x = scanArea.X; x < scanArea.Width; x += columnSize.Width)
                {
                    if (x == lastX)
                        columnSize = new Size(lastSize.Width, block.Height);

                    Rectangle currentBlock = new Rectangle(x, block.Y, columnSize.Width, block.Height);

                    if (HasBlockChanged(encBuffer, pScan0, currentBlock, stride, pixelSize))
                    {
                        MergeOrAddBlock(currentBlock);
                    }
                }
            }
        }

        private unsafe bool HasBlockChanged(byte* encBuffer, byte* pScan0, Rectangle block,
            int stride, int pixelSize)
        {
            uint blockStride = (uint)(pixelSize * block.Width);

            for (int j = 0; j < block.Height; j++)
            {
                int blockOffset = (stride * (block.Y + j)) + (pixelSize * block.X);
                if (NativeMethods.memcmp(encBuffer + blockOffset, pScan0 + blockOffset, blockStride) != 0)
                    return true;
            }

            return false;
        }

        private void MergeOrAddBlock(Rectangle currentBlock)
        {
            if (_finalUpdates.Count > 0)
            {
                int lastIndex = _finalUpdates.Count - 1;
                Rectangle lastBlock = _finalUpdates[lastIndex];

                if (lastBlock.X + lastBlock.Width == currentBlock.X && lastBlock.Y == currentBlock.Y)
                {
                    _finalUpdates[lastIndex] = new Rectangle(lastBlock.X, lastBlock.Y,
                        lastBlock.Width + currentBlock.Width, lastBlock.Height);
                    return;
                }
            }

            _finalUpdates.Add(currentBlock);
        }

        private unsafe void WriteCompressedBlocks(byte* pScan0, int stride, int pixelSize, Stream outStream)
        {
            for (int i = 0; i < _finalUpdates.Count; i++)
            {
                WriteCompressedBlock(pScan0, _finalUpdates[i], stride, pixelSize, outStream);
            }
        }

        private unsafe void WriteCompressedBlock(byte* pScan0, Rectangle rect, int stride,
            int pixelSize, Stream outStream)
        {
            int blockStride = pixelSize * rect.Width;
            int blockSize = blockStride * rect.Height;
            EnsureDeltaBufferSize(blockSize);

            fixed (byte* previousPtr = _encodeBuffer)
            {
                int deltaOffset = 0;
                for (int j = 0; j < rect.Height; j++)
                {
                    int rowOffset = ((rect.Y + j) * stride) + (rect.X * pixelSize);
                    for (int k = 0; k < blockStride; k++)
                    {
                        byte current = pScan0[rowOffset + k];
                        byte previous = previousPtr[rowOffset + k];
                        _deltaBuffer[deltaOffset + k] = (byte)(current ^ previous);
                        previousPtr[rowOffset + k] = current;
                    }
                    deltaOffset += blockStride;
                }
            }

            byte compressionFlag;
            byte[] payload = CompressBuffer(_deltaBuffer, blockSize, out compressionFlag);
            BlockFlags flags = compressionFlag == CompressionFlagDeflate ? BlockFlags.Compressed : BlockFlags.None;

            Array.Copy(BitConverter.GetBytes(rect.X), 0, _metadataBuffer, 0, 4);
            Array.Copy(BitConverter.GetBytes(rect.Y), 0, _metadataBuffer, 4, 4);
            Array.Copy(BitConverter.GetBytes(rect.Width), 0, _metadataBuffer, 8, 4);
            Array.Copy(BitConverter.GetBytes(rect.Height), 0, _metadataBuffer, 12, 4);
            Array.Copy(BitConverter.GetBytes(payload.Length), 0, _metadataBuffer, 16, 4);
            Array.Copy(BitConverter.GetBytes((int)flags), 0, _metadataBuffer, 20, 4);

            outStream.Write(_metadataBuffer, 0, BlockMetadataSize);
            outStream.Write(payload, 0, payload.Length);
        }

        private void WriteDeltaFrameHeader(Stream outStream, int pixelSize)
        {
            byte[] header = new byte[DeltaFrameHeaderSize];
            header[0] = CodecVersion;
            header[1] = PacketTypeDelta;
            header[2] = CompressionFlagNone;
            header[3] = (byte)pixelSize;
            outStream.Write(header, 0, header.Length);
        }

        private void EnsureDeltaBufferSize(int requiredSize)
        {
            if (_deltaBuffer == null || _deltaBuffer.Length < requiredSize)
            {
                _deltaBuffer = new byte[requiredSize];
            }
        }

        private void EnsureCompressedBufferSize(int requiredSize)
        {
            if (_compressedBlockBuffer == null || _compressedBlockBuffer.Length < requiredSize)
            {
                _compressedBlockBuffer = new byte[requiredSize];
            }
        }

        private void EnsureDecodedFrameBufferSize(int requiredSize)
        {
            if (_decodedFrameBuffer == null || _decodedFrameBuffer.Length < requiredSize)
            {
                _decodedFrameBuffer = new byte[requiredSize];
            }
        }

        private byte[] CompressBuffer(byte[] buffer, int length, out byte compressionFlag)
        {
            if (length <= 0)
            {
                compressionFlag = CompressionFlagNone;
                return Array.Empty<byte>();
            }

            if (CompressionLevel == CompressionLevel.NoCompression)
            {
                compressionFlag = CompressionFlagNone;
                byte[] rawCopy = new byte[length];
                Buffer.BlockCopy(buffer, 0, rawCopy, 0, length);
                return rawCopy;
            }

            using (var ms = new MemoryStream())
            {
                using (var ds = new DeflateStream(ms, CompressionLevel, true))
                {
                    ds.Write(buffer, 0, length);
                }
                byte[] compressed = ms.ToArray();
                if (compressed.Length >= length)
                {
                    compressionFlag = CompressionFlagNone;
                    byte[] rawCopy = new byte[length];
                    Buffer.BlockCopy(buffer, 0, rawCopy, 0, length);
                    return rawCopy;
                }

                compressionFlag = CompressionFlagDeflate;
                return compressed;
            }
        }

        private void DecompressInto(byte[] source, int sourceLength, byte[] destination, int expectedLength, byte compressionFlag)
        {
            if (compressionFlag == CompressionFlagNone)
            {
                if (sourceLength < expectedLength)
                    throw new InvalidDataException("Insufficient uncompressed data.");

                Buffer.BlockCopy(source, 0, destination, 0, expectedLength);
                return;
            }

            if (compressionFlag != CompressionFlagDeflate)
                throw new InvalidDataException("Unsupported compression flag.");

            using (var ms = new MemoryStream(source, 0, sourceLength))
            using (var ds = new DeflateStream(ms, CompressionMode.Decompress))
            {
                int offset = 0;
                while (offset < expectedLength)
                {
                    int read = ds.Read(destination, offset, expectedLength - offset);
                    if (read <= 0)
                        throw new InvalidDataException("Unexpected end of compressed block.");
                    offset += read;
                }
            }
        }

        private unsafe void ApplyFrameBufferToBitmap()
        {
            if (_decodedBitmap == null || _decodedFrameBuffer == null)
                return;

            BitmapData data = null;

            try
            {
                data = _decodedBitmap.LockBits(new Rectangle(0, 0, _decodedBitmap.Width, _decodedBitmap.Height),
                    ImageLockMode.WriteOnly, _decodedFormat);

                fixed (byte* srcPtr = _decodedFrameBuffer)
                {
                    byte* destPtr = (byte*)data.Scan0.ToPointer();
                    for (int y = 0; y < _decodedBitmap.Height; y++)
                    {
                        IntPtr src = new IntPtr(srcPtr + (y * _decodedStride));
                        IntPtr dest = new IntPtr(destPtr + (y * data.Stride));
                        NativeMethods.memcpy(dest, src, (uint)Math.Min(_decodedStride, data.Stride));
                    }
                }
            }
            finally
            {
                if (data != null)
                {
                    _decodedBitmap.UnlockBits(data);
                }
            }
        }

        private void ApplyDeltaToFrame(Rectangle rect, int blockStride)
        {
            int deltaOffset = 0;
            for (int j = 0; j < rect.Height; j++)
            {
                int rowOffset = ((rect.Y + j) * _decodedStride) + (rect.X * _decodedPixelSize);
                for (int k = 0; k < blockStride; k++)
                {
                    _decodedFrameBuffer[rowOffset + k] ^= _deltaBuffer[deltaOffset + k];
                }
                deltaOffset += blockStride;
            }
        }

        public unsafe Bitmap DecodeData(IntPtr codecBuffer, uint length)
        {
            if (_disposed) throw new ObjectDisposedException("UnsafeStreamCodec");
            if (length < 4) return _decodedBitmap;

            int managedLength = checked((int)length);
            byte[] managed = new byte[managedLength];
            fixed (byte* dest = managed)
            {
                NativeMethods.memcpy(new IntPtr(dest), codecBuffer, length);
            }

            using (MemoryStream stream = new MemoryStream(managed, 0, managedLength, writable: false, publiclyVisible: true))
            {
                return DecodeData(stream);
            }
        }

        public Bitmap DecodeData(Stream inStream)
        {
            if (_disposed) throw new ObjectDisposedException("UnsafeStreamCodec");

            ReadFully(inStream, _lengthBuffer, 0, 4);
            int dataSize = BitConverter.ToInt32(_lengthBuffer, 0);

            if (dataSize <= 0)
            {
                return _decodedBitmap;
            }

            byte[] payload = new byte[dataSize];
            ReadFully(inStream, payload, 0, dataSize);

            using (MemoryStream payloadStream = new MemoryStream(payload, writable: false))
            {
                return DecodePayload(payloadStream, dataSize);
            }
        }

        private Bitmap DecodePayload(Stream payloadStream, int payloadLength)
        {
            int version = payloadStream.ReadByte();
            if (version < 0)
                return _decodedBitmap;

            if (version != CodecVersion)
                throw new InvalidDataException($"Unsupported codec version {version}.");

            int packetTypeValue = payloadStream.ReadByte();
            if (packetTypeValue < 0)
                throw new InvalidDataException("Missing packet type.");

            byte packetType = (byte)packetTypeValue;
            byte compressionFlag = (byte)payloadStream.ReadByte();
            byte pixelSize = (byte)payloadStream.ReadByte();

            if (packetType == PacketTypeKeyframe)
            {
                return DecodeKeyframe(payloadStream, payloadLength, compressionFlag, pixelSize);
            }

            if (packetType == PacketTypeDelta)
            {
                return DecodeDelta(payloadStream, payloadLength, compressionFlag, pixelSize);
            }

            throw new InvalidDataException($"Unknown packet type {packetType}.");
        }

        private Bitmap DecodeKeyframe(Stream payloadStream, int payloadLength, byte compressionFlag, byte pixelSize)
        {
            int pixelFormatValue = ReadInt32(payloadStream);
            int width = ReadInt32(payloadStream);
            int height = ReadInt32(payloadStream);
            int stride = ReadInt32(payloadStream);
            int rawLength = ReadInt32(payloadStream);

            int remaining = payloadLength - KeyframeHeaderSize;
            if (remaining < 0)
                throw new InvalidDataException("Invalid keyframe payload length.");

            if (width <= 0 || height <= 0)
                throw new InvalidDataException("Invalid keyframe dimensions.");

            if (stride <= 0 || rawLength <= 0)
                throw new InvalidDataException("Invalid keyframe buffer parameters.");

            PixelFormat format = (PixelFormat)pixelFormatValue;
            int expectedPixelSize = GetPixelSize(format);
            if (pixelSize != expectedPixelSize)
                throw new InvalidDataException("Pixel size mismatch for keyframe.");

            if (rawLength != stride * height)
                throw new InvalidDataException("Keyframe raw length does not match stride and height.");

            byte[] compressedFrame = new byte[remaining];
            ReadFully(payloadStream, compressedFrame, 0, remaining);

            EnsureDecodedFrameBufferSize(rawLength);
            DecompressInto(compressedFrame, remaining, _decodedFrameBuffer, rawLength, compressionFlag);

            _decodedStride = stride;
            _decodedFormat = format;
            _decodedPixelSize = expectedPixelSize;

            if (_decodedBitmap != null)
            {
                _decodedBitmap.Dispose();
            }

            _decodedBitmap = new Bitmap(width, height, _decodedFormat);
            ApplyFrameBufferToBitmap();

            return _decodedBitmap;
        }

        private Bitmap DecodeDelta(Stream payloadStream, int payloadLength, byte headerCompressionFlag, byte pixelSize)
        {
            if (_decodedBitmap == null || _decodedFrameBuffer == null)
                throw new InvalidOperationException("Delta frame received before keyframe.");

            if (headerCompressionFlag != CompressionFlagNone)
                throw new InvalidDataException("Unsupported delta frame compression flag.");

            if (pixelSize != _decodedPixelSize)
                throw new InvalidDataException("Pixel size mismatch for delta frame.");

            if (payloadLength < DeltaFrameHeaderSize)
                throw new InvalidDataException("Delta frame header is incomplete.");

            int remainingData = payloadLength - DeltaFrameHeaderSize;
            DecodeDeltaBlocks(payloadStream, remainingData);
            ApplyFrameBufferToBitmap();
            return _decodedBitmap;
        }

        private void DecodeDeltaBlocks(Stream payloadStream, int remainingData)
        {
            while (remainingData > 0)
            {
                if (remainingData < BlockMetadataSize)
                    throw new InvalidDataException("Incomplete block metadata.");

                ReadFully(payloadStream, _metadataBuffer, 0, BlockMetadataSize);
                remainingData -= BlockMetadataSize;

                Rectangle rect = new Rectangle(
                    BitConverter.ToInt32(_metadataBuffer, 0),
                    BitConverter.ToInt32(_metadataBuffer, 4),
                    BitConverter.ToInt32(_metadataBuffer, 8),
                    BitConverter.ToInt32(_metadataBuffer, 12));

                if (rect.Width <= 0 || rect.Height <= 0)
                    throw new InvalidDataException("Invalid block dimensions in delta frame.");

                if (rect.Right > _decodedBitmap.Width || rect.Bottom > _decodedBitmap.Height)
                    throw new InvalidDataException("Delta block exceeds bitmap bounds.");

                int updateLength = BitConverter.ToInt32(_metadataBuffer, 16);
                BlockFlags flags = (BlockFlags)BitConverter.ToInt32(_metadataBuffer, 20);

                if (updateLength < 0 || updateLength > remainingData)
                    throw new InvalidDataException("Invalid block payload length.");

                EnsureCompressedBufferSize(updateLength);
                ReadFully(payloadStream, _compressedBlockBuffer, 0, updateLength);
                remainingData -= updateLength;

                int blockStride = rect.Width * _decodedPixelSize;
                int blockSize = blockStride * rect.Height;
                EnsureDeltaBufferSize(blockSize);

                byte compressionFlag = (flags & BlockFlags.Compressed) != 0 ? CompressionFlagDeflate : CompressionFlagNone;
                DecompressInto(_compressedBlockBuffer, updateLength, _deltaBuffer, blockSize, compressionFlag);
                ApplyDeltaToFrame(rect, blockStride);
            }
        }

        private int ReadInt32(Stream stream)
        {
            ReadFully(stream, _lengthBuffer, 0, 4);
            return BitConverter.ToInt32(_lengthBuffer, 0);
        }

        private void ReadFully(Stream stream, byte[] buffer, int offset, int count)
        {
            int totalRead = 0;
            while (totalRead < count)
            {
                int read = stream.Read(buffer, offset + totalRead, count - totalRead);
                if (read <= 0)
                    throw new EndOfStreamException("Unexpected end of stream.");
                totalRead += read;
            }
        }
    }
}