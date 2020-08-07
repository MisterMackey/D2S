namespace D2S.Library.Helpers
{
    using D2S.Library.Services;
    using System;
    using System.Collections.Generic;
    using System.Text;

    /// <summary>
    /// String and text related functions 
    /// </summary>
    public static class StringAndText
    {
        #region Members

        // DateTime formatting: case sensitive (otherwise it will not work)
        // http://www.csharp-examples.net/string-format-datetime
        // http://www.dotnetspider.com/resources/266-Formatting-Date-Time-using-e-DateTime-object.aspx

        public const string FormatDate_ddMMyy = @"ddMMyy";
        public const string FormatDate_ddMMyyyy = @"ddMMyyyy";

        public const string FormatDate_dd__MM__yy = @"dd-MM-yy";
        public const string FormatDate_dd__MM__yyyy = @"dd-MM-yyyy";

        public const string FormatDate_yy__MM__dd = @"yy-MM-dd";
        public const string FormatDate_yyyy__MM__dd = @"yyyy-MM-dd";

        public const string FormatDate_MM_dd_yy = @"MM/dd/yy";
        public const string FormatDate_MM_dd_yyyy = @"MM/dd/yyyy";

        public const string FormatDate_MM__dd__yyyy = @"MM-dd-yyyy";
        public const string FormatDateFullMonthName = @"MMMM dd, yyyy";

        public const string FormatDate_yyyyMMdd = @"yyyyMMdd";
        public const string FormatDate_yyyy_MM__dd = @"yyyy-MM-dd";
        public const string FormatDate_yyyy__MM_dd = @"yyyy-MM/dd";

        public const string FormatTime = @"hh:mm:ss";
        public const string FormatTimeNoSeconds = @"hh:mm";

        public const string LineBreakSystem = "\r\n";
        public const string LineBreakHtml = "<br />";
        public const string LineItemPrefix = "[+]";

        /// <summary>
        /// Space split character
        /// </summary>
        public static char SplitSeperatorSpace = ' ';

        /// <summary>
        /// Comma split character
        /// </summary>
        public static char SplitSeperatorComma = ',';

        /// <summary>
        /// Pipe split character
        /// </summary>
        public static char SplitSeperatorPipe = '|';

        /// <summary>
        /// Semicolon split character
        /// </summary>
        public static char SplitSeperatorSemicolon = ';';

        /// <summary>
        /// Split word breaker characters (allows '-', '\'' inside words)
        /// </summary>
        public static char[] SplitWordBreakers = 
            { ' ', '.', ',', ';', ':', '!', '?', '|', '\"', '\\', '/', '\r', '\t', '\n' };

        #endregion Members

        #region Methods

        /// <summary>
        /// Returns a string of random digits (up to 10)
        /// </summary>
        /// <returns></returns>
        public static string GenerateRandomDigits(int maxCount)
        {
            // For generating random numbers.
            Random random = new Random((int)DateTime.Now.Ticks);
            string s = "";

            if (maxCount < 0 || maxCount > 10)
            {
                var outputMessage = $"GenerateRandomCode() : maxCount value ({maxCount}) is out of range.";
                LogService.Instance.Error(outputMessage);
                throw new ArgumentOutOfRangeException(outputMessage);
            }

            for (int i = 0; i < maxCount; i++)
            {
                s = String.Concat(s, random.Next(10).ToString());
            }

            return s;
        }

        /// <summary>
        /// Split strings while preserving possible delimiter characters inside the strings
        /// (this function performace is better than using regular expressions)
        /// <para>
        /// https://stackoverflow.com/questions/3776458/split-a-comma-separated-string-with-both-quoted-and-unquoted-strings
        /// </para>
        /// </summary>
        /// <param name="record"></param>
        /// <param name="delimiter"></param>
        /// <param name="qualifier"></param>
        /// <param name="trimData"></param>
        /// <returns></returns>
        public static string[] SplitRow(string record, string delimiter, string qualifier, bool trimData)
        {
            //call the version that isn't checking for qualifiers (save the anima- i mean CPU cycles!!)
            if (qualifier == null)
            {
                return SplitRow(record, delimiter, trimData);
            }
            // In-Line for example, but I implemented as string extender in production code
            Func<string, int, int> IndexOfNextNonWhiteSpaceChar = delegate (string source, int startIndex)
            {
                if (startIndex >= 0)
                {
                    if (source != null)
                    {
                        for (int i = startIndex; i < source.Length; i++)
                        {
                            if (!char.IsWhiteSpace(source[i]))
                            {
                                return i;
                            }
                        }
                    }
                }

                return -1;
            };

            var results = new List<string>();
            var result = new StringBuilder();
            var inQualifier = false;
            var inField = false;

            // We add new columns at the delimiter, so append one for the parser.
            var row = $"{record}{delimiter}";

            for (var idx = 0; idx < row.Length; idx++)
            {
                // A delimiter character...
                if (row[idx] == delimiter[0])
                {
                    // Are we inside qualifier? If not and we use a single char delimiter we are done with this field. otherwise peek ahead and check if the full delimiter is encountered.
                    if (!inQualifier)
                    {
                        bool delimiterMatch = true;
                        for (int i = 1; i < delimiter.Length; i++) //this loop is not entered for single char delimiters
                        {
                            if ((idx + i) < row.Length && row[idx + i] != delimiter[i]) //the row length check is there to prevent indexoutofrange exceptions when dealing with certain delimiters like ||
                            {
                                //set success to false if we don't encounter the full multichar delimiter)
                                delimiterMatch = false;
                            }
                        }
                        
                        if (delimiterMatch)
                        {
                            //increment the idx variable so we don't read characters that belong to the delimiter into the result
                            idx += (delimiter.Length - 1);
                            results.Add(trimData ? result.ToString().Trim() : result.ToString());
                            result.Clear();
                            inField = false; 
                        }
                        else
                        {
                            result.Append(row[idx]);
                        }
                    }
                    else
                    {
                        result.Append(row[idx]);
                    }
                }

                // NOT a delimiter character...
                else
                {
                    // ...Not a space character
                    if (row[idx] != ' ')
                    {
                        // A qualifier character...
                        if (row[idx] == qualifier[0])
                        {
                            // Qualifier is closing qualifier...
                            if (inQualifier && row[IndexOfNextNonWhiteSpaceChar(row, idx + 1)] == delimiter[0])
                            {
                                inQualifier = false;
                                continue;
                            }

                            else
                            {
                                // ...Qualifier is opening qualifier
                                if (!inQualifier)
                                {
                                    inQualifier = true;
                                }

                                // ...It's a qualifier inside a qualifier.
                                else
                                {
                                    inField = true;
                                    result.Append(row[idx]);
                                }
                            }
                        }

                        // Not a qualifier character...
                        else
                        {
                            result.Append(row[idx]);
                            inField = true;
                        }
                    }

                    // ...A space character
                    else
                    {
                        if (inQualifier || inField)
                        {
                            result.Append(row[idx]);
                        }
                    }
                }
            }

            return results.ToArray(); // .ToArray<string>();
        }

        /// <summary>
        /// same as the other splitrow but it doesn't have the qualifier checking bit. Called from the other one when quali is set to null in the args.
        /// </summary>
        /// <param name="record"></param>
        /// <param name="delimiter"></param>
        /// <param name="trimData"></param>
        /// <returns></returns>
        private static string[] SplitRow(string record, string delimiter, bool trimData)
        { 

            var results = new List<string>();
            var result = new StringBuilder();
            var inField = false;

            // We add new columns at the delimiter, so append one for the parser.
            var row = $"{record}{delimiter}";

            for (var idx = 0; idx < row.Length; idx++)
            {
                // A delimiter character...
                if (row[idx] == delimiter[0])
                {
                    bool delimiterMatch = true;
                    for (int i = 1; i < delimiter.Length; i++) //this loop is not entered for single char delimiters
                    {
                        if ((idx + i) < row.Length && row[idx + i] != delimiter[i]) //the row length check is there to prevent indexoutofrange exceptions when dealing with certain delimiters like ||
                        {
                            //set success to false if we don't encounter the full multichar delimiter)
                            delimiterMatch = false;
                        }
                    }

                    if (delimiterMatch)
                    {
                        //increment the idx variable so we don't read characters that belong to the delimiter into the result
                        idx += (delimiter.Length - 1);
                        results.Add(trimData ? result.ToString().Trim() : result.ToString());
                        result.Clear();
                        inField = false;
                    }
                    else
                    {
                        result.Append(row[idx]);
                    }
                }

                // NOT a delimiter character...
                else
                {
                    // ...Not a space character
                    if (row[idx] != ' ')
                    {
                        result.Append(row[idx]);
                        inField = true;                        
                    }

                    // ...A space character
                    else
                    {
                        if (inField)
                        {
                            result.Append(row[idx]);
                        }
                    }
                }
            }

            return results.ToArray(); 

        }

        /// <summary>
        /// Verify that a string does not have more than the maximum allowed length
        /// (if it does then resize the string to the maximum allowed)
        /// </summary>
        /// <param name="maxLengthAllowed"></param>
        /// <param name="str"></param>
        /// <returns>string</returns>
        public static string VerifyLength(int maxLengthAllowed, string str)
        {
            string verifiedStr = str;

            if (string.IsNullOrEmpty(str) || maxLengthAllowed < 1)
            {
                return (str);
            }

            if (str.Length > maxLengthAllowed)
            {
                verifiedStr = str.Substring(0, maxLengthAllowed);
            }

            return (verifiedStr);
        }

        #endregion Methods
    }
}