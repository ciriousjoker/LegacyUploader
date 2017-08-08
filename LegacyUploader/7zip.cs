using com.ciriousjoker.lib;
using SevenZip;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using static LegacyManagerUploader.MainWindow;

//using com.ciriousjoker.lib;

namespace LegacyManagerUploader
{
    class _7zip
    {
        // Singleton for the processes created by this class
        // Right now there's only one max, but that might change if I'm bored one day
        private static List<Process> Processes = new List<Process>();

        public static void CreateZip(string ChosenProjectFolder, string OutputArchive, BackgroundWorker sendingworker)
        {
            // Load the .dll depending on the process architecture
            if (Environment.Is64BitProcess)
            {
                SevenZipExtractor.SetLibraryPath(Path.Combine("tools", "7z64.dll"));
            } else
            {
                SevenZipExtractor.SetLibraryPath(Path.Combine("tools", "7z.dll"));
            }
            
            SevenZipCompressor compressor = new SevenZipCompressor();

            // Configure the compressor
            compressor.ArchiveFormat = OutArchiveFormat.SevenZip;
            compressor.CompressionMode = CompressionMode.Create;
            compressor.TempFolderPath = Path.GetDirectoryName(OutputArchive);
            compressor.CompressionMethod = CompressionMethod.Lzma2;

            // Probably unneccessary (EDIT: Really??)
            //compressor.PreserveDirectoryRoot = true;
            compressor.DirectoryStructure = true;
            compressor.IncludeEmptyDirectories = true;

            // Add event callbacks
            compressor.Compressing += (sender, e) => Compressor_Compressing(sender, e, sendingworker);
            compressor.FileCompressionStarted += (sender, e) => Compressor_FileCompressionStarted(sender, e, sendingworker);

            // Compress everything
            compressor.CompressDirectory(ChosenProjectFolder, OutputArchive, true);
        }

        private static void Compressor_FileCompressionStarted(object sender, FileNameEventArgs e, BackgroundWorker sendingworker)
        {
            // Triggered for every new file that's added to the archive
            // This cancellation only works within FileCompressionStarted, because the library is shit
            if (sendingworker.CancellationPending)
            {
                e.Cancel = true;
            }
        }

        private static void Compressor_Compressing(object sender, ProgressEventArgs e, BackgroundWorker sendingworker)
        {
            // cancelling wont work in the Compressing event, because the library is shit. However, return; is neccessary so that at least the output stops
            if (sendingworker.CancellationPending)
            {
                e.Cancel = true;
                return;
            }
            int percent = e.PercentDone;
            sendingworker.ReportProgress(0, new StatusHolder() { txt = "Compressing: ", progress = percent });
        }

        // TODO: Remove this
        public static void CreateZipLegacy(List<string> Files, string targetName, BackgroundWorker sendingworker)
        {
            if (File.Exists(targetName))
            {
                File.Delete(targetName);
            }
            

            string file_list = "";
            foreach (string file in Files)
            {
                file_list += " \"" + file.Replace(Path.DirectorySeparatorChar, '/') + "\"";
            }
            string sourceName = Files[0];
            ProcessStartInfo p = new ProcessStartInfo();
            p.FileName = Path.Combine("tools", "7zr.exe");
            if(!File.Exists(p.FileName))
            {
                sendingworker.ReportProgress(0, new StatusHolder() { txt = "7zr.exe not found. Please download the software again. If the problem persists, please contact the software developer.", progress = -1 });
                sendingworker.CancelAsync();
                return;
            }
            p.Arguments = "a -y \"" + targetName.Replace(Path.DirectorySeparatorChar, '/') + "\" " + file_list;
            p.WindowStyle = ProcessWindowStyle.Hidden;
            p.CreateNoWindow = true;
            p.RedirectStandardOutput = true;
            p.RedirectStandardError = true;
            p.UseShellExecute = false;

            Processes = new List<Process>();
            Processes.Insert(0, Process.Start(p));
            Processes[0].WaitForExit();


            while (!Processes[0].StandardOutput.EndOfStream)
            {
                // Clean up if the process was canceled
                if (sendingworker.CancellationPending)
                {
                    if(!Processes[0].HasExited)
                    {
                        Processes[0].Kill();
                    }
                    File.AppendAllText(AppLogFile, "Extraction was cancelled ...");
                    return;
                }

                // Check if the line starts with "Creating archive: " and display it as "Created archive: "
                string line = Processes[0].StandardOutput.ReadLine();

                Regex rxStatusLine = new Regex("\\bCreating archive: ");
                Match matchStatusLine = rxStatusLine.Match(line);

                if (matchStatusLine.Success)
                {
                    sendingworker.ReportProgress(0, new StatusHolder() { txt = line.Replace("Creating", "Created"), progress = -1 });
                }

                File.AppendAllText(Path.Combine(Path.GetDirectoryName(targetName), AppLogFile), line + "\n");
            }
        }

        // Kill all instances of 7zip created by this class
        public static void killProcesses()
        {
            foreach(Process proc in Processes)
            {
                if(!proc.HasExited)
                {
                    proc.Kill();
                }
            }
        }
    }
}