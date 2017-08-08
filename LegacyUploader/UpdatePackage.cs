using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LegacyManagerUploader
{
    class UpdatePackage
    {
        public static double CHUNK_SIZE = 100 * 1024 * 1024; // Build 100mb chunks (on average) when packaging the update
        
        // Hold all the indexed files that belong to this package
        Dictionary<string, HashEntry> IndexedFiles = new Dictionary<string, HashEntry>();

        public long currentSize { get; set; }

        // Add a new file with a combination of the hash and the fullpath as the index to avoid duplicates; Note that the filepath is probably enough, but I'll keep it like that for now)
        public void addFile(HashEntry newEntry)
        {
            IndexedFiles.Add(newEntry.hash + newEntry.path, newEntry);
            currentSize += newEntry.size;
        }

        public Dictionary<string, HashEntry> getList()
        {
            return IndexedFiles;
        }
    }
}
