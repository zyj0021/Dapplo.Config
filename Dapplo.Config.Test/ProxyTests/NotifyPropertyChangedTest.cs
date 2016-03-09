﻿//  Dapplo - building blocks for desktop applications
//  Copyright (C) 2015-2016 Dapplo
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
//  You should have Config a copy of the GNU Lesser General Public License
//  along with Dapplo.Config. If not, see <http://www.gnu.org/licenses/lgpl.txt>.

#region using

using System.ComponentModel;
using Dapplo.Config.Test.ProxyTests.Interfaces;
using Dapplo.LogFacade;
using Xunit;
using Xunit.Abstractions;

#endregion

namespace Dapplo.Config.Test.ProxyTests
{
	public class NotifyPropertyChangedTest
	{
		private const string NoChange = "NOCHANGE";
		private const string TestValue1 = "VALUE1";
		private const string TestValue2 = "VALUE2";
		private readonly IPropertyProxy<INotifyPropertyChangedTest> _propertyProxy;

		public NotifyPropertyChangedTest(ITestOutputHelper testOutputHelper)
		{
			XUnitLogger.RegisterLogger(testOutputHelper, LogLevel.Verbose);
			_propertyProxy = ProxyBuilder.CreateProxy<INotifyPropertyChangedTest>();
		}

		[Fact]
		public void TestNotifyPropertyChanged()
		{
			var properties = _propertyProxy.PropertyObject;
			string changedPropertyName = null;

			var propChanged = new PropertyChangedEventHandler((sender, eventArgs) => { changedPropertyName = eventArgs.PropertyName; });

			// Test event handler
			properties.PropertyChanged += propChanged;
			properties.Name = TestValue1;
			Assert.Equal("Name", changedPropertyName);

			// Ensure that if the value is the same, we don't get an event
			changedPropertyName = NoChange;
			properties.Name = TestValue1;
			Assert.Equal(NoChange, changedPropertyName);

			// Test if event handler is unregistered
			properties.PropertyChanged -= propChanged;
			changedPropertyName = NoChange;
			properties.Name = TestValue2;
			Assert.Equal(NoChange, changedPropertyName);
		}
	}
}