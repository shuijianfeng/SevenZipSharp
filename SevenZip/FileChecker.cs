namespace SevenZip
{
    using System;
    using System.IO;

#if UNMANAGED
    /// <summary>
    /// 签名检查器类。原始代码由 Siddharth Uppal 编写，由 Markhor 改编。
    /// </summary>
    /// <remarks>基于 http://blog.somecreativity.com/2008/04/08/how-to-check-if-a-file-is-compressed-in-c/# 上的代码</remarks>
    internal static class FileChecker
    {
        private const int SIGNATURE_SIZE = 21;
        private const int SFX_SCAN_LENGTH = 256 * 1024;

        private static bool SpecialDetect(Stream stream, int offset, InArchiveFormat expectedFormat)
        {
            if (stream.Length <= offset + SIGNATURE_SIZE)
            {
                return false;
            }

            var signature = new byte[SIGNATURE_SIZE];
            ReadExact(stream, offset, signature);

            // 快速路径：直接将原始字节与预期签名进行比较，
            // 而无需将整个缓冲区转换为十六进制字符串。
            if (!Formats.InSignatureFormatsReversed.TryGetValue(expectedFormat, out var expectedHex))
            {
                return false;
            }

            return HexStartsWith(signature, expectedHex);
        }

        /// <summary>
        /// 获取特定扩展名对应的 InArchiveFormat。
        /// </summary>
        /// <param name="stream">要识别的流。</param>
        /// <param name="offset">归档起始偏移量。</param>
        /// <param name="isExecutable">如果流的原始格式为 PE，则为 true；否则为 false。</param>
        /// <returns>对应的 InArchiveFormat。</returns>
        public static InArchiveFormat CheckSignature(Stream stream, out int offset, out bool isExecutable)
        {
            offset = 0;

            if (!stream.CanRead)
            {
                throw new ArgumentException("The stream must be readable.");
            }

            if (stream.Length < SIGNATURE_SIZE)
            {
                throw new ArgumentException("The stream is invalid.");
            }

            #region Get file signature

            var signature = new byte[SIGNATURE_SIZE];
            ReadExact(stream, 0, signature);

            #endregion

            var suspectedFormat = InArchiveFormat.XZ; // 除 PE 和 Cab 之外的任意格式
            isExecutable = false;

            foreach (var (expectedSignature, detectedFormat) in Formats.InSignatureFormats)
            {
                if (HexStartsWith(signature, expectedSignature) ||
                    HexStartsWith(signature, 6, expectedSignature) &&
                    detectedFormat == InArchiveFormat.Lzh)
                {
                    if (detectedFormat == InArchiveFormat.PE)
                    {
                        suspectedFormat = InArchiveFormat.PE;
                        isExecutable = true;
                    }
                    else
                    {
                        return detectedFormat;
                    }
                }
            }

            // 许多 Microsoft 格式
            if (HexStartsWith(signature, "D0-CF-11-E0-A1-B1-1A-E1"))
            {
                suspectedFormat = InArchiveFormat.Cab; // != InArchiveFormat.XZ
            }

            #region SpecialDetect

            try
            {
                SpecialDetect(stream, 257, InArchiveFormat.Tar);
            }
            catch (ArgumentException) {}

            if (SpecialDetect(stream, 0x8001, InArchiveFormat.Iso))
            {
                return InArchiveFormat.Iso;
            }

            if (SpecialDetect(stream, 0x8801, InArchiveFormat.Iso))
            {
                return InArchiveFormat.Iso;
            }

            if (SpecialDetect(stream, 0x9001, InArchiveFormat.Iso))
            {
                return InArchiveFormat.Iso;
            }

            if (SpecialDetect(stream, 0x200, InArchiveFormat.Gpt))
            {
                return InArchiveFormat.Gpt;
            }

            if (SpecialDetect(stream, 0x400, InArchiveFormat.Hfs))
            {
                return InArchiveFormat.Hfs;
            }

            #region Last resort for tar - can mistake

            if (stream.Length >= 1024)
            {
                stream.Seek(-1024, SeekOrigin.End);
                var buf = new byte[1024];
                var read = 0;
                while (read < 1024)
                {
                    var n = stream.Read(buf, read, 1024 - read);
                    if (n <= 0) break;
                    read += n;
                }
                var isTar = true;

                for (var i = 0; i < read; i++)
                {
                    if (buf[i] != 0)
                    {
                        isTar = false;
                        break;
                    }
                }

                if (isTar)
                {
                    return InArchiveFormat.Tar;
                }
            }

            #endregion

            #endregion

            #region Check if it is an SFX archive or a file with an embedded archive.

            if (suspectedFormat != InArchiveFormat.XZ)
            {
                #region Get first Min(stream.Length, SFX_SCAN_LENGTH) bytes

                var scanLength = Math.Min(stream.Length, SFX_SCAN_LENGTH);
                var scanLengthInt = (int)scanLength;
                signature = new byte[scanLengthInt];
                ReadExact(stream, 0, signature);

                #endregion

                foreach (var format in new[] 
                {
                    InArchiveFormat.Zip, 
                    InArchiveFormat.SevenZip,
                    InArchiveFormat.Rar4,
                    InArchiveFormat.Rar,
                    InArchiveFormat.Cab,
                    InArchiveFormat.Arj
                })
                {
                    var expectedSig = Formats.InSignatureFormatsReversed[format];
                    var pos = HexIndexOf(signature, expectedSig);

                    if (pos > -1)
                    {
                        offset = pos / 3;
                        return format;
                    }
                }

                // 没有找到
                if (suspectedFormat == InArchiveFormat.PE)
                {
                    return InArchiveFormat.PE;
                }
            }

            #endregion

            throw new ArgumentException("The stream is invalid or no corresponding signature was found.");
        }

        /// <summary>
        /// 获取特定文件名对应的 InArchiveFormat。
        /// </summary>
        /// <param name="fileName">归档文件名。</param>
        /// <param name="offset">归档起始偏移量。</param>
        /// <param name="isExecutable">如果文件的原始格式为 PE，则为 true；否则为 false。</param>
        /// <returns>对应的 InArchiveFormat。</returns>
        /// <exception cref="System.ArgumentException"/>
        public static InArchiveFormat CheckSignature(string fileName, out int offset, out bool isExecutable)
        {
            using (var fs = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, bufferSize: 4096, FileOptions.SequentialScan))
            {
                try
                {
                    return CheckSignature(fs, out offset, out isExecutable);
                }
                catch (ArgumentException)
                {
                    offset = 0;
                    isExecutable = false;
                    return Formats.FormatByFileName(fileName, true);
                }
            }
        }

        #region Hex helpers (avoid allocating full hex strings)

        private static void ReadExact(Stream stream, int offset, byte[] buffer)
        {
            stream.Seek(offset, SeekOrigin.Begin);
            var bytesRequired = buffer.Length;
            var index = 0;
            while (bytesRequired > 0)
            {
                var bytesRead = stream.Read(buffer, index, bytesRequired);
                if (bytesRead <= 0) break;
                bytesRequired -= bytesRead;
                index += bytesRead;
            }
        }

        private static readonly byte[] s_hexUpper = "0123456789ABCDEF-"u8.ToArray();

        /// <summary>
        /// 检查字节缓冲区转换为十六进制字符串后是否以
        /// <paramref name="expectedHex"/> 开头。避免分配完整的十六进制字符串。
        /// </summary>
        private static bool HexStartsWith(byte[] data, string expectedHex)
        {
            var hexLen = expectedHex.Length;
            // 每个字节产生 3 个十六进制字符（"XX-"）；最后一个字节没有尾随的 '-'。
            // 产生 hexLen 个字符所需的字节数：ceil((hexLen + 1) / 3)。
            var bytesNeeded = (hexLen + 1 + 2) / 3;
            if (bytesNeeded > data.Length)
            {
                return false;
            }

            var hi = 0;
            for (var i = 0; i < bytesNeeded; i++)
            {
                var b = data[i];
                if (expectedHex[hi++] != s_hexUpper[b >> 4]) return false;
                if (hi >= hexLen) return true;
                if (expectedHex[hi++] != s_hexUpper[b & 0xF]) return false;
                if (hi >= hexLen) return true;
                if (hi < hexLen)
                {
                    if (expectedHex[hi++] != '-') return false;
                }
            }
            return true;
        }

        /// <summary>
        /// 检查从 <paramref name="dataOffset"/> 字节开始的字节缓冲区
        /// 是否产生以 <paramref name="expectedHex"/> 开头的十六进制字符串。
        /// </summary>
        private static bool HexStartsWith(byte[] data, int dataOffset, string expectedHex)
        {
            var hexLen = expectedHex.Length;
            var bytesNeeded = (hexLen + 1 + 2) / 3;
            if (dataOffset + bytesNeeded > data.Length)
            {
                return false;
            }

            var hi = 0;
            for (var i = 0; i < bytesNeeded; i++)
            {
                var b = data[dataOffset + i];
                if (expectedHex[hi++] != s_hexUpper[b >> 4]) return false;
                if (hi >= hexLen) return true;
                if (expectedHex[hi++] != s_hexUpper[b & 0xF]) return false;
                if (hi >= hexLen) return true;
                if (hi < hexLen)
                {
                    if (expectedHex[hi++] != '-') return false;
                }
            }
            return true;
        }

        /// <summary>
        /// 在 <paramref name="data"/> 中查找十六进制表示以
        /// <paramref name="expectedHex"/> 开头的字节偏移量。返回十六进制字符串索引（字节
        /// 索引 * 3），如果未找到则返回 -1。
        /// </summary>
        private static int HexIndexOf(byte[] data, string expectedHex)
        {
            var hexLen = expectedHex.Length;
            var bytesNeeded = (hexLen + 1 + 2) / 3;
            if (bytesNeeded > data.Length)
            {
                return -1;
            }

            // 逐字节扫描；第 i 个字节的十六进制字符串从索引 i*3 开始。
            var maxByte = data.Length - bytesNeeded;
            for (var start = 0; start <= maxByte; start++)
            {
                var hi = 0;
                var ok = true;
                for (var i = 0; i < bytesNeeded && ok; i++)
                {
                    var b = data[start + i];
                    if (expectedHex[hi++] != s_hexUpper[b >> 4]) { ok = false; break; }
                    if (hi >= hexLen) break;
                    if (expectedHex[hi++] != s_hexUpper[b & 0xF]) { ok = false; break; }
                    if (hi >= hexLen) break;
                    if (hi < hexLen)
                    {
                        if (expectedHex[hi++] != '-') { ok = false; break; }
                    }
                }
                if (ok)
                {
                    return start * 3;
                }
            }
            return -1;
        }

        #endregion
    }
#endif
}