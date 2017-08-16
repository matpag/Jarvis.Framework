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
    [TestFixture]
    public class MongoAppenderTests : MongoAppenderTestsBaseClass
    {
        [Test]
        public void Verify_single_log()
        {
            _sut.Debug("This is a logger");
            _appender.Flush();
            Assert.That(_logCollection.Count(Builders<BsonDocument>.Filter.Empty), Is.EqualTo(1));
        }

        [Test]
        public void Verify_file_name()
        {
            _sut.Debug("This is a logger");
            _appender.Flush();
            var log = _logCollection.Find<BsonDocument>(Builders<BsonDocument>.Filter.Empty).Single();
            Assert.That(log["fi"].ToString(), Does.EndWith("MongoAppenderTests.cs"));
        }

        [Test]
        public void Verify_lots_of_log()
        {
            for (int i = 0; i < 1000; i++)
            {
                _sut.Debug("This is a logger with a big string: " + new string('x', 10000));
            }
            _appender.Flush();
            var log = _logCollection.Find<BsonDocument>(Builders<BsonDocument>.Filter.Empty).Count();
            Assert.That(log, Is.EqualTo(1000));
        }

        [Test]
        public void Verify_inner_exception()
        {
            try
            {
                var inner1 = new ApplicationException("Inner 1");
                var inner2 = new ApplicationException("Inner 2", inner1);
                throw new ApplicationException("outer", inner2);
            }
            catch (Exception ex)
            {
                _sut.Error("Exception", ex);
            }
            _appender.Flush();
            var log = _logCollection.Find<BsonDocument>(Builders<BsonDocument>.Filter.Empty).First();
            var innerExceptionList = log["ie"] as BsonArray;
            Assert.That(innerExceptionList.Count, Is.EqualTo(2));

            var exception = log["ex"].ToString();
            Assert.That(exception, Does.Not.EndWith("Inner 1"), "Exception should not contain inner exception");
            Assert.That(exception, Does.Not.EndWith("Inner 2"), "Exception should not contain inner exception");
        }

        [Test]
        public void Verify_inner_exception_log_most_inner_exception()
        {
            try
            {
                var inner1 = new ApplicationException("Inner 1");
                var inner2 = new ApplicationException("Inner 2", inner1);
                var inner3 = new ApplicationException("Inner 3", inner2);
                throw new ApplicationException("outer", inner3);
            }
            catch (Exception ex)
            {
                _sut.Error("Exception", ex);
            }
            _appender.Flush();
            var log = _logCollection.Find<BsonDocument>(Builders<BsonDocument>.Filter.Empty).First();
            var firstException = log["fe"];
            Assert.That(firstException["me"].AsString, Is.EqualTo("Inner 1"));
        }
    }
}
