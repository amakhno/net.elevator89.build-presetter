using System;
using UnityEditor;
using UnityEngine;

namespace Elevator89.BuildPresetter
{
	[Serializable]
	public class AndroidOptions
	{
		[SerializeField]
		public AndroidSdkVersions MinAndriodSdkVersion;

		[SerializeField]
		public AndroidBuildSystem AndroidBuildSystem;

		[SerializeField]
		public string KeystoreFilePath;

		[SerializeField]
		public string KeystorePassword;

		[SerializeField]
		public string KeyaliasName;

		[SerializeField]
		public string KeyaliasPassword;
	}
}