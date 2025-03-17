using AutoFixture;
using Chrono.Graph.Adapter.Neo4j;
using Chrono.Graph.Core.Application;
using NanoidDotNet;
using Neo4j.Driver;

namespace Chrono.Graph.Test.Integration
{
    public class BaseTest
    {
        protected string Id(string? existing = null) => TestUtils.Id(existing);
        protected IGraphDatabase _db;
        protected Fixture _fixture;
        public BaseTest()
        {
            _db = new Neo4jDatabase(
                GraphDatabase.Driver(
                    Environment.GetEnvironmentVariable("NEO4J_URI") ?? "neo4j+s://demo.neo4jlabs.com",
                    AuthTokens.Basic(
                        Environment.GetEnvironmentVariable("NEO4J_USER") ?? "movies",
                        Environment.GetEnvironmentVariable("NEO4J_PASSWORD") ?? "movies"
                    )
                ));

            _fixture = new Fixture();
            _fixture.Behaviors.OfType<ThrowingRecursionBehavior>().ToList()
                .ForEach(b => _fixture.Behaviors.Remove(b));
            _fixture.Behaviors.Add(new OmitOnRecursionBehavior(1));


        }
        protected async Task DeleteTestData<T>(string notes) where T : Base
        {
            if(!string.IsNullOrEmpty(notes))
                await _db.Delete<T>(q => q.Where<T, string>(o => o.Notes, Is.Equal(notes)));

        }

    }
}
//To achieve an "upsert" in Cypher, you can use MERGE to either create a node/relationship if it doesn't exist or match it if it does.
//Here's the modified version of the Cypher script that does an upsert (i.e., if the nodes and relationships already exist, it updates them):
//// Upsert nodes with different labels and properties
//MERGE (a:NodeA {name: 'Node A'})
//ON CREATE SET a.property1 = 'value1A', a.property2 = 123, a.property3 = true
//ON MATCH SET a.property1 = 'value1A', a.property2 = 123, a.property3 = true

//MERGE (b:NodeB {name: 'Node B'})
//ON CREATE SET b.property1 = 'value1B', b.property2 = 456, b.property3 = false
//ON MATCH SET b.property1 = 'value1B', b.property2 = 456, b.property3 = false

//MERGE (c:NodeC {name: 'Node C'})
//ON CREATE SET c.property1 = 'value1C', c.property2 = 789, c.property3 = true
//ON MATCH SET c.property1 = 'value1C', c.property2 = 789, c.property3 = true

//MERGE (d:NodeD {name: 'Node D'})
//ON CREATE SET d.property1 = 'value1D', d.property2 = 1011, d.property3 = false
//ON MATCH SET d.property1 = 'value1D', d.property2 = 1011, d.property3 = false

//// Upsert bidirectional relationships with random edge labels and properties
//MERGE (a)-[r1:RELATIONSHIP_1]->(b)
//ON CREATE SET r1.weight = 1.5, r1.timestamp = datetime()
//ON MATCH SET r1.weight = 1.5, r1.timestamp = datetime()

//MERGE (b)-[r2:RELATIONSHIP_1]->(a)
//ON CREATE SET r2.weight = 2.0, r2.timestamp = datetime()
//ON MATCH SET r2.weight = 2.0, r2.timestamp = datetime()

//MERGE (a)-[r3:RELATIONSHIP_2]->(c)
//ON CREATE SET r3.duration = 5, r3.active = true
//ON MATCH SET r3.duration = 5, r3.active = true

//MERGE (c)-[r4:RELATIONSHIP_2]->(a)
//ON CREATE SET r4.duration = 7, r4.active = false
//ON MATCH SET r4.duration = 7, r4.active = false

//MERGE (a)-[r5:RELATIONSHIP_3]->(d)
//ON CREATE SET r5.score = 98.7, r5.status = 'ongoing'
//ON MATCH SET r5.score = 98.7, r5.status = 'ongoing'

//MERGE (d)-[r6:RELATIONSHIP_3]->(a)
//ON CREATE SET r6.score = 87.6, r6.status = 'completed'
//ON MATCH SET r6.score = 87.6, r6.status = 'completed'

//MERGE (b)-[r7:RELATIONSHIP_4]->(c)
//ON CREATE SET r7.speed = 150, r7.metric = 'km/h'
//ON MATCH SET r7.speed = 150, r7.metric = 'km/h'

//MERGE (c)-[r8:RELATIONSHIP_4]->(b)
//ON CREATE SET r8.speed = 130, r8.metric = 'km/h'
//ON MATCH SET r8.speed = 130, r8.metric = 'km/h'

//MERGE (b)-[r9:RELATIONSHIP_5]->(d)
//ON CREATE SET r9.accuracy = 99.9, r9.method = 'AI'
//ON MATCH SET r9.accuracy = 99.9, r9.method = 'AI'

//MERGE (d)-[r10:RELATIONSHIP_5]->(b)
//ON CREATE SET r10.accuracy = 97.8, r10.method = 'ML'
//ON MATCH SET r10.accuracy = 97.8, r10.method = 'ML'

//MERGE (c)-[r11:RELATIONSHIP_6]->(d)
//ON CREATE SET r11.time = 10, r11.result = 'success'
//ON MATCH SET r11.time = 10, r11.result = 'success'

//MERGE (d)-[r12:RELATIONSHIP_6]->(c)
//ON CREATE SET r12.time = 12, r12.result = 'failure'
//ON MATCH SET r12.time = 12, r12.result = 'failure';

//Key Points:
//Nodes Upsert: MERGE ensures that each node (NodeA, NodeB, NodeC, NodeD) either gets created or matched based on the name property.
//The properties are then updated using ON CREATE SET and ON MATCH SET to handle both creation and updates.

//Relationships Upsert: Similarly, relationships (RELATIONSHIP_1 to RELATIONSHIP_6) are created or matched between the nodes with MERGE.
//The properties on these relationships are updated using ON CREATE SET and ON MATCH SET.

//This approach ensures that if the nodes or relationships already exist, they will be updated, and if they don’t exist, they will be created.gg