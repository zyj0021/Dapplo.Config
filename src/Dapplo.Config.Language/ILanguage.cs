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
using System.Collections.Generic;
using Dapplo.Config.Interfaces;

#endregion

namespace Dapplo.Config.Language
{
	/// <summary>
	///     The base interface for all language objects.
	///     My advice is that you extend your inteface with this, and the INotifyPropertyChanged,
	///     so language changes are directly reflected in the UI.
	///     This extends IDefaultValue, as this it is very common to start with default translations.
	///     These defaults, usually en-US, can be set with the DefaultValueAttribute
	/// </summary>
	public interface ILanguage : IConfiguration<string>
	{
		/// <summary>
		///     Get the translation for a key
		/// </summary>
		/// <param name="languageKey">Key for the translation</param>
		/// <returns>string or null for the translation</returns>
		string this[string languageKey] { get; }

		/// <summary>
        /// Tries to get a translation
        /// </summary>
        /// <param name="languageKey">key to translate</param>
        /// <param name="translation">value for the translation</param>
        /// <returns>bool true if the key was available, false if not</returns>
        bool TryGetTranslation(string languageKey, out string translation);

        /// <summary>
        ///     Get all the language keys, this includes also the ones that don't have an access property.
        ///     This is a method, could have been a property, but this differentiates it from the properties in the extending
        ///     interface.
        /// </summary>
        /// <returns>IEnumerable of string</returns>
        IEnumerable<string> Keys();

		/// <summary>
		///     Get all the language keys which don't have an access property.
		/// </summary>
		/// <returns>IEnumerable of string</returns>
        IEnumerable<string> PropertyFreeKeys();

        /// <summary>
        ///     This event is triggered after the language has been changed
        ///     Added for Dapplo.Config/issues/10
        /// </summary>
        event EventHandler<EventArgs> LanguageChanged;

		/// <summary>
		///     Get the prefix / module name of this ILanguage
		/// </summary>
		/// <returns>string</returns>
		string PrefixName();
	}
}