namespace Botje.DB
{
    /// <summary>
    /// Database interface.
    /// </summary>
    public interface IDatabase
    {
        /// <summary>
        /// Get the collection.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="table">Defaults to the name of the type.</param>
        /// <returns></returns>
        DbSet<T> GetCollection<T>(string table = null) where T : IAtom;
    }
}
