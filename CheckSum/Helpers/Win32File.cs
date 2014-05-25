using CheckSum.Res;
using Microsoft.Win32.SafeHandles;
using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;

namespace CheckSum.Helpers
{
    internal class Win32File
    {

        // Error
        private const int ERROR_ALREADY_EXISTS = 183;
        // seek location
        private const uint FILE_BEGIN = 0x0;
        private const uint FILE_CURRENT = 0x1;
        private const uint FILE_END = 0x2;


        // access
        private const uint GENERIC_READ = 0x80000000;
        private const uint GENERIC_WRITE = 0x40000000;
        private const uint GENERIC_EXECUTE = 0x20000000;
        private const uint GENERIC_ALL = 0x10000000;

        private const uint FILE_APPEND_DATA = 0x00000004;

        // attribute
        private const uint FILE_ATTRIBUTE_NORMAL = 0x80;

        // share
        private const uint FILE_SHARE_DELETE = 0x00000004;
        private const uint FILE_SHARE_READ = 0x00000001;
        private const uint FILE_SHARE_WRITE = 0x00000002;

        //mode
        private const uint CREATE_NEW = 1;
        private const uint CREATE_ALWAYS = 2;
        private const uint OPEN_EXISTING = 3;
        private const uint OPEN_ALWAYS = 4;
        private const uint TRUNCATE_EXISTING = 5;


        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern SafeFileHandle CreateFileW(string lpFileName, uint dwDesiredAccess,
            uint dwShareMode, IntPtr lpSecurityAttributes, uint dwCreationDisposition,
            uint dwFlagsAndAttributes, IntPtr hTemplateFile);

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern uint SetFilePointer(SafeFileHandle hFile, long lDistanceToMove,
            IntPtr lpDistanceToMoveHigh, uint dwMoveMethod);

        // uint GetMode( FileMode mode )
        // Converts the filemode constant to win32 constant

        #region GetMode

        private static uint GetMode(FileMode mode)
        {
            uint umode = 0;
            switch (mode)
            {
                case FileMode.CreateNew:
                    umode = CREATE_NEW;
                    break;
                case FileMode.Create:
                    umode = CREATE_ALWAYS;
                    break;
                case FileMode.Append:
                    umode = OPEN_ALWAYS;
                    break;
                case FileMode.Open:
                    umode = OPEN_EXISTING;
                    break;
                case FileMode.OpenOrCreate:
                    umode = OPEN_ALWAYS;
                    break;
                case FileMode.Truncate:
                    umode = TRUNCATE_EXISTING;
                    break;
            }
            return umode;
        }

        #endregion


        // uint GetAccess(FileAccess access)
        // Converts the FileAccess constant to win32 constant

        #region GetAccess

        private static uint GetAccess(FileAccess access)
        {
            uint uaccess = 0;
            switch (access)
            {
                case FileAccess.Read:
                    uaccess = GENERIC_READ;
                    break;
                case FileAccess.ReadWrite:
                    uaccess = GENERIC_READ | GENERIC_WRITE;
                    break;
                case FileAccess.Write:
                    uaccess = GENERIC_WRITE;
                    break;
            }
            return uaccess;
        }

        #endregion

        // uint GetShare(FileShare share)
        // Converts the FileShare constant to win32 constant

        #region GetShare

        private static uint GetShare(FileShare share)
        {
            uint ushare = 0;
            switch (share)
            {
                case FileShare.Read:
                    ushare = FILE_SHARE_READ;
                    break;
                case FileShare.ReadWrite:
                    ushare = FILE_SHARE_READ | FILE_SHARE_WRITE;
                    break;
                case FileShare.Write:
                    ushare = FILE_SHARE_WRITE;
                    break;
                case FileShare.Delete:
                    ushare = FILE_SHARE_DELETE;
                    break;
                case FileShare.None:
                    ushare = 0;
                    break;

            }
            return ushare;
        }

        #endregion


        public static FileStream Open(string filepath, FileMode mode)
        {
            //opened in the specified mode and path, with read/write access and not shared
            FileStream fs = null;
            uint umode = GetMode(mode);
            uint uaccess = GENERIC_READ | GENERIC_WRITE;
            uint ushare = 0; //not shared
            if (mode == FileMode.Append)
                uaccess = FILE_APPEND_DATA;
            // If file path is disk file path then prepend it with \\?\
            // if file path is UNC prepend it with \\?\UNC\ and remove \\ prefix in unc path.
            if (filepath.StartsWith(@"\\"))
            {
                filepath = @"\\?\UNC\" + filepath.Substring(2, filepath.Length - 2);
            }
            else
                filepath = @"\\?\" + filepath;
            SafeFileHandle sh = CreateFileW(filepath, uaccess, ushare, IntPtr.Zero, umode, FILE_ATTRIBUTE_NORMAL,
                IntPtr.Zero);
            int iError = Marshal.GetLastWin32Error();
            if ((iError > 0 && !(mode == FileMode.Append && iError == ERROR_ALREADY_EXISTS)) || sh.IsInvalid)
            {
                throw new Win32Exception(iError, string.Format(Resource.Win32File_Error_opening_file, filepath));
            }
            else
            {
                fs = new FileStream(sh, FileAccess.ReadWrite);
            }

            // if opened in append mode
            if (mode == FileMode.Append)
            {
                if (!sh.IsInvalid)
                {
                    SetFilePointer(sh, 0, IntPtr.Zero, FILE_END);
                }
            }

            return fs;
        }

        public static FileStream Open(string filepath, FileMode mode, FileAccess access)
        {
            //opened in the specified mode and access and not shared
            FileStream fs = null;
            uint umode = GetMode(mode);
            uint uaccess = GetAccess(access);
            uint ushare = 0; //not shared
            if (mode == FileMode.Append)
                uaccess = FILE_APPEND_DATA;
            // If file path is disk file path then prepend it with \\?\
            // if file path is UNC prepend it with \\?\UNC\ and remove \\ prefix in unc path.
            if (filepath.StartsWith(@"\\"))
            {
                filepath = @"\\?\UNC\" + filepath.Substring(2, filepath.Length - 2);
            }
            else
                filepath = @"\\?\" + filepath;
            SafeFileHandle sh = CreateFileW(filepath, uaccess, ushare, IntPtr.Zero, umode, FILE_ATTRIBUTE_NORMAL,
                IntPtr.Zero);
            int iError = Marshal.GetLastWin32Error();
            if ((iError > 0 && !(mode == FileMode.Append && iError != ERROR_ALREADY_EXISTS)) || sh.IsInvalid)
            {
                throw new Win32Exception(iError, string.Format(Resource.Win32File_Error_opening_file, filepath));
            }
            else
            {
                fs = new FileStream(sh, access);
            }
            // if opened in append mode
            if (mode == FileMode.Append)
            {
                if (!sh.IsInvalid)
                {
                    SetFilePointer(sh, 0, IntPtr.Zero, FILE_END);
                }
            }
            return fs;

        }

        public static FileStream Open(string filepath, FileMode mode, FileAccess access, FileShare share)
        {
            //opened in the specified mode , access and  share
            FileStream fs = null;
            uint umode = GetMode(mode);
            uint uaccess = GetAccess(access);
            uint ushare = GetShare(share);
            if (mode == FileMode.Append)
                uaccess = FILE_APPEND_DATA;
            // If file path is disk file path then prepend it with \\?\
            // if file path is UNC prepend it with \\?\UNC\ and remove \\ prefix in unc path.
            if (filepath.StartsWith(@"\\"))
            {
                filepath = @"\\?\UNC\" + filepath.Substring(2, filepath.Length - 2);
            }
            else
                filepath = @"\\?\" + filepath;
            SafeFileHandle sh = CreateFileW(filepath, uaccess, ushare, IntPtr.Zero, umode, FILE_ATTRIBUTE_NORMAL,
                IntPtr.Zero);
            int iError = Marshal.GetLastWin32Error();
            if ((iError > 0 && !(mode == FileMode.Append && iError != ERROR_ALREADY_EXISTS)) || sh.IsInvalid)
            {
                throw new Win32Exception(iError, string.Format(Resource.Win32File_Error_opening_file, filepath));
            }
            else
            {
                fs = new FileStream(sh, access);
            }
            // if opened in append mode
            if (mode == FileMode.Append)
            {
                if (!sh.IsInvalid)
                {
                    SetFilePointer(sh, 0, IntPtr.Zero, FILE_END);
                }
            }
            return fs;
        }

        public static FileStream OpenRead(string filepath)
        {

            // Open readonly file mode open(String, FileMode.Open, FileAccess.Read, FileShare.Read)
            return Open(filepath, FileMode.Open, FileAccess.Read, FileShare.Read);
        }

        public static FileStream OpenWrite(string filepath)
        {
            //open writable open(String, FileMode.OpenOrCreate, FileAccess.Write, FileShare.None).
            return Open(filepath, FileMode.OpenOrCreate, FileAccess.Write, FileShare.None);
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        internal static extern bool GetFileSizeEx(SafeHandle hFile, ref long lpFileSize);

        public static long GetFileSize(string filepath, bool exceptionFlag = true)
        {
            if (filepath.StartsWith(@"\\"))
            {
                filepath = @"\\?\UNC\" + filepath.Substring(2, filepath.Length - 2);
            }
            else
                filepath = @"\\?\" + filepath;

            WIN32_FILE_ATTRIBUTE_DATA filedata;
            GetFileAttributesEx(filepath, GET_FILEEX_INFO_LEVELS.GetFileExInfoStandard, out filedata);
            int iError = Marshal.GetLastWin32Error();
            if (iError > 0)
            {
                if (exceptionFlag)
                    throw new Win32Exception();
                else
                    return 0;
            }
            return ((long) filedata.nFileSizeHigh) << 32 | filedata.nFileSizeLow;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct WIN32_FILE_ATTRIBUTE_DATA
        {
            public FileAttributes dwFileAttributes;
            public System.Runtime.InteropServices.ComTypes.FILETIME ftCreationTime;
            public System.Runtime.InteropServices.ComTypes.FILETIME ftLastAccessTime;
            public System.Runtime.InteropServices.ComTypes.FILETIME ftLastWriteTime;
            public uint nFileSizeHigh;
            public uint nFileSizeLow;
        }

        public enum GET_FILEEX_INFO_LEVELS
        {
            GetFileExInfoStandard,
            GetFileExMaxInfoLevel
        }

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetFileAttributesEx(string lpFileName,
            GET_FILEEX_INFO_LEVELS fInfoLevelId, out WIN32_FILE_ATTRIBUTE_DATA fileData);


    }
}