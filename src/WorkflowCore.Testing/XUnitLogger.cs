using System;
using System.Text;
using Microsoft.Extensions.Logging;
using Xunit.Abstractions;

namespace WorkflowCore.Testing
{
    internal class XUnitLogger : ILogger
    {
        private readonly ITestOutputHelper _testOutputHelper;
        private readonly string _categoryName;
        private readonly LoggerExternalScopeProvider _scopeProvider;

        public static ILogger CreateLogger(ITestOutputHelper testOutputHelper) =>
            new XUnitLogger(testOutputHelper, new LoggerExternalScopeProvider(), "");

        public static ILogger<T> CreateLogger<T>(ITestOutputHelper testOutputHelper) =>
            new XUnitLogger<T>(testOutputHelper, new LoggerExternalScopeProvider());

        public XUnitLogger(ITestOutputHelper testOutputHelper, LoggerExternalScopeProvider scopeProvider,
            string categoryName)
        {
            _testOutputHelper = testOutputHelper;
            _scopeProvider = scopeProvider;
            _categoryName = categoryName;
        }

        public bool IsEnabled(LogLevel logLevel) => logLevel != LogLevel.None;

        public IDisposable BeginScope<TState>(TState state) => _scopeProvider.Push(state);

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception,
            Func<TState, Exception, string> formatter)
        {
            if (_testOutputHelper == null) return;
            var sb = new StringBuilder();
            sb.Append(DateTime.Now.ToString("HH:mm:ss.fff"))
                .Append(" ")
                .Append(GetLogLevelString(logLevel))
                .Append(" [").Append(_categoryName).Append("] ")
                .Append(formatter(state, exception));

            if (exception != null)
            {
                sb.Append('\n').Append(exception);
            }

            // Append scopes
            _scopeProvider.ForEachScope((scope, s) =>
            {
                s.Append("\n => ");
                s.Append(scope);
            }, sb);

            _testOutputHelper.WriteLine(sb.ToString());
        }

        private static string GetLogLevelString(LogLevel logLevel)
        {
            return logLevel.ToString().ToUpper();
        }
    }
    
    internal sealed class XUnitLogger<T> : XUnitLogger, ILogger<T>
    {
        public XUnitLogger(ITestOutputHelper testOutputHelper, LoggerExternalScopeProvider scopeProvider)
            : base(testOutputHelper, scopeProvider, typeof(T).FullName)
        {
        }
    }
    
    internal sealed class XUnitLoggerProvider : ILoggerProvider
    {
        private readonly ITestOutputHelper _testOutputHelper;
        private readonly LoggerExternalScopeProvider _scopeProvider = new LoggerExternalScopeProvider();

        public XUnitLoggerProvider(ITestOutputHelper testOutputHelper)
        {
            _testOutputHelper = testOutputHelper;
        }

        public ILogger CreateLogger(string categoryName)
        {
            return new XUnitLogger(_testOutputHelper, _scopeProvider, categoryName);
        }

        public void Dispose()
        {
        }
    }
}