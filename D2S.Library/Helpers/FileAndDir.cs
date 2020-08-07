namespace D2S.Library.Helpers
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using D2S.Library.Common;
    using D2S.Library.Services;

    /// <summary>
    /// File and directory related functions
    /// </summary>
    public class FileAndDir
    {
        #region Members

        private static volatile FileAndDir _instance;
        private static readonly object SyncRoot = new Object();

        #endregion Members

        #region Properties

        /// <summary>
        /// Get an instance of the class using the singleton pattern for maximum performance
        /// <para>The class can be used directly like a static class without being instantiated</para>
        /// <para>Singleton pattern - http://en.wikipedia.org/wiki/Singleton_pattern</para>
        /// <para>Implementing the Singleton Pattern - http://www.yoda.arachsys.com/csharp/singleton.html</para>
        /// <para>The singleton pattern VS static classes - http://dotnet.org.za/reyn/archive/2004/07/07/2606.aspx</para>
        /// </summary>
        public static FileAndDir Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (SyncRoot)
                    {
                        if (_instance == null)
                        {
                            _instance = new FileAndDir();
                        }
                    }
                }

                return (_instance);
            }
        }

        #endregion

        #region Events

        /// <summary>
        /// Constructor
        /// </summary>
        public FileAndDir()
        {
        }

        #endregion

        #region Methods

        /// <summary>
        /// Copy a file
        /// </summary>
        /// <param name="filePathSource">string</param>
        /// <param name="filePathTarget">string</param>
        /// <param name="overwrite">bool</param>
        /// <param name="logErrors">bool</param>
        /// <returns>bool</returns>
        public bool FileCopy(
            string filePathSource,
            string filePathTarget,
            bool overwrite = true,
            bool logErrors = true)
        {
            string errorType;
            string errorMessage;

            return FileCopy(filePathSource, filePathTarget, out errorType, out errorMessage, overwrite, logErrors);
        }

        /// <summary>
        /// Copy a file (return errorType and errorMessage as a reference)
        /// </summary>
        /// <param name="filePathSource">string</param>
        /// <param name="filePathTarget">string</param>
        /// <param name="errorType">out string</param>
        /// <param name="errorMessage">out string</param>
        /// <param name="overwrite">bool</param>
        /// <param name="logErrors">bool</param>
        /// <returns>bool</returns>
        public bool FileCopy(
            string filePathSource,
            string filePathTarget,
            out string errorType,
            out string errorMessage,
            bool overwrite = true,
            bool logErrors = true)
        {
            var result = true;
            errorType = string.Empty;
            errorMessage = string.Empty;

            try
            {
                if (File.Exists(filePathTarget))
                {
                    // Remove ReadOnly attribute - can cause issues when overwriting files between network locations
                    var fileInfo = new FileInfo(filePathTarget);
                    if (fileInfo.IsReadOnly)
                    {
                        fileInfo.IsReadOnly = false;
                        fileInfo.Refresh();
                    }
                }

                File.Copy(filePathSource, filePathTarget, overwrite);
            }
            catch (UnauthorizedAccessException unauthorizedAccessEx)
            {
                errorType = FileCopyError.UnauthorizedAccessException.ToString();
                errorMessage = "The caller does not have the required permission. -or-destFileName is read-only.";
                if (logErrors) LogService.Instance.Error(unauthorizedAccessEx.Message);
                result = false;
            }
            catch (ArgumentNullException argumentNullEx)
            {
                errorType = FileCopyError.ArgumentNullException.ToString();
                errorMessage = "sourceFileName or destFileName is null.";
                if (logErrors) LogService.Instance.Error(argumentNullEx.Message);
                result = false;
            }
            catch (ArgumentException argumentEx)
            {
                errorType = FileCopyError.ArgumentException.ToString();
                errorMessage = "sourceFileName or destFileName is a zero-length string, contains only white space, or contains one or more invalid characters as defined by System.IO.Path.InvalidPathChars.-or- sourceFileName or destFileName specifies a directory.";
                if (logErrors) LogService.Instance.Error(argumentEx.Message);
                result = false;
            }
            catch (PathTooLongException pathTooLongEx)
            {
                errorType = FileCopyError.PathTooLongException.ToString();
                errorMessage = "The specified path, file name, or both exceed the system-defined maximum length. For example, on Windows-based platforms, paths must be less than 248 characters, and file names must be less than 260 characters.";
                if (logErrors) LogService.Instance.Error(pathTooLongEx.Message);
                result = false;
            }
            catch (DirectoryNotFoundException directoryNotFoundEx)
            {
                errorType = FileCopyError.DirectoryNotFoundException.ToString();
                errorMessage = "The path specified in sourceFileName or destFileName is invalid (for example, it is on an unmapped drive).";
                if (logErrors) LogService.Instance.Error(directoryNotFoundEx.Message);
                result = false;
            }
            catch (FileNotFoundException fileNotFoundEx)
            {
                errorType = FileCopyError.FileNotFoundException.ToString();
                errorMessage = "sourceFileName was not found.";
                if (logErrors) LogService.Instance.Error(fileNotFoundEx.Message);
                result = false;
            }
            catch (IOException ioEx)
            {
                errorType = FileCopyError.IoException.ToString();
                errorMessage = "destFileName exists and overwrite is false.-or- An I/O error has occurred.";
                if (logErrors) LogService.Instance.Error(ioEx.Message);
                result = false;
            }
            catch (NotSupportedException notSupportedEx)
            {
                errorType = FileCopyError.NotSupportedException.ToString();
                errorMessage = "sourceFileName or destFileName is in an invalid format.";
                if (logErrors) LogService.Instance.Error(notSupportedEx.Message);
                result = false;
            }
            catch (Exception ex)
            {
                errorType = FileCopyError.Exception.ToString();
                errorMessage = "The file copy operation has failed.";
                if (logErrors) LogService.Instance.Error(ex.Message);
                result = false;
            }

            return result;
        }

        /// <summary>
        /// Delete a file
        /// </summary>
        /// <param name="filePathTarget">string</param>
        /// <param name="logErrors">bool</param>
        /// <returns>bool</returns>
        public bool FileDelete(
            string filePathTarget,
            bool logErrors = true)
        {
            string errorType;
            string errorMessage;

            return FileDelete(filePathTarget, out errorType, out errorMessage, logErrors);
        }

        /// <summary>
        /// Delete a file
        /// </summary>
        /// <param name="filePathTarget">string</param>
        /// <param name="errorType">out string</param>
        /// <param name="errorMessage">out string</param>
        /// <param name="logErrors">bool</param>
        /// <returns>bool</returns>
        public bool FileDelete(
            string filePathTarget,
            out string errorType,
            out string errorMessage,
            bool logErrors = true)
        {
            var result = true;
            errorType = string.Empty;
            errorMessage = string.Empty;

            try
            {
                if (File.Exists(filePathTarget))
                {
                    // Remove ReadOnly attribute - can cause issues when overwriting files between network locations
                    var fileInfo = new FileInfo(filePathTarget);
                    if (fileInfo.IsReadOnly)
                    {
                        fileInfo.IsReadOnly = false;
                        fileInfo.Refresh();
                    }
                }

                File.Delete(filePathTarget);
            }
            catch (ArgumentNullException argumentNullExceptionEx)
            {
                errorType = FileDeleteError.ArgumentException.ToString();
                errorMessage = "path is null.";
                if (logErrors) LogService.Instance.Error(argumentNullExceptionEx.Message);
                result = false;
            }
            catch (ArgumentException argumentExceptionEx)
            {
                errorType = FileDeleteError.ArgumentException.ToString();
                errorMessage = "path is a zero-length string, contains only white space, or contains one or more invalid characters as defined by System.IO.Path.InvalidPathChars.";
                if (logErrors) LogService.Instance.Error(argumentExceptionEx.Message);
                result = false;
            }
            catch (DirectoryNotFoundException directoryNotFoundExceptionEx)
            {
                errorType = FileDeleteError.DirectoryNotFoundException.ToString();
                errorMessage = "The specified path is invalid (for example, it is on an unmapped drive).";
                if (logErrors) LogService.Instance.Error(directoryNotFoundExceptionEx.Message);
                result = false;
            }
            catch (PathTooLongException pathTooLongExceptionEx)
            {
                errorType = FileDeleteError.NotSupportedException.ToString();
                errorMessage = "The specified path, file name, or both exceed the system-defined maximum length. For example, on Windows-based platforms, paths must be less than 248 characters, and file names must be less than 260 characters.";
                if (logErrors) LogService.Instance.Error(pathTooLongExceptionEx.Message);
                result = false;
            }
            catch (IOException ioExceptionEx)
            {
                errorType = FileDeleteError.IoException.ToString();
                errorMessage = "The specified file is in use. -or-There is an open handle on the file, and the operating system is Windows XP or earlier. This open handle can result from enumerating directories and files. For more information, see How to: Enumerate Directories and Files.";
                if (logErrors) LogService.Instance.Error(ioExceptionEx.Message);
                result = false;
            }
            catch (NotSupportedException notSupportedEx)
            {
                errorType = FileDeleteError.NotSupportedException.ToString();
                errorMessage = "path is in an invalid format.";
                if (logErrors) LogService.Instance.Error(notSupportedEx.Message);
                result = false;
            }
            catch (UnauthorizedAccessException unauthorizedAccessExceptionEx)
            {
                errorType = FileDeleteError.UnauthorizedAccessException.ToString();
                errorMessage = "The caller does not have the required permission.-or- path is a directory.-or- path specified a read-only file.";
                if (logErrors) LogService.Instance.Error(unauthorizedAccessExceptionEx.Message);
                result = false;
            }
            catch (Exception ex)
            {
                errorType = FileDeleteError.Exception.ToString();
                errorMessage = "The file delete operation has failed.";
                if (logErrors) LogService.Instance.Error(ex.Message);
                result = false;
            }

            return result;
        }

        /// <summary>
        /// Move a file
        /// </summary>
        /// <param name="filePathSource">string</param>
        /// <param name="filePathTarget">string</param>
        /// <param name="logErrors">bool</param>
        /// <returns>bool</returns>
        public bool FileMove(
            string filePathSource,
            string filePathTarget,
            bool logErrors = true)
        {
            string errorType;
            string errorMessage;

            return FileMove(filePathSource, filePathTarget, out errorType, out errorMessage, logErrors);
        }

        /// <summary>
        /// Move a file (return errorType and errorMessage as a reference)
        /// <para>
        /// https://stackoverflow.com/questions/1324788/what-is-the-difference-between-file-and-fileinfo-in-c#1324808
        /// </para>
        /// </summary>
        /// <param name="filePathSource">string</param>
        /// <param name="filePathTarget">string</param>
        /// <param name="errorType">out string</param>
        /// <param name="errorMessage">out string</param>
        /// <param name="logErrors">bool</param>
        /// <returns>bool</returns>
        public bool FileMove(
            string filePathSource,
            string filePathTarget,
            out string errorType,
            out string errorMessage,
            bool logErrors = true)
        {
            var result = true;
            errorType = string.Empty;
            errorMessage = string.Empty;

            try
            {
                if (File.Exists(filePathTarget))
                {
                    // Remove ReadOnly attribute - can cause issues when overwriting files between network locations
                    var fileInfo = new FileInfo(filePathTarget);
                    if (fileInfo.IsReadOnly)
                    {
                        fileInfo.IsReadOnly = false;
                        fileInfo.Refresh();
                    }
                }

                File.Move(filePathSource, filePathTarget);
            }
            catch (UnauthorizedAccessException unauthorizedAccessEx)
            {
                errorType = FileCopyError.UnauthorizedAccessException.ToString();
                errorMessage = "The caller does not have the required permission. -or-destFileName is read-only.";
                if (logErrors) LogService.Instance.Error(unauthorizedAccessEx.Message);
                result = false;
            }
            catch (ArgumentNullException argumentNullEx)
            {
                errorType = FileCopyError.ArgumentNullException.ToString();
                errorMessage = "sourceFileName or destFileName is null.";
                if (logErrors) LogService.Instance.Error(argumentNullEx.Message);
                result = false;
            }
            catch (ArgumentException argumentEx)
            {
                errorType = FileCopyError.ArgumentException.ToString();
                errorMessage = "sourceFileName or destFileName is a zero-length string, contains only white space, or contains one or more invalid characters as defined by System.IO.Path.InvalidPathChars.-or- sourceFileName or destFileName specifies a directory.";
                if (logErrors) LogService.Instance.Error(argumentEx.Message);
                result = false;
            }
            catch (PathTooLongException pathTooLongEx)
            {
                errorType = FileCopyError.PathTooLongException.ToString();
                errorMessage = "The specified path, file name, or both exceed the system-defined maximum length. For example, on Windows-based platforms, paths must be less than 248 characters, and file names must be less than 260 characters.";
                if (logErrors) LogService.Instance.Error(pathTooLongEx.Message);
                result = false;
            }
            catch (DirectoryNotFoundException directoryNotFoundEx)
            {
                errorType = FileCopyError.DirectoryNotFoundException.ToString();
                errorMessage = "The path specified in sourceFileName or destFileName is invalid (for example, it is on an unmapped drive).";
                if (logErrors) LogService.Instance.Error(directoryNotFoundEx.Message);
                result = false;
            }
            catch (FileNotFoundException fileNotFoundEx)
            {
                errorType = FileCopyError.FileNotFoundException.ToString();
                errorMessage = "sourceFileName was not found.";
                if (logErrors) LogService.Instance.Error(fileNotFoundEx.Message);
                result = false;
            }
            catch (IOException ioEx)
            {
                errorType = FileCopyError.IoException.ToString();
                errorMessage = "destFileName exists and overwrite is false.-or- An I/O error has occurred.";
                if (logErrors) LogService.Instance.Error(ioEx.Message);
                result = false;
            }
            catch (NotSupportedException notSupportedEx)
            {
                errorType = FileCopyError.NotSupportedException.ToString();
                errorMessage = "sourceFileName or destFileName is in an invalid format.";
                if (logErrors) LogService.Instance.Error(notSupportedEx.Message);
                result = false;
            }
            catch (Exception ex)
            {
                errorType = FileCopyError.Exception.ToString();
                errorMessage = "The file copy operation has failed.";
                if (logErrors) LogService.Instance.Error(ex.Message);
                result = false;
            }

            return result;
        }

        /// <summary>
        /// Create a directory if it does not exist
        /// </summary>
        /// <param name="directoryPath">string</param>
        /// <param name="logErrors">bool</param>
        /// <returns>bool</returns>
        public bool DirectoryCreate(
            string directoryPath,
            bool logErrors = true)
        {
            string errorType;
            string errorMessage;

            return DirectoryCreate(directoryPath, out errorType, out errorMessage, logErrors);
        }

        /// <summary>
        /// Create a directory if it does not exist (return errorType and errorMessage as a reference)
        /// </summary>
        /// <param name="directoryPath">string</param>
        /// <param name="errorType">out string</param>
        /// <param name="errorMessage">out string</param>
        /// <param name="logErrors">bool</param>
        /// <returns>bool</returns>
        public bool DirectoryCreate(
            string directoryPath,
            out string errorType,
            out string errorMessage,
            bool logErrors = true)
        {
            bool result;
            errorType = string.Empty;
            errorMessage = string.Empty;

            try
            {
                if (!Directory.Exists(directoryPath))
                {
                    Directory.CreateDirectory(directoryPath);
                }
                else // Directory already exist
                {
                    errorType = DirectoryCreateError.DirectoryAlreadyExist.ToString();
                    errorMessage = "The specified directory already exists.";
                }

                result = true; // The directory was created successfully or already exist
            }
            catch (DirectoryNotFoundException directoryNotFoundEx)
            {
                errorType = DirectoryCreateError.DirectoryNotFoundException.ToString();
                errorMessage = "The specified path is invalid (for example, it is on an unmapped drive).";
                if (logErrors) LogService.Instance.Error(directoryNotFoundEx.Message);
                result = false;
            }
            catch (PathTooLongException pathTooLongEx)
            {
                errorType = DirectoryCreateError.PathTooLongException.ToString();
                errorMessage = "The specified path, file name, or both exceed the system-defined maximum length. For example, on Windows-based platforms, paths must be less than 248 characters and file names must be less than 260 characters.";
                if (logErrors) LogService.Instance.Error(pathTooLongEx.Message);
                result = false;
            }
            catch (IOException ioEx)
            {
                errorType = DirectoryCreateError.IoException.ToString();
                errorMessage = "The directory specified by path is a file.-or-The network name is not known.";
                if (logErrors) LogService.Instance.Error(ioEx.Message);
                result = false;
            }
            catch (UnauthorizedAccessException unauthorizedAccessEx)
            {
                errorType = DirectoryCreateError.UnauthorizedAccessException.ToString();
                errorMessage = "The caller does not have the required permission.";
                if (logErrors) LogService.Instance.Error(unauthorizedAccessEx.Message);
                result = false;
            }
            catch (ArgumentNullException argumentNullEx)
            {
                errorType = DirectoryCreateError.ArgumentNullException.ToString();
                errorMessage = "path is null.";
                if (logErrors) LogService.Instance.Error(argumentNullEx.Message);
                result = false;
            }
            catch (ArgumentException argumentEx)
            {
                errorType = DirectoryCreateError.ArgumentException.ToString();
                errorMessage = "path is a zero-length string, contains only white space, or contains one or more invalid characters as defined by System.IO.Path.InvalidPathChars.-or-path is prefixed with, or contains only a colon character (:).";
                if (logErrors) LogService.Instance.Error(argumentEx.Message);
                result = false;
            }
            catch (NotSupportedException notSupportedEx)
            {
                errorType = DirectoryCreateError.NotSupportedException.ToString();
                errorMessage = "path contains a colon character (:) that is not part of a drive label (\"C:\\\").";
                if (logErrors) LogService.Instance.Error(notSupportedEx.Message);
                result = false;
            }
            catch (Exception ex)
            {
                errorType = DirectoryCreateError.Exception.ToString();
                errorMessage = "The directory creation operation has failed.";
                if (logErrors) LogService.Instance.Error(ex.Message);
                result = false;
            }

            return result;
        }

        /// <summary>
        /// Delete a directory
        /// </summary>
        /// <param name="directoryPath">string</param>
        /// <param name="logErrors">bool</param>
        /// <param name="deleteRecursively">bool</param>
        /// <returns>bool</returns>
        public bool DirectoryDelete(
            string directoryPath,
            bool logErrors = true,
            bool deleteRecursively = false
            )
        {
            string errorType;
            string errorMessage;

            return DirectoryDelete(directoryPath, out errorType, out errorMessage, logErrors, deleteRecursively);
        }

        /// <summary>
        /// Delete a directory 
        /// </summary>
        /// <param name="directoryPath">string</param>
        /// <param name="logErrors">bool</param>
        /// <param name="errorType">out string</param>
        /// <param name="errorMessage">out string</param>
        /// <param name="deleteRecursively">bool</param>
        /// <returns>bool</returns>
        public bool DirectoryDelete(
            string directoryPath,
            out string errorType,
            out string errorMessage,
            bool logErrors = true,
            bool deleteRecursively = false)
        {
            bool result;
            errorType = string.Empty;
            errorMessage = string.Empty;

            try
            {
                if (Directory.Exists(directoryPath))
                {
                    Directory.Delete(directoryPath, deleteRecursively);
                }
                else // Directory does not exist
                {
                    errorType = DirectoryDeleteError.DirectoryAlreadyExist.ToString();
                    errorMessage = "The specified directory already exists.";
                }

                result = true; // The directory deleted successfully or it does not exist
            }
            catch (PathTooLongException pathTooLongExceptionEx)
            {
                errorType = DirectoryDeleteError.PathTooLongException.ToString();
                errorMessage = "The specified path, file name, or both exceed the system-defined maximum length. For example, on Windows-based platforms, paths must be less than 248 characters and file names must be less than 260 characters.";
                if (logErrors) LogService.Instance.Error(pathTooLongExceptionEx.Message);
                result = false;
            }
            catch (DirectoryNotFoundException directoryNotFoundExceptionEx)
            {
                errorType = DirectoryDeleteError.DirectoryNotFoundException.ToString();
                errorMessage = "path does not exist or could not be found.-or-path refers to a file instead of a directory.-or-The specified path is invalid (for example, it is on an unmapped drive).";
                if (logErrors) LogService.Instance.Error(directoryNotFoundExceptionEx.Message);
                result = false;
            }
            catch (IOException ioEx)
            {
                errorType = DirectoryDeleteError.IoException.ToString();
                errorMessage = "A file with the same name and location specified by path exists.-or-The directory specified by path is read-only, or recursive is false and path is not an empty directory. -or-The directory is the application's current working directory. -or-The directory contains a read-only file.-or-The directory is being used by another process.There is an open handle on the directory or on one of its files, and the operating system is Windows XP or earlier. This open handle can result from enumerating directories and files. For more information, see How to: Enumerate Directories and Files.";
                if (logErrors) LogService.Instance.Error(ioEx.Message);
                result = false;
            }
            catch (UnauthorizedAccessException unauthorizedAccessEx)
            {
                errorType = DirectoryDeleteError.UnauthorizedAccessException.ToString();
                errorMessage = "The caller does not have the required permission.";
                if (logErrors) LogService.Instance.Error(unauthorizedAccessEx.Message);
                result = false;
            }
            catch (ArgumentNullException argumentNullEx)
            {
                errorType = DirectoryDeleteError.ArgumentNullException.ToString();
                errorMessage = "path is null.";
                if (logErrors) LogService.Instance.Error(argumentNullEx.Message);
                result = false;
            }
            catch (ArgumentException argumentEx)
            {
                errorType = DirectoryDeleteError.ArgumentException.ToString();
                errorMessage = "path is a zero-length string, contains only white space, or contains one or more invalid characters as defined by System.IO.Path.InvalidPathChars.-or-path is prefixed with, or contains only a colon character (:).";
                if (logErrors) LogService.Instance.Error(argumentEx.Message);
                result = false;
            }
            catch (Exception ex)
            {
                errorType = DirectoryDeleteError.Exception.ToString();
                errorMessage = string.Format("The directory delete operation has failed ({0}).", deleteRecursively ? "Recursive" : "Non Recursive");
                if (logErrors) LogService.Instance.Error(ex.Message);
                result = false;
            }

            return result;
        }

        /// <summary>
        /// Open a directory location with the Windows default file explorer application
        /// </summary>
        /// <param name="directoryPath">string</param>
        /// <returns>string</returns>
        public bool DirectoryOpenLocation(string directoryPath)
        {
            var result = true;

            if (!Directory.Exists(directoryPath))
            {
                result = false;
            }
            else
            {
                System.Diagnostics.Process.Start(directoryPath);
            }

            return result;
        }

        /// <summary>
        /// Validate a directory to make sure it can be created (uses Path.GetFullPath to see if the path is valid)
        /// </summary>
        /// <param name="directoryPath">string</param>
        /// <returns>bool</returns>
        public bool DirectoryValidate(string directoryPath)
        {
            var result = true;

            try { var rootPath = Path.GetFullPath(directoryPath); }
            catch { result = false; }

            return result;
        }

        /// <summary>
        /// Recursively get list of folders and files in the provided search path 
        /// (do not use GetFiles with SearchOption.AllDirectories in order to be able to log files with access isuues)
        /// http://stackoverflow.com/questions/3710617/list-recursively-all-files-and-folders-under-the-giving-path
        /// http://stackoverflow.com/questions/6061957/get-all-files-and-directories-in-specific-path-fast
        /// http://stackoverflow.com/questions/2106877/is-there-a-faster-way-than-this-to-find-all-the-files-in-a-directory-and-all-sub
        /// </summary>
        /// <param name="searchPath">string</param>
        /// <param name="rootPath">string</param>
        /// <param name="searchListFiles">ref List Of string</param>
        /// <param name="searchListDirectories">ref List Of string</param>
        /// <returns>List Of string</returns>
        public void DirectorySearchRecursive(
            string searchPath,
            string rootPath,
            ref List<string> searchListFiles,
            ref List<string> searchListDirectories)
        {
            try
            {
                // (do not use GetFiles with SearchOption.AllDirectories in order to be able to log files with access isuues)
                foreach (var currentFile in Directory.GetFiles(searchPath))
                {
                    searchListFiles.Add(string.Format("{0}", currentFile.Remove(0, rootPath.Length + 1)));
                }

                try
                {
                    foreach (var currentDirectory in Directory.GetDirectories(searchPath))
                    {
                        searchListDirectories.Add(string.Format("{0}", currentDirectory.Remove(0, rootPath.Length + 1)));

                        DirectorySearchRecursive(currentDirectory, rootPath, ref searchListFiles, ref searchListDirectories);
                    }
                }
                catch (UnauthorizedAccessException unauthorizedAccessEx)
                {
                    LogService.Instance.Error(unauthorizedAccessEx.Message);
                }
                catch (Exception ex)
                {
                    LogService.Instance.Error(ex.Message);
                }
            }
            catch (Exception ex)
            {
                LogService.Instance.Error(ex.Message);
            }
        }

        /// <summary>
        /// Get a list of all files and folders in the provided search path
        /// </summary>
        /// <param name="searchPath">string</param>
        /// <param name="rootPath">string</param>
        /// <param name="searchListFiles">out List Of string</param>
        /// <param name="searchListDirectories">out List Of string</param>
        /// <returns>bool</returns>
        public bool DirectorySearch(string searchPath, string rootPath, out List<string> searchListFiles, out List<string> searchListDirectories)
        {
            var result = false;
            searchListFiles = new List<string>();
            searchListDirectories = new List<string>();

            if (!string.IsNullOrEmpty(rootPath) &&
                !string.IsNullOrEmpty(searchPath) &&
                searchPath.StartsWith(rootPath) &&
                Directory.Exists(searchPath))
            {
                DirectorySearchRecursive(
                    searchPath,
                    rootPath,
                    ref searchListFiles,
                    ref searchListDirectories);

                result = (searchListDirectories.Count > 0); // at least one folder
            }

            return result;
        }

        /// <summary>
        /// Returns the extension part of the file name or path
        /// </summary>
        /// <param name="fileNameOrPath">string</param>
        /// <param name="includeDot">bool</param>
        /// <returns>string</returns>
        public string GetFileExtensionOnly(string fileNameOrPath, bool includeDot = false)
        {
            //string filePath = ha.Context.Request.FilePath;
            //string fileExtension = VirtualPathUtility.GetExtension(filePath);

            if (string.IsNullOrEmpty(fileNameOrPath))
            {
                return (null);
            }

            string extension = null;
            int dotIndex = fileNameOrPath.LastIndexOf(".", StringComparison.Ordinal);

            if (dotIndex > 0)
            {
                if (includeDot)
                {
                    extension = fileNameOrPath.Substring(dotIndex, fileNameOrPath.Length - dotIndex);
                }
                else
                {
                    extension = fileNameOrPath.Substring(dotIndex + 1, fileNameOrPath.Length - dotIndex - 1);
                }
            }

            return (extension);
        }

        /// <summary>
        /// Returns the name part of the file name or path
        /// <para>Strip the extension off</para>
        /// </summary>
        /// <param name="fileNameOrPath"></param>
        /// <returns>string</returns>
        public string GetFileNameOnly(string fileNameOrPath)
        {
            if (string.IsNullOrEmpty(fileNameOrPath))
            {
                return (null);
            }

            int dotIndex = fileNameOrPath.LastIndexOf(".", StringComparison.Ordinal);

            if (dotIndex > 0)
            {
                return (fileNameOrPath.Substring(0, dotIndex));
            }
            else
            {
                return (fileNameOrPath);
            }
        }

        /// <summary>
        /// Delete files older than a specific date (e.g. DateTime.Now.AddDays(-1))
        /// Inspired by: http://www.woodwardweb.com/programming/delete_old_file.html
        /// </summary>
        /// <param name="path">string</param>
        /// <param name="lastDateToKeep">DateTime</param>
        /// <param name="deleteInSubFolders">bool</param>
        /// <param name="deleteEmptyFolders">bool</param>
        /// <returns>bool</returns>
        public bool DeleteFiles(string path, DateTime lastDateToKeep, bool deleteInSubFolders, bool deleteEmptyFolders)
        {
            if ((!Directory.Exists(path)))
            {
                return (false);
            }

            var dirInfo = new DirectoryInfo(path);

            FileInfo[] files = dirInfo.GetFiles();

            foreach (FileInfo file in files)
            {
                if ((file.LastWriteTime < lastDateToKeep))
                {
                    file.IsReadOnly = false;

                    file.Delete();
                }
            }

            // Recursively delete in all sub directories
            if ((deleteInSubFolders))
            {
                DirectoryInfo[] dirs = dirInfo.GetDirectories();

                foreach (DirectoryInfo dir in dirs)
                {
                    DeleteFiles(dir.FullName, lastDateToKeep, deleteInSubFolders, deleteEmptyFolders);

                    // Delete empty folders
                    if ((deleteEmptyFolders & dir.GetFiles().Length == 0))
                    {
                        dir.Delete();
                    }
                }
            }

            return (true);
        }

        public string GetContentsAsString(string file)
        {
            string response;

            using (var sr = File.OpenText(file))
            {
                response = sr.ReadToEnd();
            }

            return response;
        }

        public bool WriteToFile(string path, string fileName, string data)
        {
            var result = false;

            try
            {
                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }

                path = Path.Combine(path, fileName);

                using (var sw = File.CreateText(path))
                {
                    sw.Write(data);
                    sw.Flush();
                }

                result = true;
            }
            catch (Exception ex)
            {
                LogService.Instance.Error(ex.Message);
            }

            return result;
        }

        /// <summary>
        /// Returns the MIME type for the specified file extension
        /// <para>(no dot is expected before the file extension)</para>
        /// </summary>
        /// <param name="fileExtension"></param>
        /// <returns>string</returns>
        public bool IsSafeFileExtension(string fileExtension)
        {
            var isSafeFileExtension = false;
            if (!string.IsNullOrEmpty(fileExtension))
            {
                switch ((fileExtension.ToUpper()))
                {
                    case "DOC":
                    case "DOCX":
                    // case "RTF": // RTF can contain some security exploites
                    case "PDF":
                    case "GIF":
                    case "JPEG":
                    case "JPG":
                    case "PNG":
                    case "SWF":
                    case "MP4":
                    case "MPEG":
                    case "MOV":
                    case "WMV":
                    case "AVI":
                        isSafeFileExtension = true;
                        break;
                    default:
                        isSafeFileExtension = false;
                        break;
                }
            }

            return (isSafeFileExtension);
        }

        /// <summary>
        /// Returns the MIME type for the specified file extension
        /// <para>(no dot is expected before the file extension)</para>
        /// </summary>
        /// <param name="fileExtension"></param>
        /// <returns>string</returns>
        public string GetMimeType(string fileExtension)
        {

            string strMimeType = null;

            switch ((fileExtension.ToUpper()))
            {
                case "EXE":
                    strMimeType = "application/exe";
                    break;
                case "ZIP":
                    strMimeType = "application/zip";
                    break;
                case "DOC":
                case "DOCX":
                    strMimeType = "application/msword";
                    break;
                case "RTF":
                    strMimeType = "application/rtf";
                    break;
                case "PDF":
                    strMimeType = "application/pdf";
                    break;
                case "GIF":
                    strMimeType = "image/gif";
                    break;
                case "JPEG":
                    strMimeType = "image/jpg";
                    break;
                case "JPG":
                    strMimeType = "image/jpg";
                    break;
                case "PNG":
                    strMimeType = "image/png";
                    break;
                case "SWF":
                    strMimeType = "application/x-shockwave-flash";
                    break;
                case "MP4":
                    strMimeType = "video/mp4";
                    break;
                case "MPEG":
                    strMimeType = "video/mpeg";
                    break;
                case "MOV":
                    strMimeType = "video/quicktime";
                    break;
                case "WMV":
                case "AVI":
                    strMimeType = "video/x-ms-wmv";
                    break;
                default:
                    strMimeType = "application/octet-stream";
                    break;
            }

            return (strMimeType);
        }
    }

        #endregion Methods
}