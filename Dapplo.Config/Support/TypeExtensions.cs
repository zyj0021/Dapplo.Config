﻿/*
 * dapplo - building blocks for desktop applications
 * Copyright (C) 2015 Robin Krom
 * 
 * For more information see: http://dapplo.net/
 * dapplo repositories are hosted on GitHub: https://github.com/dapplo
 * 
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 1 of the License, or
 * (at your option) any later version.
 * 
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 * 
 * You should have received a copy of the GNU General Public License
 * along with this program. If not, see <http://www.gnu.org/licenses/>.
 */

using Dapplo.Config.Converters;
using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace Dapplo.Config.Support
{
	/// <summary>
	/// Extension for types
	/// </summary>
	public static class TypeExtensions
	{
		/// <summary>
		/// check if the type is a generic dictionary: (I)Dictionary of T/>
		/// </summary>
		/// <param name="valueType">Type to check for</param>
		/// <returns>true if it is a generic dictionary</returns>
		public static bool IsGenericDirectory(this Type valueType)
		{
			return valueType.IsGenericType && (valueType.GetGenericTypeDefinition() == typeof(Dictionary<,>) || valueType.GetGenericTypeDefinition() == typeof(IDictionary<,>));
		}

		/// <summary>
		/// check if the type is a generic list (I)List of T/>
		/// </summary>
		/// <param name="valueType">Type to check for</param>
		/// <returns>true if it is a generic list</returns>
		public static bool IsGenericList(this Type valueType)
		{
			return valueType.IsGenericType && (valueType.GetGenericTypeDefinition() == typeof(List<>) || valueType.GetGenericTypeDefinition() == typeof(IList<>));
		}

		/// <summary>
		/// Create an instance of the supplied type
		/// </summary>
		/// <param name="valueType"></param>
		/// <returns></returns>
		public static object CreateInstance(this Type valueType)
		{
			if (valueType == typeof(string) || valueType.IsArray)
			{
				return null;
			}
			else if (IsGenericList(valueType))
			{
				return Activator.CreateInstance(typeof(List<>).MakeGenericType(valueType.GetGenericArguments()[0]));
			}
			else if (IsGenericDirectory(valueType))
			{
				Type type1 = valueType.GetGenericArguments()[0];
				Type type2 = valueType.GetGenericArguments()[1];
				return Activator.CreateInstance(typeof(Dictionary<,>).MakeGenericType(type1, type2));
			}
			else if (valueType.IsInterface || valueType.GetConstructor(Type.EmptyTypes) == null)
			{
				return null;
			}
			return Activator.CreateInstance(valueType);
		}

		/// <summary>
		/// Create a type converter for the supplied type
		/// </summary>
		/// <param name="valueType"></param>
		/// <returns>TypeConverter</returns>
		public static TypeConverter GetTypeConverter(this Type valueType)
		{
			if (IsGenericList(valueType))
			{
				return (TypeConverter)Activator.CreateInstance(typeof(StringToGenericListConverter<>).MakeGenericType(valueType.GetGenericArguments()[0]));
			}
			else if (IsGenericDirectory(valueType))
			{
				Type type1 = valueType.GetGenericArguments()[0];
				Type type2 = valueType.GetGenericArguments()[1];
				return (TypeConverter)Activator.CreateInstance(typeof(GenericDictionaryConverter<,>).MakeGenericType(type1, type2));
			}
			return TypeDescriptor.GetConverter(valueType);
		}

		/// <summary>
		/// Convert or Cast the value so it matches the targetType
		/// </summary>
		/// <param name="targetType">target type</param>
		/// <param name="value">value to convert</param>
		/// <param name="typeConverter">A type converter can be passed for special cases</param>
		/// <returns>object as targetType</returns>
		public static object ConvertOrCastValueToType(this Type targetType, object value, TypeConverter typeConverter = null)
		{
			if (value == null)
			{
				return null;
			}
			var valueType = value.GetType();
			if (targetType != valueType && !targetType.IsAssignableFrom(valueType))
			{
				if (typeConverter == null)
				{
					typeConverter = targetType.GetTypeConverter();
				}
				if (typeConverter.CanConvertFrom(valueType))
				{
					var stringValue = value as string;
					if (stringValue != null)
					{
						return typeConverter.ConvertFromInvariantString(stringValue);
					}
					else
					{
						return typeConverter.ConvertFrom(value);
					}
				}
				try
				{
					return Convert.ChangeType(value, targetType);
				}
				catch
				{
					// Ignore, can't convert the value
				}
			}
			return value;
		}
	}
}
