using System.Collections.Generic;

namespace Elevator89.BuildPresetter.FolderHierarchy
{
	public class HierarchyAsset
	{
		public readonly string Name;
		public readonly List<HierarchyAsset> Children = new List<HierarchyAsset>();
		public bool IsIncluded = false;

		public HierarchyAsset(string name) : this(name, new List<HierarchyAsset>())
		{ }

		public HierarchyAsset(string name, List<HierarchyAsset> children)
		{
			Name = name;
			Children = children;
		}
	}
}
