namespace D2S.Library.Common
{
    /// <summary>
    /// DirectoryCreateError
    /// </summary>
    public enum DirectoryCreateError
    {
        /// <summary>
        /// The specified directory already exists.
        /// </summary>
        DirectoryAlreadyExist = 0,

        /// <summary>
        /// The specified path is invalid (for example, it is on an unmapped drive).
        /// </summary>
        DirectoryNotFoundException = 1,

        /// <summary>
        /// The specified path, file name, or both exceed the system-defined maximum length. For example, on Windows-based platforms, paths must be less than 248 characters and file names must be less than 260 characters.
        /// </summary>
        PathTooLongException = 2,

        /// <summary>
        /// The directory specified by path is a file.-or-The network name is not known.
        /// </summary>
        IoException = 3,

        /// <summary>
        /// The caller does not have the required permission.
        /// </summary>
        UnauthorizedAccessException = 4,

        /// <summary>
        /// path is null.
        /// </summary>
        ArgumentNullException = 5,

        /// <summary>
        /// path is a zero-length string, contains only white space, or contains one or more invalid characters as defined by System.IO.Path.InvalidPathChars.-or-path is prefixed with, or contains only a colon character (:).
        /// </summary>
        ArgumentException = 6,

        /// <summary>
        /// path contains a colon character (:) that is not part of a drive label ("C:\").
        /// </summary>
        NotSupportedException = 7,

        /// <summary>
        /// The directory creation operation has failed.
        /// </summary>
        Exception = 8,
    }

    /// <summary>
    /// DirectoryDeleteError
    /// </summary>
    public enum DirectoryDeleteError
    {
        /// <summary>
        /// The specified directory already exists.
        /// </summary>
        DirectoryAlreadyExist = 0,

        /// <summary>
        /// path does not exist or could not be found.-or-path refers to a file instead of a directory.-or-The specified path is invalid (for example, it is on an unmapped drive).
        /// </summary>
        DirectoryNotFoundException = 1,

        /// <summary>
        /// The specified path, file name, or both exceed the system-defined maximum length. For example, on Windows-based platforms, paths must be less than 248 characters and file names must be less than 260 characters.
        /// </summary>
        PathTooLongException = 2,

        /// <summary>
        /// A file with the same name and location specified by path exists.-or-The directory specified by path is read-only, or recursive is false and path is not an empty directory. -or-The directory is the application's current working directory. -or-The directory contains a read-only file.-or-The directory is being used by another process.There is an open handle on the directory or on one of its files, and the operating system is Windows XP or earlier. This open handle can result from enumerating directories and files. For more information, see How to: Enumerate Directories and Files.
        /// </summary>
        IoException = 3,

        /// <summary>
        /// The caller does not have the required permission.
        /// </summary>
        UnauthorizedAccessException = 4,

        /// <summary>
        /// path is null.
        /// </summary>
        ArgumentNullException = 5,

        /// <summary>
        /// path is a zero-length string, contains only white space, or contains one or more invalid characters as defined by System.IO.Path.InvalidPathChars.-or-path is prefixed with, or contains only a colon character (:).
        /// </summary>
        ArgumentException = 6,

        /// <summary>
        /// The directory delete operation has failed.
        /// </summary>
        Exception = 7,
    }

    /// <summary>
    /// FileCopyError
    /// </summary>
    public enum FileCopyError
    {
        /// <summary>
        /// The caller does not have the required permission. -or-destFileName is read-only.
        /// </summary>
        UnauthorizedAccessException = 0,

        /// <summary>
        /// sourceFileName or destFileName is null.
        /// </summary>
        ArgumentNullException = 1,

        /// <summary>
        /// sourceFileName or destFileName is a zero-length string, contains only white space, or contains one or more invalid characters as defined by System.IO.Path.InvalidPathChars.-or- sourceFileName or destFileName specifies a directory.
        /// </summary>
        ArgumentException = 2,

        /// <summary>
        /// The specified path, file name, or both exceed the system-defined maximum length. For example, on Windows-based platforms, paths must be less than 248 characters, and file names must be less than 260 characters.
        /// </summary>
        PathTooLongException = 3,

        /// <summary>
        /// The path specified in sourceFileName or destFileName is invalid (for example, it is on an unmapped drive).
        /// </summary>
        DirectoryNotFoundException = 4,

        /// <summary>
        /// sourceFileName was not found.
        /// </summary>
        FileNotFoundException = 5,

        /// <summary>
        /// destFileName exists and overwrite is false.-or- An I/O error has occurred.
        /// </summary>
        IoException = 6,

        /// <summary>
        /// sourceFileName or destFileName is in an invalid format.
        /// </summary>
        NotSupportedException = 7,

        /// <summary>
        /// The file copy operation has failed.
        /// </summary>
        Exception = 8,
    }

    /// <summary>
    /// FileDeleteError
    /// </summary>
    public enum FileDeleteError
    {
        /// <summary>
        /// path is null.
        /// </summary>
        ArgumentNullException = 0,

        /// <summary>
        /// path is a zero-length string, contains only white space, or contains one or more invalid characters as defined by System.IO.Path.InvalidPathChars.
        /// </summary>
        ArgumentException = 1,

        /// <summary>
        /// The specified path is invalid (for example, it is on an unmapped drive).
        /// </summary>
        DirectoryNotFoundException = 2,

        /// <summary>
        /// The specified path, file name, or both exceed the system-defined maximum length. For example, on Windows-based platforms, paths must be less than 248 characters, and file names must be less than 260 characters.
        /// </summary>
        PathTooLongException = 3,

        /// <summary>
        /// The specified file is in use. -or-There is an open handle on the file, and the operating system is Windows XP or earlier. This open handle can result from enumerating directories and files. For more information, see How to: Enumerate Directories and Files.
        /// </summary>
        IoException = 4,

        /// <summary>
        /// path is in an invalid format.
        /// </summary>
        NotSupportedException = 5,

        /// <summary>
        /// The caller does not have the required permission.-or- path is a directory.-or- path specified a read-only file.
        /// </summary>
        UnauthorizedAccessException = 6,

        /// <summary>
        /// The file delete operation has failed.
        /// </summary>
        Exception = 7,
    }
}