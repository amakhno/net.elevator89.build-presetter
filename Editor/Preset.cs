using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Elevator89.BuildPresetter
{
	[Serializable]
	public class Preset
	{
		[SerializeField]
		public string Name;

		[SerializeField]
		public string AppId;

		[SerializeField]
		public string AppName;

		[SerializeField]
		public string AppIconPath;

		[SerializeField]
		public BuildTargetGroup BuildTargetGroup;

		[SerializeField]
		public BuildTarget BuildTarget;

		[SerializeField]
		public ScriptingImplementation ScriptingImplementation;

		[SerializeField]
		public bool IncrementalIl2CppBuild;

		[SerializeField]
		public List<string> IncludedScenes = new List<string>();

		[SerializeField]
		public int InitialSceneIndex;

		[SerializeField]
		public List<string> IncludedResources = new List<string>();

		[SerializeField]
		public List<string> IncludedStreamingAssets = new List<string>();

		[SerializeField]
		public AndroidOptions AndroidOptions;

		[SerializeField]
		public string BuildDirectory;

		[SerializeField]
		public string BuildFileName;

		[SerializeField]
		public string DefineSymbols;

		[SerializeField]
		public bool DevelopmentBuild;

		[SerializeField]
		public bool ServerBuild;

		[SerializeField]
		public bool ConnectWithProfiler;

		[SerializeField]
		public bool UseIncrementalGC;
	}
}

