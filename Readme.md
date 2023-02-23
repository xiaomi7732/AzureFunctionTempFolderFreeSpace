# Getting temp folder free space in Azure Function

Temp folder is mounted. DriveInfo can't be used directly to get the available space of it.

* The working way - Use Win32 API:

    ```csharp
    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetDiskFreeSpaceEx(string lpDirectoryName,
        out ulong lpFreeBytesAvailable,
        out ulong lpTotalNumberOfBytes,
        out ulong lpTotalNumberOfFreeBytes);

    private static ulong GetDiskFreeSpace(string path)
    {
        // Making sure there's tailing slash.
        // Refer to https://learn.microsoft.com/en-us/windows/win32/api/fileapi/nf-fileapi-getdiskfreespaceexa
        path = path.TrimEnd('\\') + '\\';

        if (string.IsNullOrEmpty(path))
        {
            throw new ArgumentNullException(nameof(path));
        }

        if (!GetDiskFreeSpaceEx(path, out ulong freeSpace, out _, out _))
        {
            throw new Win32Exception(Marshal.GetLastWin32Error());
        }

        return freeSpace;
    }
    ```

    To call it:

    ```csharp
    ulong availableSpace = GetDiskFreeSpace(tempFolder);
    ```


* The caveat:

    ```csharp
    // Assuming temp is at c:\local\temp
    // This roots to the drive of C usually, and return the available space of it.
    // Problem is that `temp` is mounted, and has different free space size than c drive.
    DriveInfo driveInfo = new (tempFolder); 
    long availableSpace = driveInfo.AvailableFreeSpace;
    ```

