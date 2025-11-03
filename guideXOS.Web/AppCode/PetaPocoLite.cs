// Minimal PetaPoco-like helper to satisfy usage in this sample. Not a full implementation.
// MIT-style helper implemented for this project only.
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Reflection;

namespace PetaPoco
{
    public class Database : IDisposable
    {
        private readonly string _connectionString;
        private readonly string _providerName;
        private SqlConnection? _connection;

        public Database(string connectionStringName)
        {
            var cs = System.Configuration.ConfigurationManager.ConnectionStrings[connectionStringName];
            if (cs == null) throw new InvalidOperationException("Missing connection string: " + connectionStringName);
            _connectionString = cs.ConnectionString;
            _providerName = cs.ProviderName ?? "System.Data.SqlClient";
            if (_providerName != "System.Data.SqlClient")
                throw new NotSupportedException("Only System.Data.SqlClient is supported by this lightweight helper.");
        }

        [Obsolete]
        private SqlConnection EnsureOpen()
        {
            if (_connection == null)
            {
                _connection = new SqlConnection(_connectionString);
            }
            if (_connection.State != ConnectionState.Open)
            {
                _connection.Open();
            }
            return _connection;
        }

        [Obsolete]
        public int Execute(string sql, params object[] args)
        {
            using (var cmd = CreateCommand(sql, args))
            {
                return cmd.ExecuteNonQuery();
            }
        }

        public T ExecuteScalar<T>(string sql, params object[] args)
        {
            using (var cmd = CreateCommand(sql, args))
            {
                object result = cmd.ExecuteScalar();
                if (result == null || result == DBNull.Value) return default(T);
                return (T)Convert.ChangeType(result, typeof(T));
            }
        }

        [Obsolete]
        public T SingleOrDefault<T>(string sql, params object[] args) where T : new()
        {
            using (var cmd = CreateCommand(sql, args))
            using (var rdr = cmd.ExecuteReader())
            {
                if (!rdr.Read()) return default(T)!;
                return Map<T>(rdr);
            }
        }

        [Obsolete]
        public IEnumerable<T> Fetch<T>(string sql, params object[] args) where T : new()
        {
            using (var cmd = CreateCommand(sql, args))
            using (var rdr = cmd.ExecuteReader())
            {
                while (rdr.Read())
                {
                    yield return Map<T>(rdr);
                }
            }
        }

        [Obsolete]
        private SqlCommand CreateCommand(string sql, params object[] args)
        {
            // Add this at the top of the file with the other using statements
            var conn = EnsureOpen();
            var cmd = conn.CreateCommand();
            cmd.CommandText = sql;
            if (args != null)
            {
                for (int i = 0; i < args.Length; i++)
                {
                    var p = cmd.CreateParameter();
                    p.ParameterName = "@p" + i;
                    p.Value = args[i] ?? DBNull.Value;
                    cmd.Parameters.Add(p);
                }
                // replace @0 style with @p0
                for (int i = 0; i < args.Length; i++)
                {
                    sql = sql.Replace("@" + i, "@p" + i);
                }
                cmd.CommandText = sql;
            }
            return cmd;
        }

        private static T Map<T>(IDataRecord rdr) where T : new()
        {
            var obj = new T();
            var t = typeof(T);
            for (int i = 0; i < rdr.FieldCount; i++)
            {
                string name = rdr.GetName(i);
                var prop = t.GetProperty(name, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
                if (prop != null && prop.CanWrite)
                {
                    object? val = rdr.IsDBNull(i) ? null : rdr.GetValue(i);
                    if (val != null)
                    {
                        var targetType = Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType;
                        val = Convert.ChangeType(val, targetType);
                    }
                    prop.SetValue(obj, val, null);
                }
            }
            return obj;
        }

        public void Dispose()
        {
            if (_connection != null)
            {
                _connection.Dispose();
                _connection = null;
            }
        }
    }
}
