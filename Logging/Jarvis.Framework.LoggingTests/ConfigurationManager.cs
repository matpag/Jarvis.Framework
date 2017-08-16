using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace Jarvis.Framework.LoggingTests
{
    public class ConfigurationManager
    {
        public static ConfigurationManager Instance { get; private set; }

        static ConfigurationManager()
        {
            Instance = new ConfigurationManager();
        }

        private IConfigurationRoot _configurationRoot;

        private ConfigurationManager()
        {
            _configurationRoot = new ConfigurationBuilder()
                .AddXmlFile(Assembly.GetExecutingAssembly().FullName)
                .Build();
        }

        public String GetConnectionString(String connectionString)
        {
            return _configurationRoot[$"configuration:connectionStrings:{connectionString}"];
        }

        public void SetConnectionString(String connectionString, String newValue)
        {
            _configurationRoot[$"configuration:connectionStrings:{connectionString}"] = newValue;
        }
    }
}
