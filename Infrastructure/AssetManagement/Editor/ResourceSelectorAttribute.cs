using System;
using UnityEngine;

namespace AssetManagement
{
	public class ResourceSelectorAttribute : PropertyAttribute
	{
		public Type Type { get; }

		public ResourceSelectorAttribute(Type type)
		{
			Type = type;
		}
	}
}
