﻿using System.Text.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Azure.Data.Tables;

namespace TableStorage.Abstractions.TableEntityConverters
{
	public static class EntityConvert
	{
		private static JsonSerializerOptions _defaultJsonSerializerOptions = new JsonSerializerOptions();

		/// <summary>
		/// Json fields will use be serialized/deserialized with these provided settings when JsonSerializerOptions are
		/// not explicitly passed into ToTableEntity/FromTableEntity
		/// </summary>
		/// <param name="JsonSerializerOptions">Note: null resets to default</param>
		public static void SetDefaultJsonSerializerOptions(JsonSerializerOptions JsonSerializerOptions = default)
		{
			_defaultJsonSerializerOptions = JsonSerializerOptions ?? new JsonSerializerOptions()
			{
				DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingDefault,
				IgnoreReadOnlyProperties = true,
			};
		}

		public static TableEntity ToTableEntity<T>(this T o, string partitionKey, string rowKey,
			params Expression<Func<T, object>>[] ignoredProperties)
		{
			return ToTableEntity(o, partitionKey, rowKey, _defaultJsonSerializerOptions, default, ignoredProperties);
		}
		public static TableEntity ToTableEntity<T>(this T o, string partitionKey, string rowKey,
			JsonSerializerOptions JsonSerializerOptions,
			PropertyConverters<T> propertyConverters = default,
			params Expression<Func<T, object>>[] ignoredProperties)
		{
			_ = JsonSerializerOptions ?? throw new ArgumentNullException(nameof(JsonSerializerOptions));
			var type = typeof(T);
			var properties = GetProperties(type);
			RemoveIgnoredProperties(properties, ignoredProperties);
			return CreateTableEntity(o, properties, partitionKey, rowKey, JsonSerializerOptions, propertyConverters);
		}

		public static TableEntity ToTableEntity<T>(this T o, Expression<Func<T, object>> partitionProperty,
			Expression<Func<T, object>> rowProperty,
			params Expression<Func<T, object>>[] ignoredProperties)
		{
			return ToTableEntity(o, partitionProperty, rowProperty, _defaultJsonSerializerOptions, null, ignoredProperties);
		}

		public static TableEntity ToTableEntity<T>(this T o, Expression<Func<T, object>> partitionProperty,
			Expression<Func<T, object>> rowProperty, JsonSerializerOptions JsonSerializerOptions,
			PropertyConverters<T> propertyConverters = default,
			params Expression<Func<T, object>>[] ignoredProperties)
		{
			_ = JsonSerializerOptions ?? throw new ArgumentNullException(nameof(JsonSerializerOptions));
			var type = typeof(T);
			var properties = GetProperties(type);
			var partitionProp =
				properties.SingleOrDefault(p => p.Name == GetPropertyNameFromExpression(partitionProperty));
			if (partitionProp == null)
			{
				throw new ArgumentException(nameof(partitionProperty));
			}

			var rowProp = properties.SingleOrDefault(p => p.Name == GetPropertyNameFromExpression(rowProperty));
			if (rowProp == null)
			{
				throw new ArgumentException(nameof(rowProperty));
			}

			properties.Remove(partitionProp);
			properties.Remove(rowProp);
			RemoveIgnoredProperties(properties, ignoredProperties);
			var partitionKey = partitionProp.GetValue(o).ToString();
			var rowKey = rowProp.GetValue(o).ToString();

			return CreateTableEntity(o, properties, partitionKey, rowKey, JsonSerializerOptions, propertyConverters);
		}

		public static T FromTableEntity<T, TP, TR>(this TableEntity entity,
			Expression<Func<T, object>> partitionProperty,
			Expression<Func<T, object>> rowProperty) where T : new()
		{
			return FromTableEntity<T, TP, TR>(entity, partitionProperty, rowProperty, _defaultJsonSerializerOptions);
		}

		public static T FromTableEntity<T, TP, TR>(this TableEntity entity,
			Expression<Func<T, object>> partitionProperty,
			Expression<Func<T, object>> rowProperty,
			JsonSerializerOptions JsonSerializerOptions,
			PropertyConverters<T> propertyConverters = default) where T : new()
		{
			_ = JsonSerializerOptions ?? throw new ArgumentNullException(nameof(JsonSerializerOptions));

			var convertPartition = new Func<string, TP>(p => (TP)Convert.ChangeType(p, typeof(TP)));
			var convertRow = new Func<string, TR>(r => (TR)Convert.ChangeType(r, typeof(TR)));

			if (typeof(TP) == typeof(Guid))
			{
				convertPartition = p => (TP)(object)Guid.Parse(p);
			}
			if (typeof(TR) == typeof(Guid))
			{
				convertRow = r => (TR)(object)Guid.Parse(r);
			}
			return FromTableEntity(entity, partitionProperty, convertPartition,
				rowProperty, convertRow, JsonSerializerOptions, propertyConverters);
		}

		public static T FromTableEntity<T, TP, TR>(this TableEntity entity,
			Expression<Func<T, object>> partitionProperty,
			Func<string, TP> convertPartitionKey, Expression<Func<T, object>> rowProperty,
			Func<string, TR> convertRowKey) where T : new()
		{
			return FromTableEntity(entity, partitionProperty, convertPartitionKey, rowProperty,
				convertRowKey, _defaultJsonSerializerOptions);
		}

		public static T FromTableEntity<T, TP, TR>(this TableEntity entity,
			Expression<Func<T, object>> partitionProperty,
			Func<string, TP> convertPartitionKey, Expression<Func<T, object>> rowProperty,
			Func<string, TR> convertRowKey, JsonSerializerOptions JsonSerializerOptions,
			PropertyConverters<T> propertyConverters = default) where T : new()
		{
			_ = JsonSerializerOptions ?? throw new ArgumentNullException(nameof(JsonSerializerOptions));

			var o = new T();
			var type = typeof(T);
			var properties = GetProperties(type);
			var partitionPropName = partitionProperty != null ? GetPropertyNameFromExpression(partitionProperty) : null;
			var rowPropName = rowProperty != null ? GetPropertyNameFromExpression(rowProperty) : null;
			if (partitionPropName != null && convertPartitionKey != null)
			{
				var partitionProp = properties.SingleOrDefault(p => p.Name == partitionPropName);
				if (partitionProp == null)
				{
					throw new ArgumentException(nameof(partitionProperty));
				}

				partitionProp.SetValue(o, convertPartitionKey(entity.PartitionKey));
				properties.Remove(partitionProp);
			}
			if (rowPropName != null && convertRowKey != null)
			{
				var rowProp = properties.SingleOrDefault(p => p.Name == rowPropName);
				if (rowProp == null)
				{
					throw new ArgumentException(nameof(rowProperty));
				}

				rowProp.SetValue(o, convertRowKey(entity.RowKey));
				properties.Remove(rowProp);
			}

			SetTimestamp(entity, o, properties);
			FillProperties(entity, o, properties, JsonSerializerOptions, propertyConverters);
			return o;
		}

		public static T FromTableEntity<T>(this TableEntity entity) where T : new()
		{
			return FromTableEntity<T>(entity, _defaultJsonSerializerOptions);
		}

		public static T FromTableEntity<T>(this TableEntity entity,
			JsonSerializerOptions JsonSerializerOptions, PropertyConverters<T> propertyConverters = default) where T : new()
		{
			_ = JsonSerializerOptions ?? throw new ArgumentNullException(nameof(JsonSerializerOptions));

			return entity.FromTableEntity<T, object, object>(null, null, null, null, JsonSerializerOptions, propertyConverters);
		}

		internal static string GetPropertyNameFromExpression<T>(Expression<Func<T, object>> exp)
		{
			var name = "";

			var body = exp.Body as MemberExpression;
			if (body == null)
			{
				var ubody = (UnaryExpression)exp.Body;
				body = ubody.Operand as MemberExpression;
				name = body.Member.Name;
			}
			else
			{
				name = body.Member.Name;
			}

			return name;
		}

		private static void SetTimestamp<T>(TableEntity entity, T o, List<PropertyInfo> properties) where T : new()
		{
			var timestampProperty = properties
					.FirstOrDefault(p => p.Name == nameof(TableEntity.Timestamp));

			if (timestampProperty != null)
			{
				if (timestampProperty.PropertyType == typeof(DateTimeOffset))
				{
					timestampProperty.SetValue(o, entity.Timestamp);
				}

				if (timestampProperty.PropertyType == typeof(DateTime))
				{
					timestampProperty.SetValue(o, entity.Timestamp?.UtcDateTime);
				}

				if (timestampProperty.PropertyType == typeof(string))
				{
					timestampProperty.SetValue(o, entity.Timestamp.ToString());
				}
			}
		}

		private static void FillProperties<T>(TableEntity entity, T o, List<PropertyInfo> properties, JsonSerializerOptions JsonSerializerOptions, PropertyConverters<T> propertyConverters) where T : new()
		{
			foreach (var propertyInfo in properties)
			{
				if (propertyConverters != null && entity.Keys.Contains(propertyInfo.Name) && propertyConverters.ContainsKey(propertyInfo.Name))
				{
					propertyConverters[propertyInfo.Name].SetObjectProperty(o, entity[propertyInfo.Name]);
				}
				else if (entity.Keys.Contains(propertyInfo.Name) && propertyInfo.Name != nameof(TableEntity.Timestamp))
				{
					var val = entity[propertyInfo.Name];

					if (val != null && (propertyInfo.PropertyType == typeof(DateTimeOffset) || propertyInfo.PropertyType == typeof(DateTimeOffset?)))
					{
						val = entity.GetDateTimeOffset(propertyInfo.Name);
					}

					if (val != null && propertyInfo.PropertyType == typeof(double))
					{
						val = entity.GetDouble(propertyInfo.Name);
					}

					if (val != null && propertyInfo.PropertyType == typeof(int))
					{
						val = entity.GetInt32(propertyInfo.Name);
					}

					if (val != null && propertyInfo.PropertyType == typeof(long))
					{
						val = entity.GetInt64(propertyInfo.Name);
					}

					if (val != null && propertyInfo.PropertyType == typeof(Guid))
					{
						val = entity.GetGuid(propertyInfo.Name);
					}

					propertyInfo.SetValue(o, val);
				}
				else if (entity.Keys.Contains($"{propertyInfo.Name}Json"))
				{
					var val = entity.GetString($"{propertyInfo.Name}Json");
					if (val != null)
					{
						var propVal = JsonSerializer.Deserialize(val, propertyInfo.PropertyType, JsonSerializerOptions ?? _defaultJsonSerializerOptions);
						propertyInfo.SetValue(o, propVal);
					}
				}
			}
		}

		private static TableEntity CreateTableEntity<T>(object o, List<PropertyInfo> properties,
			string partitionKey, string rowKey, JsonSerializerOptions JsonSerializerOptions, PropertyConverters<T> propertyConverters)
		{
			var entity = new TableEntity(partitionKey, rowKey);
			foreach (var propertyInfo in properties)
			{
				var name = propertyInfo.Name;
				var val = propertyInfo.GetValue(o);
				object entityProperty;
				if (propertyConverters != null && propertyConverters.ContainsKey(name))
				{
					entityProperty = propertyConverters[name].ToTableEntityProperty((T)o);
				}
				else
				{
					switch (val)
					{
						case int x:
							entityProperty = x;
							break;
						case short x:
							entityProperty = x;
							break;
						case byte x:
							entityProperty = x;
							break;
						case string x:
							entityProperty = x;
							break;
						case double x:
							entityProperty = x;
							break;
						case DateTime x:
							entityProperty = x;
							break;
						case DateTimeOffset x:
							entityProperty = x;
							break;
						case bool x:
							entityProperty = x;
							break;
						case byte[] x:
							entityProperty = x;
							break;
						case long x:
							entityProperty = x;
							break;
						case Guid x:
							entityProperty = x;
							break;
						case null:
							entityProperty = null;
							break;
						default:
							name += "Json";
							entityProperty = JsonSerializer.Serialize(val,
								JsonSerializerOptions ?? _defaultJsonSerializerOptions);
							break;
					}
				}

				entity[name] = entityProperty;
			}
			return entity;
		}

		private static List<PropertyInfo> GetProperties(Type type)
		{
			var properties = type.GetProperties(BindingFlags.Instance | BindingFlags.Public).Where(p => p.CanRead && p.CanWrite)
				.ToList();
			return properties;
		}

		private static void RemoveIgnoredProperties<T>(List<PropertyInfo> properties,
			Expression<Func<T, object>>[] ignoredProperties)
		{
			if (ignoredProperties != null)
			{
				for (int i = 0; i < ignoredProperties.Length; i++)
				{
					var ignoredProperty = properties.SingleOrDefault(p => p.Name == GetPropertyNameFromExpression(ignoredProperties[i]));
					properties.Remove(ignoredProperty);
				}
			}
		}
	}


	public class PropertyConverter<T>
	{
		public Func<T, object> ToTableEntityProperty { get; }
		public Action<T, object> SetObjectProperty { get; }

		public PropertyConverter(Func<T, object> toTableEntityProperty, Action<T, object> setObjectProperty)
		{
			ToTableEntityProperty = toTableEntityProperty;
			SetObjectProperty = setObjectProperty;
		}

	}
	public class PropertyConverters<T> : Dictionary<string, PropertyConverter<T>>
	{

	}
}
