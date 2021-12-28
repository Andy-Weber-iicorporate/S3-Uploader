using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace S3_Uploader.Editor
{
    public class Logger
    {
        public string FilePath { get; }
        public string FilePathPrevious { get; }


        public Logger(Fidelity fidelity, Version version)
        {
            try
            {
                var name = "upload-log.txt";
                var buildTarget = EditorUserBuildSettings.activeBuildTarget.ToString();
                var localFilePath = Path.Combine("ServerData", fidelity.ToString(), version.ToString(), buildTarget);
                FilePathPrevious = Path.Combine(localFilePath, "upload-log-previous.txt");
                FilePath = Path.Combine(localFilePath, name);
                if (Directory.Exists(localFilePath) == false)
                    Directory.CreateDirectory(localFilePath);
                if(File.Exists(FilePathPrevious))
                    File.Delete(FilePathPrevious);
                if (File.Exists(FilePath))
                    File.Move(FilePath, FilePathPrevious);

                using var sw = new StreamWriter(FilePath);
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
            using var file = new StreamWriter(FilePath, true);
            file.WriteLine($"Log type: {type}");
            file.WriteLine(condition);
            file.WriteLine(stackTrace);
            file.WriteLine(" ");
        }
    }
}