using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LegacyManagerUploader
{
    // TODO: Remove this

    /*
     * This is the same structure used by both this software and
     * the corresponding client.
     */
    public class HashEntry
    {
        public string platform { get; set; }    // Optional: specify only certain platforms for a file; Currently only implemented on the clientside
        public string path { get; set; }        // Path to the file relative to the installation basepath with forward slashes. Example: /path/to/the/fol der/with-a/fi.le
        public string hash { get; set; }        // The Base64 representation of the file's MD5 hash
        public string url { get; set; }         // Direct download url. Example: https://drive.google.com/uc?export=download&id=file_id
        public int package { get; set; }        // Id of the package this file belongs to. Having package IDs and Urls allows for mirror urls, but for now, this feature isn't important
        public long size { get; set; }          // Filesize in Bytes. Go figure.
    }
}
