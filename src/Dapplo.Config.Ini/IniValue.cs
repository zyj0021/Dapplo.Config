﻿//  Dapplo - building blocks for desktop applications
//  Copyright (C) 2016-2018 Dapplo
// 
//  For more information see: http://dapplo.net/
//  Dapplo repositories are hosted on GitHub: https://github.com/dapplo
// 
//  This file is part of Dapplo.Config
// 
//  Dapplo.Config is free software: you can redistribute it and/or modify
//  it under the terms of the GNU Lesser General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
// 
//  Dapplo.Config is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU Lesser General Public License for more details.
// 
//  You should have a copy of the GNU Lesser General Public License
//  along with Dapplo.Config. If not, see <http://www.gnu.org/licenses/lgpl.txt>.

#region using

using System;
using System.ComponentModel;
using System.Reflection;

#endregion

namespace Dapplo.Config.Ini
{
	/// <summary>
	///     Container for supplying the properties to the Ini file
	/// </summary>
	public class IniValue
	{
		private readonly IIniSection _iniSection;
        private readonly PropertyInfo _propertyInfo;

        /// <summary>
        ///     The constructor of an IniValue
        /// </summary>
        /// <param name="iniSection">IIniSection</param>
        /// <param name="propertyInfo">PropertyInfo</param>
        public IniValue(IIniSection iniSection, PropertyInfo propertyInfo)
		{
			_iniSection = iniSection;
            _propertyInfo = propertyInfo;
        }

		/// <summary>
		///     Specific behavior of the ini-value
		///     This describes if the value should be read or written
		/// </summary>
		public IniPropertyBehaviorAttribute Behavior { get; set; }

		/// <summary>
		///     Category, which was set via the CategoryAttribute on the property
		/// </summary>
		public string Category { get; set; }

		/// <summary>
		///     Return the TypeConverter for this value, when not set defaults are used
		/// </summary>
		public TypeConverter Converter { get; set; }

		/// <summary>
		///     Default value, from the DefaultAttribute
		/// </summary>
		public object DefaultValue { get; set; }

		/// <summary>
		///     Description, which was set via the DescriptionAttribute on the property
		/// </summary>
		public string Description { get; set; }

		/// <summary>
		///     If this is set to true, we also should write the default value to the file
		/// </summary>
		public bool EmitDefaultValue { get; set; }

		/// <summary>
		///     Check if this IniValue has a value
		/// </summary>
		public bool HasValue => _iniSection.TryGetIniValue(PropertyName, out var _);

		/// <summary>
		///     Name of the property in the file, this could be different
		/// </summary>
		public string IniPropertyName { get; set; }

		/// <summary>
		///     Check if this IniValue needs to be written.
		///     This returns false if there is no value, or if the value is the default and if EmitDefaultValue is false (Default)
		/// </summary>
		public bool IsWriteNeeded
		{
			get
			{
				// Never write!!
				if (!Behavior.Write)
				{
					return false;
				}
				// if EmitDefaultValue is true, we should always write this value (without checking if it is default
				if (EmitDefaultValue)
				{
					return true;
				}

				// Check if our value is default
				var isDefault = Equals(Value, DefaultValue);
				return !isDefault;
			}
		}

        /// <summary>
        ///     Name of the property in the interface
        /// </summary>
        public string PropertyName => _propertyInfo.Name;

        /// <summary>
        ///     Current value
        /// </summary>
        public object Value
		{
            get => _iniSection.Getter(_propertyInfo.Name);
			set => _iniSection.Setter(_propertyInfo.Name, value);
		}

        /// <summary>
        ///     Type for the value, needed for conversion when reading.
        /// </summary>
        public Type ValueType => _propertyInfo.PropertyType;

        /// <summary>
        ///     Reset the value to a default
        /// </summary>
        public void ResetToDefault()
		{
			_iniSection.RestoreToDefault(PropertyName);
		}
	}
}