using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Xml;
using System.IO;
using System.Threading.Tasks;
using FileBackupService;
using FileBackupService.Properties;

namespace FileBackupService
{
    public partial class BackupService : ServiceBase
    {
        public struct FileCopySpec
        {
            public string fromLoc;
            public string toLoc;
        }

        // List to hold all the FileSystemWatcher objects.
        List<FileSystemWatcherExt> dirWatcherList = new List<FileSystemWatcherExt>();
        Queue<FileCopySpec> CopyRequests = new Queue<FileCopySpec>();

        public BackupService()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {           
            LoadDirectories();            
        }

        private void WriteToLog(string[] message)
        {
            StreamWriter swLog = new StreamWriter(Settings.Default.AppLogFile, true);
            // Update log
            try
            { 
                foreach(string line in message)
                {
                    swLog.WriteLine(line);
                }

                swLog.WriteLine("");
                swLog.Flush();
                swLog.Close();
            }
            catch (Exception ex)
            {
                //throw ex;
            }
        }

        private void LoadDirectories()
        {
            // Load all the directories from the CSV file.
            StreamReader dirs = new StreamReader("Directories.csv");
            FileSystemWatcherExt fswNew;
            string currentLine, dirSource = "", dirDest = "";
            bool dirActive = false, validLine = false, dirIncludeSubs = false;
            string[] lineParse;
            try
            {
                while (!dirs.EndOfStream)
                {
                    // Read the next line.
                    currentLine = dirs.ReadLine();
                    validLine = false;

                    // Make sure the line starts with #
                    if (currentLine.StartsWith("#"))
                    {
                        // Delimit by comma and make sure there are four fields.
                        lineParse = currentLine.Remove(0, 1).Split(',');
                        if(lineParse.Length == 4)
                        {
                            // If the last field evaluates to True ...
                            dirActive = (lineParse[3].Trim() != "0");

                            if (dirActive)
                            { 
                                // Grab the source and destination directories.
                                validLine = true;
                                dirSource = lineParse[0].Trim().Trim('"');
                                dirDest = lineParse[1].Trim().Trim('"');
                                dirIncludeSubs = (lineParse[2].Trim() != "0");
                            }
                        }
                    }

                    // If the line was valid, create a new FileWatcher and
                    // add it to the list.
                    if (validLine)
                    {
                        fswNew = CreateFileWatcher(dirSource, dirIncludeSubs, dirDest);

                        if (fswNew != null)
                        {
                            fswNew.EnableRaisingEvents = true;
                            fswNew.Changed += fswModel_Changed;
                            fswNew.Created += fswModel_Created;
                            fswNew.Deleted += fswModel_Deleted;
                            fswNew.Renamed += fswModel_Renamed;
                            dirWatcherList.Add(fswNew);
                        }                   
                    }
                }
            }
            catch (Exception ex)
            {
                WriteToLog(new string[] { DateTime.Now.ToString(), ex.Message });
            }
        }


        private FileSystemWatcherExt CreateFileWatcher(string FileSource, bool IncludeSubdirs, string Destination)
        {
            // Create and return a new file system watcher.
            FileSystemWatcherExt fswReturn = new FileSystemWatcherExt(Destination);
            string filePath = "";
            int charPlace;

            try
            {
                // If the string that was passed is an actual directory, use it.
                if (Directory.Exists(FileSource))
                {
                    filePath = FileSource;
                }
                else
                {
                    // Otherwise, parse the string for the directory and the pattern.
                    charPlace = FileSource.LastIndexOf(@"\");
                    filePath = FileSource.Substring(0, charPlace);

                    // Test again ...
                    if (Directory.Exists(filePath))
                    {
                        fswReturn.Path = filePath;
                        fswReturn.Filter = FileSource.Substring(charPlace + 1);
                    }
                }

                // Instruct the file watcher to include subdirectories.
                fswReturn.IncludeSubdirectories = IncludeSubdirs;

            }
            catch (Exception ex)
            {
                WriteToLog(new string[] { DateTime.Now.ToString(),
                    "Error creating FileWatcher on " + FileSource + ".",  ex.Message});
            }

            // If the path was valid, return a file watcher ...
            if(fswReturn.Path.Length > 0)
            {
                return fswReturn;
            }
            else
            {
                // Otherwise, return null.
                return null;
            }
        }

        protected override void OnStop()
        {

        }

        internal void TestStartandStop(string[] args)
        {
            // If started from Visual studio, run through the events.
            this.OnStart(args);

            //Let the timer event play.
            while (backupTimer.Enabled)
            {
                Console.ReadLine();
            }

            this.OnStop();
        }

        private FileObject GetAffectedFile(string Location)
        {
            FileInfo newFileInfo = new FileInfo(Location);
            string fileExtension = newFileInfo.Extension;
            FileObject returnFile = null;

            try
            {
                // Return the correct type by extension
                switch (fileExtension.ToUpper())
                {
                    case "ZIP":
                        returnFile = new ZIPArchive(Location);
                        break;
                    case "MP3":
                        returnFile = new MP3Audio(Location);
                        break;
                    default:
                        returnFile = new FileObject(Location);
                        break;
                }
            }
            catch (Exception ex)
            {
                WriteToLog(new string[] { DateTime.Now.ToString(),
                    "Error classifying file: " + Location + ".",  ex.Message});
            }

            return returnFile;
        }

        private void CopyChangedFile(FileSystemWatcherExt FileWatcher, string SourceFilePath)
        {
            string fullDestPath = "";
            string fileWatcherName = FileWatcher.Path.Replace(@"\","_").Replace(":", "_");
            FileCopySpec fcsCopy = new FileCopySpec(); 

            bool allowCopy = true;

            try
            {
                // Get the destination path.
                fullDestPath = SourceFilePath.Replace(FileWatcher.Path, FileWatcher.DestinationDirectory + 
                    "\\" + fileWatcherName).Replace("\\\\", "\\");

                // If the destination file has been accessed in the last five seconds,
                // Cancel the copy.
                if (FileWatcher.LastFileAccessed == fullDestPath)
                    allowCopy = (FileWatcher.LastOperationTime < DateTime.Now.AddSeconds(-5));

                // Otherwise, go ahead and copy it.
                if (allowCopy)
                {
                    // If it doesn't exist already, create it.
                    if (!Directory.Exists(Directory.GetParent(fullDestPath).ToString()))
                    {
                        Directory.CreateDirectory(Directory.GetParent(fullDestPath).ToString());
                    }

                    // If it's a directory, create the corresponding directory.
                    // Otherwise, just copy the file.
                    if (Directory.Exists(SourceFilePath))
                    {
                        Directory.CreateDirectory(fullDestPath);
                    }
                    else
                    {
                        // Add it to the copy queue
                        fcsCopy.fromLoc = SourceFilePath;
                        fcsCopy.toLoc = fullDestPath;
                        CopyRequests.Enqueue(fcsCopy);
                    }

                    // Update the filewatcher.
                    FileWatcher.LastFileAccessed = fullDestPath;
                    FileWatcher.LastOperationTime = DateTime.Now;
                }

            }
            catch (Exception ex)
            {
                WriteToLog(new string[] { DateTime.Now.ToString(),
                    "Error copying file from " + SourceFilePath + " to " + fullDestPath + ".",  ex.Message});
            }
        }

        private void RenameBackupFile(FileSystemWatcherExt FileWatcher, string OldFile, string NewFile)
        {
            string origPath = "", newPath = "";
            string fileWatcherName = FileWatcher.Path.Replace(@"\", "_").Replace(":", "_");

            try
            {
                // Get the full path of the original backup file.
                origPath = OldFile.Replace(FileWatcher.Path, FileWatcher.DestinationDirectory +
                    "\\" + fileWatcherName).Replace("\\\\", "\\");

                newPath = NewFile.Replace(FileWatcher.Path, FileWatcher.DestinationDirectory +
                    "\\" + fileWatcherName).Replace("\\\\", "\\");

                // If it's a directory, rename it.
                if (Directory.Exists(origPath))
                {
                    Directory.Move(origPath, newPath);
                }
                else
                {
                    // If it's a file, rename it.
                    File.Move(origPath, newPath);
                }

            }
            catch (Exception ex)
            {
                WriteToLog(new string[] { DateTime.Now.ToString(),
                    "Error copying file from " + origPath + " to " + newPath + ".",  ex.Message});
            }
        }

        private void DeleteFile(FileSystemWatcherExt FileWatcher, string SourceFilePath)
        {
            string fullDestPath;
            string fileWatcherName = FileWatcher.Path.Replace(@"\", "_").Replace(":", "_");

            try
            {
                // Get the destination path.
                fullDestPath = SourceFilePath.Replace(FileWatcher.Path, FileWatcher.DestinationDirectory +
                    "\\" + fileWatcherName).Replace("\\\\", "\\");

                // If the file or directory exists, delete it.
                if (File.Exists(fullDestPath))
                {
                    // Delete the file.
                    File.Delete(fullDestPath);
                }
                else if (Directory.Exists(fullDestPath))
                {
                    // Delete the directory.
                    Directory.Delete(fullDestPath, true);
                }

            }
            catch (Exception ex)
            {
                WriteToLog(new string[] { DateTime.Now.ToString(),
                    "Error deleting file from " + SourceFilePath + ".",  ex.Message});
            }
        }

        private void fswModel_Changed(object sender, FileSystemEventArgs e)
        {
            // Get the current file system watcher.
            FileSystemWatcherExt fswExt = (FileSystemWatcherExt)sender;
            
            // Copy the file
            CopyChangedFile(fswExt, e.FullPath);
        }

        private void fswModel_Created(object sender, FileSystemEventArgs e)
        {
            // Get the current file system watcher and file type object
            // File type object is just used as a demo for now.
            FileSystemWatcherExt fswExt = (FileSystemWatcherExt)sender;
            FileObject changedFile = GetAffectedFile(e.FullPath);

            // Copy the file
            CopyChangedFile(fswExt, e.FullPath);
        }

        private void fswModel_Deleted(object sender, FileSystemEventArgs e)
        {
            FileSystemWatcherExt fswExt = (FileSystemWatcherExt)sender;
        }

        private void fswModel_Renamed(object sender, RenamedEventArgs e)
        {
            FileSystemWatcherExt fswExt = (FileSystemWatcherExt)sender;
            
            // Delete the backup file and copy the new one.

            RenameBackupFile(fswExt, e.OldFullPath, e.FullPath);
        }

        private void backupTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            FileCopySpec fcsCopy;

            try
            {              
                // If there are files to be copied.
                while(CopyRequests.Count > 0)
                {
                    fcsCopy = CopyRequests.Peek();
                    File.Copy(fcsCopy.fromLoc, fcsCopy.toLoc, true);
                    File.SetAttributes(fcsCopy.toLoc, FileAttributes.Normal);
                    // Remove from queue if successful.
                    fcsCopy = CopyRequests.Dequeue();
                    backupTimer.Interval = 30000;
                }                
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            finally
            {
                // Re-enable the backup timer.
                backupTimer.Interval = 5000;
            }
        }
    }

    public class FileSystemWatcherExt : System.IO.FileSystemWatcher
    {
        private string _destDirectory;
        private string _lastFile;
        private DateTime _lastOpTime;

        public FileSystemWatcherExt(string DestinationDirectory)
        {
            // Constructor
            _destDirectory = DestinationDirectory;
        }

        public DateTime LastOperationTime
        {
            get { return _lastOpTime; }
            set { _lastOpTime = value; }
        }

        public string LastFileAccessed
        {
            get { return _lastFile; }
            set { _lastFile = value; }
        }


        public string DestinationDirectory
        {
            // Destination directory for changed files
            get { return _destDirectory; }
            set { _destDirectory = value; }
        }

    }
}
