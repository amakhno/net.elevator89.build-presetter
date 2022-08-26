using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Elevator89.BuildPresetter.Data;
using Elevator89.BuildPresetter.FolderHierarchy;

namespace Elevator89.BuildPresetter
{
	public static class Presetter
	{
		public static Preset GetCurrent()
		{
			Preset emptyPreset = new Preset()
			{
				Name = "_Current",
				BuildDirectory = null,
				BuildFileName = null,
			};

			FillFromCurrent(emptyPreset);
			return emptyPreset;
		}

		public static void FillFromCurrent(Preset preset)
		{
			BuildTarget buildTarget = EditorUserBuildSettings.activeBuildTarget;
			BuildTargetGroup buildTargetGroup = EditorUserBuildSettings.selectedBuildTargetGroup;

			preset.AppName = PlayerSettings.productName;
			preset.AppId = PlayerSettings.applicationIdentifier;
			preset.BuildTarget = buildTarget;
			preset.BuildTargetGroup = buildTargetGroup;
			preset.ScriptingImplementation = PlayerSettings.GetScriptingBackend(buildTargetGroup);
			preset.IncrementalIl2CppBuild = PlayerSettings.GetIncrementalIl2CppBuild(buildTargetGroup);

			preset.DefineSymbols = PlayerSettings.GetScriptingDefineSymbolsForGroup(buildTargetGroup);
			preset.IncludedScenes = EditorBuildSettings.scenes.Select(scene => scene.path).ToList();
			preset.InitialSceneIndex = 0;

			preset.IncludedResources = Util.FindResourcesFolders(searchIncluded: true, searchExcluded: false).ToList();

			HierarchyAsset streamingAssetsHierarchy = Util.GetHierarchyByCurrentSetup();
			preset.StreamingAssetsOptions = Util.GetStreamingAssetsOptionsByHierarchy(streamingAssetsHierarchy);

			preset.AndroidOptions = new AndroidOptions()
			{
				MinAndriodSdkVersion = PlayerSettings.Android.minSdkVersion,
				KeystoreFilePath = PlayerSettings.Android.keystoreName,
				KeystorePassword = PlayerSettings.Android.keystorePass,
				KeyaliasName = PlayerSettings.Android.keyaliasName,
				KeyaliasPassword = PlayerSettings.Android.keyaliasPass,
				AndroidBuildSystem = EditorUserBuildSettings.androidBuildSystem
			};

			preset.DevelopmentBuild = EditorUserBuildSettings.development;
			preset.ServerBuild = EditorUserBuildSettings.enableHeadlessMode;
			preset.ConnectWithProfiler = EditorUserBuildSettings.connectProfiler;
			preset.UseIncrementalGC = PlayerSettings.gcIncremental;
		}

		public static void SetCurrent(Preset preset)
		{
			PlayerSettings.productName = preset.AppName;
			Texture2D icon = AssetDatabase.LoadAssetAtPath<Texture2D>(preset.AppIconPath);
			PlayerSettings.SetIconsForTargetGroup(BuildTargetGroup.Standalone,
				new[] { icon, icon, icon, icon, icon, icon, icon, icon });
			PlayerSettings.applicationIdentifier = preset.AppId;

			PlayerSettings.SetScriptingBackend(preset.BuildTargetGroup, preset.ScriptingImplementation);
			PlayerSettings.SetIncrementalIl2CppBuild(preset.BuildTargetGroup, preset.IncrementalIl2CppBuild);

			PlayerSettings.SetScriptingDefineSymbolsForGroup(preset.BuildTargetGroup, preset.DefineSymbols);

			List<EditorBuildSettingsScene> enabledScenes = preset.IncludedScenes.Select(path => new EditorBuildSettingsScene(path, true)).ToList();
			EditorBuildSettingsScene initialScene = enabledScenes[preset.InitialSceneIndex];
			enabledScenes.RemoveAt(preset.InitialSceneIndex);
			enabledScenes.Insert(0, initialScene);

			EditorBuildSettings.scenes = enabledScenes.ToArray();

			foreach (string resourcesFolderPath in Util.FindResourcesFolders(searchIncluded: true, searchExcluded: true))
				Util.SetResourcesEnabled(resourcesFolderPath, preset.IncludedResources.Contains(resourcesFolderPath));

			HierarchyAsset streamingAssetsHierarchy = Util.GetHierarchyByStreamingAssetsOptions(preset.StreamingAssetsOptions);
			Util.ApplyHierarchyToCurrentSetup(streamingAssetsHierarchy);

			foreach (string streamingAssetPath in Util.FindStreamingAssetFiles(searchIncluded: true, searchExcluded: true))
				Util.SetStreamingAssetIncluded(streamingAssetPath, preset.StreamingAssetsOptions.IndividuallyIncludedAssets.Contains(streamingAssetPath));

			AssetDatabase.Refresh();

			PlayerSettings.Android.minSdkVersion = preset.AndroidOptions.MinAndriodSdkVersion;
			PlayerSettings.Android.keystoreName = preset.AndroidOptions.KeystoreFilePath;
			PlayerSettings.Android.keystorePass = preset.AndroidOptions.KeystorePassword;
			PlayerSettings.Android.keyaliasName = preset.AndroidOptions.KeyaliasName;
			PlayerSettings.Android.keyaliasPass = preset.AndroidOptions.KeyaliasPassword;
			EditorUserBuildSettings.androidBuildSystem = preset.AndroidOptions.AndroidBuildSystem;

			EditorUserBuildSettings.development = preset.DevelopmentBuild;
			EditorUserBuildSettings.enableHeadlessMode = preset.ServerBuild;

			PlayerSettings.gcIncremental = preset.UseIncrementalGC;
		}
	}
}
