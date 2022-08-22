using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Elevator89.BuildPresetter
{
	[ExecuteInEditMode]
	public class Builder : EditorWindow
	{
		private const string PresetsFilePath = "../ProjectSettings/BuildPresets.json";

		[MenuItem("Build/Build with Acive Preset", false, 100)]
		private static void BuildWithAcivePreset()
		{
			PresetList presets = LoadPresetList();
			Build(presets.GetPreset(presets.ActivePresetName), false, null);
		}

		[MenuItem("Build/Build and Run with Acive Preset", false, 100)]
		private static void BuildAndRunWithAciveConfiguration()
		{
			PresetList presets = LoadPresetList();
			Build(presets.GetPreset(presets.ActivePresetName), true, null);
		}

		[MenuItem("Build/Presets...", false, 200)]
		private static void OpenSettings()
		{
			Builder window = GetWindow<Builder>(false, "Build Presets", true);
			window.minSize = new Vector2(400.0f, 380.0f);
			window.Show();
		}

		/// <summary>
		/// This method is called from CI system
		/// </summary>
		public static void BuildFromCommandLine()
		{
			string[] args = null;
			try
			{
				args = Environment.GetCommandLineArgs();
			}
			catch
			{
				throw new UnityException("Command line args not found");
			}

			if (args == null)
			{
				throw new UnityException("Commandline args unaccessable");
			}

			string buildPresetName = GetNamedArgument(args, "-buildPresetName");
			string buildDirectory = GetNamedArgument(args, "-buildDirectory");
			string version = GetNamedArgument(args, "-appVersion");

			PresetList presets = LoadPresetList();
			Preset preset = presets.GetPreset(buildPresetName);

			if (preset == null)
			{
				throw new UnityException("No build preset with name " + buildPresetName + " was found");
			}

			if (buildDirectory != null)
			{
				preset.BuildDirectory = buildDirectory;
			}

			Build(preset, false, version);
		}

		private static string GetNamedArgument(string[] args, string name)
		{
			for (int i = 0; i < args.Length; i++)
			{
				if (args[i] == name && args.Length > i + 1)
				{
					return args[i + 1];
				}
			}
			return null;
		}

		#region Settings GUI

		private PresetList _presets;

		private Vector2 _scrollPos;
		private Vector2 _scrollPosScenes;
		private Vector2 _scrollPosResources;

		private void OnEnable()
		{
			_presets = LoadPresetList();
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
					_presets = LoadPresetList();
				}
				if (GUILayout.Button("Save All", GUILayout.ExpandWidth(false)))
				{
					SavePresetList(_presets);
				}
				GUILayout.FlexibleSpace();
			}
			GUILayout.EndHorizontal();
			EditorGUILayout.EndScrollView();

			switch (buildMode)
			{
				case BuildMode.Build:
					Build(selectedPreset, false, null);
					break;
				case BuildMode.BuildAndRun:
					Build(selectedPreset, true, null);
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
							EnabledResources = new List<string>(preset.EnabledResources),
							EnabledScenes = new List<string>(preset.EnabledScenes),
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
					HashSet<string> enabledSceneAssetPaths = new HashSet<string>(preset.EnabledScenes.Where(scenePath => allSccenes.Contains(scenePath)));

					foreach (string scenePath in allSccenes)
					{
						if (EditorGUILayout.ToggleLeft(scenePath, enabledSceneAssetPaths.Contains(scenePath)))
							enabledSceneAssetPaths.Add(scenePath);
						else
							enabledSceneAssetPaths.Remove(scenePath);
					}

					preset.EnabledScenes = enabledSceneAssetPaths.ToList();
				}
				EditorGUILayout.EndScrollView();

				preset.InitialSceneIndex = EditorGUILayout.Popup("Initial Scene", preset.InitialSceneIndex, preset.EnabledScenes.Select(scenePath => scenePath.Replace('/', '\u2215')).ToArray());

				GUILayout.Space(5);

				GUILayout.Label("Resources to include:");
				_scrollPosResources = EditorGUILayout.BeginScrollView(_scrollPosResources, EditorStyles.helpBox);
				{
					string[] allResourcesFolders = Util.FindAllResourcesFolders().ToArray();
					HashSet<string> enabledResourcesFolders = new HashSet<string>(preset.EnabledResources.Where(path => allResourcesFolders.Contains(path)));

					foreach (string resourcesFolder in allResourcesFolders)
					{
						if (EditorGUILayout.ToggleLeft(resourcesFolder, enabledResourcesFolders.Contains(resourcesFolder)))
							enabledResourcesFolders.Add(resourcesFolder);
						else
							enabledResourcesFolders.Remove(resourcesFolder);
					}
					preset.EnabledResources = enabledResourcesFolders.ToList();
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

		#endregion Settings GUI

		private static void Build(Preset preset, bool run, string version)
		{
			if (preset.BuildDirectory == null)
			{
				throw new UnityException("Build directory is not set");
			}

			AddressableAssetSettings.CleanPlayerContent();
			AddressableAssetSettings.BuildPlayerContent();

			Preset previousPreset = Presetter.GetCurrent();

			Presetter.SetCurrent(preset);

			PlayerSettings.bundleVersion = string.IsNullOrWhiteSpace(version) ? "" : version;

			try
			{
				BuildInternal(preset, run);
			}
			finally
			{
				Presetter.SetCurrent(previousPreset);
			}
		}

		private static void BuildInternal(Preset preset, bool run)
		{
			EditorUserBuildSettings.SwitchActiveBuildTarget(preset.BuildTargetGroup, preset.BuildTarget);

			if (preset.BuildDirectory != null && !Directory.Exists(preset.BuildDirectory))
			{
				Directory.CreateDirectory(preset.BuildDirectory);
			}

			BuildOptions buildOptions = BuildOptions.None;
			if (preset.DevelopmentBuild)
				buildOptions = buildOptions | BuildOptions.Development;
			if (preset.ServerBuild)
				buildOptions = buildOptions | BuildOptions.EnableHeadlessMode;
			if (preset.ConnectWithProfiler)
				buildOptions = buildOptions | BuildOptions.ConnectWithProfiler;
			if (run)
				buildOptions = buildOptions | BuildOptions.AutoRunPlayer;

			List<string> enabledScenes = preset.EnabledScenes.ToList();
			string initialScene = enabledScenes[preset.InitialSceneIndex];
			enabledScenes.RemoveAt(preset.InitialSceneIndex);
			enabledScenes.Insert(0, initialScene);
			string buildPath = Path.Combine(preset.BuildDirectory, preset.BuildFileName);

			BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions()
			{
				target = preset.BuildTarget,
				targetGroup = preset.BuildTargetGroup,
				options = buildOptions,
				scenes = enabledScenes.ToArray(),
				locationPathName = buildPath
			};

			BuildReport buildReport = BuildPipeline.BuildPlayer(buildPlayerOptions);

			if (buildReport.summary.result != BuildResult.Succeeded)
			{
				string[] errorLog = buildReport.steps.SelectMany(
					step =>
						step.messages.Where(
							m => m.type == LogType.Assert || m.type == LogType.Error || m.type == LogType.Exception)
							.Select(m => m.content)).ToArray();

				throw new Exception("BuildPlayer failure: " + string.Join(Environment.NewLine, errorLog));
			}
		}

		private static PresetList LoadPresetList()
		{
			string presetListFullPath = Path.Combine(Application.dataPath, PresetsFilePath);

			if (!File.Exists(presetListFullPath))
			{
				Debug.LogError("No preset list file found at path: " + PresetsFilePath);
				return new PresetList();
			}

			string presetListJson = File.ReadAllText(presetListFullPath);

			try
			{
				return JsonUtility.FromJson<PresetList>(presetListJson);
			}
			catch (Exception)
			{
				Debug.LogError("Preset list asset " + presetListFullPath + " has bad format");
				return new PresetList();
			}
		}

		private static void SavePresetList(PresetList presetList)
		{
			string presetListJson = JsonUtility.ToJson(presetList, true);
			string presetListFullPath = Path.Combine(Application.dataPath, PresetsFilePath);
			File.WriteAllText(presetListFullPath, presetListJson);
		}
	}
}
