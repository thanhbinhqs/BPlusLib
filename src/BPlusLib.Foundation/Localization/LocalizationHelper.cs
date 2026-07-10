// <copyright file="LocalizationHelper.cs" company="thanhbinhqs">
// Copyright (c) thanhbinhqs. All rights reserved.
// Licensed under the MIT license.
// </copyright>

namespace BPlusLib.Foundation.Localization
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Resources;

    /// <summary>
    /// Provides a thread-safe helper for loading and retrieving localized strings
    /// using <see cref="System.Resources.ResourceManager"/>.
    /// Supports multiple cultures, fallback cultures, and additional resource files.
    /// Fully cross-platform — no P/Invoke, works on Linux, macOS, and Windows.
    /// </summary>
    /// <example>
    /// <code>
    /// var l10n = new LocalizationHelper("MyApp.Resources.UI");
    /// string welcome = l10n.GetString("WelcomeMessage");
    /// string formatted = l10n.GetString("HelloUser", userName);
    /// </code>
    /// </example>
    public class LocalizationHelper
    {
        private readonly string _baseName;
        private readonly string? _resourceDirectory;
        private readonly ConcurrentDictionary<string, ResourceManager> _managers;
        private readonly ConcurrentDictionary<string, ResourceManager> _additionalResources;
        private CultureInfo _fallbackCulture;
        private readonly Assembly _callingAssembly;

        /// <summary>
        /// Initializes a new instance of the <see cref="LocalizationHelper"/> class.
        /// </summary>
        /// <param name="baseName">The base name of the embedded .resx resources (e.g. "MyApp.Resources.Strings").</param>
        /// <param name="resourceDirectory">
        /// Optional path to a directory containing satellite assembly or .resources files.
        /// When <c>null</c>, only embedded resources are used.
        /// </param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="baseName"/> is null or empty.</exception>
        public LocalizationHelper(string baseName, string? resourceDirectory = null)
        {
            if (string.IsNullOrEmpty(baseName))
            {
                throw new ArgumentNullException(nameof(baseName));
            }

            _baseName = baseName;
            _resourceDirectory = resourceDirectory;
            _managers = new ConcurrentDictionary<string, ResourceManager>(StringComparer.OrdinalIgnoreCase);
            _additionalResources = new ConcurrentDictionary<string, ResourceManager>(StringComparer.OrdinalIgnoreCase);
            _fallbackCulture = CultureInfo.InvariantCulture;
            _callingAssembly = Assembly.GetCallingAssembly();
        }

        /// <summary>
        /// Loads resources for a specific culture (e.g. "en-US", "vi-VN").
        /// Creates a <see cref="ResourceManager"/> if one does not already exist for that culture,
        /// then calls <see cref="ResourceManager.GetResourceSet(CultureInfo, bool, bool)"/> to warm the cache.
        /// </summary>
        /// <param name="cultureCode">The culture code (e.g. "en-US", "vi-VN", "de").</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="cultureCode"/> is null or empty.</exception>
        /// <exception cref="CultureNotFoundException">Thrown when the culture code is invalid.</exception>
        public void LoadCulture(string cultureCode)
        {
            if (string.IsNullOrEmpty(cultureCode))
            {
                throw new ArgumentNullException(nameof(cultureCode));
            }

            var culture = CultureInfo.GetCultureInfo(cultureCode);

            _managers.GetOrAdd(cultureCode, key =>
            {
                var manager = new ResourceManager(_baseName, _callingAssembly);
                manager.GetResourceSet(culture, true, true);
                return manager;
            });
        }

        /// <summary>
        /// Gets a localized string for the specified key.
        /// The culture used is <see cref="CultureInfo.CurrentUICulture"/>.
        /// If the key is not found, returns <c>"[key]"</c>.
        /// </summary>
        /// <param name="key">The resource key to look up.</param>
        /// <returns>The localized string, or <c>"[key]"</c> if not found.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="key"/> is null or empty.</exception>
        public string GetString(string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                throw new ArgumentNullException(nameof(key));
            }

            // Try primary ResourceManager.
            var manager = _managers.GetOrAdd("__default", _ => new ResourceManager(_baseName, _callingAssembly));
            string? value = manager.GetString(key, CultureInfo.CurrentUICulture);

            if (value is not null)
            {
                return value;
            }

            // Try fallback culture.
            if (!_fallbackCulture.Equals(CultureInfo.InvariantCulture))
            {
                value = manager.GetString(key, _fallbackCulture);
                if (value is not null)
                {
                    return value;
                }
            }

            // Try additional resource files.
            foreach (var additional in _additionalResources.Values)
            {
                value = additional.GetString(key, CultureInfo.CurrentUICulture);
                if (value is not null)
                {
                    return value;
                }
            }

            return $"[{key}]";
        }

        /// <summary>
        /// Gets a localized string for the specified key and formats it with the provided arguments
        /// using <see cref="string.Format(IFormatProvider, string, object[])"/>.
        /// </summary>
        /// <param name="key">The resource key to look up.</param>
        /// <param name="args">The arguments to format into the localized string.</param>
        /// <returns>The formatted localized string, or <c>"[key]"</c> if the key is not found.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="key"/> is null or empty.</exception>
        public string GetString(string key, params object[] args)
        {
            string format = GetString(key);
            if (format == $"[{key}]")
            {
                return format;
            }

            try
            {
                return string.Format(CultureInfo.CurrentCulture, format, args);
            }
            catch (FormatException)
            {
                return format;
            }
        }

        /// <summary>
        /// Checks whether the specified resource key exists in any loaded resource set.
        /// </summary>
        /// <param name="key">The resource key to check.</param>
        /// <returns><c>true</c> if the key exists; otherwise <c>false</c>.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="key"/> is null or empty.</exception>
        public bool HasKey(string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                throw new ArgumentNullException(nameof(key));
            }

            var manager = _managers.GetOrAdd("__default", _ => new ResourceManager(_baseName, _callingAssembly));

            try
            {
                var resourceSet = manager.GetResourceSet(CultureInfo.CurrentUICulture, true, false);
                if (resourceSet is not null && resourceSet.GetString(key) is not null)
                {
                    return true;
                }
            }
            catch (MissingManifestResourceException)
            {
                // No resources for this culture — try fallback.
            }

            // Try fallback culture.
            if (!_fallbackCulture.Equals(CultureInfo.InvariantCulture))
            {
                try
                {
                    var fallbackSet = manager.GetResourceSet(_fallbackCulture, true, false);
                    if (fallbackSet is not null && fallbackSet.GetString(key) is not null)
                    {
                        return true;
                    }
                }
                catch (MissingManifestResourceException)
                {
                }
            }

            // Try additional resources.
            foreach (var additional in _additionalResources.Values)
            {
                try
                {
                    var set = additional.GetResourceSet(CultureInfo.CurrentUICulture, true, false);
                    if (set is not null && set.GetString(key) is not null)
                    {
                        return true;
                    }
                }
                catch (MissingManifestResourceException)
                {
                }
            }

            return false;
        }

        /// <summary>
        /// Returns a list of all available culture codes that have been loaded
        /// or discovered via satellite assemblies in the resource directory.
        /// </summary>
        /// <returns>A read-only list of culture codes (e.g. "en-US", "vi-VN", "de").</returns>
        public IReadOnlyList<string> GetAvailableCultures()
        {
            var cultures = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            // Add explicitly loaded cultures.
            foreach (var key in _managers.Keys)
            {
                if (key != "__default")
                {
                    cultures.Add(key);
                }
            }

            // Add invariant / neutral culture from the default manager.
            cultures.Add(CultureInfo.InvariantCulture.Name);

            // Discover satellite assemblies in the resource directory, if specified.
            if (!string.IsNullOrEmpty(_resourceDirectory) && Directory.Exists(_resourceDirectory))
            {
                try
                {
                    foreach (var subDir in Directory.EnumerateDirectories(_resourceDirectory))
                    {
                        var dirName = Path.GetFileName(subDir);
                        if (IsValidCultureName(dirName))
                        {
                            cultures.Add(dirName);
                        }
                    }
                }
                catch (IOException)
                {
                    // Ignore directory enumeration errors.
                }
                catch (UnauthorizedAccessException)
                {
                    // Ignore permission errors.
                }
            }

            return cultures.ToList().AsReadOnly();
        }

        /// <summary>
        /// Gets the name of the current UI culture from the system (e.g. "en-US").
        /// </summary>
        /// <returns>The <see cref="CultureInfo.CurrentUICulture"/> name.</returns>
        public static string GetSystemUICulture()
        {
            return CultureInfo.CurrentUICulture.Name;
        }

        /// <summary>
        /// Gets the name of the current culture from the system (e.g. "en-US").
        /// </summary>
        /// <returns>The <see cref="CultureInfo.CurrentCulture"/> name.</returns>
        public static string GetSystemCulture()
        {
            return CultureInfo.CurrentCulture.Name;
        }

        /// <summary>
        /// Sets the fallback culture to use when a key is not found in the current UI culture.
        /// The default fallback is <see cref="CultureInfo.InvariantCulture"/>.
        /// </summary>
        /// <param name="cultureCode">The culture code to use as fallback (e.g. "en", "en-US").</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="cultureCode"/> is null or empty.</exception>
        /// <exception cref="CultureNotFoundException">Thrown when the culture code is invalid.</exception>
        public void SetDefaultCulture(string cultureCode)
        {
            if (string.IsNullOrEmpty(cultureCode))
            {
                throw new ArgumentNullException(nameof(cultureCode));
            }

            _fallbackCulture = CultureInfo.GetCultureInfo(cultureCode);
        }

        /// <summary>
        /// Loads an additional .resources file and makes its strings available for lookup.
        /// The resources are keyed by their file path to prevent duplicate loading.
        /// </summary>
        /// <param name="filePath">The path to the .resources file.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="filePath"/> is null or empty.</exception>
        /// <exception cref="FileNotFoundException">Thrown when the file does not exist.</exception>
        public void AddResourceFile(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
            {
                throw new ArgumentNullException(nameof(filePath));
            }

            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException("Resource file not found.", filePath);
            }

            _additionalResources.GetOrAdd(filePath, path =>
            {
                var manager = ResourceManager.CreateFileBasedResourceManager(
                    Path.GetFileNameWithoutExtension(path),
                    Path.GetDirectoryName(path)!,
                    null!);
                return manager;
            });
        }

        /// <summary>
        /// Determines whether a directory name looks like a valid culture name.
        /// </summary>
        /// <param name="name">The directory name to check.</param>
        /// <returns><c>true</c> if the name matches a culture pattern; otherwise <c>false</c>.</returns>
        private static bool IsValidCultureName(string name)
        {
            try
            {
                // ReSharper disable once ReturnValueOfPureMethodIsNotUsed
                CultureInfo.GetCultureInfo(name);
                return true;
            }
            catch (CultureNotFoundException)
            {
                return false;
            }
        }
    }
}
