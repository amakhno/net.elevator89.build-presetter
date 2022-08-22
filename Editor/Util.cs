using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Elevator89.BuildPresetter
{
	public static class Util
	{
		private const string ALL_FILES = "*.*";
		private const string DisabledResourcesFolderPrefix = "~";
		private const string ResourcesFolderName = "Resources";
		private const string BaseAssetsFolder = "Assets";
		// Info about ~ at the end of folder name look at https://docs.unity3d.com/Manual/SpecialFolders.html
		private const string HiddenAssetsFolder = "Assets/Hidden~";

		public static IEnumerable<string> FindAllResourcesFolders(bool includeDisabled = true)
		{
			return AssetDatabase
				.FindAssets($"resources \"{ResourcesFolderName}\" t:defaultAsset", new string[] { BaseAssetsFolder })
				.Select(AssetDatabase.GUIDToAssetPath)
				.Where(path => path.EndsWith($"/{ResourcesFolderName}") || includeDisabled && path.EndsWith($"/{DisabledResourcesFolderPrefix}{ResourcesFolderName}"))
				.Select(GetEnabledResourcesPath);
		}

		public static IEnumerable<string> FindAllScenes()
		{
			return AssetDatabase
				.FindAssets("t:sceneAsset", new string[] { BaseAssetsFolder })
				.Select(AssetDatabase.GUIDToAssetPath)
				.ToArray();
		}

		public static void DisableResourcesPath(string resourcesPath)
		{
			string enabledResourcesPath = GetEnabledResourcesPath(resourcesPath);
			string disabledResourcesPath = GetDisabledResourcesPath(resourcesPath);

			MoveDatabaseAsset(enabledResourcesPath, disabledResourcesPath);
		}

		public static void EnableResourcesPath(string resourcesPath)
		{
			string enabledResourcesPath = GetEnabledResourcesPath(resourcesPath);
			string disabledResourcesPath = GetDisabledResourcesPath(resourcesPath);

			MoveDatabaseAsset(disabledResourcesPath, enabledResourcesPath);
		}

		public static IEnumerable<string> FindAllDisabledAssets()
		{
			if (!Directory.Exists(HiddenAssetsFolder))
			{
				return Enumerable.Empty<string>();
			}
			return Directory.GetFiles(HiddenAssetsFolder, ALL_FILES, SearchOption.AllDirectories)
				   .Select(f => f.Replace(HiddenAssetsFolder + Path.DirectorySeparatorChar, string.Empty)
								 //we are using relative paths in resources with / as directory separators. 
								 // Directory.GetFiles on windows return paths with \ as directory separators
								 // This code would not make any difference on other than windows platforms
								 .Replace(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
		}

		public static bool IsAssetDisabled(string assetPath)
		{
			string hiddenPath = CombinePath(HiddenAssetsFolder, assetPath);
			// unity does not store hidden path in its database so we only could use .net way
			return File.Exists(hiddenPath);
		}

		public static void DisableAsset(string assetPath)
		{
			string hiddenPath = CombinePath(HiddenAssetsFolder, assetPath);
			if (!assetPath.StartsWith(BaseAssetsFolder))
			{
				assetPath = CombinePath(BaseAssetsFolder, assetPath);
			}
			if (!MoveNotDatabaseAsset(assetPath, hiddenPath))
			{
				Debug.LogWarning($"Asset at {assetPath} was not hidden to {hiddenPath}");
			}
		}

		public static void EnableAsset(string assetPath)
		{
			string hiddenPath = CombinePath(HiddenAssetsFolder, assetPath);
			if (!assetPath.StartsWith(BaseAssetsFolder))
			{
				assetPath = CombinePath(BaseAssetsFolder, assetPath);
			}
			if (!MoveNotDatabaseAsset(hiddenPath, assetPath))
			{
				Debug.LogWarning($"Asset at {assetPath} was not revealed from {hiddenPath}");
			}
		}

		private static string CombinePath(params string[] folders)
		{
			folders = folders.Where(f => !string.IsNullOrEmpty(f)).ToArray();
			if (folders.Length == 1)
			{
				return folders.First();
			}
			return string.Join(separator: Path.AltDirectorySeparatorChar.ToString(), folders);
		}

		private static void MoveDatabaseAsset(string oldPath, string newPath)
		{
			string moveValidationResult = AssetDatabase.ValidateMoveAsset(oldPath, newPath);

			if (string.IsNullOrEmpty(moveValidationResult))
			{
				AssetDatabase.MoveAsset(oldPath, newPath);
			}
			else
			{
				Debug.Log($"Asset move from {oldPath} to {newPath} failed with result {moveValidationResult}");
			}
		}

		private static bool MoveNotDatabaseAsset(string oldPath, string newPath)
		{
			if (Directory.Exists(oldPath))
			{
				Directory.Move(oldPath, newPath);
			}
			else if (File.Exists(oldPath))
			{
				string newDir = Path.GetDirectoryName(newPath);
				if (!Directory.Exists(newDir))
				{
					Directory.CreateDirectory(newDir);
				}
				File.Move(oldPath, newPath);
			}
			else
			{
				return false;
			}
			return true;
		}

		private static string GetEnabledResourcesPath(string path)
		{
			return path.Replace($"/{DisabledResourcesFolderPrefix}{ResourcesFolderName}", $"/{ResourcesFolderName}");
		}

		private static string GetDisabledResourcesPath(string path)
		{
			return path.Replace($"/{ResourcesFolderName}", $"/{DisabledResourcesFolderPrefix}{ResourcesFolderName}");
		}

		public static IEnumerable<T> GetAllAssetsOfType<T>(string pathPart = null) where T : Object
		{
			string[] allAssetsPaths = string.IsNullOrWhiteSpace(pathPart) ? AssetDatabase.GetAllAssetPaths() : AssetDatabase.FindAssets(pathPart);

			if (!string.IsNullOrWhiteSpace(pathPart))
				allAssetsPaths = allAssetsPaths.Where(path => path.Contains(pathPart)).ToArray();

			return allAssetsPaths.Select(a => AssetDatabase.LoadAssetAtPath<T>(a)).Where(a => a != null);
		}

		public static IEnumerable<GameObject> FindObjectsWithTag(UnityEngine.SceneManagement.Scene scene, string tag)
		{
			return scene.GetRootGameObjects()
				.SelectMany(
					rootGameObject => rootGameObject
					.GetComponentsInChildren<Transform>(includeInactive: true)
					.Where(c => c.tag == tag).Select(c => c.gameObject));
		}
	}
}
