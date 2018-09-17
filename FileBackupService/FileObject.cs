using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace FileBackupService
{
    public class FileObject
    {
        private string _filePath;
        private bool _markForDelete;
        private FileInfo _fileObjInfo;

        // Constructors
        public FileObject()
        {
            _filePath = "";
        }
        
        internal FileObject(string FilePath)
        {
            _filePath = FilePath;
            _fileObjInfo = new FileInfo(FilePath);
        }

        public bool MarkedForDeletion
        {
            // Enables the file to be marked for deletion by the program.
            get { return _markForDelete; }
            set { _markForDelete = value; }
        }

        public long FileSize
        {
            // Get the physical file size
            get { return (_fileObjInfo != null) ? _fileObjInfo.Length : 0; }
        }

        public string FilePathOnly
        {
            // Get the physical file path only
            get { return (_fileObjInfo != null) ? _fileObjInfo.DirectoryName : ""; }
        }

        public string FileNameOnly
        {
            // Get the physical file name only
            get { return (_fileObjInfo != null) ? _fileObjInfo.Name : ""; }
        }

        public bool FileExists
        {
            // Check if file actually exists
            get { return File.Exists(_filePath); }
        }

        public string FileLocation
        {
            set
            {
                _filePath = value;

                // If the file actually exists, populate the file info.
                if (File.Exists(value))
                {
                    _fileObjInfo = new FileInfo(value); 
                }
            }
        }

        public bool ReadOnly
        {
            // Return a boolean based on the readonly attribute.
            get { return (_fileObjInfo != null) ? _fileObjInfo.Attributes.HasFlag(FileAttributes.ReadOnly) : false; }
        }

        public bool HiddenFile
        {
            // Return a boolean based on the hidden attribute.
            get { return (_fileObjInfo != null) ? _fileObjInfo.Attributes.HasFlag(FileAttributes.Hidden) : false; }
        }

        public bool SystemFile
        {
            // Return a boolean based on the system attribute.
            get { return (_fileObjInfo != null) ? _fileObjInfo.Attributes.HasFlag(FileAttributes.System) : false; }
        }

        public DateTime Created
        {
            // Time the file was physically created.
            get { return (_fileObjInfo != null) ? _fileObjInfo.CreationTime : DateTime.MinValue ; }
        }

        public DateTime Modified
        {
            // Time the file was last modified.
            get { return (_fileObjInfo != null) ? _fileObjInfo.LastWriteTime : DateTime.MinValue; }
        }

        public override string ToString()
        {
            // Return the file path and name.
            return _fileObjInfo.FullName;
        }

    }
}
