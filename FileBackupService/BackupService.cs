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

namespace FileBackupService
{
    public partial class BackupService : ServiceBase
    {
        List<FileSystemWatcherExt> dirList = new List<FileSystemWatcherExt>();

        public BackupService()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {           
            LoadDirectories();   
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
                            fswModel.Renamed += fswModel_Renamed;
                            dirList.Add(fswNew);
                        }                   
                    }
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        private FileSystemWatcherExt CreateFileWatcher(string FileSource, bool IncludeSubdirs, string Destination)
        {
            FileSystemWatcherExt fswReturn = new FileSystemWatcherExt(Destination);
            string filePath = "";
            int charPlace;

            try
            {
                if (Directory.Exists(FileSource))
                {
                    filePath = FileSource;
                }
                else
                {
                    charPlace = FileSource.LastIndexOf(@"\");
                    filePath = FileSource.Substring(0, charPlace);

                    if (Directory.Exists(filePath))
                    {
                        fswReturn.Path = filePath;
                        fswReturn.Filter = FileSource.Substring(charPlace + 1);
                    }
                }

                fswReturn.IncludeSubdirectories = IncludeSubdirs;

            }
            catch (Exception ex)
            {

                throw ex;
            }

            if(fswReturn.Path.Length > 0)
            {
                return fswReturn;
            }
            else
            {
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
            FileObject returnFile;

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
                throw ex;
            }

            return returnFile;
        }

        private void CopyChangedFile(FileSystemWatcherExt FileWatcher, string SourceFilePath)
        {
            string fullDestPath;

            try
            {
                // Get the destination path.
                fullDestPath = SourceFilePath.Replace(FileWatcher.Path, FileWatcher.DestinationDirectory)
                    .Replace("\\\\", "\\");

                // If it doesn't exist already, create it.
                if (!Directory.Exists(Directory.GetParent(fullDestPath).ToString()))
                {
                    Directory.CreateDirectory(fullDestPath);
                }

                // Copy the file.
                File.Copy(SourceFilePath, fullDestPath);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        private void DeleteFile(FileSystemWatcherExt FileWatcher, string SourceFilePath)
        {
            string fullDestPath;

            try
            {
                // Get the destination path.
                fullDestPath = SourceFilePath.Replace(FileWatcher.Path, FileWatcher.DestinationDirectory)
                    .Replace("\\\\", "\\");

                // If the file exists, delete it.
                if (!File.Exists(fullDestPath))
                {
                    // Copy the file.
                    File.Delete(fullDestPath);
                }

            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        private void fswModel_Changed(object sender, FileSystemEventArgs e)
        {
            // Get the current file system watcher.
            FileSystemWatcherExt fswExt = (FileSystemWatcherExt)sender;

            // Create the specific file object for the file type.
            FileObject changedFile = GetAffectedFile(e.FullPath);

            // Copy the file
            CopyChangedFile(fswExt, e.FullPath);

        }

        private void fswModel_Created(object sender, FileSystemEventArgs e)
        {
            FileSystemWatcherExt fswExt = (FileSystemWatcherExt)sender;
            FileObject changedFile = GetAffectedFile(e.FullPath);

            // Copy the file
            CopyChangedFile(fswExt, e.FullPath);
        }

        private void fswModel_Deleted(object sender, FileSystemEventArgs e)
        {
            FileSystemWatcherExt fswExt = (FileSystemWatcherExt)sender;
            FileObject changedFile = GetAffectedFile(e.FullPath);
        }

        private void fswModel_Renamed(object sender, RenamedEventArgs e)
        {
            FileSystemWatcherExt fswExt = (FileSystemWatcherExt)sender;
            FileObject changedFile = GetAffectedFile(e.FullPath);
            
            // Delete the backup file and copy the new one.
            DeleteFile(fswExt, e.FullPath);
            CopyChangedFile(fswExt, e.OldFullPath);
        }
    }

    public class FileSystemWatcherExt : System.IO.FileSystemWatcher
    {
        private string _destDirectory;  

        public string DestinationDirectory
        {
            // Destination directory for changed files
            get { return _destDirectory; }
            set { _destDirectory = value; }
        }

        public FileSystemWatcherExt(string DestinationDirectory)
        {
            // Constructor
            _destDirectory = DestinationDirectory;
        }
    }
}
