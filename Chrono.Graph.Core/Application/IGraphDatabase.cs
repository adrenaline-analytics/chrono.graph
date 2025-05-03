using System.Reflection;

namespace Chrono.Graph.Core.Application
{
    public interface IGraphDatabase
    {
        /// <summary>
        /// Retrieve an individual object.  Syntax sugar works the same as Get<>().Any() ? .FirstOrDefault()
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="clauser"></param>
        /// <returns></returns>
        Task<T?> GetScalar<T>(Action<IQueryClause> clauser) where T : class;
        /// <summary>
        /// Retrieve an individual object.  Syntax sugar works the same as Get<>().Any() ? .FirstOrDefault()
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="clauser"></param>
        /// <param name="joiner">What child objects to join, either forced or optional</param>
        /// <returns></returns>
        Task<T?> GetScalar<T>(Action<IQueryClause> clauser, Action<IJoiner>? joiner) where T : class;

        /// <summary>
        /// Get all objects with child objects that match the clause 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="clauser"></param>
        /// <param name="joiner"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        Task<IEnumerable<T>> Get<T>(Action<IQueryClause> clauser, Action<IJoiner>? joiner) where T : class;
        /// <summary>
        /// Get all objects matching the clause
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="clauser"></param>
        /// <returns></returns>
        Task<IEnumerable<T>> Get<T>(Action<IQueryClause> clauser) where T : class;
        Task AddEdge<T, TT>(T from, string verb, TT to) where T : class;
        /// <summary>
        /// Create a brand new object
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="thing"></param>
        /// <returns></returns>
        Task Post<T>(T thing) where T : class;
        /// <summary>
        /// Create a brand new object
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="thing"></param>
        /// <param name="joiner"></param>
        /// <returns></returns>
        Task Post<T>(T thing, Action<IJoiner> joiner) where T : class;
        /// <summary>
        /// Put a brand new object or update an existing one.  
        /// NULL VALUES IN THE UPDATED OBJECT WILL OVERWRITE (DELETE) the value on the existing object
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="thing"></param>
        /// <returns></returns>
        Task Put<T>(T thing) where T : class;
        /// <summary>
        /// Put a brand new object or update an existing one.  Null values in the updated object will overwrite (delete) the value on the existing object
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="thing"></param>
        /// <param name="joiner"></param>
        /// <returns></returns>
        Task Put<T>(T thing, Action<IJoiner> joiner) where T : class;
        /// <summary>
        /// Put a brand new objects recursively or update an existing one.  Null values in the updated object will overwrite (delete) the value on the existing object
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="thing"></param>
        /// <param name="idProp"></param>
        /// <param name="joiner"></param>
        /// <returns></returns>
        Task Put<T>(T thing, PropertyInfo idProp, Action<IJoiner> joiner) where T : class;
        /// <summary>
        /// Put a brand new objects recursively or update an existing one.  Null values in the updated object will overwrite (delete) the value on the existing object
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="thing"></param>
        /// <param name="clauser"></param>
        /// <param name="joiner"></param>
        /// <returns></returns>
        Task Put<T>(T thing, Action<IQueryClause> clauser, Action<IJoiner> joiner) where T : class;
        /// <summary>
        /// Put a brand new object or update an existing one.  Null values in the updated object will overwrite (delete) the value on the existing object
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="o"></param>
        /// <param name="clauser"></param>
        /// <param name="joiner"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        /// <summary>
        /// Update an existing object
        /// NULL VALUES ARE SAFE and will NOT overwrite the value on the existing object
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="o"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        Task Patch<T>(T thing) where T : class;
        /// <summary>
        /// Update an existing object
        /// NULL VALUES ARE SAFE and will NOT overwrite the value on the existing object
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="thing"></param>
        /// <param name="nodeDepth"></param>
        /// <returns></returns>
        Task Patch<T>(T thing, Action<IJoiner> joiner) where T : class;
        /// <summary>
        /// Update an existing object
        /// NULL VALUES ARE SAFE and will NOT overwrite the value on the existing object
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="o"></param>
        /// <param name="clauser"></param>
        /// <returns></returns>
        Task Patch<T>(T o, Action<IQueryClause> clauser) where T : class;
        /// <summary>
        /// Update an existing object
        /// NULL VALUES ARE SAFE and will NOT overwrite the value on the existing object
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="o"></param>
        /// <param name="clauser"></param>
        /// <param name="nodeDepth"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        Task Patch<T>(T thing, Action<IQueryClause> clauser, Action<IJoiner> joiner) where T : class;
        /// <summary>
        /// Delete an object
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="clauser"></param>
        /// <returns></returns>
        Task Delete<T>(Action<IQueryClause> clauser) where T : class;
        /// <summary>
        /// Delete an object
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="clauser"></param>
        /// <returns></returns>
        Task Delete<T>(Action<IQueryClause> clauser, Action<IJoiner> joiner) where T : class;
        /// <summary>
        /// Delete an object
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="thing"></param>
        /// <returns></returns>
        Task Delete<T>(T thing) where T : class;
        /// <summary>
        /// Delete an object
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="thing"></param>
        /// <param name="idProp"></param>
        /// <returns></returns>
        /// <exception cref="AmbiguousMatchException"></exception>
        Task Delete<T>(T thing, PropertyInfo idProp) where T : class;
        /// <summary>
        /// Delete an object and its child objects
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="clauser"></param>
        /// <returns></returns>
        Task Delete<T>(T thing, int nodeDepth) where T : class;
    }
}
