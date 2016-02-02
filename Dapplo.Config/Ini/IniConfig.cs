﻿/*
 * dapplo - building blocks for desktop applications
 * Copyright (C) 2015-2016 Dapplo
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

using Dapplo.Config.Support;
using Dapplo.LogFacade;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Dapplo.Config.Ini
{

	/// <summary>
	/// The IniConfig is used to bind IIniSection proxy objects to an ini file.
	/// </summary>
	public class IniConfig
	{
		private static readonly LogSource Log = new LogSource();
		private readonly string _fileName;
		private readonly string _applicationName;
		private const string Defaults = "-defaults";
		private const string Constants = "-constants";
		private const string IniExtension = "ini";
		private static readonly IDictionary<string, IniConfig> ConfigStore = new Dictionary<string, IniConfig>();
		private readonly AsyncLock _asyncLock = new AsyncLock();
		private readonly string _fixedDirectory;
		private readonly IDictionary<string, IIniSection> _iniSections = new SortedDictionary<string, IIniSection>();
		private ReadFrom _initialRead = ReadFrom.Nothing;
		private readonly IDictionary<Type, Action<IIniSection>> _afterLoadActions = new Dictionary<Type, Action<IIniSection>>();
		private readonly IDictionary<Type, Action<IIniSection>> _beforeSaveActions = new Dictionary<Type, Action<IIniSection>>();
		private readonly IDictionary<Type, Action<IIniSection>> _afterSaveActions = new Dictionary<Type, Action<IIniSection>>();
		private IDictionary<string, IDictionary<string, string>> _defaults;
		private IDictionary<string, IDictionary<string, string>> _constants;
		private IDictionary<string, IDictionary<string, string>> _ini = new SortedDictionary<string, IDictionary<string, string>>();
		private readonly bool _watchFileChanges;
		private FileSystemWatcher _configFileWatcher;
		private System.Timers.Timer _saveTimer;

		/// <summary>
		/// Used to detect if we have an intial read, and if so from where.
		/// This is important for the auto-save & FileSystemWatcher
		/// </summary>
		private enum ReadFrom
		{
			Nothing,
			File,
			Stream
		}

		/// <summary>
		/// Assign your own error handler to get all the write errors
		/// </summary>
		public Action<IIniSection, IniValue, Exception> WriteErrorHandler
		{
			get;
			set;
		}

		/// <summary>
		/// Assign your own error handler to get all the read errors
		/// </summary>
		public Action<IIniSection, IniValue, Exception> ReadErrorHandler
		{
			get;
			set;
		}

		/// <summary>
		/// Location of the file where the ini config is stored
		/// </summary>
		public string IniLocation
		{
			get;
		}

		/// <summary>
		/// Static helper to remove the IniConfig from the store.
		/// This is interal, mainly for tests, normally it should not be needed.
		/// </summary>
		/// <param name="applicationName"></param>
		/// <param name="fileName"></param>
		public static void Delete(string applicationName, string fileName)
		{
			var identifier = $"{applicationName}.{fileName}";
			IniConfig iniConfig;
			if (ConfigStore.TryGetValue(identifier, out iniConfig))
			{
				foreach(var section in iniConfig.Sections)
				{
					ProxyBuilder.DeleteProxy(section.GetType());
				}
                ConfigStore.Remove(identifier);
			}
		}

		/// <summary>
		/// Static helper to retrieve the IniConfig that was created with the supplied parameters
		/// </summary>
		/// <param name="applicationName"></param>
		/// <param name="fileName"></param>
		/// <returns>IniConfig</returns>
		public static IniConfig Get(string applicationName, string fileName)
		{
			return ConfigStore[$"{applicationName}.{fileName}"];
		}

		/// <summary>
		/// Static helper to retrieve the first IniConfig, the result when multiple IniConfigs are used is undefined!
		/// </summary>
		/// <returns>IniConfig or null if none</returns>
		public static IniConfig Current => ConfigStore.FirstOrDefault().Value;

		/// <summary>
		/// Setup the management of an .ini file location
		/// </summary>
		/// <param name="applicationName"></param>
		/// <param name="fileName"></param>
		/// <param name="fixedDirectory">Specify a path if you don't want to use the default loading</param>
		/// <param name="autoSaveInterval">0 to disable or the amount of milliseconds that pending changes are written</param>
		/// <param name="watchFileChanges">True to enable file system watching</param>
		public IniConfig(string applicationName, string fileName, string fixedDirectory = null, uint autoSaveInterval = 1000, bool watchFileChanges = true)
		{
			_applicationName = applicationName;
			_fileName = fileName;
			_fixedDirectory = fixedDirectory;
			_watchFileChanges = watchFileChanges;
			// Look for the ini file, this is only done 1 time.
			IniLocation = CreateFileLocation(false, "", _fixedDirectory);

			// Configure the auto save
			if (autoSaveInterval > 0)
			{
				_saveTimer = new System.Timers.Timer
				{
					Interval = autoSaveInterval, Enabled = true, AutoReset = true
				};
				_saveTimer.Elapsed += async (sender, eventArgs) => {
					// If we didn't read from a file we can stop the "timer tick"
					if (_initialRead != ReadFrom.File)
					{
						return;
					}
					bool needsSave = false;
					foreach(var iniSection in _iniSections.Values)
					{
						if (iniSection.HasChanges())
						{
							needsSave = true;
							iniSection.ResetHasChanges();
						}
					}
					if (needsSave)
					{
						try
						{
							await WriteAsync();
                        }
						catch (Exception ex)
						{
							Log.Warn().WriteLine(ex.Message);
						}
					}
				};
			}

			// Add error handlers for writing
			WriteErrorHandler = (iniSection, iniValue, exception) =>
			{
				if (!iniValue.Behavior.IgnoreErrors)
				{
					throw exception;
				}
			};

			// Add error handlers for reading
			ReadErrorHandler = (iniSection, iniValue, exception) =>
			{
				if (!iniValue.Behavior.IgnoreErrors)
				{
					throw exception;
				}
			};

			// Used for lookups
			ConfigStore.Add($"{applicationName}.{fileName}", this);

			// Make sure the configuration is save when the domain is exited
			AppDomain.CurrentDomain.ProcessExit += (sender, eventArgs) => Task.Run(async () => {
				// But only if there was reading from a file
				if (_initialRead == ReadFrom.File)
				{
					await WriteAsync();
				}
			}).Wait();
		}

		/// <summary>
		/// Create a FileSystemWatcher to detect changes
		/// </summary>
		/// <param name="enable">true to enable the watcher</param>
		private void EnableFileWatcher(bool enable)
		{
			if (!_watchFileChanges)
			{
				return;
			}

			// If it is already created, just change the enable
			if (_configFileWatcher != null)
			{
				_configFileWatcher.EnableRaisingEvents = enable;
				return;
			}

			if (!enable)
			{
				// if it is not created, and enable = false, do nothing
				return;
			}

			// If the ini-location directory is not yet created, we can't watch as this would cause an exception
			var watchPath = Path.GetDirectoryName(IniLocation);
			if (!Directory.Exists(watchPath))
			{
				return;
			}

			// Configure file change watching
			_configFileWatcher = new FileSystemWatcher
			{
				Path = watchPath,
				IncludeSubdirectories = false,
				NotifyFilter = NotifyFilters.LastWrite,
				Filter = Path.GetFileName(IniLocation),
				EnableRaisingEvents = true
			};

			// add change handling
			_configFileWatcher.Changed += async (sender, eventArgs) =>
			{
				try
				{
					// Disable events before
					_configFileWatcher.EnableRaisingEvents = false;
					await ReloadAsync();
				}
				catch (Exception ex)
				{
					Log.Warn().WriteLine(ex.Message);
				}
				finally
				{
					// Disable events after
					_configFileWatcher.EnableRaisingEvents = true;
				}
			};
		}

		/// <summary>
		/// Get all the names (from the IniSection annotation) for the sections
		/// </summary>
		/// <returns>all keys</returns>
		public ICollection<string> SectionNames
		{
			get
			{
				return _iniSections.Keys;
			}
		}

		/// <summary>
		/// Get all sections
		/// </summary>
		/// <returns>all keys</returns>
		public IEnumerable<IIniSection> Sections
		{
			get
			{
				return _iniSections.Values;
			}
		}

		/// <summary>
		/// Set the after load action for a IIniSection
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="afterLoadAction"></param>
		/// <returns></returns>
		public IniConfig AfterLoad<T>(Action<T> afterLoadAction) where T : IIniSection
		{
			_afterLoadActions.SafelyAddOrOverwrite(typeof(T), section => afterLoadAction((T)section));
			return this;
		}

		/// <summary>
		/// Set before save action for a IIniSection, this can be used to chance values before they are stored
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="beforeSaveAction"></param>
		/// <returns></returns>
		public IniConfig BeforeSave<T>(Action<T> beforeSaveAction)
		{
			_beforeSaveActions.SafelyAddOrOverwrite(typeof(T), section => beforeSaveAction((T)section));
			return this;
		}


		/// <summary>
		/// Set after save action for a IIniSection, this can be used to change value changed in before save back, after saving
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="afterSaveAction"></param>
		/// <returns>this</returns>
		public IniConfig AfterSave<T>(Action<T> afterSaveAction)
		{
			_afterSaveActions.SafelyAddOrOverwrite(typeof(T), section => afterSaveAction((T)section));
			return this;
		}

		/// <summary>
		/// Register a Property Interface to this ini config, this method will return the property object 
		/// </summary>
		/// <typeparam name="T">Your property interface, which extends IIniSection</typeparam>
		/// <returns>instance of type T</returns>
		public async Task<T> RegisterAndGetAsync<T>(CancellationToken token = default(CancellationToken)) where T : IIniSection
		{
			return (T)await RegisterAndGetAsync(typeof(T), token).ConfigureAwait(false);
		}

		/// <summary>
		/// Register the supplied types
		/// </summary>
		/// <param name="types">Types to register, these must extend IIniSection</param>
		/// <param name="token"></param>
		/// <returns>List with instances for the supplied types</returns>
		public async Task<IList<IIniSection>> RegisterAndGetAsync(IEnumerable<Type> types, CancellationToken token = default(CancellationToken))
		{
			IList<IIniSection> sections = new List<IIniSection>();
			foreach (var type in types)
			{
				sections.Add(await RegisterAndGetAsync(type, token).ConfigureAwait(false));
			}
			return sections;
		}

		/// <summary>
		/// Get the specified ini type
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <returns>T</returns>
		public T Get<T>() where T : IIniSection
		{
			return (T)this[typeof(T)];
		}

		/// <summary>
		/// Get the specified IIniSection type
		/// </summary>
		/// <param name="type">IIniSection to look for</param>
		/// <returns>IIniSection</returns>
		public IIniSection Get(Type type)
		{
			return this[type];
		}

		/// <summary>
		/// Get the specified IIniSection type
		/// </summary>
		/// <param name="type">IIniSection to look for</param>
		/// <returns>IIniSection</returns>
		public IIniSection this[Type type]
		{
			get
			{
				if (!typeof(IIniSection).IsAssignableFrom(type))
				{
					throw new ArgumentException("type is not a IIniSection");
				}
				if (_initialRead == ReadFrom.Nothing)
				{
					throw new InvalidOperationException("Please load before retrieving the ini-sections");
				}
				var propertyProxy = ProxyBuilder.GetProxy(type);
				var iniSection = (IIniSection)propertyProxy.PropertyObject;
				return iniSection;
			}
		}

		/// <summary>
		/// A simple get by name (from the IniSection annotation) for the IniSection
		/// </summary>
		/// <param name="sectionName"></param>
		/// <returns>IIniSection</returns>
		public IIniSection Get(string sectionName)
		{
			return this[sectionName];
		}

		/// <summary>
		/// A simple indexer by name (from the IniSection annotation) for the IniSection
		/// </summary>
		/// <param name="sectionName"></param>
		/// <returns>IIniSection</returns>
		public IIniSection this[string sectionName]
		{
			get
			{
				return _iniSections[sectionName];
			}
		}

		/// <summary>
		/// A simple try get by name for the IniSection
		/// </summary>
		/// <param name="sectionName">Name of the section</param>
		/// <param name="section">out parameter with the IIniSection</param>
		/// <returns>bool with true if it worked</returns>
		public bool TryGet(string sectionName, out IIniSection section)
		{
			return _iniSections.TryGetValue(sectionName, out section);
		}

		/// <summary>
		/// Register a Property Interface to this ini config, this method will return the property object 
		/// </summary>
		/// <typeparam name="T">Type to register, this must extend IIniSection</typeparam>
		/// <returns>instance of T</returns>
		public T RegisterAndGet<T>()
		{
			return (T)RegisterAndGet(typeof(T));
        }

		/// <summary>
		/// Register a Property Interface to this ini config, this method will return the property object 
		/// </summary>
		/// <param name="type">Type to register, this must extend IIniSection</param>
		/// <returns>instance of type</returns>
		public IIniSection RegisterAndGet(Type type)
		{
			if (!typeof(IIniSection).IsAssignableFrom(type))
			{
				throw new ArgumentException("type is not a IIniSection");
			}
			var propertyProxy = ProxyBuilder.GetOrCreateProxy(type);
			var iniSection = (IIniSection)propertyProxy.PropertyObject;
			var sectionName = iniSection.GetSectionName();

			if (_iniSections.ContainsKey(sectionName))
			{
				return iniSection;
			}
			// Add before loading, so it will be handled automatically
			_iniSections.Add(sectionName, iniSection);
			FillSection(iniSection);

			return iniSection;
		}

		/// <summary>
		/// Register a Property Interface to this ini config, this method will return the property object 
		/// </summary>
		/// <param name="type">Type to register, this must extend IIniSection</param>
		/// <param name="token"></param>
		/// <returns>instance of type</returns>
		public async Task<IIniSection> RegisterAndGetAsync(Type type, CancellationToken token = default(CancellationToken))
		{
			if (!typeof(IIniSection).IsAssignableFrom(type))
			{
				throw new ArgumentException("type is not a IIniSection");
			}
			var propertyProxy = ProxyBuilder.GetOrCreateProxy(type);
			var iniSection = (IIniSection)propertyProxy.PropertyObject;
			var sectionName = iniSection.GetSectionName();

			using (await _asyncLock.LockAsync().ConfigureAwait(false))
			{
				if (_iniSections.ContainsKey(sectionName))
				{
					return iniSection;
				}
				// Add before loading, so it will be handled automatically
				_iniSections.Add(sectionName, iniSection);
				if (_initialRead == ReadFrom.Nothing)
				{
					await ReloadInternalAsync(false, token).ConfigureAwait(false);
				}
				else
				{
					FillSection(iniSection);
				}
			}

			return iniSection;
		}

		/// <summary>
		/// Helper to create the location of a file
		/// </summary>
		/// <param name="checkStartupDirectory"></param>
		/// <param name="postfix"></param>
		/// <param name="specifiedDirectory"></param>
		/// <returns>File location</returns>
		private string CreateFileLocation(bool checkStartupDirectory, string postfix = "", string specifiedDirectory = null)
		{
			string file = null;
			if (specifiedDirectory != null)
			{
				file = Path.Combine(specifiedDirectory, $"{_fileName}{postfix}.{IniExtension}");
			}
			else
			{
				if (checkStartupDirectory)
				{
					var startPath = FileLocations.StartupDirectory;
					if (startPath != null)
					{
						file = Path.Combine(startPath, $"{_fileName}{postfix}.{IniExtension}");
					}
				}
				if (file == null || !File.Exists(file))
				{
					string appDataDirectory = FileLocations.RoamingAppDataDirectory(_applicationName);
					file = Path.Combine(appDataDirectory, $"{_fileName}{postfix}.{IniExtension}");
				}
			}
			return file;
		}

		/// <summary>
		/// Reset all the values, in all the registered ini sections, to their defaults
		/// </summary>
		public async Task ResetAsync(CancellationToken token = default(CancellationToken))
		{
			using (await _asyncLock.LockAsync().ConfigureAwait(false))
			{
				ResetInternal();
			}
		}

		/// <summary>
		/// Reset all the values, in all the registered ini sections, to their defaults
		/// This is for internal usage, so no lock
		/// </summary>
		public void ResetInternal()
		{
			foreach (var iniSection in _iniSections.Values)
			{
				foreach (var propertyName in iniSection.GetIniValues().Keys)
				{
					// TODO: Do we need to skip read/write protected values here?
					iniSection.RestoreToDefault(propertyName);
				}
				// Call the after load action
				Action<IIniSection> afterLoadAction;
				if (_afterLoadActions.TryGetValue(iniSection.GetType(), out afterLoadAction))
				{
					afterLoadAction(iniSection);
				}
			}
		}

		/// <summary>
		/// Write the ini file
		/// </summary>
		public async Task WriteAsync(CancellationToken token = default(CancellationToken))
		{
			// Make sure only one write to file is running, other request will have to wait
			using (await _asyncLock.LockAsync().ConfigureAwait(false))
			{
				string path = Path.GetDirectoryName(IniLocation);

				// Create the directory to write to, if it doesn't exist yet
				if (path != null && !Directory.Exists(path))
				{
					Directory.CreateDirectory(path);
				}

				// disable the File-Watcher so we don't get events from ourselves
				EnableFileWatcher(false);

				// Create the file as a stream
				using (var stream = new FileStream(IniLocation, FileMode.Create, FileAccess.Write))
				{
					// Write the registered ini sections to the stream
					await WriteToStreamInternalAsync(stream, token).ConfigureAwait(false);
				}

				// Enable the File-Watcher so we get events again
				EnableFileWatcher(true);
			}
		}

		/// <summary>
		/// Store the ini to the supplied stream
		/// </summary>
		/// <param name="stream"></param>
		/// <param name="token"></param>
		/// <returns>Task to await</returns>
		private async Task WriteToStreamInternalAsync(Stream stream, CancellationToken token = default(CancellationToken))
		{
			var iniSectionsComments = new SortedDictionary<string, IDictionary<string, string>>();

			// Loop over the "registered" sections
			foreach (var iniSection in _iniSections.Values.ToList())
			{
				Action<IIniSection> beforeSaveAction;
				if (_beforeSaveActions.TryGetValue(iniSection.GetType(), out beforeSaveAction))
				{
					beforeSaveAction(iniSection);
				}
				try
				{
					CreateSaveValues(iniSection, iniSectionsComments);
				}
				finally
				{
					// Eventually set the values back
					Action<IIniSection> afterSaveAction;
					if (_beforeSaveActions.TryGetValue(iniSection.GetType(), out afterSaveAction))
					{
						afterSaveAction(iniSection);
					}

				}
			}
			await IniFile.WriteAsync(stream, Encoding.UTF8, _ini, iniSectionsComments, token).ConfigureAwait(false);
			await stream.FlushAsync(token).ConfigureAwait(false);
		}

		/// <summary>
		/// Write all the IIniSections to the stream, this is also used for testing
		/// </summary>
		/// <param name="stream">Stream to write to</param>
		/// <param name="token"></param>
		/// <returns>Task</returns>
		public async Task WriteToStreamAsync(Stream stream, CancellationToken token = default(CancellationToken))
		{
			using (await _asyncLock.LockAsync().ConfigureAwait(false))
			{
				await WriteToStreamInternalAsync(stream, token);
			}
		}

		/// <summary>
		/// Helper method to create ini section values for writing.
		/// The actual values are stored in the _ini
		/// </summary>
		/// <param name="iniSection">Section to write</param>
		/// <param name="iniSectionsComments">Comments</param>
		private void CreateSaveValues(IIniSection iniSection, IDictionary<string, IDictionary<string, string>> iniSectionsComments)
		{
			// This flag tells us if the header for the section is already written
			bool isSectionCreated = false;
			var sectionName = iniSection.GetSectionName();

			var sectionProperties = new SortedDictionary<string, string>();
			var sectionComments = new SortedDictionary<string, string>();
			// Loop over the ini values, this automatically skips all NonSerialized properties
			foreach (var iniValue in iniSection.GetIniValues().Values)
			{
				// Check if we need to write the value, this is not needed when it has the default or if write is disabled
				if (!iniValue.IsWriteNeeded)
				{
					continue;
				}

				// Before we are going to write, we need to check if the section header "[Sectionname]" is already written.
				// If not, do so now before writing the properties of the section itself
				if (!isSectionCreated)
				{
					if (_ini.ContainsKey(sectionName))
					{
						_ini.Remove(sectionName);
					}
					_ini.Add(sectionName, sectionProperties);
					iniSectionsComments.Add(sectionName, sectionComments);

					string description = iniSection.GetSectionDescription();
					if (!string.IsNullOrEmpty(description))
					{
						sectionComments.Add(sectionName, description);
					}
					// Mark section as created!
					isSectionCreated = true;
				}

				// Check if the property has a description, if so write it in the ini comment before the property
				if (!string.IsNullOrEmpty(iniValue.Description))
				{
					sectionComments.Add(iniValue.IniPropertyName, iniValue.Description);
				}

				ITypeDescriptorContext context = null;
				try
				{
					var propertyDescription = TypeDescriptor.GetProperties(iniSection.GetType()).Find(iniValue.PropertyName, true);
					context = new TypeDescriptorContext(iniSection, propertyDescription);
				}
				catch (Exception ex)
				{
					Log.Warn().WriteLine(ex.Message);
				}

				// Get specified converter
				var converter = iniValue.Converter;

				// Special case, for idictionary derrivated types
				if (iniValue.ValueType.IsGenericType && iniValue.ValueType.GetGenericTypeDefinition() == typeof(IDictionary<,>))
				{
					var subSection = TypeExtensions.ConvertOrCastValueToType<IDictionary<string, string>>(iniValue.Value, converter, context, false);
					if (subSection != null)
					{
						try
						{
							// Use this to build a separate "section" which is called "[section-propertyname]"
							string dictionaryIdentifier = $"{sectionName}-{iniValue.IniPropertyName}";
							if (_ini.ContainsKey(dictionaryIdentifier))
							{
								_ini.Remove(dictionaryIdentifier);
							}
							_ini.Add(dictionaryIdentifier, subSection);
							if (!string.IsNullOrWhiteSpace(iniValue.Description))
							{
								var dictionaryComments = new SortedDictionary<string, string>();
								dictionaryComments.Add(dictionaryIdentifier, iniValue.Description);
								iniSectionsComments.Add(dictionaryIdentifier, dictionaryComments);
							}
						}
						catch (Exception ex)
						{
							Log.Warn().WriteLine(ex.Message);
							WriteErrorHandler(iniSection, iniValue, ex);
						}
						continue;
					}
				}

				try
				{
					// Convert the value to a string
					string writingValue = TypeExtensions.ConvertOrCastValueToType<string>(iniValue.Value, converter, context, false);
					// And write the value with the IniPropertyName (which does NOT have to be the property name) to the file
					sectionProperties.Add(iniValue.IniPropertyName, writingValue);
				}
				catch (Exception ex)
				{
					Log.Warn().WriteLine(ex.Message);
					WriteErrorHandler(iniSection, iniValue, ex);
				}
			}
		}

		/// <summary>
		/// This is reloading all the .ini files, and will refill the sections.
		/// If reset = true, ALL setting are lost
		/// Otherwise only the properties in the files will overwrite your settings.
		/// Usually this should not directly be called, unless you know that the file was changed by an external process.
		/// </summary>
		public async Task ReloadAsync(bool reset = true, CancellationToken token = default(CancellationToken))
		{
			using (await _asyncLock.LockAsync().ConfigureAwait(false))
			{
				await ReloadInternalAsync(reset, token).ConfigureAwait(false);
			}
		}

		/// <summary>
		/// This is reloading all the .ini files, and will refill the sections.
		/// If reset = true, ALL setting are lost
		/// Otherwise only the properties in the files will overwrite your settings.
		/// Usually this should not directly be called, unless you know that the file was changed by an external process.
		/// </summary>
		private async Task ReloadInternalAsync(bool reset = true, CancellationToken token = default(CancellationToken))
		{
			if (reset)
			{
				ResetInternal();
            }
			_defaults = await IniFile.ReadAsync(CreateFileLocation(true, Defaults, _fixedDirectory), Encoding.UTF8, token).ConfigureAwait(false);
			_constants = await IniFile.ReadAsync(CreateFileLocation(true, Constants, _fixedDirectory), Encoding.UTF8, token).ConfigureAwait(false);
			var newIni = await IniFile.ReadAsync(IniLocation, Encoding.UTF8, token).ConfigureAwait(false);

			// As we readed the file, make sure we enable the event raising (if the file watcher is wanted)
			EnableFileWatcher(true);
            if (newIni != null)
			{
				_ini = newIni;
			}
			_initialRead = ReadFrom.File;

			// Reset the sections that have already been registered
			FillSections();
		}

		/// <summary>
		/// Internal method, use the supplied ini-sections & properties to fill the sectoins
		/// </summary>
		private void FillSections()
		{
			foreach (var iniSection in _iniSections.Values)
			{
				FillSection(iniSection);
			}
		}

		/// <summary>
		/// Helper method to fill the values of one section
		/// </summary>
		/// <param name="iniSection"></param>
		private void FillSection(IIniSection iniSection)
		{
			if (_saveTimer != null)
			{
				_saveTimer.Enabled = false;
            }
			// Make sure there is no write protection
			iniSection.RemoveWriteProtection();
			// Defaults:
			if (_defaults != null)
			{
				FillSection(_defaults, iniSection);
			}
			// Ini:
			if (_ini != null)
			{
				FillSection(_ini, iniSection);
			}
			// Constants:
			if (_constants != null)
			{
				iniSection.StartWriteProtecting();
				FillSection(_constants, iniSection);
				iniSection.StopWriteProtecting();
			}

			// After loadd
			Action<IIniSection> afterLoadAction;
			if (_afterLoadActions.TryGetValue(iniSection.GetType(), out afterLoadAction))
			{
				afterLoadAction(iniSection);
			}
			iniSection.ResetHasChanges();
            if (_saveTimer != null)
			{
				_saveTimer.Enabled = true;
			}
		}

		/// <summary>
		/// Put the values from the iniProperties to the proxied object
		/// </summary>
		/// <param name="iniSections"></param>
		/// <param name="iniSection"></param>
		private void FillSection(IDictionary<string, IDictionary<string, string>> iniSections, IIniSection iniSection)
		{
			IDictionary<string, string> iniProperties;
			var sectionName = iniSection.GetSectionName();
			// Might be null
			iniSections.TryGetValue(sectionName, out iniProperties);

			var iniValues = (from iniValue in iniSection.GetIniValues().Values
											   where iniValue.Behavior.Read
											   select iniValue);

			foreach (var iniValue in iniValues)
			{
				ITypeDescriptorContext context = null;
				try
				{
					var propertyDescription = TypeDescriptor.GetProperties(iniSection.GetType()).Find(iniValue.PropertyName, true);
					context = new TypeDescriptorContext(iniSection, propertyDescription);
				}
				catch (Exception ex)
				{
					Log.Warn().WriteLine(ex.Message);
				}

				// Test if there is a separate section for this inivalue, this is used for Dictionaries
				IDictionary<string, string> value;
				if (iniSections.TryGetValue($"{sectionName}-{iniValue.IniPropertyName}", out value))
				{
					try
					{
						iniValue.Value = iniValue.ValueType.ConvertOrCastValueToType(value, iniValue.Converter, context, true);
						continue;
					}
					catch (Exception ex)
					{
						Log.Warn().WriteLine(ex.Message);
						ReadErrorHandler(iniSection, iniValue, ex);
					}
				}
				// Skip if the iniProperties doesn't have anything
				if (iniProperties == null || iniProperties.Count == 0)
				{
					continue;
				}
				string stringValue;
				// Skip values that don't have a property
				if (!iniProperties.TryGetValue(iniValue.IniPropertyName, out stringValue))
				{
					continue;
				}

				// convert
				try
				{
					iniValue.Value = iniValue.ValueType.ConvertOrCastValueToType(stringValue, iniValue.Converter, context, true);
				}
				catch (Exception ex)
				{
					Log.Warn().WriteLine(ex.Message);
					ReadErrorHandler(iniSection, iniValue, ex);
				}
			}
		}

		/// <summary>
		/// Initialize the IniConfig by reading all the properties from the stream
		/// If this is called directly after construction, no files will be read which is useful for testing!
		/// </summary>
		public async Task ReadFromStreamAsync(Stream stream, CancellationToken token = default(CancellationToken))
		{
			_initialRead = ReadFrom.Stream;
			// This is for testing, clear all defaults & constants as the 
			_defaults = null;
			_constants = null;
			_ini = await IniFile.ReadAsync(stream, Encoding.UTF8, token).ConfigureAwait(false);

			// Reset the current sections
			FillSections();
		}
	}
}