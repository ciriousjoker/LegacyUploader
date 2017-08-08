namespace LegacyManagerUploader
{
    /*
     * This structure contains all the data of one release
     */

    public class GameInfo
    {
        public int Version { get; set; }            // 
        public int PatchFor { get; set; }           // 
        public string VersionString { get; set; }   // 
        public long Size { get; set; }              // 
        public string Url { get; set; }              // 
        public bool RequiresPassword { get; set; }  // 

        // TODO: Add function to produce json
        public GameInfo()
        {
            this.Version = 0;
            this.PatchFor = -1;
            this.VersionString = "";
            this.Size = -1;
            this.Url = "";
            this.RequiresPassword = false;
        }
    }
}