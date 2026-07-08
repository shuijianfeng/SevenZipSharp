#if UNMANAGED

namespace SevenZip
{
    /// <summary>
    /// Zip 加密方法枚举。
    /// </summary>
    public enum ZipEncryptionMethod
    {
        /// <summary>
        /// ZipCrypto 加密方法。
        /// </summary>
        ZipCrypto,
        /// <summary>
        /// AES 128 位加密方法。
        /// </summary>
        Aes128,
        /// <summary>
        /// AES 192 位加密方法。
        /// </summary>
        Aes192,
        /// <summary>
        /// AES 256 位加密方法。
        /// </summary>
        Aes256
    }
}

#endif
