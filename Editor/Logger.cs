using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace S3_Uploader.Editor
{
    public class Logger
    {
        private readonly string _path;
        
        
        public Logger(Fidelity fidelity, Version version)
        {
            try
            {
                var name = "upload-log.txt";
                var buildTarget = EditorUserBuildSettings.activeBuildTarget.ToString();
                var localFilePath = Path.Combine("ServerData", fidelity.ToString(), version.ToString(), buildTarget);
                var filePrevious = Path.Combine(localFilePath, "upload-log-previous.txt");
                _path = Path.Combine(localFilePath, name);
                if (Directory.Exists(localFilePath) == false)
                    Directory.CreateDirectory(localFilePath);
                if(File.Exists(filePrevious))
                    File.Delete(filePrevious);
                if (File.Exists(_path))
                    File.Move(_path, filePrevious);

                using var sw = new StreamWriter(_path);
                sw.WriteLine($"Content upload started at: '{DateTime.UtcNow}', By: '{Environment.UserName}'");
            }
            catch (Exception e)
            {
                Debug.Log(e);
                throw;
            }
        }


        public void Log(string condition, string stackTrace, LogType type)
        {
            using var file = new StreamWriter(_path, true);
            file.WriteLine($"Log type: {type}");
            file.WriteLine(condition);
            file.WriteLine(stackTrace);
            file.WriteLine(" ");
        }
    }
}