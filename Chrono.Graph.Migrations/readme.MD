# Chrono.Graph.Migrations

A lightweight, flexible Neo4j migration tool designed for initializing and managing schema/data scripts using Cypher. Built for the **Chrono Graph** system, but adaptable to any Neo4j-based project.

Supports tracking applied scripts, dry runs, rollbacks, and colorful output — all from the command line.

---

## 🚀 Features

- 🔄 Tracks applied scripts using `:ScriptRun` nodes in your database
- 📦 Loads and runs `.cypher` scripts from a folder
- 📝 Supports dry-run mode for safe previews
- 🔁 Rollbacks with matching `.rollback.cypher` files
- 🧠 Smart CLI: `--help`, `--list`, `--plan`, `--verbose`, `--summary`
- 🎨 Beautiful color-coded console output
- ✅ Integrates easily with CI/CD or manual setup

---

## 🧠 Usage

```bash
dotnet run --project Chrono.Graph.Migrations [options]
```

### Example

```bash
dotnet run --project Chrono.Graph.Migrations --db=adrenaline --summary
```

---

## 🔧 CLI Options

| Option                    | Description                                                                 |
|---------------------------|-----------------------------------------------------------------------------|
| `--db=<name>`             | Target Neo4j database (default: `neo4j` or `$NEO4J_DATABASE`)               |
| `--uri=<uri>`             | Neo4j URI (default: `bolt://localhost:7687` or `$NEO4J_URI`)               |
| `--user=<username>`       | Username (default: `neo4j` or `$NEO4J_USER`)                                |
| _(password is read only from)_<br>`NEO4J_PASSWORD` | Password environment variable                          |
| `--dir=<path>`            | Path to `.cypher` scripts (default: `scripts/init`)                         |
| `--dry-run`               | Simulate execution without applying changes                                |
| `--only=<filename>`       | Only run a specific script                                                  |
| `--rollback=<filename>`   | Roll back a script by name (e.g., `001_class_list_template.cypher`)         |
| `--list`                  | Show all scripts and whether they’ve been applied                          |
| `--plan`                  | Preview which scripts would be run without executing them                  |
| `--verbose`               | Print every Cypher statement before execution                              |
| `--summary`               | Show a summary after running or planning scripts                           |
| `--help`, `-h`            | Show CLI usage                                                              |

---

## 💾 Tracking Applied Scripts

Each time a script is run, the following Cypher is recorded:

```cypher
CREATE (:ScriptRun {
  name: "filename.cypher",
  appliedAt: datetime()
})
```

This prevents re-running the same script unless explicitly passed via `--only=` or rolled back.

---

## 🔁 Rollbacks

To support rollback for a script, add a file next to it:

```
scripts/init/001_class_list_template.rollback.cypher
```

Then run:

```bash
dotnet run --project Chrono.Graph.Migrations --rollback=001_class_list_template.cypher
```

This:
- Runs the rollback file if present
- Removes the corresponding `:ScriptRun` node

Supports `--dry-run` and `--verbose` as well.

---

## ✅ Recommended Conventions

- Prefix files with numbers: `001_`, `002_`, etc.
- Keep rollback scripts in the same folder with `.rollback.cypher` extension
- Use `--only=` for surgical testing
- Always use `--dry-run` + `--verbose` in local/dev before applying

---

## 📜 License

MIT — but this tool was born from the need to inject sweet motocross class lists into graph databases. Use responsibly. 🎽

---

## 🙌 Inspired by

- RedGate SQL Compare
- EF Migrations
- dbmate
- Liquibase

…but with less boilerplate and more Adrenaline.
