using System;
using System.IO;
using UnityEngine;

namespace S3_Uploader.Editor
{
    public class Logger
    {
        private DateTime _time;
        private readonly string _path;
        
        
        public Logger()
        {
            _time = DateTime.UtcNow;
            _path = Path.Combine("ServerData", $"S3-Upload-Log-{_time}");
            File.Create(_path);
        }
        
        
        public void Log(string condition, string stackTrace, LogType type)
        {
            using var file = new StreamWriter(_path, append: true);
            file.WriteLine($"Log type: {type}");
            file.WriteLine(condition);
            file.WriteLine(stackTrace);
            file.WriteLine("--long end--");
            file.WriteLine(" ");
        }
    }
}