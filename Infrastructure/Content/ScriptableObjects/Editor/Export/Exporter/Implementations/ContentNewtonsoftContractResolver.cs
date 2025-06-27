using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Fusumity.Utility;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Sapientia.Extensions;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Content.ScriptableObjects.Editor
{
	public class ContentNewtonsoftContractResolver : DefaultContractResolver
	{
		private const string CLIENT_NAMESPACE = "Client";

		private readonly ContentTypeFiltering _filtering;

		public ContentNewtonsoftContractResolver(ContentTypeFiltering filtering)
		{
			_filtering = filtering;
		}

		protected override JsonContract CreateContract(Type objectType)
		{
			var contract = base.CreateContract(objectType);
			if (!IsAllowedType(objectType, _filtering) && contract is JsonObjectContract objContract)
			{
				objContract.ItemNullValueHandling = NullValueHandling.Ignore;
				objContract.Properties.Clear();
			}

			return contract;
		}

		protected override IList<JsonProperty> CreateProperties(Type type, MemberSerialization memberSerialization)
		{
			var props = new List<JsonProperty>();
			if (!IsAllowedType(type, _filtering))
				return props;

			var fields = type
			   .GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
			   .Where(f => !f.IsPrivate || f.IsDefined(typeof(SerializeField), true))
			   .Where(f => !f.IsAssembly)
			   .Where(f => !typeof(Object).IsAssignableFrom(f.FieldType))
			   .Where(f => !f.Name.Contains("k__BackingField"))
			   .Where(f => !f.IsDefined(typeof(NonSerializedAttribute), true))
			   .Where(f => !_filtering.skipTypesWithClientOnlyAttribute || !f.IsDefined(typeof(ClientOnlyAttribute), true));

			foreach (var field in fields)
			{
				var prop = CreateProperty(field, memberSerialization);
				prop.Readable = true;
				prop.Writable = true;
				props.Add(prop);
			}

			return props;
		}

		protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
		{
			var prop = base.CreateProperty(member, memberSerialization);

			if (prop.PropertyType == typeof(string))
			{
				prop.ShouldSerialize = instance =>
				{
					if (prop.ValueProvider != null)
					{
						var value = prop.ValueProvider.GetValue(instance) as string;
						return !value.IsNullOrEmpty();
					}

					return false;
				};
			}
			else
			{
				prop.ShouldSerialize = instance => prop.ValueProvider?.GetValue(instance) != null;
			}

			return prop;
		}

		public static bool IsAllowedType(Type type, ContentTypeFiltering filtering)
		{
			foreach (var skipName in filtering.skipNamespaces)
			{
				if (ShouldSkipNamespace(type, skipName))
					return false;
			}

			foreach (var tag in filtering.skipClassNameTags)
			{
				if (tag.IsNullOrWhiteSpace())
					continue;

				if (type?.FullName?.Contains(tag) ?? false)
					return false;
			}

			if (filtering.skipTypesWithClientOnlyAttribute)
			{
				if (ShouldSkipNamespace(type, CLIENT_NAMESPACE))
					return false;

				return !type.HasAttribute<ClientOnlyAttribute>();
			}

			return true;

			static bool ShouldSkipNamespace(Type type, string @namespace)
			{
				if (type == null)
					return true;

				if (type.Assembly.FullName.Contains(@namespace) || type.Namespace?.Contains(@namespace) == true)
					return true;

				return false;
			}
		}
	}
}
