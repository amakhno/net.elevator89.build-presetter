using NUnit.Framework;
using System;
using System.IO;
using System.Linq;
using UnityEditor;

namespace Elevator89.BuildPresetter.Tests
{
	internal static class TestUtil
	{
		public static void AssertFileAndFoldersAssetsExist(string rootFolder, string relativeFolderPath, string message)
		{
			string[] relativePathParts = relativeFolderPath.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);

			for (int i = 1; i <= relativePathParts.Length; ++i) // Skip root Assets folder
			{
				string currentFolderOrAssetPath = rootFolder + "/" + string.Join("/", relativePathParts.Take(i));
				Assert.True(Util.DoesAssetExist(currentFolderOrAssetPath), message);
			}
		}

		public static void AssertFileAndFoldersAssetsExist(string folderPath, string message)
		{
			string[] relativePathParts = folderPath.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);

			for (int i = 1; i <= relativePathParts.Length; ++i) // Skip root Assets folder
			{
				string currentFolderOrAssetPath = string.Join("/", relativePathParts.Take(i));
				Assert.True(Util.DoesAssetExist(currentFolderOrAssetPath), message);
			}
		}

		public static void AssertFileAndFoldersAssetsDontExist(string rootFolder, string relativeFolderPath, string message)
		{
			string[] relativePathParts = relativeFolderPath.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);

			for (int i = 1; i <= relativePathParts.Length; ++i) // Skip root Assets folder
			{
				string currentFolderOrAssetPath = rootFolder + "/" + string.Join("/", relativePathParts.Take(i));
				Assert.False(Util.DoesAssetExist(currentFolderOrAssetPath), message);
			}
		}

		public static void CreateAsset(string assetPath)
		{
			Util.EnsurePath(assetPath);
			File.WriteAllText(assetPath, "some text");
			AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceUncompressedImport);
		}
	}
}
