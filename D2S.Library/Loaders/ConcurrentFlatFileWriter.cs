using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Threading;
using D2S.Library.Utilities;

namespace D2S.Library.Loaders
{
    public class ConcurrentFlatFileWriter
    {
        #region private fields
        private ConcurrentBag<string> m_CurrentCollection;
        private ConcurrentBag<string> m_BackupCollection;
        private int m_CurrentCount;
        private int m_MaxCount;
        private object m_SyncRoot;
        private Task writeTask;
        private StreamWriter m_Writer;
        private PipelineContext m_Context;
        #endregion
        #region constructor
        public ConcurrentFlatFileWriter(PipelineContext context)
        {
            m_Context = context;
            m_CurrentCount = 0;
            m_MaxCount = m_Context.TotalObjectsInSequentialPipe;
            m_SyncRoot = new object();
            m_Writer = new StreamWriter(m_Context.DestinationFilePath, !m_Context.IsTruncatingTable);
            m_CurrentCollection = new ConcurrentBag<string>();
            m_BackupCollection = new ConcurrentBag<string>();
        }
        #endregion
        #region public methods
        public void WriteLine(string line)
        {
            lock (m_SyncRoot)
            {
                m_CurrentCollection.Add(line); 
                if (++m_CurrentCount >= m_MaxCount)
                {
                    m_CurrentCount = 0;
                    TriggerWriteAndSwitchCollections();
                }
            }
        }

        public void Close()
        {
            Flush();
            m_Writer.Close();
            m_Writer.Dispose();
        }

        #endregion
        #region private methods
        private void TriggerWriteAndSwitchCollections()
        {
            //make sure the backupCollection is available, if we must wait on this we will effectivly block furhter writes as we are holding the lock at this point.
            if (writeTask != null)
            {
                writeTask.Wait(); 
            }
            //swap collections.
            var tempRef = m_CurrentCollection;
            m_CurrentCollection = m_BackupCollection;
            m_BackupCollection = tempRef;
            //collection references are now swapped and we may resume calling Write() after triggering an async job to write out the old collection.
            writeTask = Task.Factory.StartNew(
                () => DoWriteCollection());
        }
        private void DoWriteCollection()
        {
            foreach (string line in m_BackupCollection)
            {
                m_Writer.WriteLine(line);                
            }
            m_BackupCollection = new ConcurrentBag<string>();
        }
        private void Flush()
        {
            bool lockTaken = false;
            //there might be other threads waiting to write some lines to the currentcollection, heck these calls might even result in a write trigger so we gotta be mindful. 
            //for sure don't bother flushing if a write is in progress
            if (writeTask != null)
            {
                writeTask.Wait(); 
            }

            //try acquire lock

            while (!lockTaken)
            {
                //wait a half second in any case, should be plenty
                Thread.Sleep(500);
                Monitor.TryEnter(m_SyncRoot, 1, ref lockTaken);
            }

            //k so we now have this lock, lets trigger the last write (we can do this syncronously)
            //dont forget to switch the collection
            m_BackupCollection = m_CurrentCollection;
            DoWriteCollection();
            Monitor.Exit(m_SyncRoot);


        }
        #endregion
    }
}
