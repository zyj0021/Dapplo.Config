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

using Dapplo.Config.Test.TestInterfaces;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Dapplo.Config.Test
{
	/// <summary>
	/// Test case to show how the default value works
	/// </summary>
	[TestClass]
	public class DefaultValueTest
	{
		private IPropertyProxy<IDefaultValueTest> _propertyProxy;

		[TestInitialize]
		public void Initialize()
		{
			_propertyProxy = ProxyBuilder.CreateProxy<IDefaultValueTest>();
		}


		[TestMethod]
		public void TestDefaultValue()
		{
			IDefaultValueTest properties = _propertyProxy.PropertyObject;
			Assert.AreEqual(properties.Age, 21);
			Assert.AreEqual(3, properties.Ages.Count);
		}

		[TestMethod]
		public void TestDefaultValueAtrribute()
		{
			IDefaultValueTest properties = _propertyProxy.PropertyObject;
			object defaultValue = properties.DefaultValueFor(x => x.Age);
			Assert.AreEqual(defaultValue, 21);
			defaultValue = properties.DefaultValueFor("Age");
			Assert.AreEqual(defaultValue, 21);
		}
	}
}