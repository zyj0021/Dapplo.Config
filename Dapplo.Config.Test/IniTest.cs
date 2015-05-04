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

using Dapplo.Config.Ini;
using Dapplo.Config.Test.TestInterfaces;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Dapplo.Config.Test
{
	[TestClass]
	public class IniTest
	{
		private IPropertyProxy<IIniTest> _propertyProxy;
		private const string Name = "Dapplo";
		private const string FirstName = "Robin";

		[ClassInitialize]
		public static void InitializeClass(TestContext textContext)
		{
			StringEncryptionTypeConverter.RgbIv = "fjr84hF49gp3911fFFg";
			StringEncryptionTypeConverter.RgbKey = "ljew3lJfrS0rlddlfeelOekfekcvbAwE";
		}

		[TestInitialize]
		public void InitializeEveryTest()
		{
			_propertyProxy = ProxyBuilder.CreateProxy<IIniTest>();
		}

		[TestMethod]
		public async Task TestIniInit()
		{
			var iniConfig = new IniConfig();

			var iniTest = _propertyProxy.PropertyObject;
			iniConfig.AddSection(iniTest);
			using (var testMemoryStream = new MemoryStream())
			{
				await iniConfig.ReadFromStream(testMemoryStream);
				Assert.IsTrue(iniTest.WindowCornerCutShape.Count > 0);
			}
		}

		[TestMethod]
		public async Task TestIniWriteRead()
		{
			var iniConfig = new IniConfig();

			var iniTest = _propertyProxy.PropertyObject;
			iniConfig.AddSection(iniTest);
			iniTest.Name = Name;
			iniTest.FirstName = FirstName;
			long ticks = DateTimeOffset.Now.UtcTicks;
			iniTest.Age = ticks;
			using (var writeStream = new MemoryStream())
			{
				await iniConfig.WriteToStream(writeStream);
				iniTest.Age = 2;

				// Test reading
				writeStream.Seek(0, SeekOrigin.Begin);
				var isFileRead = await iniConfig.ReadFromStream(writeStream);
				if (isFileRead)
				{
					Assert.AreEqual(Name, iniTest.Name);
					Assert.AreEqual(FirstName, iniTest.FirstName);
					Assert.AreEqual(ticks, iniTest.Age);
				}
			}
		}
	}
}
