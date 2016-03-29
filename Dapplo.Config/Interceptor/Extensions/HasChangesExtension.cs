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

using Dapplo.Config.Interceptor.Implementation;
using Dapplo.Config.Interfaces;
using Dapplo.Config.Support;

#endregion

namespace Dapplo.Config.Interceptor.Extensions
{
	/// <summary>
	///     This implements logic to add change detection to your proxied interface.
	/// </summary>
	[Extension(typeof (IHasChanges))]
	internal class HasChangesExtension : AbstractInterceptorExtension
	{
		// This boolean has the value true if we have changes sind the last "reset"
		private bool _hasChanges;

		/// <summary>
		/// Register setter & methods
		/// </summary>
		public override void Initialize()
		{
			Interceptor.RegisterSetter((int) CallOrder.Last, HasChangesSetter);

			// Use Lambdas to make refactoring possible
			Interceptor.RegisterMethod(ExpressionExtensions.GetMemberName<IHasChanges>(x => x.ResetHasChanges()), ResetHasChanges);
			Interceptor.RegisterMethod(ExpressionExtensions.GetMemberName<IHasChanges>(x => x.HasChanges()), HasChanges);
		}

		/// <summary>
		///     This returns true if we have set (changed) values
		/// </summary>
		/// <param name="methodCallInfo">MethodCallInfo</param>
		private void HasChanges(MethodCallInfo methodCallInfo)
		{
			methodCallInfo.ReturnValue = _hasChanges;
		}

		/// <summary>
		///     This is the implementation of the set logic
		/// </summary>
		/// <param name="setInfo">SetInfo with all the information on the set call</param>
		private void HasChangesSetter(SetInfo setInfo)
		{
			_hasChanges = !setInfo.HasOldValue || !Equals(setInfo.NewValue, setInfo.OldValue);
		}

		/// <summary>
		///     Reset the has changes flag
		/// </summary>
		/// <param name="methodCallInfo">MethodCallInfo</param>
		private void ResetHasChanges(MethodCallInfo methodCallInfo)
		{
			_hasChanges = false;
		}
	}
}