using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Ionic.Zip;

namespace FileBackupService
{
    class ZIPArchive : FileObject
    {
        private ZipFile _zipArc;

        public ZIPArchive()
        {
            _zipArc = new ZipFile();
        }

        public ZIPArchive(string FilePath) :base(FilePath)
        {
            _zipArc = new ZipFile(FilePath);
        }

        public int InnerFileCount
        {
            get { return (FileExists) ? _zipArc.Count() : 0; }
        }

        public bool AddFile(string File)
        {
            bool returnValue = false;

            try
            {
                // Create new zip file if one does not already exist.
                if (_zipArc == null && FileExists)
                {
                    _zipArc = new ZipFile(base.ToString());
                }

                _zipArc.AddFile(File);
                _zipArc.Save();
                returnValue = true;
            }
            catch (Exception ex)
            {
                returnValue = false;
                throw ex;
            }

            return returnValue;

        }
    }
}
