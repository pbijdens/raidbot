using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Botje.DB
{
    /// <summary>
    /// Named dataset of objects. Returned objects are always clones of the database objects. To update the database invoke .Update(obj) on the DbSet.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class DbSet<T> where T : IAtom
    {
        /// <summary>
        /// Get the name of this dataset.
        /// </summary>
        public string Name { get; set; }

        internal Database database;
        private object _lockObject = new object(); // the lock object 

        /// <summary>
        /// Public only for persistence.
        /// </summary>
        public List<T> Data { get; set; }

        /// <summary>
        /// Create a new database
        /// </summary>
        public DbSet()
        {
            Data = new List<T>();
        }

        public DbSet(String name, Database database) : this()
        {
            Name = name;
            this.database = database;
        }

        /// <summary>
        /// Search for all elements matching a certain predicate.
        /// </summary>
        /// <param name="p"></param>
        /// <returns></returns>
        public IEnumerable<T> Find(Func<T, bool> p)
        {
            return Data.Where(p).Select(x => Clone(x));
        }

        /// <summary>
        /// Deep-clone an object.
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        private T Clone(T obj)
        {
            return JsonConvert.DeserializeObject<T>(JsonConvert.SerializeObject(obj));
        }

        /// <summary>
        /// Updates the specified object in the dataset.
        /// </summary>
        /// <param name="evt"></param>
        public void Update(T evt)
        {
            lock (_lockObject)
            {
                var record = Data.Where(x => x.UniqueID == evt.UniqueID).First();
                Data.Remove(record);
                Data.Add(Clone(evt));
                Persist();
            }
        }

        /// <summary>
        /// Inserts the object into the dataset. It gets a new unique ID when inserted.
        /// </summary>
        /// <param name="record"></param>
        public void Insert(T record)
        {
            lock (_lockObject)
            {
                record.UniqueID = Guid.NewGuid();
                Data.Add(Clone(record));
                Persist();
            }
        }

        /// <summary>
        /// Remove all matching objects from the dataset.
        /// </summary>
        /// <param name="predicate"></param>
        /// <returns></returns>
        public int Delete(Predicate<T> predicate)
        {
            lock (_lockObject)
            {
                int result = Data.RemoveAll(predicate);
                if (result > 0) Persist();
                return result;
            }
        }

        /// <summary>
        /// Return all objects in the dataset.
        /// </summary>
        /// <returns></returns>
        public IEnumerable<T> FindAll()
        {
            return Data.Select(Clone);
        }

        /// <summary>
        /// Returns the number of objects in the dataset.
        /// </summary>
        /// <returns></returns>
        public int Count()
        {
            return Data.Count();
        }

        /// <summary>
        /// Persist this table.
        /// </summary>
        private void Persist()
        {
            database.PersistTable(this);
        }
    }
}
