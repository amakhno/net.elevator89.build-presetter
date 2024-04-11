using Elevator89.BuildPresetter.Data;
using Elevator89.BuildPresetter.FolderHierarchy;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

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
			AssetDatabase.Refresh();

			BuildTarget buildTarget = EditorUserBuildSettings.activeBuildTarget;
			BuildTargetGroup buildTargetGroup = EditorUserBuildSettings.selectedBuildTargetGroup;
			NamedBuildTarget namedBuildTarget = NamedBuildTarget.FromBuildTargetGroup(buildTargetGroup);

			preset.AppName = PlayerSettings.productName;
			preset.AppId = PlayerSettings.applicationIdentifier;
			preset.BuildTarget = buildTarget;
			preset.BuildTargetGroup = buildTargetGroup;
			preset.ScriptingImplementation = PlayerSettings.GetScriptingBackend(buildTargetGroup);
			preset.IncrementalIl2CppBuild = PlayerSettings.GetIncrementalIl2CppBuild(buildTargetGroup);
			preset.Il2CppCompilerConfiguration = PlayerSettings.GetIl2CppCompilerConfiguration(namedBuildTarget);
			preset.Il2CppCodeGeneration = PlayerSettings.GetIl2CppCodeGeneration(namedBuildTarget);
			preset.DefineSymbols = PlayerSettings.GetScriptingDefineSymbolsForGroup(buildTargetGroup);
			preset.IncludedScenes = EditorBuildSettings.scenes.Select(scene => scene.path).ToList();
			preset.InitialSceneIndex = 0;

			preset.IncludedResources = Util.FindResourcesFolders(searchIncluded: true, searchExcluded: false).ToList();

			HierarchyAsset streamingAssetsHierarchy = StreamingAssetsUtil.GetStreamingAssetsHierarchyByCurrentSetup();
			preset.IncludedStreamingAssets = StreamingAssetsUtil.GetAssetsListsByHierarchy(streamingAssetsHierarchy);

			preset.DevelopmentBuild = EditorUserBuildSettings.development;
			preset.ConnectWithProfiler = EditorUserBuildSettings.connectProfiler;
			preset.UseIncrementalGC = PlayerSettings.gcIncremental;
		}

		public static void SetCurrent(Preset preset)
		{
			AssetDatabase.Refresh();

			PlayerSettings.productName = preset.AppName;
			Texture2D icon = AssetDatabase.LoadAssetAtPath<Texture2D>(preset.AppIconPath);
			PlayerSettings.SetIconsForTargetGroup(BuildTargetGroup.Standalone,
				new[] { icon, icon, icon, icon, icon, icon, icon, icon });
			PlayerSettings.applicationIdentifier = preset.AppId;

			PlayerSettings.SetScriptingBackend(preset.BuildTargetGroup, preset.ScriptingImplementation);
			PlayerSettings.SetIncrementalIl2CppBuild(preset.BuildTargetGroup, preset.IncrementalIl2CppBuild);
			PlayerSettings.SetIl2CppCompilerConfiguration(preset.BuildTargetGroup, preset.Il2CppCompilerConfiguration);
			NamedBuildTarget namedBuildTarget = NamedBuildTarget.FromBuildTargetGroup(preset.BuildTargetGroup);
			PlayerSettings.SetIl2CppCodeGeneration(namedBuildTarget, preset.Il2CppCodeGeneration);

			PlayerSettings.SetScriptingDefineSymbolsForGroup(preset.BuildTargetGroup, preset.DefineSymbols);

			List<EditorBuildSettingsScene> enabledScenes = preset.IncludedScenes.Select(path => new EditorBuildSettingsScene(path, true)).ToList();
			EditorBuildSettingsScene initialScene = enabledScenes[preset.InitialSceneIndex];
			enabledScenes.RemoveAt(preset.InitialSceneIndex);
			enabledScenes.Insert(0, initialScene);

			EditorBuildSettings.scenes = enabledScenes.ToArray();

			foreach (string resourcesFolderPath in Util.FindResourcesFolders(searchIncluded: true, searchExcluded: true))
				Util.SetResourcesEnabled(resourcesFolderPath, preset.IncludedResources.Contains(resourcesFolderPath));

			HierarchyAsset streamingAssetsHierarchy = StreamingAssetsUtil.GetStreamingAssetsHierarchyByLists(preset.IncludedStreamingAssets);
			StreamingAssetsUtil.ApplyStreamingAssetsHierarchyToCurrentSetup(streamingAssetsHierarchy);

			AssetDatabase.Refresh();

			EditorUserBuildSettings.development = preset.DevelopmentBuild;

			PlayerSettings.gcIncremental = preset.UseIncrementalGC;
		}
	}
}
