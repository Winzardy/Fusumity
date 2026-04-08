using System;

namespace Audio.Editor
{
	[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
	public sealed class AudioEventRequestPlayButtonAttribute : Attribute
	{
	}
}
