using System;

namespace Botje.DB
{
    /// <summary>
    /// Anything that can be stored in this NoSQL database should be of this type. The UniqueID is set automatically when the object is inserted.
    /// </summary>
    public interface IAtom
    {
        /// <summary>
        /// System-generated unique identifier that may be used to uniquely identify the object inside a collection.
        /// </summary>
        Guid UniqueID { get; set; }
    }
}
