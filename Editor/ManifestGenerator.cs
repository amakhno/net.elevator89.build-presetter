using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Elevator89.BuildPresetter
{
    public class ManifestGenerator : IPreprocessBuildWithReport
    {
        public int callbackOrder => 0;
        
        public static string ManifestFilePath => Path.Combine(StreamingAssetsPath, "manifest.txt");
        private static string StreamingAssetsPath => Application.streamingAssetsPath;

        public void OnPreprocessBuild(BuildReport report)
        {
            if (report.summary.platform != BuildTarget.WebGL && report.summary.platform != BuildTarget.Android) 
                return;

            if (File.Exists(ManifestFilePath))
                File.Delete(ManifestFilePath);

            StringBuilder manifestContent = new StringBuilder();
            IEnumerable<string> files = Directory.GetFiles(StreamingAssetsPath, "*", SearchOption.AllDirectories)
                .Where(file => !file.EndsWith(".meta"));

            foreach (string file in files)
            {
                string relativePath = file.Substring(StreamingAssetsPath.Length + 1).Replace("\\", "/");
                manifestContent.AppendLine(relativePath);
            }

            File.WriteAllText(ManifestFilePath, manifestContent.ToString());
            AssetDatabase.Refresh();
            Debug.Log($"Manifest generated at: {ManifestFilePath}");
        }
    }
}
