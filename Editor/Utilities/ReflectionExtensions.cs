using System;
using System.Collections.Generic;
using System.Reflection;

namespace Fusumity.Editor.Utilities
{
	public static class ReflectionExtensions
	{
		public const BindingFlags fieldBindingFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.GetField;
		public const BindingFlags internalFieldBindingFlags = BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.GetField;
		public const BindingFlags privateFieldBindingFlags = BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.GetField;

		public const BindingFlags methodBindingFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
		public const BindingFlags overridenMethodBindingFlags = BindingFlags.Instance | BindingFlags.DeclaredOnly;
		public const BindingFlags privateMethodBindingFlags = BindingFlags.NonPublic | BindingFlags.Instance;

		public const char pathSplitChar = '.';
		public const char arrayDataTerminator = ']';
		public const string arrayDataBeginner = "data[";

		private static readonly Dictionary<Type, Type[]> _assignableFrom = new Dictionary<Type, Type[]>();
		private static readonly Dictionary<Type, Type[]> _typesWithNull = new Dictionary<Type, Type[]>();
		private static readonly Dictionary<Type, Type[]> _typesWithoutNull = new Dictionary<Type, Type[]>();

		public static Type[] GetInheritorTypes(this Type baseType, bool insertNull = false)
		{
			Type[] inheritorTypes;
			if (insertNull)
			{
				if (_typesWithNull.TryGetValue(baseType, out inheritorTypes))
					return inheritorTypes;
			}
			else if (_typesWithoutNull.TryGetValue(baseType, out inheritorTypes))
				return inheritorTypes;

			if (!_assignableFrom.TryGetValue(baseType, out var typeArray))
			{
				var assemblies = AppDomain.CurrentDomain.GetAssemblies();
				var typeList = new List<Type>();
				for (int a = 0; a < assemblies.Length; a++)
				{
					var types = assemblies[a].GetTypes();
					for (int t = 0; t < types.Length; t++)
					{
						if (baseType.IsAssignableFrom(types[t]) && !types[t].IsInterface && !types[t].IsAbstract &&
						    !types[t].IsGenericType)
						{
							typeList.Add(types[t]);
						}
					}
				}

				typeArray = typeList.ToArray();
				_assignableFrom.Add(baseType, typeArray);
			}

			if (insertNull)
			{
				inheritorTypes = new Type[typeArray.Length + 1];
				Array.ConstrainedCopy(typeArray, 0, inheritorTypes, 1, typeArray.Length);
			}
			else
			{
				inheritorTypes = typeArray;
			}

			(insertNull ? _typesWithNull : _typesWithoutNull).Add(baseType, inheritorTypes);

			return inheritorTypes;
		}

		public static object GetObjectByLocalPath(object source, string objectPath)
		{
			var target = source;
			if (string.IsNullOrEmpty(objectPath))
				return target;

			var pathComponents = objectPath.Split(pathSplitChar);

			for (var p = 0; p < pathComponents.Length; p++)
			{
				var pathComponent = pathComponents[p];
				if (target is Array array)
				{
					if (p < pathComponents.Length - 1 && pathComponents[p + 1].StartsWith(arrayDataBeginner))
					{
						var index = int.Parse(pathComponents[++p].Replace(arrayDataBeginner, "").Replace($"{arrayDataTerminator}", ""));
						target = array.GetValue(index);
					}
				}
				else
				{
					var field = GetAnyField(target.GetType(), pathComponent);
					target = field.GetValue(target);
				}
			}

			return target;
		}

		public static Type GetTypeByLocalPath(object source, string propertyPath)
		{
			return GetObjectByLocalPath(source, propertyPath).GetType();
		}

		public static string GetParentPath(string propertyPath)
		{
			var parentPath = propertyPath;

			var removeIndex = parentPath.LastIndexOf(pathSplitChar);
			if (removeIndex >= 0)
			{
				parentPath = parentPath.Remove(removeIndex, propertyPath.Length - removeIndex);

				if (propertyPath[propertyPath.Length - 1] != arrayDataTerminator)
					return parentPath;

				// Remove "{field name}.Array"
				removeIndex = parentPath.LastIndexOf(pathSplitChar);
				parentPath = parentPath.Remove(removeIndex, parentPath.Length - removeIndex);

				removeIndex = parentPath.LastIndexOf(pathSplitChar);
				parentPath = removeIndex >= 0 ? parentPath.Remove(removeIndex, parentPath.Length - removeIndex) : "";
			}
			else
			{
				parentPath = "";
			}
			return parentPath;
		}

		public static FieldInfo GetAnyField(this Type type, string fieldName)
		{
			var field = type.GetField(fieldName, fieldBindingFlags);
			while (field == null)
			{
				type = type.BaseType;
				field = type.GetField(fieldName, privateFieldBindingFlags);
			}

			return field;
		}

		public static MethodInfo GetAnyMethod_WithoutArguments(this Type type, string methodName)
		{
			var methodInfo = type.GetMethod(methodName, methodBindingFlags, null, new Type[]{}, null);
			while (methodInfo == null)
			{
				type = type.BaseType;
				methodInfo = type.GetMethod(methodName, privateMethodBindingFlags, null, new Type[]{}, null);
			}

			return methodInfo;
		}

		public static void InvokeMethodByLocalPath(object source, string methodPath)
		{
			var targetPath = "";
			var methodName = methodPath;

			var removeIndex = methodPath.LastIndexOf(pathSplitChar);
			if (removeIndex >= 0)
			{
				targetPath = methodPath.Remove(removeIndex, methodPath.Length - removeIndex);
				methodName = methodPath.Remove(0, removeIndex + 1);
			}

			var target = GetObjectByLocalPath(source, targetPath);
			var methodInfo = target.GetType().GetAnyMethod_WithoutArguments(methodName);

			methodInfo.Invoke(target, null);
		}

		public static string AppendPath(this string sourcePath, string additionalPath)
		{
			if (string.IsNullOrEmpty(sourcePath))
				return additionalPath;
			if (string.IsNullOrEmpty(additionalPath))
				return sourcePath;

			return sourcePath + pathSplitChar + additionalPath;
		}
	}
}
