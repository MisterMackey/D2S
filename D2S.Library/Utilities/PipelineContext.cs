using D2S.Library.Helpers;
using D2S.Library.Services;
using D2S.Library.Extractors;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.DataLake.Store;
using System.Text.RegularExpressions;
using System.Configuration;

namespace D2S.Library.Utilities
{
    /// <summary>
    /// PipelineContext stores generic information on the pipeline and exposes it to various parts of the pipeline that require it (for example the path to a source file). 
    /// basically it tracks a whole bunch of global state. 
    /// </summary>
    public class PipelineContext
    {
        public bool FirstLineContainsHeaders { get; set; }
        public bool WriteHeaderLineToTargetFile { get; set; }
        public string ProcessedFolder { get; set; }
        private string _SourceFilePath;
        public string SourceFilePath { get => getSourcePath(); set => _SourceFilePath = value; }
        public string DestinationFilePath { get; set; }
        public string ExcelWorksheetName { get; set; }
        public string SourceTableName { get; set; }
        public string DestinationTableName { get; set; }
        public List<string> SqlSourceColumnsSelected { get; set; }
        private string[] _ColumnNames { get; set; }
        public string[] ColumnNames { get { if (_ColumnNames == null) { _ColumnNames = GetColumnNames(); return _ColumnNames; } else return _ColumnNames; } set { _ColumnNames = value; } }
        private string[] _ColumnNamesSelection { get; set; }
        public string[] ColumnNamesSelection { get => GetColumnNamesSelection(); set => SetColumnNamesSelection(value); }
        public  bool IsOrdinalColumnRanking { get; set; }
        private string[] _DataTypes { get; set; }
        public string[] DataTypes
        {
            get
            {
                if (_DataTypes != null) { return _DataTypes; }
                else { _DataTypes = GetDataTypes(); return _DataTypes; }
            }
        }       
        public string Delimiter { get; set; }
        public double StringPadding { get; set; }
        public bool IsSuggestingDataTypes { get; set; }
        public bool IsCreatingTable { get; set; }
        public bool IsDroppingTable { get; set; }
        public bool IsTruncatingTable { get; set; }
        public bool SourceFileIsSourcedFromDial { get; set; }
        public bool IsReadingFromDataLake { get; set; }
        public string DataLakeAdress { get; set; }
        public bool PromptAzureLogin { get; set; }
        public string Qualifier { get; set; }
        /// <summary>
        /// Source file date format to be used from the file name,
        /// used for locating the file if a Portfolio date is specified
        /// to construct the file name based on the Portfolio date and match it with the file name,
        /// for example: colline_csa_pos_DDMMYY*.csv
        /// </summary>
        public string DateFormat
        {
            get; set;
        }

        /// <summary>
        /// Difference in days between the date on the file name (not actual file date) and the data inside the file,
        /// for example: if Protfolio date is X but data in the file is X-1 then DaysDifference should be set to 1)
        /// Otherwise if not specified then 0 is by default
        /// </summary>
        public int DaysDifference
        {
            get; set;
        }

        /// <summary>
        /// Default field length as NVARCHAR when suggested data types are not used
        /// </summary>
        public int DefaultFieldLength { get; set; }

        /// <summary>
        /// Convert spaces in header to a specific character
        /// (for example, space " ", underscore "_", none "")
        /// </summary>
        public string SetHeaderSpaceReplacement { get; set; }

        /// <summary>
        /// Display minimum output data (for readable log files)
        /// </summary>
        public bool SilentMode { get; set; }
        public bool IsSkippingError { get; set; }
        public int TotalObjectsInSequentialPipe { get; set; }        
        public int ADLStreamBufferSize { get; set; }
        public int CpuCountUsedToComputeParallalism { get; set; }
        public bool TryMatchFileNameInSourceFolderBasedOnRegex { get; set; }
        public string AzureTenant { get; set; }
        public string AzureClientId { get; set; }
        public string AzureSecretKey { get; set; }
        public string AzureSubscription { get; set; }
        /// <summary>
        /// the string value that represent a sql NULL in the file used.
        /// </summary>
        public string DbNullStringValue { get; set; }

        //constructor to provide default values
        public PipelineContext()
        {
            FirstLineContainsHeaders = true;
            ProcessedFolder = string.Empty;
            SourceFilePath = string.Empty;
            ExcelWorksheetName = string.Empty;
            SourceTableName = string.Empty;
            DestinationTableName = string.Empty;
            SqlSourceColumnsSelected = new List<string>();
            Delimiter = "|";
            StringPadding = 100;
            IsSuggestingDataTypes = false;
            IsCreatingTable = false;
            IsDroppingTable = false;
            IsTruncatingTable = false;
            DateFormat = StringAndText.FormatDate_ddMMyy; // Case sensitive (otherwise it will not work)
            DaysDifference = 0;
            DefaultFieldLength = ConfigVariables.Instance.Default_Field_Length;
            SetHeaderSpaceReplacement = " "; // " " Space by default
            SilentMode = false;
            SourceFileIsSourcedFromDial = false;
            IsReadingFromDataLake = false;
            Qualifier = "\"";
            IsSkippingError = false;
            TotalObjectsInSequentialPipe = 100 * 1000;
            CpuCountUsedToComputeParallalism = Environment.ProcessorCount;
            TryMatchFileNameInSourceFolderBasedOnRegex = false;
            ADLStreamBufferSize = 10 * 1024 * 1024;
            AzureTenant = ConfigurationManager.AppSettings.Get("DefaultTenant");
            AzureClientId = ConfigurationManager.AppSettings.Get("DefaultClientId");
            AzureSubscription = ConfigurationManager.AppSettings.Get("AzureSubscription");
            IsOrdinalColumnRanking = false;
            DbNullStringValue = null;
        }

        private string getSourcePath()
        {
            if (string.IsNullOrEmpty(_SourceFilePath))
            {
                return string.Empty;
            }
            if (TryMatchFileNameInSourceFolderBasedOnRegex)
            {
                PerformRegexMatching();
            }

            return _SourceFilePath;
        }
        private string[] GetDataTypes()
        {
            if (IsSuggestingDataTypes)
            {
                //warning: not supported with all combinations of options
                DataTypeSuggester suggest = new DataTypeSuggester(this);
                return suggest.SuggestDataType();
            }
            else
            {
                string[] dataTypes = new string[ColumnNamesSelection.Length];
                for (int i = 0; i < dataTypes.Length; i++)
                {
                    dataTypes[i] = $"NVARCHAR({DefaultFieldLength})";
                }
                return dataTypes;
            }
        }

        /// <summary>
        /// Get a list of column names using provided headers or enumerate names if headers are not present,
        /// Also wrap column names with brackets to avoid issues with reserved words or spaces on SQL Server.
        /// <para>Note: if field name is not unique then the column name will be added with random digits.</para>
        /// </summary>
        /// <returns>string[]</returns>
        private string[] GetColumnNames()
        {
            string[] firstRow;
            firstRow = ReadOrGenerateHeaders();

            if (!FirstLineContainsHeaders)
            {
                // Enumerate column names if headers are not present
                for (int i = 0; i < firstRow.Length; i++)
                {
                    var columnIndexNumber = (i + 1).ToString("00");
                    firstRow[i] = $"Column{SetHeaderSpaceReplacement}{columnIndexNumber}"; // For example: [Column_01]
                }
            }
            else
            {
                List<string> uniques = new List<string>();
                for (int i = 0; i < firstRow.Length; i++)
                {
                    // Maximum column name length in SQL server is 128
                    var maxColumnNameLength = 128;
                    var currentColumnName = StringAndText.VerifyLength(maxColumnNameLength, firstRow[i]);

                    // If the current column name already exist then add random digits to it
                    if (uniques.Exists(x => x.Equals(currentColumnName)))
                    {
                        var randomGeneratedDigits = StringAndText.GenerateRandomDigits(10); // "_1234567890"
                        if (currentColumnName.Length < maxColumnNameLength)
                        {
                            currentColumnName += $"{SetHeaderSpaceReplacement}{randomGeneratedDigits}";
                        }
                        else
                        {
                            var removeIndex = SetHeaderSpaceReplacement.Length - (randomGeneratedDigits.Length + 1);
                            currentColumnName += $"{SetHeaderSpaceReplacement.Remove(removeIndex)}{randomGeneratedDigits}";
                            LogService.Instance.Info($"Column name was trimmed, currentColumnName [{currentColumnName}] exceeded the maximum column name length in SQL server ({maxColumnNameLength})");
                        }
                    }

                    // Add current column name in order to check for other unique values
                    uniques.Add(currentColumnName);

                    // Also convert spaces in the field name itself
                    if (SetHeaderSpaceReplacement != " ") // " " space by default
                        currentColumnName = currentColumnName.Replace(" ", SetHeaderSpaceReplacement);

                    // Wrap column names with brackets to avoid issues with reserved words or spaces on SQL Server
                    firstRow[i] = $"{currentColumnName}";
                }
            }

            return firstRow;
        }

        private string[] ReadOrGenerateHeaders()
        {
            string[] firstRow;
            if (SourceIsExcel(SourceFilePath))
            {

                using (var stream = File.Open(SourceFilePath, FileMode.Open, FileAccess.Read))
                {
                    using (var reader = ExcelDataReader.ExcelReaderFactory.CreateReader(stream))
                    {
                        bool FoundSheet = false;
                        do
                        {
                            if (reader.Name.Equals(ExcelWorksheetName, StringComparison.InvariantCultureIgnoreCase))
                            {
                                FoundSheet = true;
                                break;
                            }
                        } while (reader.NextResult());
                        if (!FoundSheet)
                        {
                            throw new IOException($"Worksheet by name {ExcelWorksheetName} was not found");
                        }

                        reader.Read();
                        firstRow = new string[reader.FieldCount];
                        for (int i = 0; i < reader.FieldCount; i++)
                        {
                            firstRow[i] = reader.GetValue(i).ToString();
                        }
                    }
                }

            }
            else if (IsReadingFromDataLake)
            {
                string firstLine;
                AzureClient clientProvider = new AzureClient(this);
                var client = clientProvider.GetDataLakeClient(DataLakeAdress, PromptAzureLogin);
                using (StreamReader reader = new StreamReader(client.GetReadStream(SourceFilePath)))
                {
                    if (SourceFileIsSourcedFromDial) { reader.ReadLine(); }
                    firstLine = reader.ReadLine();

                    firstRow = StringAndText.SplitRow(firstLine, Delimiter.ToString(), "\"", false);
                }
            }
            else
            {
                string firstLine;
                using (StreamReader reader = new StreamReader(SourceFilePath))
                {
                    //skip an extra line for dial data
                    if (SourceFileIsSourcedFromDial) { reader.ReadLine(); }
                    firstLine = reader.ReadLine();

                    firstRow = StringAndText.SplitRow(firstLine, Delimiter.ToString(), "\"", false);
                }
            }

            return firstRow;
        }

        private string TryMatchFileName(AdlsClient client, string name)
        {
            // the regex part of the file should be enclosed in curly brackets {} we will extract that here
            string RegexPatternWithCurlyBraces = Regex.Match(name, @"{.+}").Value;
            string RegexPattern = RegexPatternWithCurlyBraces.Replace("{", "").Replace("}", "");
            //grab a list of files in the folder (and grab the folder while were at it)
            //this regex grabs the value of the full path up until (but not including) the first {.
            string folder = Regex.Match(name, @"[^{]+").Value;
            var files = client.EnumerateDirectory(folder);
            //we don't want to enumerate a collection that represents a remote resource more than we need to so i will store the entries in memory
            List<DirectoryEntry> list = files.ToList();
            //the following lambda will check if the regex is satisfied for any file and returns the first file for which is does so
            foreach (DirectoryEntry d in list)
            {
                if (Regex.IsMatch(d.Name, RegexPattern))
                {
                    return d.FullName;
                }
            }
            //if we reach here we have not found any match
            var msg = $"No file was found in folder {folder} which matches the regex {RegexPattern}";
            LogService.Instance.Error(msg);
            Console.WriteLine(msg);
            return null;

        }
        private string TryMatchFileName(string name)
        {
            // the regex part of the file should be enclosed in curly brackets {} we will extract that here
            string RegexPatternWithCurlyBraces = Regex.Match(name, @"{.+}").Value;
            string RegexPattern = RegexPatternWithCurlyBraces.Replace("{", "").Replace("}", "");
            //grab a list of files in the folder (and grab the folder while were at it)
            //this regex grabs the value of the full path up until (but not including) the first {.
            string folder = Regex.Match(name, @"[^{]+").Value;
            var files = Directory.GetFiles(folder);
            //we don't want to enumerate a collection that represents a remote resource more than we need to so i will store the entries in memory
            List<string> list = files.ToList();
            //the following lambda will check if the regex is satisfied for any file and returns the first file for which is does so
            foreach (string d in list)
            {
                if (Regex.IsMatch(d, RegexPattern))
                {
                    return d;
                }
            }
            //if we reach here we have not found any match
            var msg = $"No file was found in folder {folder} which matches the regex {RegexPattern}";
            LogService.Instance.Error(msg);
            Console.WriteLine(msg);
            return null;

        }
        private bool SourceIsExcel(string fileName)
        {
            string extension = StringAndText.SplitRow(fileName, ".", "/", false).Last();
            return extension.Contains("xls");
        }

        private void PerformRegexMatching()
        {
            if (IsReadingFromDataLake)
            {
                AzureClient clientProvider = new AzureClient(this);
                var client = clientProvider.GetDataLakeClient(DataLakeAdress, PromptAzureLogin);
                string match = TryMatchFileName(client, SourceFilePath);
                if (match == null)
                {
                    throw new FileNotFoundException($"File not found {SourceFilePath}");
                }
                else
                {
                    var msg = $"Filename updated from {SourceFilePath} to {match}";
                    SourceFilePath = match;
                    LogService.Instance.Info(msg);
                    Console.WriteLine(msg);
                    TryMatchFileNameInSourceFolderBasedOnRegex = false; //to prevent other parts of the framework from attempting this thingy.
                } 
            }
            else
            {
                string match = TryMatchFileName(SourceFilePath);
                if (match == null)
                {
                    throw new FileNotFoundException($"File not found {SourceFilePath}");
                }
                else
                {
                    var msg = $"Filename updated from {SourceFilePath} to {match}";
                    SourceFilePath = match;
                    LogService.Instance.Info(msg);
                    Console.WriteLine(msg);
                    TryMatchFileNameInSourceFolderBasedOnRegex = false; //to prevent other parts of the framework from attempting this thingy.
                }
            }
        }

        private string[] GetColumnNamesSelection()
        {
            if (_ColumnNamesSelection == null)
                return ColumnNames;
            else
                return _ColumnNamesSelection;
        }

        private void SetColumnNamesSelection(string[] value)
        {
            //verify that every column in the selection is also present in the source columns available
            if (value.All(columnName => ColumnNames.Any(name => name.Equals(columnName, StringComparison.InvariantCultureIgnoreCase))))
                _ColumnNamesSelection = SortSelection(value);
            else
                throw new ArgumentException("One or more of the selected columns are not present in the source dataset");
        }
        //because i write terrible spaghetti (srsly i leave this tuff in the cooker too long) it is important to ensure that the ordering of this selection lines up with the ordering
        //of the source columns (ordinal rankings are used in other places and are assumed to line up)
        private string[] SortSelection(string[] val)
        {
            string[] sortedArray = new string[val.Count()];
            int indx = 0;
            foreach (string name in ColumnNames)
            {
                if (val.Any(x => x.Equals(name, StringComparison.InvariantCultureIgnoreCase)))
                {
                    sortedArray[indx++] = name;
                }
            }
            return sortedArray;
        }
    }
}
