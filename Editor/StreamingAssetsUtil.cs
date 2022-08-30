using Elevator89.BuildPresetter.Data;
using Elevator89.BuildPresetter.FolderHierarchy;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;

namespace Elevator89.BuildPresetter
{
	public static class StreamingAssetsUtil
	{
		private const string StreamingAssetsFolder = "Assets/StreamingAssets";
		private const string ExcludedStreamingAssetsFolder = "Assets/~StreamingAssets";

		public static AssetsLists GetAssetsListsByHierarchy(HierarchyAsset hierarchy)
		{
			AssetsLists lists = new AssetsLists();

			foreach (HierarchyAsset child in hierarchy.Children) // Hierarchy root is not processed, as its folder may not exist
				FillAssetsListsByHierarchy(child, "", lists);

			return lists;
		}

		private static void FillAssetsListsByHierarchy(HierarchyAsset hierarchyAsset, string accumulatedPath, AssetsLists lists)
		{
			string assetPath = string.IsNullOrEmpty(accumulatedPath)
				? hierarchyAsset.Name
				: accumulatedPath + "/" + hierarchyAsset.Name;

			if (hierarchyAsset.Children.Count == 0)
			{
				if (hierarchyAsset.IsIncluded)
					lists.Files.Add(assetPath);
			}
			else
			{
				if (hierarchyAsset.IsIncluded)
					lists.Folders.Add(assetPath);
				else
					foreach (HierarchyAsset child in hierarchyAsset.Children)
						FillAssetsListsByHierarchy(child, assetPath, lists);
			}
		}

		public static HierarchyAsset GetStreamingAssetsHierarchyByLists(AssetsLists lists)
		{
			HierarchyAsset hierarchy = BuildStreamingAssetsHierarchyWithVirtualRoot();

			foreach (HierarchyAsset child in hierarchy.Children) // Hierarchy root is not processed, as its folder may not exist
				MarkAssetsHierarchyByLists(child, "", lists);

			return hierarchy;
		}

		private static void MarkAssetsHierarchyByLists(HierarchyAsset hierarchyAsset, string accumulatedPath, AssetsLists lists)
		{
			string assetPath = string.IsNullOrEmpty(accumulatedPath)
				? hierarchyAsset.Name
				: accumulatedPath + "/" + hierarchyAsset.Name;

			if (hierarchyAsset.Children.Count == 0)
				hierarchyAsset.IsIncluded = lists.Files.Contains(assetPath);
			else
			{
				bool folderIsIncluded = lists.Folders.Contains(assetPath);
				hierarchyAsset.IsIncluded = folderIsIncluded;

				foreach (HierarchyAsset child in hierarchyAsset.Children)
					if (folderIsIncluded)
						MarkAssetsHierarchyWithValue(child, included: true);
					else
						MarkAssetsHierarchyByLists(child, assetPath, lists);
			}
		}

		private static void MarkAssetsHierarchyWithValue(HierarchyAsset hierarchyAsset, bool included)
		{
			hierarchyAsset.IsIncluded = included;

			foreach (HierarchyAsset child in hierarchyAsset.Children)
				MarkAssetsHierarchyWithValue(child, included);
		}

		public static HierarchyAsset GetStreamingAssetsHierarchyByCurrentSetup()
		{
			HierarchyAsset hierarchy = BuildStreamingAssetsHierarchyWithVirtualRoot();

			foreach (HierarchyAsset child in hierarchy.Children) // Hierarchy root is not processed, as its folder may not exist
				MarkStreamingAssetsHierarchyByCurrentSetup(child, "");

			return hierarchy;
		}

		private static void MarkStreamingAssetsHierarchyByCurrentSetup(HierarchyAsset hierarchyAsset, string accumulatedPath)
		{
			string assetPath = string.IsNullOrEmpty(accumulatedPath)
				? hierarchyAsset.Name
				: accumulatedPath + "/" + hierarchyAsset.Name;

			if (hierarchyAsset.Children.Count == 0)
			{
				hierarchyAsset.IsIncluded = IsStreamingAssetIncluded(assetPath);
			}
			else
			{
				bool folderIsRecursivelyIncluded = true;
				foreach (HierarchyAsset child in hierarchyAsset.Children)
				{
					MarkStreamingAssetsHierarchyByCurrentSetup(child, assetPath);
					if (!child.IsIncluded)
						folderIsRecursivelyIncluded = false;
				}

				hierarchyAsset.IsIncluded = folderIsRecursivelyIncluded;
			}
		}

		public static void ApplyStreamingAssetsHierarchyToCurrentSetup(HierarchyAsset hierarchy)
		{
			foreach (HierarchyAsset child in hierarchy.Children) // Hierarchy root is not processed, as its folder may not exist
				ApplyStreamingAssetsHierarchyToCurrentSetup(child, "");
		}

		private static void ApplyStreamingAssetsHierarchyToCurrentSetup(HierarchyAsset hierarchyAsset, string accumulatedPath)
		{
			string assetRelativePath = string.IsNullOrEmpty(accumulatedPath)
				? hierarchyAsset.Name
				: accumulatedPath + "/" + hierarchyAsset.Name;

			if (hierarchyAsset.Children.Count == 0)
			{
				SetStreamingAssetIncluded(assetRelativePath, hierarchyAsset.IsIncluded);
			}
			else
			{
				if (hierarchyAsset.IsIncluded)
					SetStreamingAssetIncluded(assetRelativePath, true);
				else
					foreach (HierarchyAsset child in hierarchyAsset.Children)
						ApplyStreamingAssetsHierarchyToCurrentSetup(child, assetRelativePath);
			}
		}

		private static HierarchyAsset BuildStreamingAssetsHierarchyWithVirtualRoot()
		{
			return Hierarchy.BuildFrom(
				GetStreamingAssets(searchIncluded: true, searchExcluded: true)
				.Select(relativePath => "StreamingAssets/" + relativePath)); // Use virtual root folder to unite all streaming assets
		}

		public static IEnumerable<string> GetStreamingAssets(bool searchIncluded, bool searchExcluded)
		{
			IEnumerable<string> streamingAssets = Enumerable.Empty<string>();

			if (searchIncluded && AssetDatabase.IsValidFolder(StreamingAssetsFolder))
				streamingAssets = streamingAssets.Concat(Util.GetAllAssetsRelativePaths(StreamingAssetsFolder));

			if (searchExcluded && AssetDatabase.IsValidFolder(ExcludedStreamingAssetsFolder))
				streamingAssets = streamingAssets.Concat(Util.GetAllAssetsRelativePaths(ExcludedStreamingAssetsFolder));

			return streamingAssets;
		}

		public static bool SetStreamingAssetIncluded(string streamingAssetRelativePath, bool include)
		{
			return Util.SetAssetIncluded(
				StreamingAssetsFolder + "/" + streamingAssetRelativePath,
				ExcludedStreamingAssetsFolder + "/" + streamingAssetRelativePath,
				include);
		}

		public static bool IsStreamingAssetIncluded(string streamingAssetRelativePath)
		{
			return Util.IsAssetIncluded(
				StreamingAssetsFolder + "/" + streamingAssetRelativePath,
				ExcludedStreamingAssetsFolder + "/" + streamingAssetRelativePath);
		}
	}
}
