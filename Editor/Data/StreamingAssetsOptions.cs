using System;
using System.Collections.Generic;
using UnityEngine;

namespace Elevator89.BuildPresetter.Data
{
	[Serializable]
	public class StreamingAssetsOptions
	{
		[SerializeField]
		public List<string> RecursivelyIncludedFolders = new List<string>();

		[SerializeField]
		public List<string> IndividuallyIncludedAssets = new List<string>();
	}
}