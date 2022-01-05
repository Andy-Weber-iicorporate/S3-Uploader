#pragma warning disable 4014
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Amazon;
using Amazon.CloudFront;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Transfer;
using S3_Uploader.Editor.Extensions;
using UnityEditor;
using UnityEngine;

namespace S3_Uploader.Editor
{
    public enum Version
    {
        Production,
        Dev
    }

    public enum Fidelity
    {
        Low,
        Medium,
        High
    }

    public struct BackUpProgress
    {
        public bool Running;
        public int Count;
        public int Completed;
    }

    public class UploadToS3 : EditorWindow
    {
        private string region;
        private string bucketName;
        private Fidelity fidelity;
        private string iamAccessKeyId;
        private string iamSecretKey;
        private bool required;
        private RegionEndpoint bucketRegion;
        private bool foldout;
        private Version version;
        private bool autoDeleteTemp = true;
        string projectName;
        private bool backup;
        BackUpProgress backupProgress;

        private bool _uploading;
        ProgressDisplay progressWindow;
        private Logger _logger;

        private AmazonS3Client _s3Client;
        private bool _running;
        private BasicAWSCredentials _credentials;


        [MenuItem("Cade/Addressable Content Uploader (S3)")]
        static void Init()
        {
            var window = (UploadToS3)GetWindow(typeof(UploadToS3), false, "S3 Uploader");
            string[] s = Application.dataPath.Split('/');
            window.projectName = s[s.Length - 2];
            window.region = EditorPrefs.GetString($"{window.projectName}_UploadToS3_region", "us-west-2");
            window.bucketName = EditorPrefs.GetString($"{window.projectName}_UploadToS3_bucketName", "Maps");
            window.fidelity = (Fidelity)EditorPrefs.GetInt($"{window.projectName}_UploadToS3_Quality", 1);
            window.version = (Version)EditorPrefs.GetInt($"{window.projectName}_UploadToS3_Version", 1);
            window.iamAccessKeyId = EditorPrefs.GetString($"{window.projectName}_UploadToS3_iamAccessKeyId", "** SET ACCESS KEY ID **");
            window.iamSecretKey = EditorPrefs.GetString($"{window.projectName}_UploadToS3_iamSecretKey", "** SET SECRET KEY **");
            window.autoDeleteTemp = EditorPrefs.GetBool($"{window.projectName}_UploadToS3_deleteTemp", true);
            window.backup = EditorPrefs.GetBool($"{window.projectName}_UploadToS3_backup", true);
            window.Show();
        }


        private void OnEnable()
        {
            string[] s = Application.dataPath.Split('/');
            projectName = s[s.Length - 2];
            region = EditorPrefs.GetString($"{projectName}_UploadToS3_region", "us-west-2");
            bucketName = EditorPrefs.GetString($"{projectName}_UploadToS3_bucketName", "Maps");
            fidelity = (Fidelity)EditorPrefs.GetInt($"{projectName}_UploadToS3_Quality", 1);
            version = (Version)EditorPrefs.GetInt($"{projectName}_UploadToS3_Version", 1);
            iamAccessKeyId = EditorPrefs.GetString($"{projectName}_UploadToS3_iamAccessKeyId", "** SET ACCESS KEY ID **");
            iamSecretKey = EditorPrefs.GetString($"{projectName}_UploadToS3_iamSecretKey", "** SET SECRET KEY **");
            autoDeleteTemp = EditorPrefs.GetBool($"{projectName}_UploadToS3_deleteTemp", true);
            backup = EditorPrefs.GetBool($"{projectName}_UploadToS3_backup", true);
        }


        void OnGUI()
        {
            EditorGUI.BeginChangeCheck();
            GUILayout.Label("Advanced Settings", EditorStyles.boldLabel);
            foldout = EditorGUILayout.Foldout(foldout, "Advanced");
            if (foldout)
            {
                region = EditorGUILayout.TextField("Region", region);
                iamAccessKeyId = EditorGUILayout.TextField("IAM Access Key Id", iamAccessKeyId);
                iamSecretKey = EditorGUILayout.TextField("IAM Secret Key", iamSecretKey);
                autoDeleteTemp = EditorGUILayout.Toggle("Delete Temp Folder", autoDeleteTemp);
                backup = EditorGUILayout.Toggle("Backup before updating", backup);
            }

            GUILayout.Label("Basic Settings", EditorStyles.boldLabel);
            bucketName = EditorGUILayout.TextField("Bucket name", bucketName);
            version = (Version)EditorGUILayout.EnumPopup("Version", version);
            fidelity = (Fidelity)EditorGUILayout.EnumPopup("Fidelity", fidelity);
            required = EditorGUILayout.Toggle("Required", required);

            if (EditorGUI.EndChangeCheck())
            {
                EditorPrefs.SetString($"{projectName}_UploadToS3_region", region);
                EditorPrefs.SetString($"{projectName}_UploadToS3_bucketName", bucketName);
                EditorPrefs.SetInt($"{projectName}_UploadToS3_Quality", (int)fidelity);
                EditorPrefs.SetInt($"{projectName}_UploadToS3_Version", (int)version);
                EditorPrefs.SetString($"{projectName}_UploadToS3_iamAccessKeyId", iamAccessKeyId);
                EditorPrefs.SetString($"{projectName}_UploadToS3_iamSecretKey", iamSecretKey);
                EditorPrefs.SetBool($"{projectName}_UploadToS3_deleteTemp", autoDeleteTemp);
                EditorPrefs.SetBool($"{projectName}_UploadToS3_backup", backup);
            }

            if (_running == false && GUILayout.Button("Upload Content"))
            {
                var requiredText = required ? "required" : "non required";
                var proceed = EditorUtility.DisplayDialog($"Upload {fidelity} to {version}?",
                    $"Are you sure want to upload {fidelity} to {version} as {requiredText}?", "Yes", "No");
                if (proceed)
                {
                    bucketRegion = RegionEndpoint.GetBySystemName(region);
                    var buildTarget = EditorUserBuildSettings.activeBuildTarget.ToString();
                    var localFilePath = Path.Combine("ServerData", fidelity.ToString(), version.ToString(), buildTarget);
                    InitiateTask(localFilePath);
                }
            }

            if (backupProgress.Running)
            {
                EditorUtility.DisplayProgressBar("Backing up content", $"Backing up content: {backupProgress.Completed}/{backupProgress.Count} copied", (float)backupProgress.Completed/backupProgress.Count);
            }
            else
            {
                EditorUtility.ClearProgressBar();
            }

            // if (GUILayout.Button("uploader"))
            // {
            //     var list = new List<FileInfo>();
            //     for (int i = 0; i < 100; i++)
            //     {
            //         list.Add(new FileInfo(GUID.Generate().ToString()));
            //     }
            //     var window = ProgressDisplay.ShowWindow(list);
            //     window.UpdateProgress("test 12", 0.5f, 0);
            // }
        }


        private async Task InitiateTask(string localFilePath)
        {
            _running = true;
            _logger = new Logger(fidelity, version);
            if(progressWindow != null)
                progressWindow.Close();
            Debug.Log("Binding logger to unity logging");
            Application.logMessageReceived += _logger.Log;
            var buildTarget = EditorUserBuildSettings.activeBuildTarget.ToString();
            var s3Directory = $"CADEsportCDN/assets/Addressables/{bucketName}/{fidelity}/{version}/{buildTarget}";
            var tempS3Directory = $"{s3Directory}-temp";
            var lockFilePresent = false;
            string exception = "";
            Debug.Log($"Starting a content upload from {localFilePath} to {s3Directory}");

            try
            {
                //Create s3Client and credentials
                _credentials = new BasicAWSCredentials(iamAccessKeyId, iamSecretKey);
                _s3Client ??= new AmazonS3Client(_credentials, bucketRegion);

                //check for lock file for preventing multiple uploads
                lockFilePresent = await Exists($"{tempS3Directory}/{fidelity}-{version}.lock", $"cycligent-downloads");
                if (lockFilePresent)
                {
                    Debug.Log("An upload is already in progress. Please try again later.");
                    Debug.LogWarning("Upload Ended early. ----Did not Complete----");
                    return;
                }

                //create lock file for preventing multiple uploads
                var file = CreateLockFile($"{fidelity}-{version}.lock", localFilePath);
                await UploadFile($"cycligent-downloads/{tempS3Directory}", file, null);

                if (backup)
                {
                    Debug.Log("Deleting previous back up directory");
                    await DeleteAllObjectsIn($"{s3Directory}-backup");
                    await BackupFiles(s3Directory);
                }

                if (required)
                {
                    // -- required upload
                    await RequiredUpload(tempS3Directory, localFilePath);
                }
                else
                {
                    // -- not required upload
                    await NonRequiredUpload(tempS3Directory, localFilePath);
                }

                await Invalidate(_credentials);
            }
            catch (AmazonS3Exception e)
            {
                Debug.LogError($"Error encountered on server. Message:'{e.Message}' when writing an object");
                exception = e.Message;
            }
            catch (Exception e)
            {
                Debug.LogError($"Unknown error encountered. Message:'{e.Message}' when writing an object");
                exception = e.Message;
            }
            finally
            {
                //Delete lock file so another upload can take place
                if (lockFilePresent == false)
                    await DeleteFile($"{tempS3Directory}/{fidelity}-{version}.lock");

                _running = false;
                progressWindow.Complete();
                Debug.Log(string.IsNullOrEmpty(exception) ? "Uploading Finished!" : $"Uploading Finished with exception: {exception}");
                Application.logMessageReceived -= _logger.Log;
                if(File.Exists(_logger.FilePathPrevious))
                    await UploadFile($"cycligent-downloads/{s3Directory}", new FileInfo(_logger.FilePathPrevious), null, false);
                await UploadFile($"cycligent-downloads/{s3Directory}", new FileInfo(_logger.FilePath), null, false);
                _logger = null;
            }
        }


        private async Task<bool> Exists(string fileKey, string bucket)
        {
            try
            {
                Debug.Log($"Checking if '{fileKey}' exist in '{bucket}'");
                await _s3Client.GetObjectMetadataAsync(new GetObjectMetadataRequest
                {
                    BucketName = bucket,
                    Key = fileKey
                });

                Debug.Log($"Found: '{fileKey}' in '{bucket}'");
                return true;
            }

            catch (AmazonS3Exception ex)
            {
                if (ex.StatusCode != System.Net.HttpStatusCode.NotFound) throw;
                Debug.Log($"Not found: '{fileKey}' in '{bucket}'");
                return false;
            }
        }


        private async Task<bool> NonRequiredUpload(string uploadPath, string localFilePath)
        {
            Debug.Log($"Starting a non required upload to {uploadPath} from {localFilePath}");
            //delete files from temp-directory
            await DeleteAllObjectsIn(uploadPath);

            //get hosted and local files
            var destination = uploadPath.Replace($"-temp", $"");
            var s3Files = await GetObjectsInBucket("cycligent-downloads", destination);
            var localFiles = GetFiles(localFilePath);

            //Get files to upload to s3 (content files + files not found on s3)
            var filesToUpload = GetFilesToUpload(localFiles, s3Files);
            /*if (filesToUpload.Count <= 2)
            {
                Debug.Log("No files to upload!");
                return false;
            }*/

            progressWindow = ProgressDisplay.ShowWindow(filesToUpload);
            //upload new files to temp-directory
            await UploadFiles(filesToUpload, uploadPath);
            //create client lock
            var lockFile = CreateLockFile($"client-{fidelity}-{version}.lock", localFilePath);
            await UploadFile($"cycligent-downloads/{destination}", lockFile, null, false);
            //copy files from temp-directory to correct directory
            await CopyTempFiles(uploadPath);
            //delete client lock
            await DeleteFile($"{destination}/client-{fidelity}-{version}.lock");
            //delete temp-directory on s3
            if (autoDeleteTemp)
                await DeleteAllObjectsIn(uploadPath);
            return true;
        }


        private async Task<bool> RequiredUpload(string uploadPath, string localFilePath)
        {
            Debug.Log($"Starting a required upload to {uploadPath} from {localFilePath}");
            //delete files from temp-directory
            await DeleteAllObjectsIn(uploadPath);

            //get hosted and local files
            var destination = uploadPath.Replace($"-temp", $"");
            var s3Files = await GetObjectsInBucket("cycligent-downloads", destination);
            var localFiles = GetFiles(localFilePath);

            //Get files to upload to s3 (content files + files not found on s3)
            var filesToUpload = GetFilesToUpload(localFiles, s3Files);
            /*if (filesToUpload.Count <= 2)
            {
                Debug.Log("No files to upload!");
                return false;
            }*/

            //create required file
            var file = CreateRequiredFile(localFilePath);
            Debug.Log($"Adding {file.Name} to files to upload");
            filesToUpload.Add(file);
            progressWindow = ProgressDisplay.ShowWindow(filesToUpload);
            //upload new files to temp-directory
            await UploadFiles(filesToUpload, uploadPath);
            var lockFile = CreateLockFile($"client-{fidelity}-{version}.lock", localFilePath);
            await UploadFile($"cycligent-downloads/{destination}", lockFile, null, false);
            //copy temp directory to main directory
            await CopyTempFiles(uploadPath);
            //delete client lock
            await DeleteFile($"{destination}/client-{fidelity}-{version}.lock");
            //delete files in s3 main directory that are not in your local folder
            await DeleteOldFiles(s3Files, localFiles);
            //delete files from temp-directory
            if (autoDeleteTemp)
                await DeleteAllObjectsIn(uploadPath);
            return true;
        }


        private static List<FileInfo> GetFilesToUpload(IEnumerable<FileInfo> localFiles, List<S3Object> s3Files)
        {
            Debug.Log("Getting files to upload to s3.");
            var filesToUpload = new List<FileInfo>();
            foreach (var localFile in localFiles)
            {
                if (localFile.Name.Contains(".lock"))
                    continue;

                if (localFile.Name.Contains("required.txt"))
                    continue;
                
                if(localFile.Name.Contains("upload-log"))
                    continue;

                if (localFile.Name.Contains("catalog_"))
                {
                    Debug.Log($"Adding {localFile.Name} to files to upload");
                    filesToUpload.Add(localFile);
                    continue;
                }

                // check if file is on s3
                var found = s3Files
                    .Select(s3File => s3File.Key.Split('/').Last())
                    .Any(s3FileName => s3FileName == localFile.Name);

                //if file not on s3 add to filesToUpload
                if (found == false)
                {
                    Debug.Log($"Adding '{localFile.Name}' to files to upload. Not found on S3. Full Name: '{localFile.FullName}'");
                    filesToUpload.Add(localFile);
                }
            }

            return filesToUpload;
        }


        private async Task DeleteOldFiles(List<S3Object> s3Files, List<FileInfo> localFiles)
        {
            Debug.Log("Deleting old files from s3 that are not part of this required upload.");
            foreach (var s3Object in s3Files)
            {
                var s3FileName = s3Object.Key.Split('/').Last();
                if (s3FileName == "required.txt")
                {
                    Debug.Log($"Trying to delete 'required.txt' but this file should never be deleted.");
                    continue;
                }
                Debug.Log($"Checking if '{s3FileName}' needs deleting. Original Name: {s3Object.Key}");
                var delete = true;
                foreach (var local in localFiles)
                {
                    Debug.Log($"Checking if '{local.Name}' is the same as '{s3FileName}'");
                    if (s3FileName == "required.txt")
                    {
                        Debug.Log("Attempting to delete 'required.txt', not sure why. Ignoring.");
                        delete = false;
                        continue;
                    }

                    if (s3FileName != local.Name) 
                        continue;
                    
                    delete = false;
                    break;
                }
                
                if (delete == false)
                    continue;
                
                Debug.Log($"{s3FileName} not found in localFiles.");
                await DeleteObject(s3Object);
            }
        }


        private async Task DeleteAllObjectsIn(string tempS3Directory)
        {
            var objsToDelete = await GetObjectsInBucket("cycligent-downloads", tempS3Directory);
            Debug.Log($"Deleting all objects from bucket: 'cycligent-downloads' with prefix: '{tempS3Directory}'");
            foreach (var s3Object in objsToDelete.Where(s3Object => s3Object.Key.Contains(".lock") == false))
            {
                await DeleteObject(s3Object);
            }
        }


        private async Task DeleteObject(S3Object s3Object)
        {
            Debug.Log($"Deleting {s3Object.Key} from bucket {s3Object.BucketName}");
            await _s3Client.DeleteObjectAsync(new DeleteObjectRequest
            {
                BucketName = s3Object.BucketName,
                Key = s3Object.Key
            });
        }


        private async Task DeleteFile(string key)
        {
            await DeleteObject(new S3Object
            {
                BucketName = "cycligent-downloads",
                Key = key
            });
        }


        private List<FileInfo> GetFiles(string path)
        {
            Debug.Log($"Getting local files from '{path}'");
            var di = new DirectoryInfo(path);
            var files = di.GetFiles("*.*", SearchOption.AllDirectories);
            return files.ToList();
        }


        private async Task UploadFiles(IReadOnlyCollection<FileInfo> filesToUpload, string uploadPath)
        {
            uploadPath = $"cycligent-downloads/{uploadPath}";
            Debug.Log($"Uploading {filesToUpload.Count} files!");
            var transferUtility = new TransferUtility(_s3Client);
            foreach (var file in filesToUpload)
            {
                await UploadFile(uploadPath, file, transferUtility);
            }
        }


        private async Task UploadFile(string uploadPath, FileInfo file, TransferUtility transferUtility = null, bool updateProgressWindow = true)
        {
            transferUtility ??= new TransferUtility(_s3Client);
            var directoryName = file.DirectoryName ?? "";
            var buildTarget = EditorUserBuildSettings.activeBuildTarget.ToString();
            var appendedPath = directoryName.Split(new[] { $"{buildTarget}" }, StringSplitOptions.None).Last()
                .Replace("\\", "/");
            var path = $"{uploadPath}{appendedPath}";
            Debug.Log($"Uploading {file.Name} to {path}! File fullName: {file.FullName}");
            var transferUtilityRequest = new TransferUtilityUploadRequest
            {
                BucketName = path,
                FilePath = file.FullName,
                StorageClass = S3StorageClass.Standard,
                CannedACL = S3CannedACL.PublicRead,
            };
            if (updateProgressWindow && progressWindow)
            {
                progressWindow.UpdateStatus(file.Name, Status.Uploading);
                transferUtilityRequest.UploadProgressEvent += UploadProgress;
            }
            await transferUtility.UploadAsync(transferUtilityRequest);
            if (updateProgressWindow && progressWindow)
            {
                progressWindow.UpdateStatus(file.Name, Status.Uploaded);
                transferUtilityRequest.UploadProgressEvent -= UploadProgress;
            }
            Debug.Log($"Upload completed for {file.Name}! Full Name: '{file.FullName}'");
        }


        private void UploadProgress(object sender, UploadProgressArgs e)
        {
            var key = e.FilePath.Split('\\').Last();
           // Debug.Log($"Uploading: {key} {e.PercentDone}. transferred: {e.TransferredBytes}/{e.TotalBytes}");
            if (progressWindow)
                progressWindow.UpdateProgress(key, e.PercentDone / 100f, e.TransferredBytes);
        }


        private async Task CopyTempFiles(string copyFrom)
        {
            var objectsToCopy = await GetObjectsInBucket("cycligent-downloads", copyFrom);
            foreach (var obj in objectsToCopy)
            {
                if (obj.Key.Contains(".lock"))
                    continue;

                var buildTarget = EditorUserBuildSettings.activeBuildTarget.ToString();
                var destination = obj.Key.Replace($"{buildTarget}-temp", $"{buildTarget}");
                Debug.Log($"Copying {obj.Key} from bucket {obj.BucketName} to {destination}");
                var request = new CopyObjectRequest
                {
                    SourceBucket = "cycligent-downloads",
                    SourceKey = obj.Key,
                    DestinationBucket = "cycligent-downloads",
                    DestinationKey = destination
                };
                var key = obj.Key.Split('/').Last();
                progressWindow.UpdateStatus(key, Status.Copying);
                await _s3Client.CopyObjectAsync(request);
                progressWindow.UpdateStatus(key, Status.Completed);
            }
        }


        private async Task BackupFiles(string copyFrom)
        {
            var objectsToCopy = await GetObjectsInBucket("cycligent-downloads", copyFrom);
            backupProgress = new BackUpProgress
            {
                Count = objectsToCopy.Count,
                Completed = 0,
                Running = true,
            };
            
            foreach (var obj in objectsToCopy.Where(obj => !obj.Key.Contains(".lock")))
            {
                var buildTarget = EditorUserBuildSettings.activeBuildTarget.ToString();
                var destination = obj.Key.Replace($"{buildTarget}", $"{buildTarget}-backup");
                Debug.Log($"Backing up {obj.Key} from bucket {obj.BucketName} to {destination}");
                var request = new CopyObjectRequest
                {
                    SourceBucket = "cycligent-downloads",
                    SourceKey = obj.Key,
                    DestinationBucket = "cycligent-downloads",
                    DestinationKey = destination
                };
                
                await _s3Client.CopyObjectAsync(request);
                backupProgress.Completed++;
            }

            backupProgress = default;
        }


        private async Task<List<S3Object>> GetObjectsInBucket(string bucket, string prefix)
        {
            var listRequest = new ListObjectsRequest
            {
                BucketName = bucket,
                Prefix = $"{prefix}/"
            };

            Debug.Log($"Getting list from: {listRequest.BucketName} with prefix {listRequest.Prefix}!");
            var s3Objs = new List<S3Object>();
            ListObjectsResponse listResponse;
            do
            {
                // Get a list of objects
                listResponse = await _s3Client.ListObjectsAsync(listRequest);
                foreach (var obj in listResponse.S3Objects)
                {
                    // Debug.Log($"Found {obj.Key} in bucket {obj.BucketName}");
                    s3Objs.Add(obj);
                }

                // Set the marker property
                listRequest.Marker = listResponse.NextMarker;
            } while (listResponse.IsTruncated);

            return s3Objs;
        }


        private async Task Invalidate(AWSCredentials credentials, string key = "/*")
        {
            var cloudFront = new AmazonCloudFrontClient(credentials, bucketRegion);
            var invalidator = new CloudFrontInvalidator(cloudFront);
            Debug.Log($"Invalidation Started for {key}, awaiting the send to return a completed send...");
            invalidator.InvalidateObject(key);
            await invalidator.SendInvalidation();
            Debug.Log("Invalidation Sent");
        }


        private static FileInfo CreateRequiredFile(string localFilePath)
        {
            Debug.Log("Creating required file");
            var fileName = Path.Combine(localFilePath, "required.txt");
            var time = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            File.WriteAllText(fileName, time.ToString());
            return new FileInfo(fileName);
        }


        private static FileInfo CreateLockFile(string name, string localFilePath)
        {
            Debug.Log($"Creating lock file {name}");
            var fileName = Path.Combine(localFilePath, name);
            if (Directory.Exists(localFilePath) == false)
                Directory.CreateDirectory(localFilePath);
            
            File.WriteAllText(fileName, string.Empty);
            Debug.Log($"Lock file created: '{name}'. Exist: '{File.Exists(fileName)}'");
            return new FileInfo(fileName);
        }


        private void OnInspectorUpdate()
        {
            Repaint();
        }
    }
}
#pragma warning restore 4014


//get files edited within last hour
//var infoFiles = GetNewFiles(_path);

//var s3Objs = await GetObjectsInBucket("cycligent-downloads", $"{s3Directory}-test");
//foreach (var obj in s3Objs)
//{
//    var s3NamesSplit = obj.Key.Split('/');
//    var s3FileName = s3NamesSplit.Last();
//    if (s3FileName != $"{quality}-{_profile}.lock")
//        continue;
//    
//    Debug.Log("An upload is already in progress. Please try again later.");
//    return;
//}

// private List<FileInfo> GetNewFiles(string path)
// {
//     var today = DateTime.Now;
//     var start = today.AddHours(-1);
//     var end = today;
//     var files = GetFilesBetween(path, start, end);
//     Debug.Log($"Getting files modified between times '{start}' and '{end}' from path: '{path}'!");
//     var fileList = files as List<FileInfo> ?? files.ToList();
//     var fileNames = new List<string>(fileList.Count);
//     fileNames.AddRange(fileList.Select(file => file.Name));
//     Debug.Log(
//         $"These files were modified within the last {(end - start).TotalHours} hours: '{string.Join(", ", fileNames)}'");
//     return fileList;
// }
//
//
// private IEnumerable<FileInfo> GetFilesBetween(string path, DateTime start, DateTime end)
// {
//     var files = GetFiles(path);
//     return files.Where(f => f.LastWriteTime.Between(start, end));
// }