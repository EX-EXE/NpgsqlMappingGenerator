using Microsoft.Extensions.Logging;
using Npgsql;
using Xunit.Abstractions;

namespace TestProject.Tests
{
    public class PrepareDataBase : IDisposable
    {
        protected NpgsqlDataSource DataSource { get; private set; }
        protected NpgsqlConnection Connection { get; private set; }

        protected ITestOutputHelper OutputHelper { get; private set; }
        public PrepareDataBase(ITestOutputHelper outputHelper,string prefixDataBaseName)
        {
            OutputHelper = outputHelper;
            var host = "localhost";
            var user = "exe";
            var pass = "exe";
            var database = "dvdrental";
            if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("UseEnv")))
            {
                OutputHelper.WriteLine($"Use Env.");
                host = Environment.GetEnvironmentVariable("host");
                user = Environment.GetEnvironmentVariable("user");
                pass = Environment.GetEnvironmentVariable("pass");
                database = Environment.GetEnvironmentVariable("database");
            }
            OutputHelper.WriteLine($"Host : {host}");
            OutputHelper.WriteLine($"User : {user}");

            var connectionString = $"Host={host};Username={user};Password={pass};";
            var createDatabase = $"{prefixDataBaseName.ToLower()}_{DateTime.Now.ToString("yyyyMMddHHmmssfff")}";
            outputHelper.WriteLine($"DB : {createDatabase}");

            var loggerFactory = LoggerFactory.Create(builder => builder.AddProvider(new TestLoggerProvider(OutputHelper)));

            // Create DataBase
            var createDataSourceBuilder = new NpgsqlDataSourceBuilder($"{connectionString}DataBase={database};");
            createDataSourceBuilder.UseLoggerFactory(loggerFactory);
            using var createDataSource = createDataSourceBuilder.Build();
            using var createConnection = createDataSource.CreateConnection();
            createConnection.Open();
            using var cmd = new NpgsqlCommand($"CREATE DATABASE {createDatabase};", createConnection);
            var create = cmd.ExecuteNonQuery();

            // Connect Database
            var connectDataSourceBuilder = new NpgsqlDataSourceBuilder($"{connectionString}DataBase={createDatabase};");
            connectDataSourceBuilder.UseLoggerFactory(loggerFactory);
            DataSource = connectDataSourceBuilder.Build();
            Connection = DataSource.CreateConnection();
            Connection.Open();
        }

        private bool disposedValue;
        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    Connection?.Dispose();
                    DataSource?.Dispose();
                }
                disposedValue = true;
            }
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }

    internal class TestLoggerProvider : ILoggerProvider
    {
        private ITestOutputHelper outputHelper;

        public TestLoggerProvider(ITestOutputHelper outputHelper)
        {
            this.outputHelper = outputHelper;
        }

        public ILogger CreateLogger(string categoryName)
        {
            return new TestLogger(outputHelper, categoryName);
        }

        public void Dispose()
        {
        }
    }


    public class TestLogger : ILogger
    {
        private readonly ITestOutputHelper outputHelper;
        private readonly string categoryName;

        public TestLogger(ITestOutputHelper outputHelper, string categoryName)
        {
            this.outputHelper = outputHelper;
            this.categoryName = categoryName;
        }

        public IDisposable BeginScope<TState>(TState state)
        {
            return NoneDisposable.Instance;
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return true;
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            outputHelper.WriteLine($"[{categoryName}][{eventId}] {formatter(state, exception)}");
            if (exception != null)
            {
                outputHelper.WriteLine(exception.ToString());
            }
        }
    }
    class NoneDisposable : IDisposable
    {
        public static IDisposable Instance = new NoneDisposable();

        NoneDisposable()
        {

        }

        public void Dispose()
        {
        }
    }
}
