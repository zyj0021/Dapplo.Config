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

using System;
using System.Linq;
using System.Threading.Tasks;
using Dapplo.Config.Language;
using Dapplo.Config.Support;
using Dapplo.Config.Test.LanguageTests.Interfaces;
using Dapplo.LogFacade;
using Xunit;
using Xunit.Abstractions;

#endregion

namespace Dapplo.Config.Test.LanguageTests
{
	/// <summary>
	///     This test class tests the transactional capabilities of the proxy
	/// </summary>
	public class LanguageLoaderTest : IDisposable
	{
		public const string Ok = "Ok";
		private readonly LanguageLoader _languageLoader;

		public LanguageLoaderTest(ITestOutputHelper testOutputHelper)
		{
			XUnitLogger.RegisterLogger(testOutputHelper, LogLevel.Verbose);
			_languageLoader = new LanguageLoader("Dapplo");
			_languageLoader.CorrectMissingTranslations();
		}

		public void Dispose()
		{
			LanguageLoader.Delete("Dapplo");
		}

		[Fact]
		public async Task TestExtension()
		{
			ILanguageLoaderTest test = null;
			// ReSharper disable once ExpressionIsAlwaysNull
			// This is actually what we want to test, extension method can work on null values :)
			var ok = test.DefaultTranslation(x => x.Ok);
			Assert.Equal("Ok", ok);

			test = await _languageLoader.RegisterAndGetAsync<ILanguageLoaderTest>();
			ok = test.TranslationOrDefault(x => x.Ok);
			Assert.Equal("Ok", ok);

			test = _languageLoader.Get<ILanguageLoaderTest>();
			ok = test.TranslationOrDefault(x => x.Ok);
			Assert.Equal("Ok", ok);
		}

		[Fact]
		public async Task TestIllegalInterface()
		{
			var ex = await Assert.ThrowsAsync<NotSupportedException>(async () => await _languageLoader.RegisterAndGetAsync<ILanguageLoaderFailTest>());
		}

		[Fact]
		public async Task TestIndexer()
		{
			var language = await _languageLoader.RegisterAndGetAsync<ILanguageLoaderTest>();
			await _languageLoader.ChangeLanguage("nl-NL");
			Assert.Equal("Afbreken", language["TestValue"]);
			// Test using the raw property name with the indexer
			Assert.Equal("Afbreken", _languageLoader["test"]["test_value"]);
			Assert.Equal("cool", _languageLoader["test"]["dapplo"]);
			Assert.True(_languageLoader["test"].Keys().Contains("dapplo"));
		}

		[Fact]
		public async Task TestModules()
		{
			// Make sure that the module (for testing) is available, we count all file-path which end with the filename 
			var count = _languageLoader.Files["en-US"].Count(file => file.EndsWith("language_mymodule-en-US.ini"));
			Assert.True(count > 0);

			var languageMyModule = await _languageLoader.RegisterAndGetAsync<ILanguageLoaderMyModuleTest>();
			Assert.Equal("Some value", languageMyModule.ModuleSettings);
		}

		[Fact]
		public async Task TestTranslations()
		{
			var language = await _languageLoader.RegisterAndGetAsync<ILanguageLoaderTest>();
			Assert.True(_languageLoader.AvailableLanguages.ContainsKey("nl-NL"));
			Assert.True(_languageLoader.AvailableLanguages.ContainsKey("en-US"));
			Assert.True(_languageLoader.AvailableLanguages.ContainsKey("de-DE"));
			Assert.True(_languageLoader.AvailableLanguages.ContainsKey("sr-Cyrl-RS"));
			Assert.Equal("Nederlands (Nederland)", _languageLoader.AvailableLanguages["nl-NL"]);
			await _languageLoader.ChangeLanguage("en-US");
			Assert.Equal(Ok, language.Ok);
			Assert.Equal("Cancel", language.TestValue);
			Assert.Equal("BlubEN", language.OnlyenUs);
			Assert.NotEqual("BlubNL", language.OnlynlNl);
			Assert.NotEqual("BlubDE", language.OnlydeDe);
			await _languageLoader.ChangeLanguage("nl-NL");
			Assert.Equal("Afbreken", language.TestValue);
			Assert.NotEqual("BlubEN", language.OnlyenUs);
			Assert.NotEqual("BlubDE", language.OnlydeDe);
			Assert.Equal("BlubNL", language.OnlynlNl);
			await _languageLoader.ChangeLanguage("de-DE");
			Assert.Equal("BlubDE", language.OnlydeDe);
		}
	}
}