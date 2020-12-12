﻿using Microsoft.Win32.SafeHandles;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Windows.Storage;
using FileAttributes = System.IO.FileAttributes;

namespace RX_Explorer.Class
{
    public static class WIN_Native_API
    {
        private enum FINDEX_INFO_LEVELS
        {
            FindExInfoStandard = 0,
            FindExInfoBasic = 1
        }

        private enum FINDEX_SEARCH_OPS
        {
            FindExSearchNameMatch = 0,
            FindExSearchLimitToDirectories = 1,
            FindExSearchLimitToDevices = 2
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        public struct WIN32_FIND_DATA : IEquatable<WIN32_FIND_DATA>
        {
            public uint dwFileAttributes;
            public FILETIME ftCreationTime;
            public FILETIME ftLastAccessTime;
            public FILETIME ftLastWriteTime;
            public uint nFileSizeHigh;
            public uint nFileSizeLow;
            public uint dwReserved0;
            public uint dwReserved1;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
            public string cFileName;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 14)]
            public string cAlternateFileName;

            public override bool Equals(object obj)
            {
                return cFileName.Equals(obj);
            }

            public override int GetHashCode()
            {
                return cFileName.GetHashCode();
            }

            public static bool operator ==(WIN32_FIND_DATA left, WIN32_FIND_DATA right)
            {
                return left.cFileName.Equals(right.cFileName);
            }

            public static bool operator !=(WIN32_FIND_DATA left, WIN32_FIND_DATA right)
            {
                return !(left.cFileName == right.cFileName);
            }

            public bool Equals(WIN32_FIND_DATA other)
            {
                return cFileName.Equals(other.cFileName);
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct SYSTEMTIME
        {
            [MarshalAs(UnmanagedType.U2)] public short Year;
            [MarshalAs(UnmanagedType.U2)] public short Month;
            [MarshalAs(UnmanagedType.U2)] public short DayOfWeek;
            [MarshalAs(UnmanagedType.U2)] public short Day;
            [MarshalAs(UnmanagedType.U2)] public short Hour;
            [MarshalAs(UnmanagedType.U2)] public short Minute;
            [MarshalAs(UnmanagedType.U2)] public short Second;
            [MarshalAs(UnmanagedType.U2)] public short Milliseconds;

            public SYSTEMTIME(DateTime dt)
            {
                dt = dt.ToUniversalTime();  // SetSystemTime expects the SYSTEMTIME in UTC
                Year = (short)dt.Year;
                Month = (short)dt.Month;
                DayOfWeek = (short)dt.DayOfWeek;
                Day = (short)dt.Day;
                Hour = (short)dt.Hour;
                Minute = (short)dt.Minute;
                Second = (short)dt.Second;
                Milliseconds = (short)dt.Millisecond;
            }
        }

        [DllImport("api-ms-win-core-file-fromapp-l1-1-0.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern IntPtr FindFirstFileExFromApp(string lpFileName,
                                                           FINDEX_INFO_LEVELS fInfoLevelId,
                                                           out WIN32_FIND_DATA lpFindFileData,
                                                           FINDEX_SEARCH_OPS fSearchOp,
                                                           IntPtr lpSearchFilter,
                                                           int dwAdditionalFlags);

        private const int FIND_FIRST_EX_CASE_SENSITIVE = 1;
        private const int FIND_FIRST_EX_LARGE_FETCH = 2;
        private const int FIND_FIRST_EX_ON_DISK_ENTRIES_ONLY = 4;

        [DllImport("api-ms-win-core-file-l1-1-0.dll", CharSet = CharSet.Unicode)]
        private static extern bool FindNextFile(IntPtr hFindFile, out WIN32_FIND_DATA lpFindFileData);

        [DllImport("api-ms-win-core-file-l1-1-0.dll")]
        private static extern bool FindClose(IntPtr hFindFile);

        [DllImport("api-ms-win-core-timezone-l1-1-0.dll", SetLastError = true)]
        private static extern bool FileTimeToSystemTime(ref FILETIME lpFileTime, out SYSTEMTIME lpSystemTime);

        [DllImport("api-ms-win-core-file-fromapp-l1-1-0.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern IntPtr CreateFileFromApp(string lpFileName, uint dwDesiredAccess, uint dwShareMode, IntPtr SecurityAttributes, uint dwCreationDisposition, uint dwFlagsAndAttributes, IntPtr hTemplateFile);

        [DllImport("api-ms-win-core-handle-l1-1-0.dll")]
        private static extern bool CloseHandle(IntPtr hObject);

        [DllImport("api-ms-win-core-file-l2-1-0.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern bool ReadDirectoryChangesW(IntPtr hDirectory, IntPtr lpBuffer, uint nBufferLength, bool bWatchSubtree, uint dwNotifyFilter, out uint lpBytesReturned, IntPtr lpOverlapped, IntPtr lpCompletionRoutine);

        [DllImport("api-ms-win-core-io-l1-1-1.dll")]
        private static extern bool CancelIoEx(IntPtr hFile, IntPtr lpOverlapped);

        [DllImport("api-ms-win-core-file-fromapp-l1-1-0.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern bool CreateDirectoryFromAppW(string lpPathName, IntPtr lpSecurityAttributes);

        [DllImport("api-ms-win-core-file-fromapp-l1-1-0.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern bool DeleteFileFromApp(string lpPathName);

        [DllImport("api-ms-win-core-file-fromapp-l1-1-0.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern bool RemoveDirectoryFromApp(string lpPathName);

        const uint GENERIC_READ = 0x80000000;
        const uint GENERIC_WRITE = 0x40000000;
        const uint FILE_LIST_DIRECTORY = 0x1;
        const uint FILE_NO_SHARE = 0x0;
        const uint FILE_SHARE_READ = 0x1;
        const uint FILE_SHARE_WRITE = 0x2;
        const uint FILE_SHARE_DELETE = 0x4;
        const uint CREATE_NEW = 1;
        const uint CREATE_ALWAYS = 2;
        const uint OPEN_EXISTING = 3;
        const uint OPEN_ALWAYS = 4;
        const uint TRUNCATE_EXISTING = 5;
        const uint FILE_FLAG_BACKUP_SEMANTICS = 0x2000000;
        const uint FILE_ATTRIBUTE_NORMAL = 0x80;
        const uint FILE_NOTIFY_CHANGE_FILE_NAME = 0x1;
        const uint FILE_NOTIFY_CHANGE_DIR_NAME = 0x2;
        const uint FILE_NOTIFY_CHANGE_LAST_WRITE = 0x10;
        static readonly string SecureFolderPath = Path.Combine(ApplicationData.Current.LocalCacheFolder.Path, "SecureFolder");

        private enum StateChangeType
        {
            Unknown_Action = 0,
            Added_Action = 1,
            Removed_Action = 2,
            Modified_Action = 3,
            Rename_Action_OldName = 4,
            Rename_Action_NewName = 5
        }

        public static bool DeleteFromPath(string Path)
        {
            try
            {
                if (CheckType(Path) == StorageItemTypes.Folder)
                {
                    IntPtr Ptr = FindFirstFileExFromApp(System.IO.Path.Combine(Path, "*"), FINDEX_INFO_LEVELS.FindExInfoBasic, out WIN32_FIND_DATA Data, FINDEX_SEARCH_OPS.FindExSearchNameMatch, IntPtr.Zero, FIND_FIRST_EX_LARGE_FETCH);

                    try
                    {
                        if (Ptr.ToInt64() != -1)
                        {
                            bool AllSuccess = true;

                            do
                            {
                                if (((FileAttributes)Data.dwFileAttributes).HasFlag(FileAttributes.Directory))
                                {
                                    if (Data.cFileName != "." && Data.cFileName != "..")
                                    {
                                        if (!DeleteFromPath(System.IO.Path.Combine(Path, Data.cFileName)))
                                        {
                                            AllSuccess = false;
                                        }
                                    }
                                }
                                else
                                {
                                    if (!DeleteFileFromApp(System.IO.Path.Combine(Path, Data.cFileName)))
                                    {
                                        AllSuccess = false;
                                    }
                                }
                            }
                            while (FindNextFile(Ptr, out Data));

                            if (AllSuccess)
                            {
                                return RemoveDirectoryFromApp(Path);
                            }
                            else
                            {
                                return false;
                            }
                        }
                        else
                        {
                            LogTracer.Log(new Win32Exception(Marshal.GetLastWin32Error()));
                            return false;
                        }
                    }
                    catch (Exception ex)
                    {
                        LogTracer.Log(ex);
                        return false;
                    }
                    finally
                    {
                        FindClose(Ptr);
                    }
                }
                else
                {
                    return DeleteFileFromApp(Path);
                }
            }
            catch (Exception ex)
            {
                LogTracer.Log(ex);
                return false;
            }
        }

        public static FileStream CreateFileStreamFromExistingPath(string Path, AccessMode AccessMode)
        {
            IntPtr Handle = IntPtr.Zero;

            switch (AccessMode)
            {
                case AccessMode.Read:
                    {
                        Handle = CreateFileFromApp(Path, GENERIC_READ, FILE_SHARE_READ | FILE_SHARE_WRITE, IntPtr.Zero, OPEN_EXISTING, FILE_ATTRIBUTE_NORMAL, IntPtr.Zero);

                        break;
                    }
                case AccessMode.Write:
                    {
                        Handle = CreateFileFromApp(Path, GENERIC_WRITE, FILE_SHARE_READ | FILE_SHARE_WRITE, IntPtr.Zero, OPEN_EXISTING, FILE_ATTRIBUTE_NORMAL, IntPtr.Zero);

                        break;
                    }
                case AccessMode.ReadWrite:
                    {
                        Handle = CreateFileFromApp(Path, GENERIC_WRITE | GENERIC_READ, FILE_SHARE_READ | FILE_SHARE_WRITE, IntPtr.Zero, OPEN_EXISTING, FILE_ATTRIBUTE_NORMAL, IntPtr.Zero);

                        break;
                    }
                case AccessMode.Exclusive:
                    {
                        Handle = CreateFileFromApp(Path, GENERIC_WRITE | GENERIC_READ, FILE_NO_SHARE, IntPtr.Zero, OPEN_EXISTING, FILE_ATTRIBUTE_NORMAL, IntPtr.Zero);

                        break;
                    }
            }

            if (Handle == IntPtr.Zero || Handle.ToInt64() == -1)
            {
                throw new Win32Exception(Marshal.GetLastWin32Error());
            }

            switch (AccessMode)
            {
                case AccessMode.Exclusive:
                case AccessMode.Read:
                    {
                        return new FileStream(new SafeFileHandle(Handle, true), FileAccess.Read);
                    }
                case AccessMode.Write:
                    {
                        return new FileStream(new SafeFileHandle(Handle, true), FileAccess.Write);
                    }
                case AccessMode.ReadWrite:
                    {
                        return new FileStream(new SafeFileHandle(Handle, true), FileAccess.ReadWrite);
                    }
                default:
                    {
                        return null;
                    }
            }
        }

        public static bool CreateDirectoryFromPath(string Path, CreateDirectoryOption Option, out string NewFolderPath)
        {
            try
            {
                PathAnalysis Analysis = new PathAnalysis(Path, string.Empty);

                while (true)
                {
                    string NextPath = Analysis.NextFullPath();

                    if (Analysis.HasNextLevel)
                    {
                        if (!CheckExist(NextPath))
                        {
                            if (!CreateDirectoryFromAppW(NextPath, IntPtr.Zero))
                            {
                                NewFolderPath = string.Empty;
                                LogTracer.Log(new Win32Exception(Marshal.GetLastWin32Error()), "An exception was threw when createdirectory");
                                return false;
                            }
                        }
                    }
                    else
                    {
                        switch (Option)
                        {
                            case CreateDirectoryOption.GenerateUniqueName:
                                {
                                    string UniquePath = GenerateUniquePath(NextPath);

                                    if (CreateDirectoryFromAppW(UniquePath, IntPtr.Zero))
                                    {
                                        NewFolderPath = UniquePath;
                                        return true;
                                    }
                                    else
                                    {
                                        NewFolderPath = string.Empty;
                                        LogTracer.Log(new Win32Exception(Marshal.GetLastWin32Error()), "An exception was threw when createdirectory");
                                        return false;
                                    }
                                }
                            case CreateDirectoryOption.OpenIfExist:
                                {
                                    if (CheckExist(NextPath))
                                    {
                                        NewFolderPath = NextPath;
                                        return true;
                                    }
                                    else
                                    {
                                        if (CreateDirectoryFromAppW(NextPath, IntPtr.Zero))
                                        {
                                            NewFolderPath = NextPath;
                                            return true;
                                        }
                                        else
                                        {
                                            NewFolderPath = string.Empty;
                                            LogTracer.Log(new Win32Exception(Marshal.GetLastWin32Error()), "An exception was threw when createdirectory");
                                            return false;
                                        }
                                    }
                                }
                        }
                    }
                }
            }
            catch
            {
                NewFolderPath = string.Empty;
                LogTracer.Log(new Win32Exception(Marshal.GetLastWin32Error()), "An exception was threw when createdirectory");
                return false;
            }
        }

        public static FileStream CreateFileFromPath(string Path, AccessMode AccessMode, CreateOption Option)
        {
            IntPtr Handle = IntPtr.Zero;

            switch (AccessMode)
            {
                case AccessMode.Read:
                    {
                        switch (Option)
                        {
                            case CreateOption.OpenIfExist:
                                {
                                    Handle = CreateFileFromApp(Path, GENERIC_READ, FILE_SHARE_READ | FILE_SHARE_WRITE, IntPtr.Zero, OPEN_ALWAYS, FILE_ATTRIBUTE_NORMAL, IntPtr.Zero);
                                    break;
                                }
                            case CreateOption.GenerateUniqueName:
                                {
                                    if (CheckExist(Path))
                                    {
                                        Handle = CreateFileFromApp(GenerateUniquePath(Path), GENERIC_READ, FILE_SHARE_READ | FILE_SHARE_WRITE, IntPtr.Zero, CREATE_NEW, FILE_ATTRIBUTE_NORMAL, IntPtr.Zero);
                                    }
                                    else
                                    {
                                        Handle = CreateFileFromApp(Path, GENERIC_READ, FILE_SHARE_READ | FILE_SHARE_WRITE, IntPtr.Zero, CREATE_NEW, FILE_ATTRIBUTE_NORMAL, IntPtr.Zero);
                                    }

                                    break;
                                }
                            case CreateOption.ReplaceExisting:
                                {
                                    Handle = CreateFileFromApp(Path, GENERIC_READ, FILE_SHARE_READ | FILE_SHARE_WRITE, IntPtr.Zero, CREATE_ALWAYS, FILE_ATTRIBUTE_NORMAL, IntPtr.Zero);
                                    break;
                                }
                        }

                        break;
                    }
                case AccessMode.Write:
                    {
                        switch (Option)
                        {
                            case CreateOption.OpenIfExist:
                                {
                                    Handle = CreateFileFromApp(Path, GENERIC_WRITE, FILE_SHARE_READ | FILE_SHARE_WRITE, IntPtr.Zero, OPEN_ALWAYS, FILE_ATTRIBUTE_NORMAL, IntPtr.Zero);
                                    break;
                                }
                            case CreateOption.GenerateUniqueName:
                                {
                                    if (CheckExist(Path))
                                    {
                                        Handle = CreateFileFromApp(GenerateUniquePath(Path), GENERIC_WRITE, FILE_SHARE_READ | FILE_SHARE_WRITE, IntPtr.Zero, CREATE_NEW, FILE_ATTRIBUTE_NORMAL, IntPtr.Zero);
                                    }
                                    else
                                    {
                                        Handle = CreateFileFromApp(Path, GENERIC_WRITE, FILE_SHARE_READ | FILE_SHARE_WRITE, IntPtr.Zero, CREATE_NEW, FILE_ATTRIBUTE_NORMAL, IntPtr.Zero);
                                    }

                                    break;
                                }
                            case CreateOption.ReplaceExisting:
                                {
                                    Handle = CreateFileFromApp(Path, GENERIC_WRITE, FILE_SHARE_READ | FILE_SHARE_WRITE, IntPtr.Zero, CREATE_ALWAYS, FILE_ATTRIBUTE_NORMAL, IntPtr.Zero);
                                    break;
                                }
                        }

                        break;
                    }
                case AccessMode.ReadWrite:
                    {
                        switch (Option)
                        {
                            case CreateOption.OpenIfExist:
                                {
                                    Handle = CreateFileFromApp(Path, GENERIC_WRITE | GENERIC_READ, FILE_SHARE_READ | FILE_SHARE_WRITE, IntPtr.Zero, OPEN_ALWAYS, FILE_ATTRIBUTE_NORMAL, IntPtr.Zero);
                                    break;
                                }
                            case CreateOption.GenerateUniqueName:
                                {
                                    if (CheckExist(Path))
                                    {
                                        Handle = CreateFileFromApp(GenerateUniquePath(Path), GENERIC_WRITE | GENERIC_READ, FILE_SHARE_READ | FILE_SHARE_WRITE, IntPtr.Zero, CREATE_NEW, FILE_ATTRIBUTE_NORMAL, IntPtr.Zero);
                                    }
                                    else
                                    {
                                        Handle = CreateFileFromApp(Path, GENERIC_WRITE | GENERIC_READ, FILE_SHARE_READ | FILE_SHARE_WRITE, IntPtr.Zero, CREATE_NEW, FILE_ATTRIBUTE_NORMAL, IntPtr.Zero);
                                    }

                                    break;
                                }
                            case CreateOption.ReplaceExisting:
                                {
                                    Handle = CreateFileFromApp(Path, GENERIC_WRITE | GENERIC_READ, FILE_SHARE_READ | FILE_SHARE_WRITE, IntPtr.Zero, CREATE_ALWAYS, FILE_ATTRIBUTE_NORMAL, IntPtr.Zero);
                                    break;
                                }
                        }

                        break;
                    }
                case AccessMode.Exclusive:
                    {
                        switch (Option)
                        {
                            case CreateOption.OpenIfExist:
                                {
                                    Handle = CreateFileFromApp(Path, GENERIC_WRITE | GENERIC_READ, FILE_NO_SHARE, IntPtr.Zero, OPEN_ALWAYS, FILE_ATTRIBUTE_NORMAL, IntPtr.Zero);
                                    break;
                                }
                            case CreateOption.GenerateUniqueName:
                                {
                                    if (CheckExist(Path))
                                    {
                                        Handle = CreateFileFromApp(GenerateUniquePath(Path), GENERIC_WRITE | GENERIC_READ, FILE_NO_SHARE, IntPtr.Zero, CREATE_NEW, FILE_ATTRIBUTE_NORMAL, IntPtr.Zero);
                                    }
                                    else
                                    {
                                        Handle = CreateFileFromApp(Path, GENERIC_WRITE | GENERIC_READ, FILE_NO_SHARE, IntPtr.Zero, CREATE_NEW, FILE_ATTRIBUTE_NORMAL, IntPtr.Zero);
                                    }

                                    break;
                                }
                            case CreateOption.ReplaceExisting:
                                {
                                    Handle = CreateFileFromApp(Path, GENERIC_WRITE | GENERIC_READ, FILE_NO_SHARE, IntPtr.Zero, CREATE_ALWAYS, FILE_ATTRIBUTE_NORMAL, IntPtr.Zero);
                                    break;
                                }
                        }

                        break;
                    }
            }

            if (Handle == IntPtr.Zero || Handle.ToInt64() == -1)
            {
                throw new Win32Exception(Marshal.GetLastWin32Error());
            }

            switch(AccessMode)
            {
                case AccessMode.ReadWrite:
                case AccessMode.Exclusive:
                    {
                        return new FileStream(new SafeFileHandle(Handle, true), FileAccess.ReadWrite);
                    }
                case AccessMode.Read:
                    {
                        return new FileStream(new SafeFileHandle(Handle, true), FileAccess.Read);
                    }
                case AccessMode.Write:
                    {
                        return new FileStream(new SafeFileHandle(Handle, true), FileAccess.Write);
                    }
                default:
                    {
                        return null;
                    }
            }
        }

        public static string GenerateUniquePath(string Path)
        {
            string UniquePath = Path;
            string NameWithoutExt = System.IO.Path.GetFileNameWithoutExtension(Path);
            string Extension = System.IO.Path.GetExtension(Path);
            string Directory = System.IO.Path.GetDirectoryName(Path);

            for (ushort Count = 1; CheckExist(UniquePath); Count++)
            {
                if (Regex.IsMatch(NameWithoutExt, @".*\(\d+\)"))
                {
                    UniquePath = System.IO.Path.Combine(Directory, $"{NameWithoutExt.Substring(0, NameWithoutExt.LastIndexOf("(", StringComparison.InvariantCultureIgnoreCase))}({Count}){Extension}");
                }
                else
                {
                    UniquePath = System.IO.Path.Combine(Directory, $"{NameWithoutExt} ({Count}){Extension}");
                }
            }

            return UniquePath;
        }

        public static void StopDirectoryWatcher(ref IntPtr hDir)
        {
            CancelIoEx(hDir, IntPtr.Zero);
            CloseHandle(hDir);
            hDir = IntPtr.Zero;
        }

        public static IntPtr CreateDirectoryWatcher(string FolderPath, Action<string> Added = null, Action<string> Removed = null, Action<string, string> Renamed = null, Action<string> Modified = null)
        {
            try
            {
                IntPtr hDir = CreateFileFromApp(FolderPath, FILE_LIST_DIRECTORY, FILE_SHARE_READ | FILE_SHARE_WRITE | FILE_SHARE_DELETE, IntPtr.Zero, OPEN_EXISTING, FILE_FLAG_BACKUP_SEMANTICS, IntPtr.Zero);

                if (hDir == IntPtr.Zero || hDir.ToInt64() == -1)
                {
                    throw new Win32Exception(Marshal.GetLastWin32Error());
                }

                Task.Factory.StartNew((Arguement) =>
                {
                    ValueTuple<IntPtr, Action<string>, Action<string>, Action<string, string>, Action<string>> Package = (ValueTuple<IntPtr, Action<string>, Action<string>, Action<string, string>, Action<string>>)Arguement;

                    while (true)
                    {
                        IntPtr BufferPointer = Marshal.AllocHGlobal(8192);

                        try
                        {
                            if (ReadDirectoryChangesW(Package.Item1, BufferPointer, 8192, false, FILE_NOTIFY_CHANGE_FILE_NAME | FILE_NOTIFY_CHANGE_DIR_NAME | FILE_NOTIFY_CHANGE_LAST_WRITE, out _, IntPtr.Zero, IntPtr.Zero))
                            {
                                IntPtr CurrentPointer = BufferPointer;
                                int Offset = 0;
                                string OldPath = null;

                                do
                                {
                                    CurrentPointer = (IntPtr)(Offset + CurrentPointer.ToInt64());

                                    // Read file length (in bytes) at offset 8
                                    int FileNameLength = Marshal.ReadInt32(CurrentPointer, 8);
                                    // Read file name (fileLen/2 characters) from offset 12
                                    string FileName = Marshal.PtrToStringUni((IntPtr)(12 + CurrentPointer.ToInt64()), FileNameLength / 2);
                                    // Read action at offset 4
                                    int ActionIndex = Marshal.ReadInt32(CurrentPointer, 4);

                                    if (ActionIndex < 1 || ActionIndex > 5)
                                    {
                                        ActionIndex = 0;
                                    }

                                    switch ((StateChangeType)ActionIndex)
                                    {
                                        case StateChangeType.Unknown_Action:
                                            {
                                                break;
                                            }
                                        case StateChangeType.Added_Action:
                                            {
                                                Package.Item2?.Invoke(Path.Combine(FolderPath, FileName));
                                                break;
                                            }
                                        case StateChangeType.Removed_Action:
                                            {
                                                Package.Item3?.Invoke(Path.Combine(FolderPath, FileName));
                                                break;
                                            }
                                        case StateChangeType.Modified_Action:
                                            {
                                                Package.Item5?.Invoke(Path.Combine(FolderPath, FileName));
                                                break;
                                            }
                                        case StateChangeType.Rename_Action_OldName:
                                            {
                                                OldPath = Path.Combine(FolderPath, FileName);
                                                break;
                                            }
                                        case StateChangeType.Rename_Action_NewName:
                                            {
                                                Package.Item4?.Invoke(OldPath, Path.Combine(FolderPath, FileName));
                                                break;
                                            }
                                    }

                                    // Read NextEntryOffset at offset 0 and move pointer to next structure if needed
                                    Offset = Marshal.ReadInt32(CurrentPointer);
                                }
                                while (Offset != 0);
                            }
                            else
                            {
                                break;
                            }
                        }
                        catch (Exception e)
                        {
                            LogTracer.Log("Exception happened when watching directory. Message: " + e.Message);
                        }
                        finally
                        {
                            if (BufferPointer != IntPtr.Zero)
                            {
                                Marshal.FreeHGlobal(BufferPointer);
                            }
                        }
                    }
                }, (hDir, Added, Removed, Renamed, Modified), CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default);

                return hDir;
            }
            catch (Exception ex)
            {
                LogTracer.Log(ex);
                return IntPtr.Zero;
            }
        }

        public static bool CheckContainsAnyItem(string FolderPath, ItemFilters Filter)
        {
            if (string.IsNullOrWhiteSpace(FolderPath))
            {
                throw new ArgumentException("Argument could not be empty", nameof(FolderPath));
            }

            IntPtr Ptr = FindFirstFileExFromApp(Path.Combine(FolderPath, "*"), FINDEX_INFO_LEVELS.FindExInfoBasic, out WIN32_FIND_DATA Data, FINDEX_SEARCH_OPS.FindExSearchNameMatch, IntPtr.Zero, FIND_FIRST_EX_LARGE_FETCH);

            try
            {
                if (Ptr.ToInt64() != -1)
                {
                    do
                    {
                        FileAttributes Attribute = (FileAttributes)Data.dwFileAttributes;

                        if (!Attribute.HasFlag(FileAttributes.System))
                        {
                            if (Attribute.HasFlag(FileAttributes.Directory) && Filter.HasFlag(ItemFilters.Folder))
                            {
                                if (Data.cFileName != "." && Data.cFileName != "..")
                                {
                                    return true;
                                }
                            }
                            else if (Filter.HasFlag(ItemFilters.File) && !Data.cFileName.EndsWith(".url", StringComparison.OrdinalIgnoreCase))
                            {
                                return true;
                            }
                        }
                    }
                    while (FindNextFile(Ptr, out Data));

                    return false;
                }
                else
                {
                    LogTracer.Log(new Win32Exception(Marshal.GetLastWin32Error()));
                    return false;
                }
            }
            catch (Exception ex)
            {
                LogTracer.Log(ex);
                return false;
            }
            finally
            {
                FindClose(Ptr);
            }
        }

        public static bool CheckExist(string Path)
        {
            if (string.IsNullOrWhiteSpace(Path))
            {
                throw new ArgumentException("Argument could not be empty", nameof(Path));
            }

            IntPtr Ptr = FindFirstFileExFromApp(System.IO.Path.GetPathRoot(Path) == Path ? System.IO.Path.Combine(Path, "*") : Path, FINDEX_INFO_LEVELS.FindExInfoBasic, out _, FINDEX_SEARCH_OPS.FindExSearchNameMatch, IntPtr.Zero, FIND_FIRST_EX_LARGE_FETCH);

            try
            {
                if (Ptr.ToInt64() != -1)
                {
                    return true;
                }
                else
                {
                    LogTracer.Log(new Win32Exception(Marshal.GetLastWin32Error()));
                    return false;
                }
            }
            catch (Exception ex)
            {
                LogTracer.Log(ex);
                return false;
            }
            finally
            {
                FindClose(Ptr);
            }
        }

        public static StorageItemTypes CheckType(string ItemPath)
        {
            if (string.IsNullOrWhiteSpace(ItemPath))
            {
                throw new ArgumentException("Argument could not be empty", nameof(ItemPath));
            }

            IntPtr Ptr = FindFirstFileExFromApp(ItemPath, FINDEX_INFO_LEVELS.FindExInfoBasic, out WIN32_FIND_DATA Data, FINDEX_SEARCH_OPS.FindExSearchNameMatch, IntPtr.Zero, FIND_FIRST_EX_LARGE_FETCH);

            try
            {
                if (Ptr.ToInt64() != -1)
                {
                    if (((FileAttributes)Data.dwFileAttributes).HasFlag(FileAttributes.Directory))
                    {
                        return StorageItemTypes.Folder;
                    }
                    else
                    {
                        return StorageItemTypes.File;
                    }
                }
                else
                {
                    LogTracer.Log(new Win32Exception(Marshal.GetLastWin32Error()));
                    return StorageItemTypes.None;
                }
            }
            catch (Exception ex)
            {
                LogTracer.Log(ex);
                return StorageItemTypes.None;
            }
            finally
            {
                FindClose(Ptr);
            }
        }

        public static bool CheckIfHidden(string ItemPath)
        {
            if (string.IsNullOrWhiteSpace(ItemPath))
            {
                throw new ArgumentException("Argument could not be empty", nameof(ItemPath));
            }

            if (System.IO.Path.GetPathRoot(ItemPath) == ItemPath)
            {
                return false;
            }

            IntPtr Ptr = FindFirstFileExFromApp(ItemPath, FINDEX_INFO_LEVELS.FindExInfoBasic, out WIN32_FIND_DATA Data, FINDEX_SEARCH_OPS.FindExSearchNameMatch, IntPtr.Zero, FIND_FIRST_EX_LARGE_FETCH);

            try
            {
                if (Ptr.ToInt64() != -1)
                {
                    if (((FileAttributes)Data.dwFileAttributes).HasFlag(FileAttributes.Hidden))
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
                else
                {
                    LogTracer.Log(new Win32Exception(Marshal.GetLastWin32Error()));
                    return false;
                }
            }
            catch (Exception ex)
            {
                LogTracer.Log(ex);
                return false;
            }
            finally
            {
                FindClose(Ptr);
            }
        }

        public static ulong CalculateFolderSize(string FolderPath, CancellationToken CancelToken = default)
        {
            if (string.IsNullOrWhiteSpace(FolderPath))
            {
                throw new ArgumentException("Argument could not be empty", nameof(FolderPath));
            }

            IntPtr Ptr = FindFirstFileExFromApp(Path.Combine(FolderPath, "*"), FINDEX_INFO_LEVELS.FindExInfoBasic, out WIN32_FIND_DATA Data, FINDEX_SEARCH_OPS.FindExSearchNameMatch, IntPtr.Zero, FIND_FIRST_EX_LARGE_FETCH);

            try
            {
                if (Ptr.ToInt64() != -1)
                {
                    ulong TotalSize = 0;

                    do
                    {
                        if (((FileAttributes)Data.dwFileAttributes).HasFlag(FileAttributes.Directory))
                        {
                            if (Data.cFileName != "." && Data.cFileName != "..")
                            {
                                TotalSize += CalculateFolderSize(Path.Combine(FolderPath, Data.cFileName), CancelToken);
                            }
                        }
                        else
                        {
                            TotalSize += ((ulong)Data.nFileSizeHigh << 32) + Data.nFileSizeLow;
                        }
                    }
                    while (FindNextFile(Ptr, out Data) && !CancelToken.IsCancellationRequested);

                    return TotalSize;
                }
                else
                {
                    LogTracer.Log(new Win32Exception(Marshal.GetLastWin32Error()));
                    return 0;
                }
            }
            catch (Exception ex)
            {
                LogTracer.Log(ex);
                return 0;
            }
            finally
            {
                FindClose(Ptr);
            }
        }

        public static ulong CalculateFileSize(string FilePath)
        {
            if (string.IsNullOrWhiteSpace(FilePath))
            {
                throw new ArgumentException("Argument could not be empty", nameof(FilePath));
            }

            IntPtr Ptr = FindFirstFileExFromApp(FilePath, FINDEX_INFO_LEVELS.FindExInfoBasic, out WIN32_FIND_DATA Data, FINDEX_SEARCH_OPS.FindExSearchNameMatch, IntPtr.Zero, FIND_FIRST_EX_LARGE_FETCH);

            try
            {
                if (Ptr.ToInt64() != -1)
                {
                    if (!((FileAttributes)Data.dwFileAttributes).HasFlag(FileAttributes.Directory))
                    {
                        return ((ulong)Data.nFileSizeHigh << 32) + Data.nFileSizeLow;
                    }
                    else
                    {
                        return 0;
                    }
                }
                else
                {
                    LogTracer.Log(new Win32Exception(Marshal.GetLastWin32Error()));
                    return 0;
                }
            }
            catch (Exception ex)
            {
                LogTracer.Log(ex);
                return 0;
            }
            finally
            {
                FindClose(Ptr);
            }
        }

        public static (uint, uint) CalculateFolderAndFileCount(string FolderPath, CancellationToken CancelToken = default)
        {
            if (string.IsNullOrWhiteSpace(FolderPath))
            {
                throw new ArgumentException("Argument could not be empty", nameof(FolderPath));
            }

            IntPtr Ptr = FindFirstFileExFromApp(Path.Combine(FolderPath, "*"), FINDEX_INFO_LEVELS.FindExInfoBasic, out WIN32_FIND_DATA Data, FINDEX_SEARCH_OPS.FindExSearchNameMatch, IntPtr.Zero, FIND_FIRST_EX_LARGE_FETCH);

            try
            {
                if (Ptr.ToInt64() != -1)
                {
                    uint FolderCount = 0;
                    uint FileCount = 0;

                    do
                    {
                        if (((FileAttributes)Data.dwFileAttributes).HasFlag(FileAttributes.Directory))
                        {
                            if (Data.cFileName != "." && Data.cFileName != "..")
                            {
                                (uint SubFolderCount, uint SubFileCount) = CalculateFolderAndFileCount(Path.Combine(FolderPath, Data.cFileName), CancelToken);
                                FolderCount += ++SubFolderCount;
                                FileCount += SubFileCount;
                            }
                        }
                        else
                        {
                            FileCount++;
                        }
                    }
                    while (FindNextFile(Ptr, out Data) && !CancelToken.IsCancellationRequested);

                    return (FolderCount, FileCount);
                }
                else
                {
                    LogTracer.Log(new Win32Exception(Marshal.GetLastWin32Error()));
                    return (0, 0);
                }
            }
            catch (Exception ex)
            {
                LogTracer.Log(ex);
                return (0, 0);
            }
            finally
            {
                FindClose(Ptr);
            }
        }

        public static List<FileSystemStorageItemBase> Search(string FolderPath, string SearchWord, bool SearchInSubFolders = false, bool IncludeHiddenItem = false, bool IsRegexExpresstion = false, bool IgnoreCase = true, CancellationToken CancelToken = default)
        {
            if (string.IsNullOrWhiteSpace(FolderPath))
            {
                throw new ArgumentException("Argument could not be empty", nameof(FolderPath));
            }

            if (string.IsNullOrEmpty(SearchWord))
            {
                throw new ArgumentException("Argument could not be empty", nameof(SearchWord));
            }

            IntPtr Ptr = FindFirstFileExFromApp(Path.Combine(FolderPath, "*"), FINDEX_INFO_LEVELS.FindExInfoBasic, out WIN32_FIND_DATA Data, FINDEX_SEARCH_OPS.FindExSearchNameMatch, IntPtr.Zero, FIND_FIRST_EX_LARGE_FETCH);

            List<FileSystemStorageItemBase> SearchResult = new List<FileSystemStorageItemBase>();

            try
            {
                if (Ptr.ToInt64() != -1)
                {
                    do
                    {
                        FileAttributes Attribute = (FileAttributes)Data.dwFileAttributes;

                        if (IncludeHiddenItem || !Attribute.HasFlag(FileAttributes.Hidden))
                        {
                            if (Attribute.HasFlag(FileAttributes.Directory))
                            {
                                if (Data.cFileName != "." && Data.cFileName != "..")
                                {
                                    string CurrentDataPath = Path.Combine(FolderPath, Data.cFileName);

                                    if (Regex.IsMatch(Data.cFileName, IsRegexExpresstion ? SearchWord : @$".*{Regex.Escape(SearchWord)}.*", IgnoreCase ? RegexOptions.IgnoreCase : RegexOptions.None))
                                    {
                                        FileTimeToSystemTime(ref Data.ftLastWriteTime, out SYSTEMTIME ModTime);
                                        DateTime ModifiedTime = new DateTime(ModTime.Year, ModTime.Month, ModTime.Day, ModTime.Hour, ModTime.Minute, ModTime.Second, ModTime.Milliseconds, DateTimeKind.Utc);

                                        FileTimeToSystemTime(ref Data.ftCreationTime, out SYSTEMTIME CreTime);
                                        DateTime CreationTime = new DateTime(CreTime.Year, CreTime.Month, CreTime.Day, CreTime.Hour, CreTime.Minute, CreTime.Second, CreTime.Milliseconds, DateTimeKind.Utc);

                                        if (Attribute.HasFlag(FileAttributes.Hidden))
                                        {
                                            SearchResult.Add(new HiddenStorageItem(Data, StorageItemTypes.Folder, CurrentDataPath, CreationTime, ModifiedTime));
                                        }
                                        else
                                        {
                                            SearchResult.Add(new FileSystemStorageItemBase(Data, StorageItemTypes.Folder, CurrentDataPath, CreationTime, ModifiedTime));
                                        }
                                    }

                                    if (SearchInSubFolders)
                                    {
                                        SearchResult.AddRange(Search(CurrentDataPath, SearchWord, true, IncludeHiddenItem, IsRegexExpresstion, IgnoreCase, CancelToken));
                                    }
                                }
                            }
                            else
                            {
                                if (Regex.IsMatch(Data.cFileName, IsRegexExpresstion ? SearchWord : @$".*{Regex.Escape(SearchWord)}.*", IgnoreCase ? RegexOptions.IgnoreCase : RegexOptions.None))
                                {
                                    string CurrentDataPath = Path.Combine(FolderPath, Data.cFileName);

                                    FileTimeToSystemTime(ref Data.ftLastWriteTime, out SYSTEMTIME ModTime);
                                    DateTime ModifiedTime = new DateTime(ModTime.Year, ModTime.Month, ModTime.Day, ModTime.Hour, ModTime.Minute, ModTime.Second, ModTime.Milliseconds, DateTimeKind.Utc);

                                    FileTimeToSystemTime(ref Data.ftCreationTime, out SYSTEMTIME CreTime);
                                    DateTime CreationTime = new DateTime(CreTime.Year, CreTime.Month, CreTime.Day, CreTime.Hour, CreTime.Minute, CreTime.Second, CreTime.Milliseconds, DateTimeKind.Utc);

                                    if (Attribute.HasFlag(FileAttributes.Hidden))
                                    {
                                        SearchResult.Add(new HiddenStorageItem(Data, StorageItemTypes.File, CurrentDataPath, CreationTime, ModifiedTime));
                                    }
                                    else if (SecureFolderPath == FolderPath)
                                    {
                                        if (Data.cFileName.EndsWith(".sle", StringComparison.OrdinalIgnoreCase))
                                        {
                                            SearchResult.Add(new SecureAreaStorageItem(Data, Path.Combine(FolderPath, Data.cFileName), CreationTime, ModifiedTime));
                                        }
                                    }
                                    else if (!Data.cFileName.EndsWith(".url", StringComparison.OrdinalIgnoreCase))
                                    {
                                        if (Data.cFileName.EndsWith(".lnk", StringComparison.OrdinalIgnoreCase))
                                        {
                                            SearchResult.Add(new HyperlinkStorageItem(Data, CurrentDataPath, CreationTime, ModifiedTime));
                                        }
                                        else
                                        {
                                            SearchResult.Add(new FileSystemStorageItemBase(Data, StorageItemTypes.File, CurrentDataPath, CreationTime, ModifiedTime));
                                        }
                                    }
                                }
                            }
                        }
                    }
                    while (FindNextFile(Ptr, out Data) && !CancelToken.IsCancellationRequested);

                    return SearchResult;
                }
                else
                {
                    return SearchResult;
                }
            }
            catch (Exception ex)
            {
                LogTracer.Log(ex);
                return SearchResult;
            }
            finally
            {
                FindClose(Ptr);
            }
        }

        public static List<FileSystemStorageItemBase> GetStorageItems(string FolderPath, bool IncludeHiddenItem, ItemFilters Filter)
        {
            if (string.IsNullOrWhiteSpace(FolderPath))
            {
                throw new ArgumentException("Argument could not be empty", nameof(FolderPath));
            }

            IntPtr Ptr = FindFirstFileExFromApp(Path.Combine(FolderPath, "*"), FINDEX_INFO_LEVELS.FindExInfoBasic, out WIN32_FIND_DATA Data, FINDEX_SEARCH_OPS.FindExSearchNameMatch, IntPtr.Zero, FIND_FIRST_EX_LARGE_FETCH);

            try
            {
                if (Ptr.ToInt64() != -1)
                {
                    List<FileSystemStorageItemBase> Result = new List<FileSystemStorageItemBase>();

                    do
                    {
                        FileAttributes Attribute = (FileAttributes)Data.dwFileAttributes;

                        if (IncludeHiddenItem || !Attribute.HasFlag(FileAttributes.Hidden))
                        {
                            if (Attribute.HasFlag(FileAttributes.Directory) && Filter.HasFlag(ItemFilters.Folder))
                            {
                                if (Data.cFileName != "." && Data.cFileName != "..")
                                {
                                    FileTimeToSystemTime(ref Data.ftLastWriteTime, out SYSTEMTIME ModTime);
                                    DateTime ModifiedTime = new DateTime(ModTime.Year, ModTime.Month, ModTime.Day, ModTime.Hour, ModTime.Minute, ModTime.Second, ModTime.Milliseconds, DateTimeKind.Utc);

                                    FileTimeToSystemTime(ref Data.ftCreationTime, out SYSTEMTIME CreTime);
                                    DateTime CreationTime = new DateTime(CreTime.Year, CreTime.Month, CreTime.Day, CreTime.Hour, CreTime.Minute, CreTime.Second, CreTime.Milliseconds, DateTimeKind.Utc);

                                    if (Attribute.HasFlag(FileAttributes.Hidden))
                                    {
                                        Result.Add(new HiddenStorageItem(Data, StorageItemTypes.Folder, Path.Combine(FolderPath, Data.cFileName), CreationTime, ModifiedTime));
                                    }
                                    else
                                    {
                                        Result.Add(new FileSystemStorageItemBase(Data, StorageItemTypes.Folder, Path.Combine(FolderPath, Data.cFileName), CreationTime, ModifiedTime));
                                    }
                                }
                            }
                            else if (Filter.HasFlag(ItemFilters.File))
                            {
                                FileTimeToSystemTime(ref Data.ftLastWriteTime, out SYSTEMTIME ModTime);
                                DateTime ModifiedTime = new DateTime(ModTime.Year, ModTime.Month, ModTime.Day, ModTime.Hour, ModTime.Minute, ModTime.Second, ModTime.Milliseconds, DateTimeKind.Utc);

                                FileTimeToSystemTime(ref Data.ftCreationTime, out SYSTEMTIME CreTime);
                                DateTime CreationTime = new DateTime(CreTime.Year, CreTime.Month, CreTime.Day, CreTime.Hour, CreTime.Minute, CreTime.Second, CreTime.Milliseconds, DateTimeKind.Utc);

                                if (Attribute.HasFlag(FileAttributes.Hidden))
                                {
                                    Result.Add(new HiddenStorageItem(Data, StorageItemTypes.File, Path.Combine(FolderPath, Data.cFileName), CreationTime, ModifiedTime));
                                }
                                else if (SecureFolderPath == FolderPath)
                                {
                                    if (Data.cFileName.EndsWith(".sle", StringComparison.OrdinalIgnoreCase))
                                    {
                                        Result.Add(new SecureAreaStorageItem(Data, Path.Combine(FolderPath, Data.cFileName), CreationTime, ModifiedTime));
                                    }
                                }
                                else if (!Data.cFileName.EndsWith(".url", StringComparison.OrdinalIgnoreCase))
                                {
                                    if (Data.cFileName.EndsWith(".lnk", StringComparison.OrdinalIgnoreCase))
                                    {
                                        Result.Add(new HyperlinkStorageItem(Data, Path.Combine(FolderPath, Data.cFileName), CreationTime, ModifiedTime));
                                    }
                                    else
                                    {
                                        Result.Add(new FileSystemStorageItemBase(Data, StorageItemTypes.File, Path.Combine(FolderPath, Data.cFileName), CreationTime, ModifiedTime));
                                    }
                                }
                            }
                        }
                    }
                    while (FindNextFile(Ptr, out Data));

                    return Result;
                }
                else
                {
                    LogTracer.Log(new Win32Exception(Marshal.GetLastWin32Error()));
                    return new List<FileSystemStorageItemBase>();
                }
            }
            catch (Exception ex)
            {
                LogTracer.Log(ex);
                return new List<FileSystemStorageItemBase>();
            }
            finally
            {
                FindClose(Ptr);
            }
        }

        public static FileSystemStorageItemBase GetStorageItem(string ItemPath, ItemFilters Filter = ItemFilters.Folder | ItemFilters.File)
        {
            if (string.IsNullOrWhiteSpace(ItemPath))
            {
                throw new ArgumentNullException(nameof(ItemPath), "Argument could not be null");
            }

            try
            {
                IntPtr Ptr = FindFirstFileExFromApp(ItemPath, FINDEX_INFO_LEVELS.FindExInfoBasic, out WIN32_FIND_DATA Data, FINDEX_SEARCH_OPS.FindExSearchNameMatch, IntPtr.Zero, FIND_FIRST_EX_LARGE_FETCH);

                try
                {
                    if (Ptr.ToInt64() != -1)
                    {
                        FileAttributes Attribute = (FileAttributes)Data.dwFileAttributes;

                        if (Attribute.HasFlag(FileAttributes.Directory) && Filter.HasFlag(ItemFilters.Folder))
                        {
                            if (Data.cFileName != "." && Data.cFileName != "..")
                            {
                                FileTimeToSystemTime(ref Data.ftLastWriteTime, out SYSTEMTIME ModTime);
                                DateTime ModifiedTime = new DateTime(ModTime.Year, ModTime.Month, ModTime.Day, ModTime.Hour, ModTime.Minute, ModTime.Second, ModTime.Milliseconds, DateTimeKind.Utc);

                                FileTimeToSystemTime(ref Data.ftCreationTime, out SYSTEMTIME CreTime);
                                DateTime CreationTime = new DateTime(CreTime.Year, CreTime.Month, CreTime.Day, CreTime.Hour, CreTime.Minute, CreTime.Second, CreTime.Milliseconds, DateTimeKind.Utc);

                                if (Attribute.HasFlag(FileAttributes.Hidden))
                                {
                                    return new HiddenStorageItem(Data, StorageItemTypes.Folder, ItemPath, CreationTime, ModifiedTime);
                                }
                                else
                                {
                                    return new FileSystemStorageItemBase(Data, StorageItemTypes.Folder, ItemPath, CreationTime, ModifiedTime);
                                }
                            }
                        }
                        else if (Filter.HasFlag(ItemFilters.File))
                        {
                            FileTimeToSystemTime(ref Data.ftLastWriteTime, out SYSTEMTIME ModTime);
                            DateTime ModifiedTime = new DateTime(ModTime.Year, ModTime.Month, ModTime.Day, ModTime.Hour, ModTime.Minute, ModTime.Second, ModTime.Milliseconds, DateTimeKind.Utc);

                            FileTimeToSystemTime(ref Data.ftCreationTime, out SYSTEMTIME CreTime);
                            DateTime CreationTime = new DateTime(CreTime.Year, CreTime.Month, CreTime.Day, CreTime.Hour, CreTime.Minute, CreTime.Second, CreTime.Milliseconds, DateTimeKind.Utc);

                            if (Attribute.HasFlag(FileAttributes.Hidden))
                            {
                                return new HiddenStorageItem(Data, StorageItemTypes.File, ItemPath, CreationTime, ModifiedTime);
                            }
                            else if (SecureFolderPath == ItemPath)
                            {
                                if (Data.cFileName.EndsWith(".sle", StringComparison.OrdinalIgnoreCase))
                                {
                                    return new SecureAreaStorageItem(Data, Path.Combine(ItemPath, Data.cFileName), CreationTime, ModifiedTime);
                                }
                            }
                            else if (!Data.cFileName.EndsWith(".url", StringComparison.OrdinalIgnoreCase))
                            {
                                if (Data.cFileName.EndsWith(".lnk", StringComparison.OrdinalIgnoreCase))
                                {
                                    return new HyperlinkStorageItem(Data, ItemPath, CreationTime, ModifiedTime);
                                }
                                else
                                {
                                    return new FileSystemStorageItemBase(Data, StorageItemTypes.File, ItemPath, CreationTime, ModifiedTime);
                                }
                            }
                        }

                        return null;
                    }
                    else
                    {
                        LogTracer.Log(new Win32Exception(Marshal.GetLastWin32Error()));
                        return null;
                    }
                }
                finally
                {
                    FindClose(Ptr);
                }
            }
            catch (Exception ex)
            {
                LogTracer.Log(ex);
                return null;
            }
        }

        public static List<FileSystemStorageItemBase> GetStorageItemInBatch(params string[] PathArray)
        {
            if (PathArray.Length == 0 || PathArray.Any((Item) => string.IsNullOrWhiteSpace(Item)))
            {
                throw new ArgumentException("Argument could not be empty", nameof(PathArray));
            }

            if (PathArray.Any((Item) => Path.GetPathRoot(Item) == Item))
            {
                throw new ArgumentException("Unsupport for root directory", nameof(PathArray));
            }

            try
            {
                List<FileSystemStorageItemBase> Result = new List<FileSystemStorageItemBase>(PathArray.Length);

                foreach (string Path in PathArray)
                {
                    IntPtr Ptr = FindFirstFileExFromApp(Path, FINDEX_INFO_LEVELS.FindExInfoBasic, out WIN32_FIND_DATA Data, FINDEX_SEARCH_OPS.FindExSearchNameMatch, IntPtr.Zero, FIND_FIRST_EX_LARGE_FETCH);

                    try
                    {
                        if (Ptr.ToInt64() != -1)
                        {
                            FileAttributes Attribute = (FileAttributes)Data.dwFileAttributes;

                            if (Attribute.HasFlag(FileAttributes.Directory))
                            {
                                if (Data.cFileName != "." && Data.cFileName != "..")
                                {
                                    FileTimeToSystemTime(ref Data.ftLastWriteTime, out SYSTEMTIME ModTime);
                                    DateTime ModifiedTime = new DateTime(ModTime.Year, ModTime.Month, ModTime.Day, ModTime.Hour, ModTime.Minute, ModTime.Second, ModTime.Milliseconds, DateTimeKind.Utc);

                                    FileTimeToSystemTime(ref Data.ftCreationTime, out SYSTEMTIME CreTime);
                                    DateTime CreationTime = new DateTime(CreTime.Year, CreTime.Month, CreTime.Day, CreTime.Hour, CreTime.Minute, CreTime.Second, CreTime.Milliseconds, DateTimeKind.Utc);

                                    if (Attribute.HasFlag(FileAttributes.Hidden))
                                    {
                                        Result.Add(new HiddenStorageItem(Data, StorageItemTypes.Folder, Path, CreationTime, ModifiedTime));
                                    }
                                    else
                                    {
                                        Result.Add(new FileSystemStorageItemBase(Data, StorageItemTypes.Folder, Path, CreationTime, ModifiedTime));
                                    }
                                }
                            }
                            else
                            {
                                FileTimeToSystemTime(ref Data.ftLastWriteTime, out SYSTEMTIME ModTime);
                                DateTime ModifiedTime = new DateTime(ModTime.Year, ModTime.Month, ModTime.Day, ModTime.Hour, ModTime.Minute, ModTime.Second, ModTime.Milliseconds, DateTimeKind.Utc);

                                FileTimeToSystemTime(ref Data.ftCreationTime, out SYSTEMTIME CreTime);
                                DateTime CreationTime = new DateTime(CreTime.Year, CreTime.Month, CreTime.Day, CreTime.Hour, CreTime.Minute, CreTime.Second, CreTime.Milliseconds, DateTimeKind.Utc);

                                if (Attribute.HasFlag(FileAttributes.Hidden))
                                {
                                    Result.Add(new HiddenStorageItem(Data, StorageItemTypes.File, Path, CreationTime, ModifiedTime));
                                }
                                else if (SecureFolderPath == Path)
                                {
                                    if (Data.cFileName.EndsWith(".sle", StringComparison.OrdinalIgnoreCase))
                                    {
                                        Result.Add(new SecureAreaStorageItem(Data, System.IO.Path.Combine(Path, Data.cFileName), CreationTime, ModifiedTime));
                                    }
                                }
                                else if (!Data.cFileName.EndsWith(".url", StringComparison.OrdinalIgnoreCase))
                                {
                                    if (Data.cFileName.EndsWith(".lnk", StringComparison.OrdinalIgnoreCase))
                                    {
                                        Result.Add(new HyperlinkStorageItem(Data, Path, CreationTime, ModifiedTime));
                                    }
                                    else
                                    {
                                        Result.Add(new FileSystemStorageItemBase(Data, StorageItemTypes.File, Path, CreationTime, ModifiedTime));
                                    }
                                }
                            }
                        }
                        else
                        {
                            LogTracer.Log(new Win32Exception(Marshal.GetLastWin32Error()));
                        }
                    }
                    finally
                    {
                        FindClose(Ptr);
                    }
                }

                return Result;
            }
            catch (Exception ex)
            {
                LogTracer.Log(ex);
                return new List<FileSystemStorageItemBase>();
            }
        }

        public static List<string> GetStorageItemsPath(string FolderPath, bool IncludeHiddenItem, ItemFilters Filter)
        {
            if (string.IsNullOrWhiteSpace(FolderPath))
            {
                throw new ArgumentNullException(nameof(FolderPath), "Argument could not be null");
            }

            IntPtr Ptr = FindFirstFileExFromApp(Path.Combine(FolderPath, "*"), FINDEX_INFO_LEVELS.FindExInfoBasic, out WIN32_FIND_DATA Data, FINDEX_SEARCH_OPS.FindExSearchNameMatch, IntPtr.Zero, FIND_FIRST_EX_LARGE_FETCH);

            try
            {
                if (Ptr.ToInt64() != -1)
                {
                    List<string> Result = new List<string>();

                    do
                    {
                        FileAttributes Attribute = (FileAttributes)Data.dwFileAttributes;

                        if (IncludeHiddenItem || !Attribute.HasFlag(FileAttributes.Hidden))
                        {
                            if (((FileAttributes)Data.dwFileAttributes).HasFlag(FileAttributes.Directory) && Filter.HasFlag(ItemFilters.Folder))
                            {
                                if (Data.cFileName != "." && Data.cFileName != "..")
                                {
                                    Result.Add(Path.Combine(FolderPath, Data.cFileName));
                                }
                            }
                            else if (Filter.HasFlag(ItemFilters.File))
                            {
                                Result.Add(Path.Combine(FolderPath, Data.cFileName));
                            }
                        }
                    }
                    while (FindNextFile(Ptr, out Data));

                    return Result;
                }
                else
                {
                    LogTracer.Log(new Win32Exception(Marshal.GetLastWin32Error()));
                    return new List<string>();
                }
            }
            catch (Exception ex)
            {
                LogTracer.Log(ex);
                return new List<string>();
            }
            finally
            {
                FindClose(Ptr);
            }
        }

        public static SafePipeHandle GetHandleFromNamedPipe(string PipeName)
        {
            IntPtr Handle = CreateFileFromApp(@$"\\.\pipe\{PipeName}", GENERIC_READ | GENERIC_WRITE, FILE_NO_SHARE, IntPtr.Zero, OPEN_EXISTING, 0, IntPtr.Zero);

            SafePipeHandle SPipeHandle = new SafePipeHandle(Handle, true);

            if (SPipeHandle.IsInvalid)
            {
                throw new Win32Exception(Marshal.GetLastWin32Error());
            }
            else
            {
                return SPipeHandle;
            }
        }
    }
}
