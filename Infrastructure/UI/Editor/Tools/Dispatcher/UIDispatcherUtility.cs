using System;

namespace UI.Editor
{
	public class UIDispatcherUtility
	{
		public static string Clear(Type type, string postfix)
		{
			const string prefix = "UI";

			var name = type.Name
				.Replace(postfix, string.Empty);

			if (name.StartsWith(prefix))
				name = name[2..];

			return name;
		}

		public static Type ResolveArgsType(Type type)
		{
			var baseType = type?.BaseType;

			if (baseType is not {IsGenericType: true})
				return null;

			var arguments = baseType.GetGenericArguments();

			Type argsType = null;
			if (baseType.Name.Contains("ViewBound"))
			{
				argsType = arguments[0];
			}
			else
			{
				if (arguments.Length < 2)
					return null;

				argsType = arguments[1];
			}

			if (argsType == typeof(EmptyArgs))
				return null;

			return argsType;
		}
	}
}
