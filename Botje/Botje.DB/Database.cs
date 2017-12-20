using Botje.Core;
using Newtonsoft.Json;
using Ninject;
using System;
using System.Collections.Generic;
using System.IO;

namespace Botje.DB
{
    /// <summary>
    /// Simple NoSQL database for storing IAtom-type objects. Nothing fancy, absolutely minimal implementation. Thread-safe making it much, much better than many existing NoSQL libraries available.
    /// </summary>
    public class Database : IDatabase, IDisposable
    {
        private object _lockObject = new object(); // for quick-and-dirty thread-safety
        private Dictionary<string, object> _tables = new Dictionary<string, object>(); // set of tables
        private bool _disposedValue = false; // To detect redundant calls
        private string _folder; // filesystem folder where the database will be stored
        private ILogger _log;

        [Inject]
        public ILoggerFactory LoggerFactory { set { _log = value.Create(GetType()); } }

        /// <summary>
        /// Configure the database folder.
        /// </summary>
        /// <param name="folder"></param>
        public void Setup(string folder)
        {
            _folder = folder;
            if (!Directory.Exists(folder))
            {
                Directory.CreateDirectory(folder);
            }
        }

        /// <summary>
        /// Returns a collection of type T from the named table. If no name is specified, the name of the type is used as a table-name.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="table">Names must be valid filesystem-identifiers.</param>
        /// <returns></returns>
        public DbSet<T> GetCollection<T>(string table = null) where T : IAtom
        {
            _log?.Trace($"GetCollection<{typeof(T).Name}>({table})");

            if (string.IsNullOrEmpty(table))
            {
                table = typeof(T).Name;
            }
            table = MakeValidFileName(table);

            lock (_lockObject)
            {
                if (_tables.ContainsKey(table))
                {
                    return _tables[table] as DbSet<T>;
                }
                else
                {
                    if (File.Exists(Path.Combine(_folder, table + ".json")))
                    {
                        var result = JsonConvert.DeserializeObject<DbSet<T>>(File.ReadAllText(Path.Combine(_folder, table + ".json")));
                        result.database = this;
                        _tables[table] = result;
                        return result;
                    }
                    else
                    {
                        var result = new DbSet<T>(table, this);
                        _tables[table] = result;
                        return result;
                    }
                }
            }
        }

        /// <summary>
        /// Dispose the object.
        /// </summary>
        public void Dispose()
        {
            _log?.Trace($"Dispose()");

            Dispose(true);
        }

        /// <summary>
        /// Dispose the object.
        /// </summary>
        /// <param name="disposing"></param>
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                }
                _disposedValue = true;
            }
        }

        /// <summary>
        /// Persist this table.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="dbSet">Table to be persisted.</param>
        internal void PersistTable<T>(DbSet<T> dbSet) where T : IAtom
        {
            _log?.Trace($"PersistTable<{typeof(T).Name}>({dbSet.Name}) => persisting {dbSet.Data?.Count} object(s)");

            Directory.CreateDirectory(_folder);
            File.WriteAllText(Path.Combine(_folder, dbSet.Name + ".json"), JsonConvert.SerializeObject(dbSet));
        }

        private static string MakeValidFileName(string name)
        {
            string invalidChars = System.Text.RegularExpressions.Regex.Escape(new string(System.IO.Path.GetInvalidFileNameChars()));
            string invalidRegStr = string.Format(@"([{0}]*\.+$)|([{0}]+)", invalidChars);

            return System.Text.RegularExpressions.Regex.Replace(name, invalidRegStr, "_");
        }
    }
}
