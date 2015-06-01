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

using Dapplo.Config.Extension;

namespace Dapplo.Config.Test.TestInterfaces
{
	/// <summary>
	/// This is the interface under test
	/// </summary>
	public interface ITagAttributeTest : ITagging<ITagAttributeTest>
	{
		string Name
		{
			get;
			set;
		}

		[Tag("Expert")]
		int Age
		{
			get;
			set;
		}

		[Tag(TestTags.Tag2), Tag(TestTags.Tag1)]
		string FirstName
		{
			get;
			set;
		}
	}
}