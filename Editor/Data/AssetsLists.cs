using System;
using System.Collections.Generic;
using UnityEngine;

namespace Elevator89.BuildPresetter.Data
{
	[Serializable]
	public class AssetsLists
	{
		[SerializeField]
		public List<string> Folders = new List<string>();

		[SerializeField]
		public List<string> Files = new List<string>();
	}
}