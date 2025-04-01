# Chrono.Graph

Chrono.Graph is a lightweight, attribute-based Object Graph Mapper (OGM) for Neo4j, designed for flexibility, rich relationship handling, and expressive querying in .NET.

It supports high-performance graph operations using strongly-typed C# models, deep relationship mapping, and chainable Cypher query builders — all without writing raw Cypher unless you want to.

---

## 🚀 Features

- 🧠 Fully-typed queries and deep joins using lambda syntax
- 📎 Rich relationship handling including nested collections and dictionaries
- ⚡️ Post, Put, Patch, Delete, and scalar Get operations
- 🧩 Composable `.Join()` and `.JoinOptional()` for depth control
- 🧼 Clean and minimal API that favors convention over configuration
- 🏷️ Attribute-driven modeling for graph labels, relationships, and node behavior

---

## ✨ Syntax Example

Here’s a simplified real-world example from a `TrackRepository`:

```csharp
public async Task<RacingOrganization?> GetOrganization(string? id)
{
    return ((await _db.Get<RacingOrganization>(
        q => q.Where<RacingOrganization, string>(o => o.Id, Is.Equal(id)),
        j => j
            .JoinOptional<RacingOrganization, PaymentSource?>(f => f.PaymentMethod)
            .JoinOptional<RacingOrganization, IList<RacingFacility>?>(f => f.Facilities)
            .JoinOptional<RacingOrganization, Dictionary<OrganizationContactType, Contact>?>(o => o.ContactInfo, jj => jj
                .JoinOptional<Contact, IList<Phone>?>(c => c.Phones)
                .JoinOptional<Contact, IList<Email>?>(c => c.Emails))
            .JoinOptional<RacingOrganization, RacingFacility?>(o => o.Headquarters, deep => deep
                .JoinOptional<RacingFacility, Address?>(f => f.Address, ddeep => ddeep
                    .JoinOptional<Address, Township?>(a => a.City)
                    .JoinOptional<Address, Province?>(a => a.Province)
                    .JoinOptional<Address, ProvincialCode?>(a => a.ProvincialCode)
                    .JoinOptional<Address, Country?>(a => a.Country))
                .JoinOptional<RacingFacility, IList<Track>>(f => f.Tracks))
            .JoinOptional<RacingOrganization, IList<RacingEvent>>(f => f.Events)
            .JoinOptional<RacingOrganization, IList<RacingSeries>>(f => f.Series))) ?? [])
        .FirstOrDefault();
}
```

---

## 🔍 Join Syntax

Use the following for relationship inclusion:

- `.Join<TParent, TChild>(...)` – required relationships
- `.JoinOptional<TParent, TChild>(...)` – optional relationships
- All join methods support nested lambdas for deeper traversal

---

## 🏷️ Attributes

Chrono.Graph uses attributes to map .NET types to graph schema elements.

### `[GraphEdge("LABEL", Direction.Outgoing)]`
Defines an edge with custom label and direction.

### `[GraphIdentifier]`
Marks a property as the primary identifier for a node (typically your `Id`).

### `[GraphKeyLabelling]`
Used on dictionaries to label relationships using the dictionary key instead of the property name.

### `[GraphLabel("CustomLabel")]`
Applies a custom label to a class or property.

### `[GraphObjectSerialize]`
Serializes an object into a JSON string stored as a property instead of creating a sub-node. Useful for templates, settings, etc.


---

## 🧰 Design Philosophy

- Maximize power from minimal syntax
- No XML or JSON config files
- Avoid unnecessary runtime reflection
- Enable testability and mocking
- Build for speed, scalability, and cross-entity querying

---

## 📦 Example Use Cases

- Retrieve an organization with all linked facilities, contacts, and races
- Deep joins for scorekeeping and event official tools
- Patch partial entity trees based on frontend input
- Store dictionary-encoded relationships (like role-based org mapping)

---

## 🛠️ License

MIT. Use it. Fork it. Build weird graphs with it.

---

## 🧬 Coming Soon

- Query caching and batching
- Better support for graph-wide projections
- .NET source generator hints for join mapping
- Relationship attribute decorators (e.g. `[GraphJoin(...)]`)

