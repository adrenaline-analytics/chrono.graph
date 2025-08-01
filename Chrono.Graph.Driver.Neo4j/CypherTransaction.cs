﻿using Chrono.Graph.Core.Application;
using Chrono.Graph.Core.Domain;
using Neo4j.Driver;
using System.Diagnostics;


namespace Chrono.Graph.Adapter.Neo4j
{
    public class CypherTransaction : IDisposable
    {
        public IQueryFactory? Factory { get; private set; }
        public IDriver Driver { get; private set; }
        public QueryConfig? QueryConfig { get; private set; }
        public Guid TransactionId { get; private set; }
        private bool _transactionExecuted;
        public CypherTransaction (IDriver driver, QueryConfig queryConfig, Func<IQueryFactory> bootstrapper)
        {
            Driver = driver;
            QueryConfig = queryConfig;
            TransactionId = Guid.NewGuid();
            Factory = bootstrapper();
        }

        public CypherTransaction (IDriver driver, QueryConfig queryConfig)
        {
            Driver = driver;
            QueryConfig = queryConfig;
            TransactionId = Guid.NewGuid();
        }
        public void ContinueWith<T>(Action<IQueryFactory> build)
        {
            if(Factory != null)
            {
                build(Factory);
                _transactionExecuted = false;
            }
        }
        public async Task<(IReadOnlyList<IRecord>, IResultSummary)> ExecuteTransaction<T>()
        {
            if (!_transactionExecuted && Factory != null)
                return await Execute(Factory.Build());

            throw new NotSupportedException("This transaction cannot be executed outside, it is either executed already or improperly setup");
        }
        public async Task<(IReadOnlyList<IRecord>, IResultSummary)> Execute(Statement? statement)
        {
            if (string.IsNullOrEmpty(statement?.Cypher))
                throw new ArgumentException("Unable to determine cypher, cannot create query");

            foreach (var preload in statement.Preloads)
                await Execute(preload.Key, preload.Value);

            return await Execute(statement.Cypher, statement.InVars.ToDictionary(v => v.Key, v => v.Value.Object));

        }
        /// <summary>
        /// Execute a raw Cypher query with optional parameters
        /// </summary>
        /// <param name="cypher">The Cypher query to execute</param>
        /// <param name="parameters">Optional parameters for the query</param>
        /// <returns>The query result as a list of dictionaries where each dictionary represents a record</returns>
        public async Task<(IReadOnlyList<IRecord>, IResultSummary)> Execute(string cypher, Dictionary<string, object?>? parameters = null)
        {
            Debug.WriteLine(cypher);
            var timein = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            var (result, summary) = await Driver
                .ExecutableQuery(cypher)
                .WithParameters(parameters)
                .WithConfig(QueryConfig ?? new QueryConfig())
                .ExecuteAsync();

            Debug.WriteLine($"Finished in {DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() -  timein} ms");
            return (result, summary);

        }
        public async Task<(IReadOnlyList<IRecord>, IResultSummary)> Execute(Func<IQueryFactory> bootstrapper) 
        {
            var factory = bootstrapper();
            return await Execute(factory.Build());
        }
        public async Task<(IReadOnlyList<IRecord>, IResultSummary)> Execute() {
            if (_transactionExecuted)
                throw new NotSupportedException("Unable to execute a closed transaction");
            if (Factory == null)
                throw new NotSupportedException("Unable to locate suitable cypher factory");

            return await Execute(Factory.Build());
        }
        public void Dispose()
        {
            if (!_transactionExecuted && Factory != null) Execute(Factory.Build()).Wait();
        }
    }
}
