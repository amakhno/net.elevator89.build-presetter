using System;
using System.Collections.Generic;
using UnityEngine;

namespace Elevator89.BuildPresetter
{
	[Serializable]
	public class PresetList
	{
		public Preset GetPreset(string platformName)
		{
			return AvailablePresets.Find(settings => settings.Name == platformName);
		}

		public void SetPreset(Preset configuration)
		{
			int index = AvailablePresets.FindIndex(s => s.Name == configuration.Name);
			if (index == -1)
			{
				AvailablePresets.Add(configuration);
			}
			else
			{
				AvailablePresets[index] = configuration;
			}
		}

		public void AddPreset(Preset configuration)
		{
			AvailablePresets.Add(configuration);
		}

		public void RemovePreset(string configurationName)
		{
			AvailablePresets.RemoveAll(conf => conf.Name == configurationName);
		}

		[SerializeField]
		public string ActivePresetName;

		[SerializeField]
		public List<Preset> AvailablePresets;
	}
}