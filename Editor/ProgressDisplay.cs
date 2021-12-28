using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace S3_Uploader.Editor
{
    public enum Status
    {
        Idle,
        Uploading,
        Uploaded,
        Copying,
        Completed,
        Failed
    }


    public struct Content
    {
        public readonly string Name;
        public float Progress;
        public Status Status;
        public string ErrorMessage;
        public float Transferred;
        public float Size;


        public Content(string name, float progress, Status status, string errorMessage, float size)
        {
            Name = name;
            Progress = progress;
            Status = status;
            ErrorMessage = errorMessage;
            Size = size;
            Transferred = 0;
        }
    }


    public class ProgressDisplay : EditorWindow
    {
        private readonly Dictionary<string, Content> _contentUploading = new Dictionary<string, Content>();
        public bool complete;

        private bool _lock;
        private Vector2 _scrollPos;


        public static ProgressDisplay ShowWindow(List<FileInfo> content)
        {
            var window = (ProgressDisplay)GetWindow(typeof(ProgressDisplay), true, "Progress");
            window.Focus();
            var height = (content.Count + 1) * 36 > 1000 ? 1000 : (content.Count + 1) * 36;
            window.minSize = new Vector2(500f, 200);
            window.maxSize =  new Vector2(Screen.currentResolution.width, height);
            var position = window.position;
            position.center = new Rect(0f, 0f, Screen.currentResolution.width, Screen.currentResolution.height).center;
            window.position = position;
            window.titleContent = new GUIContent("Uploader");
            foreach (var c in content)
            {
                window._contentUploading.Add(c.Name, new Content(c.Name, 0, Status.Idle, "", c.Length));
            }

            window.ShowUtility();
            return window;
        }


        public void UpdateProgress(string content, float progress, float transferred)
        {
            if (_contentUploading.ContainsKey(content))
            {
                var updatedContent = _contentUploading[content];
                if (updatedContent.Status == Status.Failed)
                    return;

                updatedContent.Progress = progress;
                updatedContent.Transferred = transferred;
                //updatedContent.Status = progress >= 100 ? Status.Uploaded : Status.Uploading;
                _contentUploading[content] = updatedContent;
            }
            else
            {
                const string errorMessage = "Content not present in '_contentUploading' dictionary while updating progress. ";
                Debug.Log(errorMessage + content);
                _contentUploading.Add(content, new Content(content, 0, Status.Failed, errorMessage, 0));
            }
        }


        public void UpdateStatus(string content, Status status)
        {
            _lock = true;
            if (_contentUploading.ContainsKey(content))
            {
                var updatedContent = _contentUploading[content];
                if (updatedContent.Status == Status.Failed)
                    return;

                updatedContent.Status = status;
                _contentUploading[content] = updatedContent;
            }
            else
            {
                const string errorMessage = "Content not present in '_contentUploading' dictionary while updating status files.";
                _contentUploading.Add(content, new Content
                (content,
                    0,
                    Status.Failed,
                    errorMessage,
                    0));
                Debug.Log(errorMessage + content);
                var height = (_contentUploading.Count + 1) * 36 < 1000 ? 1000 : (_contentUploading.Count + 1) * 36;
                maxSize =  new Vector2(Screen.currentResolution.width, height);
            }

            _lock = false;
        }


        private void OnGUI()
        {
            if (_lock)
                return;

            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos, GUILayout.Height(position.height));
            foreach (var content in _contentUploading.ToList())
            {
                Rect rect0 = GUILayoutUtility.GetRect(0, 18, GUIStyle.none);
                EditorGUI.LabelField(rect0, content.Value.Name);
                if (content.Value.Status == Status.Failed)
                {
                    Rect rect1 = GUILayoutUtility.GetRect(0, 18, GUIStyle.none);
                    EditorGUI.LabelField(rect1, content.Value.ErrorMessage);
                }

                GUILayout.BeginHorizontal();
                Rect rect2 = GUILayoutUtility.GetRect(position.width - 70, 18, GUIStyle.none);
                Rect rect3 = GUILayoutUtility.GetRect(70, 18, GUIStyle.none);
                GUILayout.EndHorizontal();
                EditorGUI.ProgressBar(rect2, content.Value.Progress,
                    $"{content.Value.Progress * 100f}%... ({content.Value.Transferred:N1}/{content.Value.Size:N1} bytes)");

                var contentColor = GUI.contentColor;
                switch (content.Value.Status)
                {
                    case Status.Idle:
                        GUI.contentColor = Color.black;
                        break;
                    case Status.Uploading:
                    case Status.Uploaded:
                    case Status.Copying:
                        GUI.contentColor = Color.yellow;
                        break;
                    case Status.Completed:
                        GUI.contentColor = Color.green;
                        break;
                    case Status.Failed:
                        GUI.contentColor = Color.red;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
                EditorGUI.LabelField(rect3, content.Value.Status.ToString());
                GUI.contentColor = contentColor;
            }

            if (complete)
            {
                if (GUILayout.Button("Completed! (click to close)"))
                {
                    Close();
                }
            }

            EditorGUILayout.EndScrollView();
        }

        public void Complete()
        {
            complete = true;
        }


        private void OnInspectorUpdate()
        {
            Repaint();
        }
    }
}