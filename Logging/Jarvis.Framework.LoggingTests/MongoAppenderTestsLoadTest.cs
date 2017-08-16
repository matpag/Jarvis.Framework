//using System.Linq;
//using Jarvis.Framework.MongoAppender;
//using log4net;
//using log4net.Appender;
//using log4net.Core;
//using log4net.Repository.Hierarchy;
//using MongoDB.Bson;
//using MongoDB.Driver;
//using NUnit.Framework;
//using System;
//using log4net.Layout;

//namespace Jarvis.Framework.LoggingTests
//{

//    [TestFixture]
//    [Explicit]
//    public class MongoAppenderTestsLoadTest : MongoAppenderTestsBaseClass
//    {
//        //[Explicit]
//        //[Test]
//        //public void verify_speed_of_multiple_logs()
//        //{
//        //    Int32 iterationCount = 11000;
//        //    Stopwatch w = new Stopwatch();
//        //    w.Start();
//        //    for (int i = 0; i < iterationCount; i++)
//        //    {
//        //        _sut.Debug("This is a logger");
//        //    }
//        //    Console.WriteLine("With MongoAppender - Before flush {0} ms", w.ElapsedMilliseconds);
//        //    _appender.Flush();
//        //    w.Stop();
//        //    Console.WriteLine("With MongoAppender - Iteration took {0} ms", w.ElapsedMilliseconds);

//        //    _logger.RemoveAllAppenders();
//        //    _logger.AddAppender(CreateMongoAppender(true, false));
//        //    w.Reset();
//        //    w.Start();
//        //    for (int i = 0; i < iterationCount; i++)
//        //    {
//        //        _sut.Debug("This is a logger");
//        //    }
//        //    Console.WriteLine("With MongoAppender Loose Fix - Before flush {0} ms", w.ElapsedMilliseconds);
//        //    _appender.Flush();
//        //    w.Stop();
//        //    Console.WriteLine("With MongoAppender Loose Fix - Iteration took {0} ms", w.ElapsedMilliseconds);

//        //    _logger.RemoveAllAppenders();
//        //    _logger.AddAppender(CreateMongoAppender(false, true));
//        //    w.Reset();
//        //    w.Start();
//        //    for (int i = 0; i < iterationCount; i++)
//        //    {
//        //        _sut.Debug("This is a logger");
//        //    }
//        //    Console.WriteLine("With MongoAppender save on different thread - Before flush {0} ms", w.ElapsedMilliseconds);
//        //    _appender.Flush();
//        //    w.Stop();
//        //    Console.WriteLine("With MongoAppender save on different thread - Iteration took {0} ms", w.ElapsedMilliseconds);

//        //    _logger.RemoveAllAppenders();
//        //    _logger.AddAppender(CreateMongoAppender(true, true));
//        //    w.Reset();
//        //    w.Start();
//        //    for (int i = 0; i < iterationCount; i++)
//        //    {
//        //        _sut.Debug("This is a logger");
//        //    }
//        //    Console.WriteLine("With MongoAppender Loose fix and save on different thread - Before flush {0} ms", w.ElapsedMilliseconds);
//        //    _appender.Flush();
//        //    w.Stop();
//        //    Console.WriteLine("With MongoAppender Loose fix and  save on different thread - Iteration took {0} ms", w.ElapsedMilliseconds);


//        //    _logger.RemoveAllAppenders();
//        //    _logger.AddAppender(CreateMongoUnbufferedAppender(false));
//        //    w.Reset();
//        //    w.Start();
//        //    for (int i = 0; i < iterationCount; i++)
//        //    {
//        //        _sut.Debug("This is a logger");
//        //    }
//        //    w.Stop();
//        //    Console.WriteLine("With Mongo Unbuffered - Iteration took {0} ms", w.ElapsedMilliseconds);

//        //    _logger.RemoveAllAppenders();
//        //    _logger.AddAppender(CreateMongoUnbufferedAppender(true));
//        //    w.Reset();
//        //    w.Start();
//        //    for (int i = 0; i < iterationCount; i++)
//        //    {
//        //        _sut.Debug("This is a logger");
//        //    }
//        //    w.Stop();
//        //    Console.WriteLine("With Mongo Unbuffered loose fix - Iteration took {0} ms", w.ElapsedMilliseconds);


//        //    _logger.RemoveAllAppenders();
//        //    _logger.AddAppender(CreateFileAppender());
//        //    w.Reset();
//        //    w.Start();
//        //    for (int i = 0; i < iterationCount; i++)
//        //    {
//        //        _sut.Debug("This is a logger");
//        //    }
//        //    Console.WriteLine("With FileAppender - Before flush {0} ms", w.ElapsedMilliseconds);
//        //    _fileAppender.Close();
//        //    w.Stop();
//        //    Console.WriteLine("With FileAppender - Iteration took {0} ms", w.ElapsedMilliseconds);


//        //    _logger.RemoveAllAppenders();
//        //    w.Reset();
//        //    w.Start();
//        //    for (int i = 0; i < iterationCount; i++)
//        //    {
//        //        _sut.Debug("This is a logger");
//        //    }
//        //    w.Stop();
//        //    Console.WriteLine("Without MongoAppender - Iteration took {0} ms", w.ElapsedMilliseconds);

//        //}

//        [Explicit]
//        [Test]
//        public void hammering_logger_to_verify_memory_consumption()
//        {
//            _logger.RemoveAllAppenders();
//            BufferedMongoDBAppender appender =
//                (BufferedMongoDBAppender)CreateMongoAppender(true, true);
//            _logger.AddAppender(appender);
//            String bigMessage = new String('X', 100000);
//            var iterationCount = 1 * 1000;
//            for (int i = 0; i < iterationCount; i++)
//            {
//                _sut.Debug(bigMessage);
//            }
//            appender.Flush();
//        }
//    }
//}
