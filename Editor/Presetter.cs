using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Elevator89.BuildPresetter
{
	public static class Presetter
	{
		public static Preset GetCurrent()
		{
			Preset buildConfiguration = new Preset()
			{
				Name = "_Current",
				BuildDirectory = null,
				BuildFileName = null,
			};

			FillFromCurrent(buildConfiguration);
			return buildConfiguration;
		}

		public static void FillFromCurrent(Preset buildConfiguration)
		{
			BuildTarget buildTarget = EditorUserBuildSettings.activeBuildTarget;
			BuildTargetGroup buildTargetGroup = EditorUserBuildSettings.selectedBuildTargetGroup;

			buildConfiguration.AppName = PlayerSettings.productName;
			buildConfiguration.AppId = PlayerSettings.applicationIdentifier;
			buildConfiguration.BuildTarget = buildTarget;
			buildConfiguration.BuildTargetGroup = buildTargetGroup;
			buildConfiguration.ScriptingImplementation = PlayerSettings.GetScriptingBackend(buildTargetGroup);
			buildConfiguration.IncrementalIl2CppBuild = PlayerSettings.GetIncrementalIl2CppBuild(buildTargetGroup);

			buildConfiguration.DefineSymbols = PlayerSettings.GetScriptingDefineSymbolsForGroup(buildTargetGroup);
			buildConfiguration.EnabledScenes = EditorBuildSettings.scenes.Select(scene => scene.path).ToList();
			buildConfiguration.InitialSceneIndex = 0;

			buildConfiguration.EnabledResources = Util.FindAllResourcesFolders(includeDisabled: false).ToList();

			buildConfiguration.DisabledAssets = Util.FindAllDisabledAssets().ToList();

			buildConfiguration.AndroidOptions = new AndroidOptions()
			{
				MinAndriodSdkVersion = PlayerSettings.Android.minSdkVersion,
				KeystoreFilePath = PlayerSettings.Android.keystoreName,
				KeystorePassword = PlayerSettings.Android.keystorePass,
				KeyaliasName = PlayerSettings.Android.keyaliasName,
				KeyaliasPassword = PlayerSettings.Android.keyaliasPass,
				AndroidBuildSystem = EditorUserBuildSettings.androidBuildSystem
			};

			buildConfiguration.DevelopmentBuild = EditorUserBuildSettings.development;
			buildConfiguration.ServerBuild = EditorUserBuildSettings.enableHeadlessMode;
			buildConfiguration.ConnectWithProfiler = EditorUserBuildSettings.connectProfiler;
			buildConfiguration.UseIncrementalGC = PlayerSettings.gcIncremental;
		}

		public static void SetCurrent(Preset buildConfiguration)
		{
			PlayerSettings.productName = buildConfiguration.AppName;
			Texture2D icon = AssetDatabase.LoadAssetAtPath<Texture2D>(buildConfiguration.AppIconPath);
			PlayerSettings.SetIconsForTargetGroup(BuildTargetGroup.Standalone,
				new[] { icon, icon, icon, icon, icon, icon, icon, icon });
			PlayerSettings.applicationIdentifier = buildConfiguration.AppId;

			PlayerSettings.SetScriptingBackend(buildConfiguration.BuildTargetGroup, buildConfiguration.ScriptingImplementation);
			PlayerSettings.SetIncrementalIl2CppBuild(buildConfiguration.BuildTargetGroup, buildConfiguration.IncrementalIl2CppBuild);

			PlayerSettings.SetScriptingDefineSymbolsForGroup(buildConfiguration.BuildTargetGroup, buildConfiguration.DefineSymbols);

			List<EditorBuildSettingsScene> enabledScenes = buildConfiguration.EnabledScenes.Select(path => new EditorBuildSettingsScene(path, true)).ToList();
			EditorBuildSettingsScene initialScene = enabledScenes[buildConfiguration.InitialSceneIndex];
			enabledScenes.RemoveAt(buildConfiguration.InitialSceneIndex);
			enabledScenes.Insert(0, initialScene);

			EditorBuildSettings.scenes = enabledScenes.ToArray();

			foreach (string assetPath in Util.FindAllDisabledAssets().Concat(buildConfiguration.DisabledAssets))
			{
				if (Util.IsAssetDisabled(assetPath))
				{
					Util.EnableAsset(assetPath);
				}
				else
				{
					Util.DisableAsset(assetPath);
				}
			}

			foreach (string resourcesFolderPath in Util.FindAllResourcesFolders())
			{
				if (buildConfiguration.EnabledResources.Contains(resourcesFolderPath))
					Util.EnableResourcesPath(resourcesFolderPath);
				else
					Util.DisableResourcesPath(resourcesFolderPath);
			}
			AssetDatabase.Refresh();

			PlayerSettings.Android.minSdkVersion = buildConfiguration.AndroidOptions.MinAndriodSdkVersion;
			PlayerSettings.Android.keystoreName = buildConfiguration.AndroidOptions.KeystoreFilePath;
			PlayerSettings.Android.keystorePass = buildConfiguration.AndroidOptions.KeystorePassword;
			PlayerSettings.Android.keyaliasName = buildConfiguration.AndroidOptions.KeyaliasName;
			PlayerSettings.Android.keyaliasPass = buildConfiguration.AndroidOptions.KeyaliasPassword;
			EditorUserBuildSettings.androidBuildSystem = buildConfiguration.AndroidOptions.AndroidBuildSystem;

			EditorUserBuildSettings.development = buildConfiguration.DevelopmentBuild;
			EditorUserBuildSettings.enableHeadlessMode = buildConfiguration.ServerBuild;

			PlayerSettings.gcIncremental = buildConfiguration.UseIncrementalGC;
		}
	}
}
