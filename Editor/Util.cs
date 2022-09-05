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
				resourcesFolders = resourcesFolders.Concat(FindFoldersPaths(ResourcesFolderName, BaseAssetsFolder));

			if (searchExcluded)
				resourcesFolders = resourcesFolders.Concat(FindFoldersPaths(ExcludedResourcesFolderName, BaseAssetsFolder).Select(ToIncludedResourcesPath));

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
			bool includedAssetIsAFolder = includedAssetExists && AssetDatabase.IsValidFolder(includedAssetPath);

			bool excludedAssetExists = DoesAssetExist(excludedAssetPath);
			bool excludedAssetIsAFolder = excludedAssetExists && AssetDatabase.IsValidFolder(excludedAssetPath);

			assetIsValidAndIncluded = false;

			if (!includedAssetExists && !excludedAssetExists)
			{
				Debug.LogErrorFormat("Neither included nor excluded {0} exist", includedAssetPath);
				return false;
			}

			// Is valid situation when both excluded and included folders exist, e.g. in case when not all child items are encluded
			if (includedAssetExists && excludedAssetExists && !(includedAssetIsAFolder && excludedAssetIsAFolder))
			{
				if (includedAssetIsAFolder != excludedAssetIsAFolder)
				{
					Debug.LogErrorFormat("Either included or excluded asset is a folder, while the other one is not", includedAssetPath);
					return false;
				}

				Debug.LogErrorFormat("Both included and excluded {0} exist", includedAssetPath);
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
				int firstSlashIndex = assetPathTail.IndexOf('/');

				if (firstSlashIndex == -1)
					return;

				string nextFolderName = assetPathTail.Substring(0, firstSlashIndex);
				string nextFolderPath = CombinePath(currentFolderPath, nextFolderName);

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
				int lastSlashIndex = folderPath.LastIndexOf('/');

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
			return FindAssetsPaths("", searchInFolder);
		}

		public static IEnumerable<string> FindFoldersPaths(string folderName, string searchInFolder)
		{
			string filter = string.IsNullOrWhiteSpace(folderName) ? " t:folder" : $"\"{folderName}\" t:folder";
			return FindAssetsPaths(filter, searchInFolder).Where(path => path.EndsWith($"/{folderName}"));
		}

		public static IEnumerable<string> FindAllScenesPaths()
		{
			return FindAssetsPaths("t:sceneAsset", BaseAssetsFolder);
		}

		public static string CombinePath(string part1, string part2)
		{
			if (part2.Length == 0)
				return part1;

			if (part1.Length == 0)
				return part2;

			if (part1[part1.Length - 1] == '/')
				return part1 + part2;

			return part1 + '/' + part2;
		}

		private static IEnumerable<string> FindAssetsPaths(string filter, string searchInFolder)
		{
			return AssetDatabase
				.FindAssets(filter, new string[] { searchInFolder })
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
