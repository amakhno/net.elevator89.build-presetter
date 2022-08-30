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

		private const string BaseAssetsFolder = "Assets";

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
					return false;

				return TryMoveAsset(excludedAssetPath, includedAssetPath);
			}
			else
			{
				if (!IsAssetIncluded(includedAssetPath, excludedAssetPath))
					return false;

				return TryMoveAsset(includedAssetPath, excludedAssetPath);
			}
		}

		public static bool IsAssetIncluded(string includedAssetPath, string excludedAssetPath)
		{
			if (!ValidateExcludableAsset(includedAssetPath, excludedAssetPath, out bool assetIsValidAndIncluded))
				throw new InvalidOperationException(string.Format("Asset {0} is in invalid excludable state", includedAssetPath));

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

				if (GetAllAssetsPaths(folderPath).Any())
					return;

				AssetDatabase.DeleteAsset(folderPath);
			}
		}

		public static bool DoesAssetExist(string path)
		{
			return !string.IsNullOrEmpty(AssetDatabase.AssetPathToGUID(path, AssetPathToGUIDOptions.OnlyExistingAssets));
		}

		public static IEnumerable<string> GetAllAssetsRelativePaths(string searchInFolder)
		{
			foreach (string fullPath in GetAllAssetsPaths(searchInFolder))
			{
				int indexOfBaseFolderPathStart = fullPath.IndexOf(searchInFolder);
				Debug.AssertFormat(indexOfBaseFolderPathStart == 0, "Asset path must start from {0}, but it is {1}", searchInFolder, fullPath);
				yield return fullPath.Substring(searchInFolder.Length + 1);
			}
		}

		public static IEnumerable<string> GetAllAssetsPaths(string searchInFolder)
		{
			return AssetDatabase
				.FindAssets("", new string[] { searchInFolder })
				.Select(AssetDatabase.GUIDToAssetPath);
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
	}
}
