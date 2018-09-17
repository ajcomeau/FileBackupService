using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TagLib;
using TagLib.Mpeg;
using FileBackupService;


namespace FileBackupService
{
    class MP3Audio : FileObject
    {
        private TagLib.Mpeg.AudioFile _mp3File;

        public MP3Audio(string FilePath) :base(FilePath)
        {
            _mp3File = new AudioFile(FilePath);
        }

        public TimeSpan Length()
        {
            return _mp3File.Properties.Duration;
        }

    }
}
