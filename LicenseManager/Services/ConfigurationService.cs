﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Configuration;
using System.Globalization;
using System.Linq;
using System.Web;
using System.Web.Configuration;
using LicenseManager.Configuration;

namespace LicenseManager.Services
{
    public class ConfigurationService : IConfigurationSource
    {
        private const string SettingPrefix = "LM.";
        private const string FeaturePrefix = "Feature.";
        private const string AuthPrefix = "Auth.";

        private IAppConfiguration _current;

        public virtual IAppConfiguration Current
        {
            get { return _current ?? (_current = ResolveSettings()); }
            set { _current = value; }
        }

        private readonly Lazy<string> _httpSiteRootThunk;
        private readonly Lazy<string> _httpsSiteRootThunk;

        public ConfigurationService()
        {
            _httpSiteRootThunk = new Lazy<string>(GetHttpSiteRoot);
            _httpsSiteRootThunk = new Lazy<string>(GetHttpsSiteRoot);
        }

        /// <summary>
        /// Gets the site root using the specified protocol
        /// </summary>
        /// <param name="useHttps">If true, the root will be returned in HTTPS form, otherwise, HTTP.</param>
        /// <returns></returns>
        public virtual string GetSiteRoot(bool useHttps)
        {
            return useHttps ? _httpsSiteRootThunk.Value : _httpSiteRootThunk.Value;
        }

        public virtual IAppConfiguration ResolveSettings()
        {
            return ResolveConfigObject(new AppConfiguration(), SettingPrefix);
        }

        public virtual T ResolveConfigObject<T>(T instance, string prefix)
        {
            // Iterate over the properties
            foreach (var property in GetConfigProperties<T>(instance))
            {
                // Try to get a config setting value
                string baseName = String.IsNullOrEmpty(property.DisplayName) ? property.Name : property.DisplayName;
                string settingName = prefix + baseName;

                string value = ReadSetting(settingName);

                if (String.IsNullOrEmpty(value))
                {
                    var defaultValue = property.Attributes.OfType<DefaultValueAttribute>().FirstOrDefault();
                    if (defaultValue != null && defaultValue.Value != null)
                    {
                        if (defaultValue.Value.GetType() == property.PropertyType)
                        {
                            property.SetValue(instance, defaultValue.Value);
                            continue;
                        }
                        else
                        {
                            value = defaultValue.Value as string;
                        }
                    }
                }

                if (!String.IsNullOrEmpty(value))
                {
                    if (property.PropertyType.IsAssignableFrom(typeof(string)))
                    {
                        property.SetValue(instance, value);
                    }
                    else if (property.Converter != null && property.Converter.CanConvertFrom(typeof(string)))
                    {
                        // Convert the value
                        property.SetValue(instance, property.Converter.ConvertFromString(value));
                    }
                }
                else if (property.Attributes.OfType<RequiredAttribute>().Any())
                {
                    throw new ConfigurationErrorsException(String.Format(CultureInfo.InvariantCulture, "Missing required configuration setting: '{0}'", settingName));
                }
            }
            return instance;
        }

        internal static IEnumerable<PropertyDescriptor> GetConfigProperties<T>(T instance)
        {
            foreach (PropertyDescriptor p in TypeDescriptor.GetProperties(instance))
            {
                if (!p.IsReadOnly) yield return p;
            }
        }

        public virtual string ReadSetting(string settingName)
        {
            string value;
            var cstr = GetConnectionString(settingName);
            if (cstr != null)
            {
                value = cstr.ConnectionString;
            }
            else
            {
                value = GetAppSetting(settingName);
            }

            string cloudValue = GetCloudSetting(settingName);
            return String.IsNullOrEmpty(cloudValue) ? value : cloudValue;
        }

        public virtual string GetCloudSetting(string settingName)
        {
            // no azure code setup yet
            return null;
        }

        public virtual string GetAppSetting(string settingName)
        {
            return WebConfigurationManager.AppSettings[settingName];
        }

        public virtual ConnectionStringSettings GetConnectionString(string settingName)
        {
            return WebConfigurationManager.ConnectionStrings[settingName];
        }

        protected virtual HttpRequestBase GetCurrentRequest()
        {
            return new HttpRequestWrapper(HttpContext.Current.Request);
        }

        private string GetHttpSiteRoot()
        {
            var request = GetCurrentRequest();
            string siteRoot;

            if (request.IsLocal)
            {
                siteRoot = request.Url.GetLeftPart(UriPartial.Authority) + '/';
            }
            else
            {
                siteRoot = Current.SiteRoot;
            }

            if (!siteRoot.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
                && !siteRoot.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("The configured site root must start with either http:// or https://.");
            }

            if (siteRoot.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                siteRoot = "http://" + siteRoot.Substring(8);
            }

            return siteRoot;
        }

        private string GetHttpsSiteRoot()
        {
            var siteRoot = _httpSiteRootThunk.Value;

            if (!siteRoot.StartsWith("http://", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("The configured HTTP site root must start with http://.");
            }

            return "https://" + siteRoot.Substring(7);
        }

        string IConfigurationSource.GetConfigurationValue(string key)
        {
            // Fudge the name because Azure cscfg system doesn't allow : in setting names
            return ReadSetting(key.Replace("::", "."));
        }
    }
}