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

        private void fswModel_Changed(object sender, FileSystemEventArgs e)
        {
            FileSystemWatcherExt fswExt =  (FileSystemWatcherExt)sender;
            Console.WriteLine(fswExt.DestinationDirectory);
        }

        private void fswModel_Created(object sender, FileSystemEventArgs e)
        {
            FileSystemWatcherExt fswExt = (FileSystemWatcherExt)sender;
            Console.WriteLine(fswExt.DestinationDirectory);
        }

        private void fswModel_Deleted(object sender, FileSystemEventArgs e)
        {
            FileSystemWatcherExt fswExt = (FileSystemWatcherExt)sender;
            Console.WriteLine(fswExt.DestinationDirectory);
        }

        private void fswModel_Renamed(object sender, RenamedEventArgs e)
        {
            FileSystemWatcherExt fswExt = (FileSystemWatcherExt)sender;
            Console.WriteLine(fswExt.DestinationDirectory);
        }
    }

    public class FileSystemWatcherExt : System.IO.FileSystemWatcher
    {
        private string _destDirectory;

        public string DestinationDirectory
        {
            get { return _destDirectory; }
            set { _destDirectory = value; }
        }

        public FileSystemWatcherExt(string DestinationDirectory)
        {
            _destDirectory = DestinationDirectory;
        }
    }
}
