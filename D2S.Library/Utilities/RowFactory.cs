using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using D2S.Library.Services;

namespace D2S.Library.Utilities
{
    public class RowFactory
    {
        //todo : add dictionary and config options etc and way to set it
        private string[] ColumnNames;
        private int ColumnCount;

        public RowFactory(string[] columns)
        {
            ColumnNames = columns;
            ColumnCount = columns.Count();
        }

        public Row CreateRow(ICollection<object> record)
        {
            if (record.Count != ColumnCount)
            {                             
                var errorMessage = $"RowFactory encountered a mismatch between the expected fields per row and the actual fields in the current row. This could mean that the input file is corrupt or has linebreaks inside of the record. In the latter case, consider using the IgnoreLineBreak switch";
                Exception ex = new Exception(errorMessage);
                LogService.Instance.Error(ex);
                throw ex;
            }
            Row NewRow = new Row(ColumnCount);
            for (int i = 0; i < ColumnCount; i++)
            {
                NewRow[ColumnNames[i]] = new Tuple<object, Type>(
                    record.ElementAt(i), 
                    record.ElementAt(i).GetType());
            }
            return NewRow;
        }
    }
}
