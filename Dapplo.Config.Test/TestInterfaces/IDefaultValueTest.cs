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
using Dapplo.Config.Extension;
using Dapplo.Config.Ini;
using System.Collections.Generic;
using System.ComponentModel;

namespace Dapplo.Config.Test.TestInterfaces
{
	/// <summary>
	/// This is the interface under test
	/// </summary>
	public interface IDefaultValueTest : IDefaultValue<IDefaultValueTest>
	{
		[DefaultValue(21)]
		int Age
		{
			get;
			set;
		}

		[DefaultValue("10,20,30"), TypeConverter(typeof (StringToGenericListConverter<int>))]
		IList<int> Ages
		{
			get;
			set;
		}
	}
}