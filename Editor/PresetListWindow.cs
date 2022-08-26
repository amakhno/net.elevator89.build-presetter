using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Elevator89.BuildPresetter.Data;

namespace Elevator89.BuildPresetter
{
	public class PresetListWindow : EditorWindow
	{
		private PresetList _presets;

		private Vector2 _scrollPos;
		private Vector2 _scrollPosScenes;
		private Vector2 _scrollPosResources;
		private Vector2 _scrollPosStreamingAssets;

		[MenuItem("Build/Build with Acive Preset", false, 100)]
		private static void BuildWithAcivePreset()
		{
			PresetList presets = PresetList.Load();
			Builder.Build(presets.GetPreset(presets.ActivePresetName), false, null);
		}

		[MenuItem("Build/Build and Run with Acive Preset", false, 100)]
		private static void BuildAndRunWithAciveConfiguration()
		{
			PresetList presets = PresetList.Load();
			Builder.Build(presets.GetPreset(presets.ActivePresetName), true, null);
		}

		[MenuItem("Build/Presets...", false, 200)]
		private static void OpenSettings()
		{
			PresetListWindow window = GetWindow<PresetListWindow>(false, "Build Presets", true);
			window.minSize = new Vector2(400.0f, 380.0f);
			window.Show();
		}

		private void OnEnable()
		{
			_presets = PresetList.Load();
		}

		private void OnGUI()
		{
			_scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);

			GUILayout.Space(10);

			string[] presetNames = _presets.AvailablePresets.Select(prst => prst.Name).ToArray();

			int selectedPresetIndex = Array.IndexOf(presetNames, _presets.ActivePresetName);
			selectedPresetIndex = EditorGUILayout.Popup("Preset", selectedPresetIndex, presetNames);

			Preset selectedPreset = _presets.GetPreset(presetNames[selectedPresetIndex]);
			_presets.ActivePresetName = selectedPreset.Name;

			BuildMode buildMode = ShowBuildPresetsGuiAndReturnBuildPress(selectedPreset, selectedPreset.AppName);

			GUILayout.Space(5);

			GUILayout.BeginHorizontal(GUILayout.ExpandHeight(false));
			{
				if (GUILayout.Button("Reload All", GUILayout.ExpandWidth(false)))
				{
					_presets = PresetList.Load();
				}
				if (GUILayout.Button("Save All", GUILayout.ExpandWidth(false)))
				{
					PresetList.Save(_presets);
				}
				GUILayout.FlexibleSpace();
			}
			GUILayout.EndHorizontal();
			EditorGUILayout.EndScrollView();

			switch (buildMode)
			{
				case BuildMode.Build:
					Builder.Build(selectedPreset, false, null);
					break;
				case BuildMode.BuildAndRun:
					Builder.Build(selectedPreset, true, null);
					break;
				case BuildMode.DoNotBuild:
				default:
					break;
			}
		}

		private BuildMode ShowBuildPresetsGuiAndReturnBuildPress(Preset preset, string header)
		{
			BuildMode buildMode = BuildMode.DoNotBuild;

			EditorGUILayout.BeginVertical("box");
			{
				GUILayout.BeginHorizontal(GUILayout.ExpandHeight(false));
				{
					EditorGUILayout.LabelField(header);
					GUILayout.FlexibleSpace();
					if (GUILayout.Button("Clone", GUILayout.ExpandWidth(false), GUILayout.MaxHeight(15)))
					{
						string[] presetNames = _presets.AvailablePresets.Select(preset => preset.Name).ToArray();

						Preset clone = new Preset
						{
							Name = GenerateName(preset.Name, presetNames),
							AppId = preset.AppId,
							AppName = preset.AppName,
							AppIconPath = preset.AppIconPath,
							BuildDirectory = preset.BuildDirectory,
							BuildFileName = preset.BuildFileName,
							BuildTarget = preset.BuildTarget,
							BuildTargetGroup = preset.BuildTargetGroup,
							ConnectWithProfiler = preset.ConnectWithProfiler,
							DefineSymbols = preset.DefineSymbols,
							DevelopmentBuild = preset.DevelopmentBuild,
							ServerBuild = preset.ServerBuild,
							AndroidOptions = new AndroidOptions()
							{
								MinAndriodSdkVersion = preset.AndroidOptions.MinAndriodSdkVersion,
								AndroidBuildSystem = preset.AndroidOptions.AndroidBuildSystem,
								KeystoreFilePath = preset.AndroidOptions.KeystoreFilePath,
								KeystorePassword = preset.AndroidOptions.KeystorePassword,
								KeyaliasName = preset.AndroidOptions.KeyaliasName,
								KeyaliasPassword = preset.AndroidOptions.KeyaliasPassword
							},
							IncludedResources = new List<string>(preset.IncludedResources),
							IncludedStreamingAssets = new List<string>(preset.IncludedStreamingAssets),
							IncludedScenes = new List<string>(preset.IncludedScenes),
							InitialSceneIndex = preset.InitialSceneIndex,
							UseIncrementalGC = preset.UseIncrementalGC,
						};

						_presets.AddPreset(clone);
						_presets.ActivePresetName = clone.Name;
					}
					if (GUILayout.Button("Delete", GUILayout.ExpandWidth(false), GUILayout.MaxHeight(15)))
					{
						_presets.RemovePreset(preset.Name);
						_presets.ActivePresetName = _presets.AvailablePresets[0].Name;
					}

				}
				GUILayout.EndHorizontal();

				GUILayout.Space(5);

				bool isActivePreset = preset.Name == _presets.ActivePresetName;

				preset.Name = EditorGUILayout.TextField("Preset Name", preset.Name);
				if (isActivePreset)
				{
					_presets.ActivePresetName = preset.Name;
				}

				preset.AppId = EditorGUILayout.TextField("App ID", preset.AppId);
				preset.AppName = EditorGUILayout.TextField("App name", preset.AppName);
				preset.AppIconPath = EditorGUILayout.TextField("App icon path", preset.AppIconPath);
				GUI.Box(new Rect(10, 150, 200, 50), new GUIContent("            <--- App Icon", AssetDatabase.LoadAssetAtPath<Texture2D>(preset.AppIconPath)));
				GUILayout.Space(70);

				GUILayout.Label("Scenes to include:");
				_scrollPosScenes = EditorGUILayout.BeginScrollView(_scrollPosScenes, EditorStyles.helpBox);
				{
					string[] allSccenes = Util.FindAllScenes().ToArray();
					HashSet<string> enabledSceneAssetPaths = new HashSet<string>(preset.IncludedScenes.Where(scenePath => allSccenes.Contains(scenePath)));

					foreach (string scenePath in allSccenes)
					{
						if (EditorGUILayout.ToggleLeft(scenePath, enabledSceneAssetPaths.Contains(scenePath)))
							enabledSceneAssetPaths.Add(scenePath);
						else
							enabledSceneAssetPaths.Remove(scenePath);
					}

					preset.IncludedScenes = enabledSceneAssetPaths.ToList();
				}
				EditorGUILayout.EndScrollView();

				preset.InitialSceneIndex = EditorGUILayout.Popup("Initial Scene", preset.InitialSceneIndex, preset.IncludedScenes.Select(scenePath => scenePath.Replace('/', '\u2215')).ToArray());

				GUILayout.Space(5);

				GUILayout.Label("Resources to include:");
				_scrollPosResources = EditorGUILayout.BeginScrollView(_scrollPosResources, EditorStyles.helpBox);
				{
					string[] allResourcesFolders = Util.FindResourcesFolders(searchIncluded: true, searchExcluded: true).ToArray();
					HashSet<string> enabledResourcesFolders = new HashSet<string>(preset.IncludedResources.Where(path => allResourcesFolders.Contains(path)));

					foreach (string resourcesFolder in allResourcesFolders)
					{
						if (EditorGUILayout.ToggleLeft(resourcesFolder, enabledResourcesFolders.Contains(resourcesFolder)))
							enabledResourcesFolders.Add(resourcesFolder);
						else
							enabledResourcesFolders.Remove(resourcesFolder);
					}
					preset.IncludedResources = enabledResourcesFolders.ToList();
				}
				EditorGUILayout.EndScrollView();

				GUILayout.Space(5);

				GUILayout.Label("Streaming assets to include:");
				_scrollPosStreamingAssets = EditorGUILayout.BeginScrollView(_scrollPosStreamingAssets, EditorStyles.helpBox);
				{
					string[] allStreamingAssets = Util.FindStreamingAssets(searchIncluded: true, searchExcluded: true).ToArray();
					HashSet<string> enabledStreamingAssets = new HashSet<string>(preset.IncludedStreamingAssets.Where(path => allStreamingAssets.Contains(path)));

					foreach (string streamingAsset in allStreamingAssets)
					{
						if (EditorGUILayout.ToggleLeft(streamingAsset, enabledStreamingAssets.Contains(streamingAsset)))
							enabledStreamingAssets.Add(streamingAsset);
						else
							enabledStreamingAssets.Remove(streamingAsset);
					}
					preset.IncludedStreamingAssets = enabledStreamingAssets.ToList();
				}
				EditorGUILayout.EndScrollView();

				GUILayout.Space(5);

				preset.BuildTargetGroup = (BuildTargetGroup)EditorGUILayout.EnumPopup("Target group", preset.BuildTargetGroup);
				preset.BuildTarget = (BuildTarget)EditorGUILayout.EnumPopup("Target", preset.BuildTarget);
				preset.ScriptingImplementation = (ScriptingImplementation)EditorGUILayout.EnumPopup("ScriptingImplementation", preset.ScriptingImplementation);

				GUI.enabled = preset.BuildTargetGroup == BuildTargetGroup.Android;
				EditorGUILayout.BeginVertical(EditorStyles.helpBox);
				{
					preset.AndroidOptions.MinAndriodSdkVersion = (AndroidSdkVersions)EditorGUILayout.EnumPopup("Min Andriod SDK version", preset.AndroidOptions.MinAndriodSdkVersion);
					preset.AndroidOptions.AndroidBuildSystem = (AndroidBuildSystem)EditorGUILayout.EnumPopup("Build System", preset.AndroidOptions.AndroidBuildSystem);
					preset.AndroidOptions.KeystoreFilePath = EditorGUILayout.TextField("Keystore File Path", preset.AndroidOptions.KeystoreFilePath);
					preset.AndroidOptions.KeystorePassword = EditorGUILayout.TextField("Keystore Password", preset.AndroidOptions.KeystorePassword);
					preset.AndroidOptions.KeyaliasName = EditorGUILayout.TextField("Keyalias Name", preset.AndroidOptions.KeyaliasName);
					preset.AndroidOptions.KeyaliasPassword = EditorGUILayout.TextField("Keyalias Password", preset.AndroidOptions.KeyaliasPassword);
				}
				EditorGUILayout.EndVertical();
				GUI.enabled = true;

				preset.BuildDirectory = EditorGUILayout.TextField("Build directory", preset.BuildDirectory);
				preset.BuildFileName = EditorGUILayout.TextField("Build file name", preset.BuildFileName);
				preset.DefineSymbols = EditorGUILayout.TextField("Define symbols", preset.DefineSymbols);

				preset.IncrementalIl2CppBuild = EditorGUILayout.Toggle("Incremental IL2CPP Build", preset.IncrementalIl2CppBuild);
				preset.DevelopmentBuild = EditorGUILayout.Toggle("Development Build", preset.DevelopmentBuild);
				preset.ServerBuild = EditorGUILayout.Toggle("Server Build", preset.ServerBuild);
				preset.ConnectWithProfiler = EditorGUILayout.Toggle("Connect Profiler", preset.ConnectWithProfiler);

				preset.UseIncrementalGC = EditorGUILayout.Toggle("Use incremental GC", preset.UseIncrementalGC);

				GUILayout.BeginHorizontal(GUILayout.ExpandHeight(false));
				{
					if (GUILayout.Button("Fill from Settings", GUILayout.ExpandWidth(false)))
					{
						Presetter.FillFromCurrent(preset);
					}

					if (GUILayout.Button("Apply to Settings", GUILayout.ExpandWidth(false)))
					{
						Presetter.SetCurrent(preset);
					}

					GUILayout.FlexibleSpace();

					if (GUILayout.Button("Build", GUILayout.ExpandWidth(false)))
					{
						buildMode = BuildMode.Build;
					}

					if (GUILayout.Button("Build and Run", GUILayout.ExpandWidth(false)))
					{
						buildMode = BuildMode.BuildAndRun;
					}
				}
				GUILayout.EndHorizontal();
			}

			EditorGUILayout.EndVertical();

			return buildMode;
		}

		private static string GenerateName(string desiredName, string[] otherNames)
		{
			if (otherNames.Contains(desiredName))
			{
				int i = 1;
				string generatedName = desiredName + i;
				while (otherNames.Contains(generatedName))
				{
					i++;
					generatedName = desiredName + i;
				}
				return generatedName;
			}
			return desiredName;
		}
	}
}
