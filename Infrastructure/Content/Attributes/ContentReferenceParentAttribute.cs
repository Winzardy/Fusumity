using System;
using System.Diagnostics;
using Fusumity.Attributes;

namespace Content
{
	[Conditional("UNITY_EDITOR")]
	public class ContentReferenceParentAttribute : Attribute, IAttributeConvertible
	{
		public Type Type { get; }
		public string TypeName { get; }
		public bool InlineEditor { get; }

		public bool Dropdown { get; }

		public ContentReferenceParentAttribute(Type type, bool inlineEditor = true, bool dropdown = false)
		{
			Type = type;
			InlineEditor = inlineEditor;
			Dropdown = dropdown;
		}

		public ContentReferenceParentAttribute(string typeName, bool inlineEditor = true, bool dropdown = false)
		{
			TypeName = typeName;
			InlineEditor = inlineEditor;
			Dropdown = dropdown;
		}

		public Attribute Convert()
			=> new ContentReferenceAttribute(Type, InlineEditor, Dropdown)
			{
				TypeName = TypeName,
			};
	}

	[Conditional("UNITY_EDITOR")]
	public class ContentReferenceDrawerSettingsParentAttribute : Attribute, IAttributeConvertible
	{
		public bool InlineEditor { get; }
		public bool Dropdown { get; }

		public ContentReferenceDrawerSettingsParentAttribute(bool inlineEditor = true, bool dropdown = false)
		{
			InlineEditor = inlineEditor;
			Dropdown = dropdown;
		}

		public Attribute Convert() => new ContentReferenceDrawerSettingsAttribute(InlineEditor, Dropdown);
	}
}
