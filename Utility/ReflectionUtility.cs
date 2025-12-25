using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Sapientia.Collections;
using Sapientia.Extensions;
using Sapientia.Pooling;
using Sapientia.Reflection;
using UnityEngine;

namespace Fusumity.Utility
{
	using UnityObject = UnityEngine.Object;

	public static class ReflectionUtility
	{
		/// <summary>
		/// Способ удешевить перебор всех сборок
		/// </summary>
		private static readonly string[] _allowedAssemblyTags =
		{
			"CSharp",
			"Game",
			"UI",
			"Audio",
			"Content",
			"Advertising",
			"InAppPurchasing",
			"Analytics",
			"AssetsManagement",
			"InputManagement",
			"Localization",
			"Notifications",
			"Trading",
			"Booting",
			"Generic"
		};

		private static readonly string _editorAssemblyTag = "Editor";

		public static bool HasAttribute<T>(this Type type, bool inherit = false) where T : Attribute
		{
			return TryGetAttribute<T>(type, out _, inherit);
		}

		public static bool TryGetAttribute<T>(this Type type, out T attribute, bool inherit = false) where T : Attribute
		{
			attribute = type.GetCustomAttribute<T>(inherit);
			return attribute != null;
		}

		public static Dictionary<Type, T> InstantiateAllTypesMap<T>(Action<T> activator = null) where T : class
		{
			var retrievedTypes = GetAllTypes<T>();
			var map = new Dictionary<Type, T>(retrievedTypes.Count);

			for (int i = 0; i < retrievedTypes.Count; i++)
			{
				var type = retrievedTypes[i];
				if (type.TryCreateInstance(out T instance))
				{
					map[type] = instance;

					activator?.Invoke(instance);
				}
			}

			return map;
		}

		public static IEnumerable<Assembly> GetAssemblies(Func<Assembly, bool> predicate)
		{
			foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
			{
				if (predicate.Invoke(assembly))
					yield return assembly;
			}
		}

		public static IEnumerable<Assembly> GetAssemblies(string[] tags, bool editor = false)
		{
			return GetAssemblies(Predicate);

			bool Predicate(Assembly assembly)
			{
				if (!editor)
					return PredicateByTags(assembly, tags, _editorAssemblyTag);
				return PredicateByTags(assembly, tags);
			}
		}

		public static IEnumerable<Assembly> GetAllowedAssemblies(bool editor = false)
		{
			return GetAssemblies(Predicate);

			bool Predicate(Assembly assembly)
			{
				if (!editor)
					return PredicateByTags(assembly, _allowedAssemblyTags, _editorAssemblyTag);
				return PredicateByTags(assembly, _allowedAssemblyTags);
			}
		}

		private static bool PredicateByTags(Assembly assembly, string[] includeTags, params string[] excludeTags)
		{
			var name = assembly.FullName;

			if (!includeTags.IsNullOrEmpty())
			{
				if (name.IsNullOrEmpty())
					return false;

				var matched = false;
				for (int i = 0; i < includeTags.Length; i++)
				{
					if (name.Contains(includeTags[i]))
					{
						matched = true;
						break;
					}
				}

				if (!matched)
					return false;
			}

			if (!name.IsNullOrEmpty() && !excludeTags.IsNullOrEmpty())
			{
				for (int i = 0; i < excludeTags.Length; i++)
				{
					if (name.Contains(excludeTags[i]))
						return false;
				}
			}

			return true;
		}

		public static bool TryGetType(string typeName, out Type type, params string[] assemblyTags)
		{
			var assemblies = assemblyTags != null
				? GetAssemblies(assemblyTags)
				: GetAllowedAssemblies();
			foreach (var assembly in assemblies)
			{
				type = assembly.GetTypeByName(typeName);
				if (type != null) return true;
			}

			type = null;
			return false;
		}

		public static bool TryGetType(string typeName, out Type type, bool checkFullName, params Assembly[] assemblies)
		{
			foreach (var assembly in assemblies)
			{
				type = assembly.GetTypeByName(typeName, checkFullName);

				if (type != null) return true;
			}

			type = null;
			return false;
		}

		public static Type GetTypeByName(this Assembly assembly, string typeName, bool checkFullName = false)
		{
			var types = assembly.GetTypes();

			for (int i = 0; i < types.Length; i++)
			{
				var nextType = types[i];

				if (nextType.Name == typeName ||
					(checkFullName && nextType.FullName == typeName))
				{
					return nextType;
				}
			}

			return null;
		}

		/// <summary>
		/// Исключает абстрактные и интерфейсные типы
		/// </summary>
		public static List<Type> GetAllTypes<T>(bool includeSelf = false, bool editor = false) =>
			GetAllTypes(typeof(T), _allowedAssemblyTags, includeSelf, editor);

		public static List<Type> GetAllTypes<T>(string[] assemblyTags, bool includeSelf = false, bool editor = false)
			=> GetAllTypes(typeof(T), assemblyTags, includeSelf, editor);

		public static List<Type> GetAllTypes(this Type type, bool includeSelf = false, bool editor = false) =>
			GetAllTypes(type, _allowedAssemblyTags, includeSelf, editor);

		public static List<Type> GetAllTypes(this Type baseType,
			string[] assemblyTags,
			bool includeSelf = false,
			bool editor = false)
		{
			List<Type> list = new List<Type>();
			foreach (Assembly assembly in GetAssemblies(assemblyTags, editor))
			{
				try
				{
					foreach (Type type in assembly.GetTypes())
					{
						if (type == baseType && !includeSelf)
							continue;

						if (baseType.IsAssignableFrom(type) &&
							!type.IsInterface &&
							!type.IsAbstract)
						{
							list.Add(type);
						}
					}
				}
				catch (ReflectionTypeLoadException e)
				{
#if UNITY_EDITOR
					UnityEngine.Debug.LogException(e);

					foreach (var loaderException in e.LoaderExceptions)
					{
						UnityEngine.Debug.LogException(e);
					}
#endif
				}
			}

			return list;
		}

		public static string GetTypeName(this object obj)
		{
			return obj.GetType().Name;
		}

		public static object GetReflectionValue(this object source, string name)
		{
			if (source == null)
				return null;
			var type = source.GetType();

			while (type != null)
			{
				var f = type.GetField(name, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
				if (f != null)
					return f.GetValue(source);

				var p = type.GetProperty(name,
					BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
				if (p != null)
					return p.GetValue(source, null);

				type = type.BaseType;
			}

			return null;
		}

		public static object GetReflectionValue(this object source, string name, int index)
		{
			var enumerable = GetReflectionValue(source, name) as IEnumerable;

			if (enumerable == null)
				return null;

			var enumerator = enumerable.GetEnumerator();
			for (int i = 0; i <= index; i++)
			{
				if (!enumerator.MoveNext()) return null;
			}

			return enumerator.Current;
		}

		public static MethodInfo GetGenericMethod(this Type type, string name, params Type[] parameters)
		{
			var methodInfo = type.GetMethod(name);

			if (methodInfo == null)
				return null;

			return methodInfo.MakeGenericMethod(parameters);
		}

		private static readonly Dictionary<MemberInfo, string> _summaryCache = new Dictionary<MemberInfo, string>();

		/// <summary>
		/// Чтобы это заработало нужно создать файл рядом с .asmdef под названием 'csc.rsp' и написать туда:
		/// <code> -doc:Library/Bee/{ASSEMBLY_NAME}.xml </code>
		/// Тогда сгенерируется xml документ со всеми комментами
		/// </summary>
		public static bool TryGetSummary(this MemberInfo info, out string summary)
		{
			if (_summaryCache.TryGetValue(info, out summary))
				return !summary.IsNullOrEmpty();

			using (DictionaryPool<XmlCommentType, string>.Get(out var comments))
			{
				if (TryFillComments(info, ref comments))
				{
					if (comments.TryGetValue(XmlCommentType.Summary, out summary))
					{
						_summaryCache[info] = summary;
						return true;
					}
				}

				_summaryCache[info] = null;
			}

			return false;
		}

		/// <summary>
		/// Чтобы это заработало нужно создать файл рядом с .asmdef под названием 'csc.rsp' и написать туда:
		/// <code>
		/// -doc:Library/Bee/{ASSEMBLY_NAME}.xml
		/// </code>
		/// Тогда сгенерируется xml документ со всеми комментами
		/// </summary>
		public static bool TryFillComments(this MemberInfo info, ref Dictionary<XmlCommentType, string> comments)
		{
			//Выбрал эту папку, так как она всегда есть...
			//ScriptAssemblies нельзя потому что она каждый раз очищается, а xml не всегда пересоздаются!
			const string FOLDER = "Bee";

			if (info == null)
				return false;

			var assembly = info.Module.Assembly;
			var xmlPath = System.IO.Path.ChangeExtension(assembly.Location, ".xml");
			xmlPath = xmlPath.Replace("ScriptAssemblies", FOLDER);

			if (!System.IO.File.Exists(xmlPath))
			{
				Debug.LogWarning($"Missing XML-doc by path: {xmlPath}");
				return false;
			}

			var xmlDoc = new System.Xml.XmlDocument();
			xmlDoc.Load(xmlPath);
			var memberName = GetMemberElementName(info);
			var memberNode = xmlDoc.SelectSingleNode($"//member[@name='{memberName}']");

			if (memberNode != null)
			{
				foreach (System.Xml.XmlNode childNode in memberNode.ChildNodes)
				{
					if (childNode.InnerText.IsNullOrWhiteSpace())
						continue;

					var key = GetXmlCommentType(childNode.Name);
					comments[key] = childNode.InnerText.Trim();
				}

				return !comments.IsNullOrEmpty();
			}

			return false;
		}

		private static XmlCommentType GetXmlCommentType(string nodeName)
			=> nodeName switch
			{
				"summary" => XmlCommentType.Summary,
				"remarks" => XmlCommentType.Remarks,
				"returns" => XmlCommentType.Returns,
				"param" => XmlCommentType.Param,
				"example" => XmlCommentType.Example,
				"exception" => XmlCommentType.Exception,
				_ => XmlCommentType.Unknown
			};

		private static string GetMemberElementName(MemberInfo member)
			=> member switch
			{
				Type type => "T:" + type.FullName,
				MethodInfo method => "M:" + method.DeclaringType.FullName + "." + method.Name,
				PropertyInfo property => "P:" + property.DeclaringType.FullName + "." + property.Name,
				FieldInfo field => "F:" + field.DeclaringType.FullName + "." + field.Name,
				EventInfo evt => "E:" + evt.DeclaringType.FullName + "." + evt.Name,
				_ => string.Empty
			};

		public static FieldInfo[] GetConstants<T>()
		{
			return GetConstants(typeof(T));
		}

		public static FieldInfo[] GetConstants(Type type)
		{
			FieldInfo[] fieldInfos = type.GetFields(BindingFlags.Public |
				BindingFlags.Static | BindingFlags.FlattenHierarchy);

			return fieldInfos.Where(fi => fi.IsLiteral && !fi.IsInitOnly).ToArray();
		}

		public static bool IsUnitySerializableType(this Type type)
		{
			if (type.IsPrimitive || type.IsEnum || type == typeof(string))
				return true;

			if (typeof(UnityObject).IsAssignableFrom(type))
				return true;

			if (type.IsArray)
				return IsUnitySerializableType(type.GetElementType());

			if (type.IsGenericType)
			{
				if (type.GetGenericTypeDefinition() != typeof(List<>))
					return false;

				return IsUnitySerializableType(type.GetGenericArguments()[0]);
			}

			return type.IsDefined(typeof(SerializableAttribute), false);
		}
	}

	public enum XmlCommentType
	{
		Summary,
		Remarks,
		Returns,
		Param,
		Example,
		Exception,
		Unknown
	}

	public struct CachedFieldInfo<T>
	{
		private FieldInfo _field;

		private readonly string _name;
		private BindingFlags _bindingFlags;

		public bool IsValid => _field != null;

		public CachedFieldInfo(string name, BindingFlags bindingFlags = BindingFlags.NonPublic | BindingFlags.Instance) : this()
		{
			_bindingFlags = bindingFlags;
			_name = name;

			var type = typeof(T);
			_field = type.GetField(_name, _bindingFlags);
		}

		public void SetValue<TValue>(T obj, TValue value) => _field.SetValue(obj, value);
		public TValue GetValue<TValue>(T obj) => (TValue) _field.GetValue(obj);
	}
}
