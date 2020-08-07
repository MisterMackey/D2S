using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using D2S.Library.Transformers;
using System.Collections.Concurrent;
using System.IO;
using D2S.Library.Helpers;
using D2S.Library.Services;

namespace D2S.Library.Utilities
{
    public class DataTypeSuggester
    {
        private PipelineContext Context;
        private ConcurrentBag<ConcurrentStack<string>> ColumnCollection;

        public DataTypeSuggester(PipelineContext c)
        {
            Context = c;
            this.ColumnCollection = new ConcurrentBag<ConcurrentStack<string>>();
        }

        public string[] SuggestDataType()
        {
            Init(Context.SourceFilePath); //initialize variables

            //put the first x lines into variables, order by column
            using (StreamReader reader = new StreamReader(Context.SourceFilePath))
            {
                var delimiterAsString = Context.Delimiter.ToString();
                if (Context.FirstLineContainsHeaders) { reader.ReadLine(); }//skip header line
                if (Context.SourceFileIsSourcedFromDial) { reader.ReadLine(); } //skip extra line for DIAL data
                string line;
                string[] splitLine;
                for (int x = 0; x < ConfigVariables.Instance.Type_Suggestion_Sample_Lines_To_Scan; x++)
                {
                    if ((line = reader.ReadLine()) != null)
                    {
                        splitLine = StringAndText.SplitRow(line, delimiterAsString, "\"", false);
                        for (int i = 0; i < splitLine.Count(); i++)
                        {
                            ColumnCollection.ElementAt(i).Push(splitLine[i]);
                        }
                    }
                }
            }

            //suggest datatypes and push these on the stacks

            DoSuggestType(Context.StringPadding);
            List<string> types = new List<string>();
            foreach (ConcurrentStack<string> type in ColumnCollection)
            {
                string HURR;
                if (type.TryPop(out HURR))
                {
                    types.Add(HURR);
                }
            }
            return types.ToArray();
        }

        private void Init(string file)
        {
            StreamReader reader = new StreamReader(file);
            var firstLine = reader.ReadLine();
            int columncount = StringAndText.SplitRow(firstLine, Context.Delimiter.ToString(), "\"", false).Count();

            for (int i = 0; i < columncount; i++)
            {
                ColumnCollection.Add(new ConcurrentStack<string>());
            }
        }

        private void DoSuggestType(double stringpadding)
        {
            Parallel.ForEach<ConcurrentStack<string>>(ColumnCollection, column =>
            {
                //dequeu all items, perform analysis, final step: enqueu the suggested datatype (preserves ordering in the bag i hope)

                int refint;
                double refdouble;
                bool refbool;
                bool CouldBeInteger = column.All<string>(x => int.TryParse(x, out refint));
                bool CouldBeDouble = column.All(x => double.TryParse(x, out refdouble));
                bool CouldBeBoolean = column.All(x => bool.TryParse(x, out refbool));
                bool CouldBeChar = column.All(x => x.Count() == 1);


                if (CouldBeBoolean) { column.Push("BIT"); }
                else if (CouldBeInteger) { column.Push("INT"); }
                else if (CouldBeDouble) { column.Push("DEC(38,8)"); }
                else if (CouldBeChar) { column.Push("CHAR"); }
                else
                {
                    int length = 0;
                    foreach (string s in column)
                    {
                        if (s.Length > length) { length = s.Length; }
                    }
                    length = (int)(length * (stringpadding / 100));//add padding and round to int
                    if (length == 0) { length = Context.DefaultFieldLength; } //incase of empty column just give it the DefaultFieldLength
                    column.Push("NVARCHAR(" + length + ")");
                }
            });
        }


    }
}
