﻿using System.Collections.Generic;
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

		public static IEnumerable<string> FindStreamingAssets(bool searchIncluded, bool searchExcluded)
		{
			IEnumerable<string> streamingAssets = Enumerable.Empty<string>();

			if (searchIncluded)
				streamingAssets = streamingAssets.Concat(FindAllFilesInFolder(StreamingAssetsFolder));

			if (searchExcluded)
				streamingAssets = streamingAssets.Concat(FindAllFilesInFolder(ExcludedStreamingAssetsFolder).Select(ToIncludedStreamingAssetPath));

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
				resourcesFolders = resourcesFolders.Concat(FindAllFolders(ResourcesFolderName, BaseAssetsFolder));

			if (searchExcluded)
				resourcesFolders = resourcesFolders.Concat(FindAllFolders(ExcludedResourcesFolderName, BaseAssetsFolder).Select(ToIncludedResourcesPath));

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

			string moveValidationResult = AssetDatabase.ValidateMoveAsset(source, destination);
			if (!string.IsNullOrEmpty(moveValidationResult))
			{
				Debug.LogError($"Asset move from {source} to {destination} failed with result {moveValidationResult}");
				return false;
			}

			AssetDatabase.MoveAsset(source, destination);
			return true;
		}

		private static bool DoesAssetExist(string path)
		{
			return !string.IsNullOrEmpty(AssetDatabase.AssetPathToGUID(path, AssetPathToGUIDOptions.OnlyExistingAssets));
		}

		public static IEnumerable<string> FindAllFilesInFolder(string searchInFolder)
		{
			return AssetDatabase
				.FindAssets("", new string[] { searchInFolder })
				.Select(AssetDatabase.GUIDToAssetPath);
		}

		public static IEnumerable<string> FindAllFolders(string folderName, string searchInFolder)
		{
			return AssetDatabase
				.FindAssets($"\"{folderName}\" t:folder", new string[] { searchInFolder })
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
