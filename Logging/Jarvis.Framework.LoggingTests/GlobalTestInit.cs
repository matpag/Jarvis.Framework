using System;
using System.Configuration;
using NUnit.Framework;

namespace Jarvis.Framework.LoggingTests
{
    [SetUpFixture]
    public class GlobalSetup
    {
        [OneTimeSetUp]
        public void ShowSomeTrace()
        {
            var overrideTestDb = Environment.GetEnvironmentVariable("TEST_MONGODB");
            if (String.IsNullOrEmpty(overrideTestDb)) return;

            var overrideTestDbQueryString = Environment.GetEnvironmentVariable("TEST_MONGODB_QUERYSTRING");
            ConfigurationManager.Instance.SetConnectionString("testDb", overrideTestDb.TrimEnd('/') + "/{0}" + overrideTestDbQueryString);
        }
    }
}
