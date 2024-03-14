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
	public class ManifestGenerator : IPreprocessBuildWithReport, IPostprocessBuildWithReport
	{
		public static string ManifestFilePath => Path.Combine(Application.streamingAssetsPath, "manifest.txt");

		public int callbackOrder => 0;

		public void OnPreprocessBuild(BuildReport report)
		{
			if (report.summary.platform != BuildTarget.WebGL && report.summary.platform != BuildTarget.Android)
				return;

			StringBuilder manifestContent = new StringBuilder();
			IEnumerable<string> files = Directory.GetFiles(Application.streamingAssetsPath, "*", SearchOption.AllDirectories)
				.Where(file => !file.EndsWith(".meta"));

			foreach (string file in files)
			{
				string relativePath = file.Substring(Application.streamingAssetsPath.Length + 1).Replace("\\", "/");
				manifestContent.AppendLine(relativePath);
			}

			File.WriteAllText(ManifestFilePath, manifestContent.ToString());
			AssetDatabase.Refresh();
			Debug.Log($"Manifest generated at: {ManifestFilePath}");
		}

		public void OnPostprocessBuild(BuildReport report)
		{
			if (report.summary.platform != BuildTarget.WebGL && report.summary.platform != BuildTarget.Android)
				return;

			if (File.Exists(ManifestFilePath))
				File.Delete(ManifestFilePath);

			AssetDatabase.Refresh();
		}
	}
}
