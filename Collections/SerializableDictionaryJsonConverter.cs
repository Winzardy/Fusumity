#if NEWTONSOFT
using System;
using System.Collections;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Fusumity.Collections
{
	public class SerializableDictionaryJsonConverter : JsonConverter
	{
		public override bool CanConvert(Type objectType)
		{
			return objectType.Name.StartsWith("SerializableDictionary");
		}


		public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
		{
			// сериализуем как обычный Dictionary
			var dict = value as IDictionary;
			if (dict == null)
			{
				writer.WriteNull();
				return;
			}

			writer.WriteStartObject();
			foreach (DictionaryEntry entry in dict)
			{
				writer.WritePropertyName(entry.Key?.ToString());
				serializer.Serialize(writer, entry.Value);
			}
			writer.WriteEndObject();
		}

		public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
		{
			var keyType = objectType.GetGenericArguments()[0];
			var valueType = objectType.GetGenericArguments()[1];

			var result = (IDictionary) Activator.CreateInstance(objectType);

			var jObject = JObject.Load(reader);
			foreach (var prop in jObject.Properties())
			{
				var key = Convert.ChangeType(prop.Name, keyType);
				var value = prop.Value.ToObject(valueType, serializer);
				result.Add(key, value);
			}

			return result;
		}
	}
}
#endif
