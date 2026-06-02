using System;

namespace Content.ScriptableObjects.Editor
{
	public readonly struct ContentValidationContext
	{
		public readonly ContentScriptableObject source;
		public readonly string path;

		public readonly object value;
		public readonly Type valueType;

		public readonly bool canBeEmpty;

		public ContentValidationContext(object value,
			Type valueType,
			string path,
			ContentScriptableObject source,
			bool canBeEmpty)
		{
			this.value = value;
			this.valueType = valueType;
			this.path = path;
			this.source = source;
			this.canBeEmpty = canBeEmpty;
		}
	}

	public interface IContentValueValidator
	{
		bool Validate(in ContentValidationContext context, out string message);
	}

	public abstract class ContentValueValidator<T> : IContentValueValidator
	{
		public bool Validate(in ContentValidationContext context, out string message)
		{
			if (context.value is T value)
				return OnValidate(value, in context, out message);

			message = null;
			return true;
		}

		protected abstract bool OnValidate(T value, in ContentValidationContext context, out string message);
	}
}
