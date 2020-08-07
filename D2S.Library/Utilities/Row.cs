using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace D2S.Library.Utilities
{
    /// <summary>
    /// represents a virtual row. Row values are stored in an internal dictionary. Keys are set to column names, values are tuples of object and type
    /// </summary>
    public class Row : IDictionary<string, Tuple<Object, Type>>, ICloneable
    {
        private IDictionary<string, Tuple<Object, Type>> Items;

        /// <summary>
        /// default constructor
        /// </summary>
        public Row()
        {
            Items = new Dictionary<string, Tuple<Object, Type>>();
        }
        /// <summary>
        /// Initializes the internal dictionary with an initial size. This size should be at least equal to the number of fields in the virtual row.
        /// </summary>
        /// <param name="FieldCount">amount of fields in the row</param>
        public Row(int FieldCount)
        {
            Items = new Dictionary<string, Tuple<Object, Type>>(FieldCount);
        }

        #region implementingInterface
        public Tuple<object, Type> this[string key] { get => Items[key]; set => Items[key] = value; }

        public ICollection<string> Keys => Items.Keys;

        public ICollection<Tuple<object, Type>> Values => Items.Values;

        public int Count => Items.Count;

        public bool IsReadOnly => Items.IsReadOnly;

        public void Add(string key, Tuple<object, Type> value)
        {
            Items.Add(key, value);
        }

        public void Add(KeyValuePair<string, Tuple<object, Type>> item)
        {
            Items.Add(item);
        }

        public void Clear()
        {
            Items.Clear();
        }

        public object Clone()
        {
            Row newRow = new Row(this.Count);
            newRow.Items = this.Items.ToDictionary(x => x.Key, y => y.Value);
            return newRow;
        }

        public bool Contains(KeyValuePair<string, Tuple<object, Type>> item)
        {
            return Items.Contains(item);
        }

        public bool ContainsKey(string key)
        {
            return Items.ContainsKey(key);
        }

        public void CopyTo(KeyValuePair<string, Tuple<object, Type>>[] array, int arrayIndex)
        {
            Items.CopyTo(array, arrayIndex);
        }

        public IEnumerator<KeyValuePair<string, Tuple<object, Type>>> GetEnumerator()
        {
            return Items.GetEnumerator();
        }

        public bool Remove(string key)
        {
            return Items.Remove(key);
        }

        public bool Remove(KeyValuePair<string, Tuple<object, Type>> item)
        {
            return Items.Remove(item);
        }

        public bool TryGetValue(string key, out Tuple<object, Type> value)
        {
            return Items.TryGetValue(key, out value);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return Items.GetEnumerator();
        }
        #endregion
    }
}
