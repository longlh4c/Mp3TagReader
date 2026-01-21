using System;

namespace Mp3TagReader
{
    public class Mp3Info
    {
        private string _path;
        private string _name;
        private DateTime _creationTime;

        public Mp3Info()
        {
        }

        public Mp3Info(string Path, string Name, DateTime CreationTime)
        {
            _path = Path;
            _name = Name;
            _creationTime = CreationTime;
        }

        public string Path
        {
            get { return _path; }
            set { _path = value; }
        }

        public string Name
        {
            get { return _name; }
            set { _name = value; }
        }

        public DateTime CreationTime
        {
            get { return _creationTime; }
            set { _creationTime = value; }
        }
    }

    public class FolderInfo : Mp3Info
    {
        public FolderInfo()
        {
        }

        public FolderInfo(string path, string name)
        {
            this.Path = path;
            this.Name = name;
        }
    }
}