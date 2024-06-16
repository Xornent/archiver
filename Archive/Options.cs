using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Archiver.Archive
{
    /// <summary>
    /// Zip encryption method enum.
    /// </summary>
    public enum ZipEncryptionMethod
    {
        /// <summary>
        /// ZipCrypto encryption method.
        /// </summary>
        ZipCrypto,
        /// <summary>
        /// AES 128 bit encryption method.
        /// </summary>
        Aes128,
        /// <summary>
        /// AES 192 bit encryption method.
        /// </summary>
        Aes192,
        /// <summary>
        /// AES 256 bit encryption method.
        /// </summary>
        Aes256
    }

    internal enum InternalCompressionMode
    {
        /// <summary>
        /// Create a new archive; overwrite the existing one.
        /// </summary>
        Create,
        /// <summary>
        /// Add data to the archive.
        /// </summary>
        Append,
        /// <summary>
        /// Modify archive data.
        /// </summary>
        Modify
    }

    /// <summary>
    /// Archive compression mode.
    /// </summary>
    public enum CompressionMode
    {
        /// <summary>
        /// Create a new archive; overwrite the existing one.
        /// </summary>
        Create,
        /// <summary>
        /// Add data to the archive.
        /// </summary>
        Append,
    }
}
