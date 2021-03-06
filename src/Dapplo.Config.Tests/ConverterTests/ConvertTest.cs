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

using System.Collections.Generic;
using Dapplo.Log;
using Dapplo.Log.XUnit;
using Dapplo.Utils.Extensions;
using Xunit;
using Xunit.Abstractions;

#endregion

namespace Dapplo.Config.Tests.ConverterTests
{
	public class ConvertTest
	{
		public ConvertTest(ITestOutputHelper testOutputHelper)
		{
			LogSettings.RegisterDefaultLogger<XUnitLogger>(LogLevels.Verbose, testOutputHelper);
		}

		[Fact]
		public void TestConvertCollection()
		{
			var collection = TypeExtensions.ConvertOrCastValueToType<ICollection<string>>("1,2,3, \"4 , 5\"  ,5,6");
			Assert.Equal(6,collection.Count);
			var list = TypeExtensions.ConvertOrCastValueToType<IList<string>>("1,2,3, 4  ,5,6");
			Assert.Equal(6, list.Count);
			var intList = TypeExtensions.ConvertOrCastValueToType<List<int>>("1,2,3, 4  ,5,6");
			Assert.Equal(6, intList.Count);
			var set = TypeExtensions.ConvertOrCastValueToType<ISet<string>>("1,2,3, 4  ,5,6");
			Assert.Equal(6, set.Count);
		}

		[Fact]
		public void TestConvertDictionary()
		{
			var instance = TypeExtensions.ConvertOrCastValueToType<IDictionary<string, string>>("name1=value1,\"name2=value2 and something more, like a comma\",name3=value3,name4=value4");
			Assert.Equal(4, instance.Count);

			var instance2 = TypeExtensions.ConvertOrCastValueToType<IDictionary<int, double>>("10=100,20=200,30=300,40=400");
			Assert.Equal(4, instance2.Count);

			var sourceDictionary = new Dictionary<string, int>
			{
				{"value1", 10},
				{"value2", 20}
			};
			var instance3 = TypeExtensions.ConvertOrCastValueToType<IDictionary<string, double>>(sourceDictionary);
			Assert.Equal(2, instance3.Count);
			TypeExtensions.ConvertOrCastValueToType<IDictionary<string, string>>(instance3);
		}
	}
}