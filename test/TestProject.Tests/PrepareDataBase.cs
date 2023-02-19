using Npgsql;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
            var createDatabase = $"{prefixDataBaseName.ToLower()}_{DateTime.Now.ToString("yyyyMMddHHmmss")}";

            // Create DataBase
            using var createDataSource = NpgsqlDataSource.Create($"{connectionString}DataBase={database};");
            using var createConnection = createDataSource.CreateConnection();
            createConnection.Open();
            using var cmd = new NpgsqlCommand($"CREATE DATABASE {createDatabase};", createConnection);
            var create = cmd.ExecuteNonQuery();

            // Connect Database
            DataSource = NpgsqlDataSource.Create($"{connectionString}DataBase={createDatabase};");
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
}
