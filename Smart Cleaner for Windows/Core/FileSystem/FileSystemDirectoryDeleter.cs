using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace Smart_Cleaner_for_Windows.Core.FileSystem
{
    public sealed class FileSystemDirectoryDeleter : IDirectoryDeleter
    {
        public void Delete(string path, DirectoryDeletionMode mode)
        {
            if (string.IsNullOrWhiteSpace(path))
                throw new ArgumentException("Path cannot be null or whitespace.", nameof(path));

            switch (mode)
            {
                case DirectoryDeletionMode.Permanent:
                    // Matches your original behavior (non-recursive); change to true if desired.
                    Directory.Delete(path, recursive: false);
                    break;

                case DirectoryDeletionMode.RecycleBin:
                    DeleteToRecycleBin(path);
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(mode), mode, "Unsupported deletion mode.");
            }
        }

        [SupportedOSPlatform("windows")]
        private static void DeleteToRecycleBin(string path)
        {
            if (!OperatingSystem.IsWindows())
                throw new PlatformNotSupportedException("Deleting to the Recycle Bin is only supported on Windows.");

            if (!Directory.Exists(path))
                throw new DirectoryNotFoundException($"Directory not found: {path}");

            // SHFileOperation expects a double-null-terminated string for pFrom.
            var from = path.EndsWith("\0", StringComparison.Ordinal)
                ? path + "\0"           // ensure double-null
                : path + "\0\0";

            var op = new Shfileopstruct
            {
                wFunc = FoDelete,
                pFrom = from,
                // Send to Recycle Bin, suppress confirmation/UI (keep error dialogs off)
                fFlags = FofAllowundo | FofNoconfirmation | FofNoerrorui | FofSilent
            };

            int result = SHFileOperation(ref op);

            // Non-zero = failure (Win32 error code). Also treat user-cancel as failure for programmatic flow.
            if (result != 0)
                throw new IOException($"Failed to move '{path}' to Recycle Bin. Win32 error: {result}.");

            if (op.fAnyOperationsAborted)
                throw new OperationCanceledException("Recycle Bin operation was canceled.");
        }

        #region P/Invoke for SHFileOperation

        private const uint FoDelete = 0x0003;

        private const ushort FofSilent = 0x0004;             // No progress UI
        private const ushort FofNoconfirmation = 0x0010;     // Don't prompt the user
        private const ushort FofAllowundo = 0x0040;          // Send to Recycle Bin (not permanent)
        private const ushort FofNoerrorui = 0x0400;          // No error UI

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct Shfileopstruct
        {
            public IntPtr hwnd;
            public uint wFunc;
            [MarshalAs(UnmanagedType.LPWStr)] public string pFrom;
            [MarshalAs(UnmanagedType.LPWStr)] public string pTo;
            public ushort fFlags;
            public bool fAnyOperationsAborted;
            public IntPtr hNameMappings;
            [MarshalAs(UnmanagedType.LPWStr)] public string lpszProgressTitle;
        }

        [DllImport("shell32.dll", CharSet = CharSet.Unicode, SetLastError = false)]
        private static extern int SHFileOperation(ref Shfileopstruct lpFileOp);

        #endregion
    }
}
