using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace D2S.Library.Utilities
{
    public class BoundedConcurrentQueu<T> : IProducerConsumerCollection<T>
    {
        private readonly IProducerConsumerCollection<T> m_Collection;
        private readonly int m_Capacity;
        private readonly SemaphoreSlim m_Semaphore;
        public int Capacity { get { return m_Capacity; } }
        public BoundedConcurrentQueu() : this(int.MaxValue)
        {
            
        }

        public BoundedConcurrentQueu(int capacity)
        {
            m_Collection = new ConcurrentQueue<T>();
            m_Capacity = capacity;
            m_Semaphore = new SemaphoreSlim(capacity, capacity);
        }

        

        public int Count => m_Collection.Count;

        public object SyncRoot => m_Collection.SyncRoot;

        public bool IsSynchronized => m_Collection.IsSynchronized;

        public void CopyTo(T[] array, int index)
        {
            m_Collection.CopyTo(array, index);
        }

        public void CopyTo(Array array, int index)
        {
            m_Collection.CopyTo(array, index);
        }

        public IEnumerator<T> GetEnumerator()
        {
            return m_Collection.GetEnumerator();
        }

        public T[] ToArray()
        {
            return m_Collection.ToArray();
        }

        public bool TryAdd(T item)
        {
            m_Semaphore.Wait();
            return m_Collection.TryAdd(item);
        }

        public bool TryTake(out T item)
        {
            if (m_Collection.TryTake(out item))
            {
                m_Semaphore.Release();
                return true;
            }
            else return false;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return m_Collection.GetEnumerator();
        }
    }
}
