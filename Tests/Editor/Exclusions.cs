using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;

namespace Elevator89.BuildPresetter.Tests
{
	[TestFixture]
	public class Exclusions
	{
		const string StreamingAssetsPath = "Assets/StreamingAssets";
		const string ExcludedStreamingAssetsPath = "Assets/~StreamingAssets";

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

		[TestCase("Assset.txt")]
		[TestCase("NestedFolder1/Assset.txt")]
		[TestCase("NestedFolder1/NestedFolder2/Assset.txt")]
		public void ExcludesAndIncludesStreamingAssets(string relativeStreamingAssetPath)
		{
			string testAssetPath = StreamingAssetsPath + "/" + relativeStreamingAssetPath;

			TestUtil.CreateAsset(testAssetPath);

			StreamingAssetsUtil.SetStreamingAssetIncluded(relativeStreamingAssetPath, false);

			TestUtil.AssertFileAndFoldersAssetsExist(ExcludedStreamingAssetsPath, relativeStreamingAssetPath, "Excluded path parts should exist for asset");
			TestUtil.AssertFileAndFoldersAssetsDontExist(StreamingAssetsPath, relativeStreamingAssetPath, "Included path parts should not exist for asset");

			StreamingAssetsUtil.SetStreamingAssetIncluded(relativeStreamingAssetPath, true);

			TestUtil.AssertFileAndFoldersAssetsDontExist(ExcludedStreamingAssetsPath, relativeStreamingAssetPath, "Excluded path parts should not exist for asset");
			TestUtil.AssertFileAndFoldersAssetsExist(StreamingAssetsPath, relativeStreamingAssetPath, "Included path parts should exist for asset");
		}

		[TestCase("NestedFolder", "Assset1.txt", "Assset2.txt")]
		[TestCase("NestedFolder1/NestedFolder2", "Assset1.txt", "Assset2.txt")]
		[TestCase("NestedFolder1/NestedFolder2/NestedFolder3", "Assset1.txt", "Assset2.txt")]
		public void FolderExclusionExcludesAssets(string relativeStreamingAssetFolder, string asset1Name, string asset2Name)
		{
			string asset1RelativePath = relativeStreamingAssetFolder + "/" + asset1Name;
			string asset2RelativePath = relativeStreamingAssetFolder + "/" + asset2Name;

			TestUtil.CreateAsset(StreamingAssetsPath + "/" + asset1RelativePath);
			TestUtil.CreateAsset(StreamingAssetsPath + "/" + asset2RelativePath);

			StreamingAssetsUtil.SetStreamingAssetIncluded(relativeStreamingAssetFolder, false);

			TestUtil.AssertFileAndFoldersAssetsExist(ExcludedStreamingAssetsPath, asset1RelativePath, "Excluded path parts should exist for asset1");
			TestUtil.AssertFileAndFoldersAssetsExist(ExcludedStreamingAssetsPath, asset2RelativePath, "Excluded path parts should exist for asset2");
			TestUtil.AssertFileAndFoldersAssetsDontExist(StreamingAssetsPath, asset1RelativePath, "Included path parts should not exist for asset1");
			TestUtil.AssertFileAndFoldersAssetsDontExist(StreamingAssetsPath, asset2RelativePath, "Included path parts should not exist for asset2");

			StreamingAssetsUtil.SetStreamingAssetIncluded(relativeStreamingAssetFolder, true);

			TestUtil.AssertFileAndFoldersAssetsDontExist(ExcludedStreamingAssetsPath, asset1RelativePath, "Excluded path parts should not exist for asset1");
			TestUtil.AssertFileAndFoldersAssetsDontExist(ExcludedStreamingAssetsPath, asset2RelativePath, "Excluded path parts should not exist for asset2");
			TestUtil.AssertFileAndFoldersAssetsExist(StreamingAssetsPath, asset1RelativePath, "Included path parts should exist for asset1");
			TestUtil.AssertFileAndFoldersAssetsExist(StreamingAssetsPath, asset2RelativePath, "Included path parts should exist for asset2");
		}

		[TestCase("NestedFolder", "Assset1.txt", "Assset2.txt")]
		[TestCase("NestedFolder1/NestedFolder2", "Assset1.txt", "Assset2.txt")]
		[TestCase("NestedFolder1/NestedFolder2/NestedFolder3", "Assset1.txt", "Assset2.txt")]
		public void AllAssetExclusionExcludesFolder(string relativeStreamingAssetFolder, string asset1Name, string asset2Name)
		{
			string asset1RelativePath = relativeStreamingAssetFolder + "/" + asset1Name;
			string asset2RelativePath = relativeStreamingAssetFolder + "/" + asset2Name;

			string asset1Path = StreamingAssetsPath + "/" + asset1RelativePath;
			string asset2Path = StreamingAssetsPath + "/" + asset2RelativePath;

			TestUtil.CreateAsset(asset1Path);
			TestUtil.CreateAsset(asset2Path);

			StreamingAssetsUtil.SetStreamingAssetIncluded(asset1RelativePath, false);
			StreamingAssetsUtil.SetStreamingAssetIncluded(asset2RelativePath, false);

			TestUtil.AssertFileAndFoldersAssetsExist(ExcludedStreamingAssetsPath, asset1RelativePath, "Excluded path parts should exist for asset1");
			TestUtil.AssertFileAndFoldersAssetsExist(ExcludedStreamingAssetsPath, asset2RelativePath, "Excluded path parts should exist for asset2");
			TestUtil.AssertFileAndFoldersAssetsDontExist(StreamingAssetsPath, asset1RelativePath, "Included path parts should not exist for asset1");
			TestUtil.AssertFileAndFoldersAssetsDontExist(StreamingAssetsPath, asset2RelativePath, "Included path parts should not exist for asset2");

			StreamingAssetsUtil.SetStreamingAssetIncluded(asset1RelativePath, true);
			StreamingAssetsUtil.SetStreamingAssetIncluded(asset2RelativePath, true);

			TestUtil.AssertFileAndFoldersAssetsDontExist(ExcludedStreamingAssetsPath, asset1RelativePath, "Excluded path parts should not exist for asset1");
			TestUtil.AssertFileAndFoldersAssetsDontExist(ExcludedStreamingAssetsPath, asset2RelativePath, "Excluded path parts should not exist for asset2");
			TestUtil.AssertFileAndFoldersAssetsExist(StreamingAssetsPath, asset1RelativePath, "Included path parts should exist for asset1");
			TestUtil.AssertFileAndFoldersAssetsExist(StreamingAssetsPath, asset2RelativePath, "Included path parts should exist for asset2");
		}
	}
}
