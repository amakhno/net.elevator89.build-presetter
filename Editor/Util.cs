using Elevator89.BuildPresetter.Data;
using Elevator89.BuildPresetter.FolderHierarchy;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Elevator89.BuildPresetter
{
	public static class Util
	{
		private const string ResourcesFolderName = "Resources";
		private const string ExcludedResourcesFolderName = "~Resources"; // Do not use "Resources~". Assets with "~" in the END are not parts of AssetDatabase.

		private const string StreamingAssetsFolder = "Assets/StreamingAssets";
		private const string ExcludedStreamingAssetsFolder = "Assets/~StreamingAssets";

		private const string BaseAssetsFolder = "Assets";

		public static StreamingAssetsOptions GetStreamingAssetsOptionsByHierarchy(HierarchyAsset hierarchy)
		{
			StreamingAssetsOptions streamingAssetsOptions = new StreamingAssetsOptions();
			FillStreamingAssetsOptionsByHierarchy(hierarchy, "", streamingAssetsOptions);
			return streamingAssetsOptions;
		}

		private static void FillStreamingAssetsOptionsByHierarchy(HierarchyAsset hierarchyAsset, string accumulatedPath, StreamingAssetsOptions streamingAssetsOptions)
		{
			string assetPath = string.IsNullOrEmpty(accumulatedPath)
				? hierarchyAsset.Name
				: accumulatedPath + "/" + hierarchyAsset.Name;

			if (hierarchyAsset.Children.Count == 0)
			{
				if (hierarchyAsset.IsIncluded)
					streamingAssetsOptions.IndividuallyIncludedAssets.Add(assetPath);
			}
			else
			{
				if (hierarchyAsset.IsIncluded)
					streamingAssetsOptions.RecursivelyIncludedFolders.Add(assetPath);
				else
					foreach (HierarchyAsset child in hierarchyAsset.Children)
						FillStreamingAssetsOptionsByHierarchy(child, assetPath, streamingAssetsOptions);
			}
		}

		public static HierarchyAsset GetHierarchyByStreamingAssetsOptions(StreamingAssetsOptions streamingAssetsOptions)
		{
			HierarchyAsset hierarchy = Hierarchy.BuildFrom(Util.FindStreamingAssetFiles(searchIncluded: true, searchExcluded: true));
			MarkHierarchyByStreamingAssetsOptions(hierarchy, "", streamingAssetsOptions);
			return hierarchy;
		}

		private static void MarkHierarchyByStreamingAssetsOptions(HierarchyAsset hierarchyAsset, string accumulatedPath, StreamingAssetsOptions streamingAssetsOptions)
		{
			string assetPath = string.IsNullOrEmpty(accumulatedPath)
				? hierarchyAsset.Name
				: accumulatedPath + "/" + hierarchyAsset.Name;

			if (hierarchyAsset.Children.Count == 0)
				hierarchyAsset.IsIncluded = streamingAssetsOptions.IndividuallyIncludedAssets.Contains(assetPath);
			else
			{
				bool folderIsRecursivelyIncluded = streamingAssetsOptions.RecursivelyIncludedFolders.Contains(assetPath);
				hierarchyAsset.IsIncluded = folderIsRecursivelyIncluded;

				foreach (HierarchyAsset child in hierarchyAsset.Children)
					if (folderIsRecursivelyIncluded)
						MarkHierarchyIncluded(child);
					else
						MarkHierarchyByStreamingAssetsOptions(child, assetPath, streamingAssetsOptions);
			}
		}

		private static void MarkHierarchyIncluded(HierarchyAsset hierarchyAsset)
		{
			hierarchyAsset.IsIncluded = true;
			foreach (HierarchyAsset child in hierarchyAsset.Children)
				MarkHierarchyIncluded(child);
		}

		public static HierarchyAsset GetHierarchyByCurrentSetup()
		{
			HierarchyAsset hierarchy = Hierarchy.BuildFrom(Util.FindStreamingAssetFiles(searchIncluded: true, searchExcluded: true));
			MarkHierarchyByCurrentSetup(hierarchy, "");
			return hierarchy;
		}

		private static void MarkHierarchyByCurrentSetup(HierarchyAsset hierarchyAsset, string accumulatedPath)
		{
			string assetPath = string.IsNullOrEmpty(accumulatedPath)
				? hierarchyAsset.Name
				: accumulatedPath + "/" + hierarchyAsset.Name;

			if (hierarchyAsset.Children.Count == 0)
			{
				hierarchyAsset.IsIncluded = Util.IsStreamingAssetIncluded(assetPath);
			}
			else
			{
				bool folderIsRecursivelyIncluded = true;
				foreach (HierarchyAsset child in hierarchyAsset.Children)
				{
					MarkHierarchyByCurrentSetup(child, assetPath);
					if (!child.IsIncluded)
						folderIsRecursivelyIncluded = false;
				}

				hierarchyAsset.IsIncluded = folderIsRecursivelyIncluded;
			}
		}

		public static void ApplyHierarchyToCurrentSetup(HierarchyAsset hierarchyAsset)
		{
			ApplyHierarchyToCurrentSetup(hierarchyAsset, "");
		}

		private static void ApplyHierarchyToCurrentSetup(HierarchyAsset hierarchyAsset, string accumulatedPath)
		{
			string assetPath = string.IsNullOrEmpty(accumulatedPath)
				? hierarchyAsset.Name
				: accumulatedPath + "/" + hierarchyAsset.Name;

			if (hierarchyAsset.Children.Count == 0)
			{
				Util.SetStreamingAssetIncluded(assetPath, hierarchyAsset.IsIncluded);
			}
			else
			{
				Util.SetStreamingAssetIncluded(assetPath, hierarchyAsset.IsIncluded);

				if (!hierarchyAsset.IsIncluded)
					foreach (HierarchyAsset child in hierarchyAsset.Children)
						ApplyHierarchyToCurrentSetup(child, assetPath);
			}
		}

		internal static HierarchyAsset BuildAllStreamingAssetsHierarchy()
		{
			return Hierarchy.BuildFrom(FindStreamingAssetFiles(searchIncluded: true, searchExcluded: true));
		}

		internal static HierarchyAsset BuildAssetsHierarchy(string searchInFolder)
		{
			IEnumerable<string> allAssets = FindAllFilesInFolder(searchInFolder);
			return Hierarchy.BuildFrom(allAssets);
		}

		public static IEnumerable<string> FindAllStreamingAssetsFolders(string searchInFolder, bool searchIncluded, bool searchExcluded)
		{
			IEnumerable<string> streamingAssets = Enumerable.Empty<string>();

			if (searchIncluded && AssetDatabase.IsValidFolder(searchInFolder))
				streamingAssets = streamingAssets.Union(FindFolders(searchInFolder));

			string excludedSearchInFolder = ToExcludedStreamingAssetPath(searchInFolder);

			if (searchExcluded && AssetDatabase.IsValidFolder(excludedSearchInFolder))
				streamingAssets = streamingAssets.Union(FindFolders(excludedSearchInFolder).Select(ToIncludedStreamingAssetPath));

			return streamingAssets.OrderBy(path => path);
		}

		public static bool IsFolderRecursivelyIncluded(string folderPath)
		{
			// True of there are no excluded assets in folder
			return FindStreamingAssetFiles(folderPath, searchIncluded: true, searchExcluded: false).Any()
				&& !FindStreamingAssetFiles(folderPath, searchIncluded: false, searchExcluded: true).Any();
		}

		public static IEnumerable<string> FindStreamingAssetFiles(bool searchIncluded, bool searchExcluded)
		{
			return FindStreamingAssetFiles(StreamingAssetsFolder, searchIncluded, searchExcluded);
		}

		public static IEnumerable<string> FindStreamingAssetFiles(string searchInFolder, bool searchIncluded, bool searchExcluded)
		{
			IEnumerable<string> streamingAssets = Enumerable.Empty<string>();

			if (searchIncluded && AssetDatabase.IsValidFolder(searchInFolder))
				streamingAssets = streamingAssets.Concat(FindAllFilesInFolder(searchInFolder));

			string excludedSearchInFolder = ToExcludedStreamingAssetPath(searchInFolder);

			if (searchExcluded && AssetDatabase.IsValidFolder(excludedSearchInFolder))
				streamingAssets = streamingAssets.Concat(FindAllFilesInFolder(excludedSearchInFolder).Select(ToIncludedStreamingAssetPath));

			return streamingAssets;
		}

		public static bool SetStreamingAssetIncluded(string streamingAssetPath, bool include)
		{
			return SetAssetIncluded(streamingAssetPath, ToExcludedStreamingAssetPath(streamingAssetPath), include);
		}

		public static bool IsStreamingAssetIncluded(string streamingAssetPath)
		{
			return IsAssetIncluded(streamingAssetPath, ToExcludedStreamingAssetPath(streamingAssetPath));
		}

		public static IEnumerable<string> FindResourcesFolders(bool searchIncluded, bool searchExcluded)
		{
			IEnumerable<string> resourcesFolders = Enumerable.Empty<string>();

			if (searchIncluded)
				resourcesFolders = resourcesFolders.Concat(FindFolders(ResourcesFolderName, BaseAssetsFolder));

			if (searchExcluded)
				resourcesFolders = resourcesFolders.Concat(FindFolders(ExcludedResourcesFolderName, BaseAssetsFolder).Select(ToIncludedResourcesPath));

			return resourcesFolders;
		}

		public static bool SetResourcesEnabled(string resourcesPath, bool enable)
		{
			return SetAssetIncluded(resourcesPath, ToExcludedResourcesPath(resourcesPath), enable);
		}

		public static bool AreResourcesEnabled(string resourcesPath)
		{
			return IsAssetIncluded(resourcesPath, ToExcludedResourcesPath(resourcesPath));
		}

		public static bool SetAssetIncluded(string includedAssetPath, string excludedAssetPath, bool include)
		{
			if (include)
			{
				if (IsAssetIncluded(includedAssetPath, excludedAssetPath))
				{
					Debug.LogErrorFormat("Asset {0} is already included", includedAssetPath);
					return false;
				}

				return TryMoveAsset(excludedAssetPath, includedAssetPath);
			}
			else
			{
				if (!IsAssetIncluded(includedAssetPath, excludedAssetPath))
				{
					Debug.LogErrorFormat("Asset {0} is already excluded", includedAssetPath);
					return false;
				}

				return TryMoveAsset(includedAssetPath, excludedAssetPath);
			}
		}

		public static bool IsAssetIncluded(string includedAssetPath, string excludedAssetPath)
		{
			if (!ValidateExcludableAsset(includedAssetPath, excludedAssetPath, out bool assetIsValidAndIncluded))
				throw new System.InvalidOperationException(string.Format("Asset {0} is in invalid excludable state", includedAssetPath));

			return assetIsValidAndIncluded;
		}

		private static bool ValidateExcludableAsset(string includedAssetPath, string excludedAssetPath, out bool assetIsValidAndIncluded)
		{
			bool includedAssetExists = DoesAssetExist(includedAssetPath);
			bool excludedAssetExists = DoesAssetExist(excludedAssetPath);
			assetIsValidAndIncluded = false;

			if (includedAssetExists && excludedAssetExists)
			{
				Debug.LogErrorFormat("Both included and excluded {0} exist", includedAssetPath);
				return false;
			}

			if (!includedAssetExists && !excludedAssetExists)
			{
				Debug.LogErrorFormat("Neither included nor excluded {0} exist", includedAssetPath);
				return false;
			}

			assetIsValidAndIncluded = includedAssetExists;
			return true;
		}

		public static bool TryMoveAsset(string source, string destination)
		{
			if (!DoesAssetExist(source))
			{
				Debug.LogError($"Asset {source} does not exist");
				return false;
			}

			if (DoesAssetExist(destination))
			{
				Debug.LogError($"Asset {destination} does already exist");
				return false;
			}

			EnsurePath(destination);

			string moveValidationResult = AssetDatabase.ValidateMoveAsset(source, destination);
			if (!string.IsNullOrEmpty(moveValidationResult))
			{
				Debug.LogError($"Asset move from {source} to {destination} failed with result {moveValidationResult}");
				return false;
			}

			AssetDatabase.MoveAsset(source, destination);

			CleanEmptyFolders(source);

			return true;
		}

		public static void EnsurePath(string assetPath)
		{
			if (!assetPath.StartsWith(BaseAssetsFolder))
				throw new ArgumentException($"Asset path {assetPath} doesn't start from {BaseAssetsFolder}", nameof(assetPath));

			string currentFolderPath = "";
			string assetPathTail = assetPath;

			while (true)
			{
				int firstSlashIndex = assetPathTail.IndexOf("/", StringComparison.InvariantCultureIgnoreCase);

				if (firstSlashIndex == -1)
					return;

				string nextFolderName = assetPathTail.Substring(0, firstSlashIndex);
				string nextFolderPath = currentFolderPath.Length > 0 ? currentFolderPath + "/" + nextFolderName : nextFolderName;

				assetPathTail = assetPathTail.Substring(firstSlashIndex + 1, assetPathTail.Length - firstSlashIndex - 1);

				if (!AssetDatabase.IsValidFolder(nextFolderPath))
					AssetDatabase.CreateFolder(currentFolderPath, nextFolderName);

				currentFolderPath = nextFolderPath;
			}
		}

		public static void CleanEmptyFolders(string assetPath)
		{
			if (!assetPath.StartsWith(BaseAssetsFolder))
				throw new ArgumentException($"Asset path {assetPath} doesn't start from {BaseAssetsFolder}", nameof(assetPath));

			string folderPath = assetPath;

			while (true)
			{
				int lastSlashIndex = folderPath.LastIndexOf("/", StringComparison.InvariantCultureIgnoreCase);

				if (lastSlashIndex == -1)
					return;

				folderPath = folderPath.Substring(0, lastSlashIndex);
				Debug.Assert(AssetDatabase.IsValidFolder(folderPath));

				if (FindAllFilesInFolder(folderPath).Any())
					return;

				AssetDatabase.DeleteAsset(folderPath);
			}
		}

		public static bool DoesAssetExist(string path)
		{
			return !string.IsNullOrEmpty(AssetDatabase.AssetPathToGUID(path, AssetPathToGUIDOptions.OnlyExistingAssets));
		}

		public static IEnumerable<string> FindAllFilesInFolder(string searchInFolder)
		{
			return AssetDatabase
				.FindAssets("", new string[] { searchInFolder })
				.Select(AssetDatabase.GUIDToAssetPath);
		}

		public static IEnumerable<string> FindFolders(string searchInFolder)
		{
			return FindFolders(null, searchInFolder);
		}

		public static IEnumerable<string> FindFolders(string folderName, string searchInFolder)
		{
			string filter = string.IsNullOrWhiteSpace(folderName) ? " t:folder" : $"\"{folderName}\" t:folder";

			return AssetDatabase
				.FindAssets(filter, new string[] { searchInFolder })
				.Select(AssetDatabase.GUIDToAssetPath)
				.Where(path => path.EndsWith($"/{folderName}"));
		}

		public static IEnumerable<string> FindAllScenes()
		{
			return AssetDatabase
				.FindAssets("t:sceneAsset", new string[] { BaseAssetsFolder })
				.Select(AssetDatabase.GUIDToAssetPath);
		}

		private static string ToIncludedResourcesPath(string path)
		{
			return path.Replace($"/{ExcludedResourcesFolderName}", $"/{ResourcesFolderName}");
		}

		private static string ToExcludedResourcesPath(string path)
		{
			return path.Replace($"/{ResourcesFolderName}", $"/{ExcludedResourcesFolderName}");
		}

		private static string ToIncludedStreamingAssetPath(string path)
		{
			return path.Replace(ExcludedStreamingAssetsFolder, StreamingAssetsFolder);
		}

		private static string ToExcludedStreamingAssetPath(string path)
		{
			return path.Replace(StreamingAssetsFolder, ExcludedStreamingAssetsFolder);
		}
	}
}
