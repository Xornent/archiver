
using System;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using Archiver.Exceptions;

namespace Archiver.Library
{
    internal static class LibraryInterface
    {
        /// <summary>
        /// Synchronization root for all locking.
        /// </summary>
        private static readonly object SyncRoot = new object();

        /// <summary>
        /// Path to the 7-zip dll.
        /// </summary>
        /// 
        /// 7zxa.dll supports only decoding from .7z archives.
        /// Features of 7za.dll: 
        ///     - Supporting 7z format;
        ///     - Built encoders: LZMA, PPMD, BCJ, BCJ2, COPY, AES-256 Encryption.
        ///     - Built decoders: LZMA, PPMD, BCJ, BCJ2, COPY, AES-256 Encryption, BZip2, Deflate.
        /// 7z.dll (from the 7-zip distribution) supports every InArchiveFormat for encoding and decoding.

        private static string? _libraryFileName;

        private static string DetermineLibraryFilePath()
        {
            if (!string.IsNullOrEmpty(ConfigurationManager.AppSettings["7zLocation"]))
                return ConfigurationManager.AppSettings["7zLocation"] 
                    ?? throw new Exceptions.SevenZipLibraryException("Undetermined library location");

            if (string.IsNullOrEmpty(Assembly.GetExecutingAssembly().Location))
                throw new Exceptions.SevenZipLibraryException("Undetermined library location");

            return Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? "", 
                Environment.Is64BitProcess ? "7z64.dll" : "7z.dll");
        }

        /// <summary>
        /// 7-zip library handle.
        /// </summary>
        private static IntPtr _modulePtr;

        /// <summary>
        /// 7-zip library features.
        /// </summary>
        private static SupportedFeatures? _features;

        private static Dictionary<object, Dictionary<InArchiveFormat, IInArchive?>>? _inArchives = null;
        private static Dictionary<object, Dictionary<OutArchiveFormat, IOutArchive?>>? _outArchives = null;
        private static int _totalUsers;
        private static bool? _modifyCapable;

        private static void InitUserInFormat(object user, InArchiveFormat format)
        {
            if (_inArchives == null) throw new Exception();
            if (!_inArchives.ContainsKey(user))
                _inArchives.Add(user, new Dictionary<InArchiveFormat, IInArchive?>());

            if (!_inArchives[user].ContainsKey(format))
            {
                _inArchives[user].Add(format, null);
                _totalUsers++;
            }
        }

        private static void InitUserOutFormat(object user, OutArchiveFormat format)
        {
            if (_outArchives == null) throw new Exception();
            if (!_outArchives.ContainsKey(user))
                _outArchives.Add(user, new Dictionary<OutArchiveFormat, IOutArchive?>());
            
            if (!_outArchives[user].ContainsKey(format))
            {
                _outArchives[user].Add(format, null);
                _totalUsers++;
            }
        }

        private static void Init()
        {
            _inArchives = new Dictionary<object, Dictionary<InArchiveFormat, IInArchive?>>();
            _outArchives = new Dictionary<object, Dictionary<OutArchiveFormat, IOutArchive?>>();
        }

        /// <summary>
        /// Loads the 7-zip library if necessary and adds user to the reference list
        /// </summary>
        /// <param name="user">Caller of the function</param>
        /// <param name="format">Archive format</param>
        public static void LoadLibrary(object user, Enum format)
        {
            lock (SyncRoot)
            {
                // init during the initialization of the class.
                if (_inArchives == null || _outArchives == null) Init();

                if (_modulePtr == IntPtr.Zero)
                {
                    if (_libraryFileName == null)
                        _libraryFileName = DetermineLibraryFilePath();

                    if (!File.Exists(_libraryFileName))
                        throw new SevenZipLibraryException("library file does not exist.");

                    if ((_modulePtr = NativeMethods.LoadLibrary(_libraryFileName)) == IntPtr.Zero)
                        throw new SevenZipLibraryException($"failed to load library from \"{_libraryFileName}\".");

                    if (NativeMethods.GetProcAddress(_modulePtr, "GetHandlerProperty") == IntPtr.Zero)
                    {
                        NativeMethods.FreeLibrary(_modulePtr);
                        throw new SevenZipLibraryException("library is invalid.");
                    }
                }

                if (format is InArchiveFormat archiveFormat)
                {
                    InitUserInFormat(user, archiveFormat);
                    return;
                }

                if (format is OutArchiveFormat outArchiveFormat)
                {
                    InitUserOutFormat(user, outArchiveFormat);
                    return;
                }

                throw new ArgumentException($"Enum {format} is not a valid archive format attribute!");
            }
        }

        /// <summary>
        /// Gets the value indicating whether the library supports modifying archives.
        /// </summary>
        public static bool ModifyCapable
        {
            get
            {
                lock (SyncRoot)
                {
                    if (!_modifyCapable.HasValue)
                    {
                        if (_libraryFileName == null)
                        {
                            _libraryFileName = DetermineLibraryFilePath();
                        }

                        var dllVersionInfo = FileVersionInfo.GetVersionInfo(_libraryFileName);
                        _modifyCapable = dllVersionInfo.FileMajorPart >= 9;
                    }

                    return _modifyCapable.Value;
                }
            }
        }

        public static SupportedFeatures CurrentLibraryFeatures
        {
            get
            {
                // i disabled the benchmarking capabilities of the source code. 
                // use the library file provided only, and tests are conducted manually.
                return SupportedFeatures.CompressAll | 
                    SupportedFeatures.ExtractAll | 
                    SupportedFeatures.Modify;
            }
        }

        /// <summary>
        /// Removes user from reference list and frees the 7-zip library if it becomes empty
        /// </summary>
        /// <param name="user">Caller of the function</param>
        /// <param name="format">Archive format</param>
        public static void FreeLibrary(object user, Enum format)
        {
            lock (SyncRoot)
            {
                if (_modulePtr != IntPtr.Zero)
                {
                    if (format is InArchiveFormat archiveFormat)
                    {
                        if (_inArchives != null && _inArchives.ContainsKey(user) &&
                            _inArchives[user].ContainsKey(archiveFormat) &&
                            _inArchives[user][archiveFormat] != null)
                        {
                            try
                            {
                                object? obj = _inArchives[user][archiveFormat];
                                if (obj != null) Marshal.ReleaseComObject(obj);

                            } catch (InvalidComObjectException) { }

                            _inArchives[user].Remove(archiveFormat);
                            _totalUsers--;

                            if (_inArchives[user].Count == 0)
                            {
                                _inArchives.Remove(user);
                            }
                        }
                    }

                    if (format is OutArchiveFormat outArchiveFormat)
                    {
                        if (_outArchives != null && _outArchives.ContainsKey(user) &&
                            _outArchives[user].ContainsKey(outArchiveFormat) &&
                            _outArchives[user][outArchiveFormat] != null)
                        {
                            try
                            {
                                object? obj = _outArchives[user][outArchiveFormat];
                                if (obj != null) Marshal.ReleaseComObject(obj);

                            } catch (InvalidComObjectException) { }

                            _outArchives[user].Remove(outArchiveFormat);
                            _totalUsers--;

                            if (_outArchives[user].Count == 0)
                            {
                                _outArchives.Remove(user);
                            }
                        }
                    }

                    if ((_inArchives == null || _inArchives.Count == 0) && (_outArchives == null || _outArchives.Count == 0))
                    {
                        _inArchives = null;
                        _outArchives = null;

                        if (_totalUsers == 0)
                        {
                            NativeMethods.FreeLibrary(_modulePtr);
                            _modulePtr = IntPtr.Zero;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Gets IInArchive interface to extract 7-zip archives.
        /// </summary>
        /// <param name="format">Archive format.</param>
        /// <param name="user">Archive format user.</param>
        public static IInArchive InArchive(InArchiveFormat format, object user)
        {
            if (_inArchives == null) throw new Exception();
            lock (SyncRoot)
            {
                if (!_inArchives.ContainsKey(user) || _inArchives[user][format] == null)
                {
                    if (_modulePtr == IntPtr.Zero)
                    {
                        LoadLibrary(user, format);

                        if (_modulePtr == IntPtr.Zero)
                        {
                            throw new SevenZipLibraryException();
                        }
                    }

                    var createObject = (NativeMethods.CreateObjectDelegate)
                        Marshal.GetDelegateForFunctionPointer(
                            NativeMethods.GetProcAddress(_modulePtr, "CreateObject"),
                            typeof(NativeMethods.CreateObjectDelegate));

                    if (createObject == null)
                        throw new SevenZipLibraryException();

                    object result;
                    var interfaceId = typeof(IInArchive).GUID;
                    var classId = Formats.InFormatGuids[format];

                    try
                    {
                        createObject(ref classId, ref interfaceId, out result);
                    } 
                    catch (Exception)
                    {
                        throw new SevenZipLibraryException("Your 7-zip library does not support this archive type.");
                    }

                    InitUserInFormat(user, format);
                    _inArchives[user][format] = result as IInArchive;
                }

                return _inArchives[user][format] ?? throw new Exception();
            }
        }

        /// <summary>
        /// Gets IOutArchive interface to pack 7-zip archives.
        /// </summary>
        /// <param name="format">Archive format.</param>  
        /// <param name="user">Archive format user.</param>
        public static IOutArchive OutArchive(OutArchiveFormat format, object user)
        {
            if (_outArchives == null) throw new Exception();
            lock (SyncRoot)
            {
                if (_outArchives[user][format] == null)
                {
                    if (_modulePtr == IntPtr.Zero)
                        throw new SevenZipLibraryException();

                    var createObject = (NativeMethods.CreateObjectDelegate)
                        Marshal.GetDelegateForFunctionPointer(
                            NativeMethods.GetProcAddress(_modulePtr, "CreateObject"),
                            typeof(NativeMethods.CreateObjectDelegate));
                    var interfaceId = typeof(IOutArchive).GUID;

                    try
                    {
                        var classId = Formats.OutFormatGuids[format];
                        createObject(ref classId, ref interfaceId, out var result);

                        InitUserOutFormat(user, format);
                        _outArchives[user][format] = result as IOutArchive;
                    } 
                    catch (Exception)
                    {
                        throw new SevenZipLibraryException("Your 7-zip library does not support this archive type.");
                    }
                }

                return _outArchives[user][format] ?? throw new Exception();
            }
        }

        public static void SetLibraryPath(string libraryPath)
        {
            // impossible, in this case, exception must have thrown.
            if (_libraryFileName == null) return;

            if (_modulePtr != IntPtr.Zero && 
                !Path.GetFullPath(libraryPath).Equals(
                    Path.GetFullPath(_libraryFileName), 
                    StringComparison.OrdinalIgnoreCase ))
                throw new SevenZipLibraryException(
                    $"can not change the library path while the library \"{_libraryFileName}\" is being used.");

            if (!File.Exists(libraryPath))
                throw new SevenZipLibraryException(
                    $"can not change the library path because the file \"{libraryPath}\" does not exist.");

            _libraryFileName = libraryPath;
            _features = null;
        }
    }
}
