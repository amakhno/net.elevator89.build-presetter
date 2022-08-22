using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Elevator89.BuildPresetter.Tests
{
	[TestFixture]
	public class UtilTests
	{
		const string AssetPath = "Assets/StreamingAssets/Configs/appsettings.json";

		[OneTimeSetUp]
		public void Init()
		{
			AssetDatabase.CreateFolder("Assets", "StreamingAssets");

			Directory.CreateDirectory("Assets/StreamingAssets/Configs");
			File.WriteAllText(AssetPath, "some text");
			AssetDatabase.Refresh();
		}

		[OneTimeTearDown]
		public void Cleanup()
		{
			try
			{
				File.Delete(AssetPath);
				AssetDatabase.Refresh();
				Directory.Delete("Assets/StreamingAssets/Configs");
				AssetDatabase.Refresh();

				AssetDatabase.DeleteAsset("Assets/StreamingAssets");
				AssetDatabase.Refresh();
			}
			catch (Exception ex)
			{
				throw ex;
			}
		}

		[Test]
		public void ExcludesAndIncludes()
		{
			Util.SetStreamingAssetIncluded(AssetPath, false);

			string[] excludedAssets = Util.FindStreamingAssets(searchIncluded: false, searchExcluded: true).ToArray();

			Assert.AreEqual(1, excludedAssets.Length);
			Assert.True(Util.IsStreamingAssetIncluded(AssetPath));

			Util.SetStreamingAssetIncluded(AssetPath, true);
			excludedAssets = Util.FindStreamingAssets(searchIncluded: false, searchExcluded: true).ToArray();
			Assert.AreEqual(0, excludedAssets.Length);
			Assert.False(Util.IsStreamingAssetIncluded(AssetPath));
		}
	}
}
