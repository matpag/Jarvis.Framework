using System.Linq;
using Jarvis.Framework.MongoAppender;
using log4net;
using log4net.Appender;
using log4net.Core;
using log4net.Repository.Hierarchy;
using MongoDB.Bson;
using MongoDB.Driver;
using NUnit.Framework;
using System;
using log4net.Layout;

namespace Jarvis.Framework.LoggingTests
{
    public class MongoAppenderTestsBaseClass
    {
        protected IMongoDatabase _db;
        protected IMongoCollection<BsonDocument> _logCollection;
        protected BufferedMongoDBAppender _appender;
        protected MongoDBAppender _mongoAppender;
        protected FileAppender _fileAppender;
        protected ILog _sut;
        protected Logger _logger;

        String connectionString;

        [OneTimeSetUp]
        public void TestFixtureSetup()
        {
            connectionString = ConfigurationManager.Instance.GetConnectionString("testDb");
            MongoUrl url = new MongoUrl(String.Format(connectionString, "test-db-log"));
            var client = new MongoClient(url);
            _db = client.GetDatabase(url.DatabaseName);
            _logCollection = _db.GetCollection<BsonDocument>("logs");
            _db.DropCollection(_logCollection.CollectionNamespace.CollectionName);
            LogManager.CreateRepository("test");

            var hierarchy = (Hierarchy)LogManager.GetRepository("test");
            _logger = hierarchy.LoggerFactory.CreateLogger(hierarchy, "logname");
            _logger.Hierarchy = hierarchy;

            _logger.AddAppender(CreateMongoAppender(false, false));

            _logger.Repository.Configured = true;

            // alternative use the LevelMap to set the Log level based on a string
            // hierarchy.LevelMap["ERROR"]
            hierarchy.Threshold = Level.Debug;
            _logger.Level = Level.Debug;

            _sut = new LogImpl(_logger);
        }

        [SetUp]
        public void SetUp()
        {
            _db.DropCollection("logs");
        }

        protected IAppender CreateMongoAppender(Boolean looseFix, Boolean multiThreadSave)
        {
            _appender = new BufferedMongoDBAppender
            {
                Settings = new MongoLog()
                {
                    ConnectionString = String.Format(connectionString, "test-db-log"),
                    CollectionName = "logs",
                    LooseFix = looseFix,
                },
                SaveOnDifferentThread = multiThreadSave,
            };
            _appender.ActivateOptions();
            return _appender;
        }

        protected IAppender CreateMongoUnbufferedAppender(Boolean looseFix)
        {
            _mongoAppender = new MongoDBAppender
            {
                Settings = new MongoLog()
                {
                    ConnectionString = String.Format(connectionString, "test-db-log"),
                    CollectionName = "logs",
                    LooseFix = looseFix,
                }
            };
            _mongoAppender.ActivateOptions();
            return _mongoAppender;
        }

        protected IAppender CreateFileAppender()
        {
            _fileAppender = new FileAppender
            {
                AppendToFile = false,
                File = "test.log",
                Layout = new PatternLayout("%date %username [%thread] %-5level %logger [%property{NDC}] - %message%newline"),
            };
            _fileAppender.ActivateOptions();
            return _fileAppender;
        }
    }
}
