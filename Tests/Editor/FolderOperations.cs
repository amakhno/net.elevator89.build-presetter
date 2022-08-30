using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;

namespace Elevator89.BuildPresetter.Tests
{
	[TestFixture]
	public class FolderOperations
	{
		private string[] _filesBefore;

		[SetUp]
		public void Init()
		{
			_filesBefore = AssetDatabase.GetAllAssetPaths();
		}

		[TearDown]
		public void Cleanup()
		{
			string[] createdFiles = AssetDatabase.GetAllAssetPaths().Except(_filesBefore).ToArray();

			List<string> failedPaths = new List<string>();
			AssetDatabase.DeleteAssets(createdFiles, failedPaths);
		}

		[TestCase("Assets/NestedFolder1", "Assset.txt")]
		[TestCase("Assets/NestedFolder1/NestedFolder2", "Assset.txt")]
		[TestCase("Assets/NestedFolder1/NestedFolder2/NestedFolder3", "Assset.txt")]
		[TestCase("Assets/StreamingAssets/NestedFolder1", "Assset.txt")]
		[TestCase("Assets/StreamingAssets/NestedFolder1/NestedFolder2/NestedFolder3", "Assset.txt")]
		[TestCase("Assets/StreamingAssets/NestedFolder1/NestedFolder2", "Assset.txt")]
		public void CreatesDestinationPaths(string folderPath, string nonExistingAssetName)
		{
			string nonExistingAssetPath = folderPath + "/" + nonExistingAssetName;

			Util.EnsurePath(nonExistingAssetPath);

			TestUtil.AssertFileAndFoldersAssetsExist(folderPath, "All folders in ensured paths must exist");
		}

		[TestCase("Assets", "NestedFolder1", "Assset.txt")]
		[TestCase("Assets", "NestedFolder1/NestedFolder2", "Assset.txt")]
		[TestCase("Assets", "NestedFolder1/NestedFolder2/NestedFolder3", "Assset.txt")]
		[TestCase("Assets/StreamingAssets", "NestedFolder1", "Assset.txt")]
		[TestCase("Assets/StreamingAssets", "NestedFolder1/NestedFolder2/NestedFolder3", "Assset.txt")]
		[TestCase("Assets/StreamingAssets", "NestedFolder1/NestedFolder2", "Assset.txt")]
		public void CleansEmptyFolders(string existingFolderPath, string relativeFolderPath, string nonExistingAssetName)
		{
			string nonExistingAssetPath = existingFolderPath + "/" + relativeFolderPath + "/" + nonExistingAssetName;

			Util.EnsurePath(nonExistingAssetPath);

			Util.CleanEmptyFolders(nonExistingAssetPath);

			TestUtil.AssertFileAndFoldersAssetsDontExist(existingFolderPath, relativeFolderPath, "All folders without assets must be deleted");
		}

		[TestCase("Assets", "Assset1.txt", "NestedFolder1", "Asset2.txt")]
		[TestCase("Assets/Folder1", "Assset1.txt", "NestedFolder1", "Asset2.txt")]
		[TestCase("Assets/Folder1", "Assset1.txt", "NestedFolder1/NestedFolder2", "Asset2.txt")]
		[TestCase("Assets/Folder1/Folder2", "Assset1.txt", "NestedFolder1/NestedFolder2", "Asset2.txt")]
		[TestCase("Assets/StreamingAssets", "Assset1.txt", "NestedFolder1", "Asset2.txt")]
		[TestCase("Assets/StreamingAssets/Folder1", "Assset1.txt", "NestedFolder1", "Asset2.txt")]
		[TestCase("Assets/StreamingAssets/Folder1", "Assset1.txt", "NestedFolder1/NestedFolder2", "Asset2.txt")]
		[TestCase("Assets/StreamingAssets/Folder1/Folder2", "Assset1.txt", "NestedFolder1/NestedFolder2", "Asset2.txt")]
		public void CleansOnlyEmptyFolders(string existingFolderPath, string existingAssetName, string relativeFolderPath, string nonExistingAssetName)
		{
			string existingAssetPath = existingFolderPath + "/" + existingAssetName;
			string nonExistingAssetPath = existingFolderPath + "/" + relativeFolderPath + "/" + nonExistingAssetName;

			TestUtil.CreateAsset(existingAssetPath);

			Util.EnsurePath(nonExistingAssetPath);

			Util.CleanEmptyFolders(nonExistingAssetPath);

			TestUtil.AssertFileAndFoldersAssetsDontExist(existingFolderPath, relativeFolderPath, "All folders without assets must be deleted");
			TestUtil.AssertFileAndFoldersAssetsExist(existingFolderPath, "All folders with assets must exist");
		}
	}
}
