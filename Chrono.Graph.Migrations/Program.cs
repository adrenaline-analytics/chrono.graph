using Neo4j.Driver;

class Program
{

    static async Task Main(string[] args)
    {
        if (args.Contains("--help") || args.Contains("-h"))
        {
            PrintHelp();
            return;
        }

        var db = GetArg(args, "--db=") ?? Environment.GetEnvironmentVariable("NEO4J_DATABASE") ?? "neo4j";
        var uri = GetArg(args, "--uri=") ?? Environment.GetEnvironmentVariable("NEO4J_URI") ?? "bolt://localhost:7687";
        var user = GetArg(args, "--user=") ?? Environment.GetEnvironmentVariable("NEO4J_USER") ?? "neo4j";
        var pass = Environment.GetEnvironmentVariable("NEO4J_PASSWORD") ?? "your_password"; // no arg allowed
        var planMode = args.Contains("--plan");
        var dir = args.FirstOrDefault(a => a.StartsWith("--dir="))?.Split("=")[1] ?? "scripts/init";
        var dryRun = args.Contains("--dry-run");
        var onlyFile = args.FirstOrDefault(a => a.StartsWith("--only="))?.Split("=")[1];
        var listMode = args.Contains("--list");
        var verbose = args.Contains("--verbose");
        var rollbackTarget = args.FirstOrDefault(a => a.StartsWith("--rollback="))?.Split("=")[1];
        var summaryMode = args.Contains("--summary");

        var driver = GraphDatabase.Driver(uri, AuthTokens.Basic(user, pass));
        if (!string.IsNullOrEmpty(rollbackTarget))
        {
            await Rollback(driver, db, dir, rollbackTarget, dryRun, verbose);
            return;
        }
        await Exec(driver, db, dir, dryRun, verbose, listMode, onlyFile, planMode, summaryMode);

    }

    static async Task Exec(IDriver driver, string db, string dir, bool dryRun, bool verbose, bool listMode, string? onlyFile, bool planMode, bool summaryMode)
    {
        int appliedCount = 0;
        var scripts = Directory.GetFiles(dir, "*.cypher")
            .Where(f => !f.EndsWith(".rollback.cypher"))
            .OrderBy(f => f)
            .ToList();

        if (planMode)
        {
            WriteColorLine("📋 Migration plan (would run):", ConsoleColor.Gray);
            foreach (var scriptPath in scripts)
            {
                WriteColorLine($"  {Path.GetFileName(scriptPath)}", ConsoleColor.Yellow);
            }

            if (summaryMode)
                WriteColorLine($"📊 Plan summary: {scripts.Count} script(s) would run.", ConsoleColor.Gray);

            WriteColorLine("📝 No scripts executed due to --plan.", ConsoleColor.DarkYellow);
            return;
        }



        if (onlyFile != null)
            scripts = scripts.Where(s => Path.GetFileName(s) == onlyFile).ToList();

        var applied = await GetAppliedScripts(driver, db);

        if (onlyFile == null)
            scripts = scripts.Where(s => !applied.Contains(Path.GetFileName(s))).ToList();

        if (listMode)
        {
            WriteColorLine("📋 Script status:", ConsoleColor.Gray);
            var paths = Directory.GetFiles(dir, "*.cypher").Where(f => !f.EndsWith(".rollback.cypher")).OrderBy(f => f);
            foreach (var path in paths)
            {
                var name = Path.GetFileName(path);
                if (applied.Contains(name))
                    WriteColorLine($"  ✅ {name}", ConsoleColor.Green);
                else
                    WriteColorLine($"  ❌ {name}", ConsoleColor.Yellow);
            }
            return;
        }

        if (!scripts.Any())
        {
            WriteColorLine("✅ No new scripts to run.", ConsoleColor.Green);
            return;
        }

        foreach (var scriptPath in scripts)
        {
            var fileName = Path.GetFileName(scriptPath);
            WriteColorLine($"{(dryRun ? "📝 Dry run" : "🚀 Running")}: {fileName}", dryRun ? ConsoleColor.DarkYellow : ConsoleColor.Cyan);

            var cypher = await File.ReadAllTextAsync(scriptPath);
            var statements = cypher.Split(";", StringSplitOptions.RemoveEmptyEntries);

            if (!dryRun)
            {
                await using var session = driver.AsyncSession(o => o.WithDatabase(db));
                await session.ExecuteWriteAsync(async tx =>
                {
                    foreach (var stmt in statements)
                    {
                        var trimmed = stmt.Trim();
                        if (!string.IsNullOrWhiteSpace(trimmed))
                        {
                            if (verbose)
                                WriteColorLine($"\n💬 Executing:\n{trimmed}\n", ConsoleColor.Cyan);

                            await tx.RunAsync(trimmed);
                            appliedCount++;
                        }
                    }

                    await tx.RunAsync(@"
                        CREATE (:ScriptRun {
                            name: $name,
                            appliedAt: datetime()
                        })", new { name = fileName });
                });
            }
        }

        await driver.DisposeAsync();
        if (summaryMode)
        {
            WriteColorLine($"📊 Migration summary: {appliedCount} script(s) applied.", ConsoleColor.Gray);
        }

        WriteColorLine("✅ Done.", ConsoleColor.Green);

    }

    static async Task Rollback(IDriver driver, string db, string dir, string rollbackTarget, bool dryRun, bool verbose)
    {
        //var rollbackTarget = args.FirstOrDefault(a => a.StartsWith("--rollback="))?.Split("=")[1];
        //var db = args.FirstOrDefault(a => a.StartsWith("--db="))?.Split("=")[1] ?? "neo4j";
        //var dir = args.FirstOrDefault(a => a.StartsWith("--dir="))?.Split("=")[1] ?? "Scripts/InitData";
        //var dryRun = args.Contains("--dry-run");
        //var verbose = args.Contains("--verbose");
        var rollbackPath = Path.Combine(dir, Path.GetFileNameWithoutExtension(rollbackTarget) + ".rollback.cypher");

        await using var session = driver.AsyncSession(o => o.WithDatabase(db));

        if (File.Exists(rollbackPath))
        {
            WriteColorLine($"🧨 Found rollback script: {Path.GetFileName(rollbackPath)}", ConsoleColor.Red);

            if (dryRun)
            {
                WriteColorLine("📝 Dry run enabled — printing rollback script:", ConsoleColor.DarkYellow);
                WriteColorLine(await File.ReadAllTextAsync(rollbackPath), ConsoleColor.Gray);
            }
            else
            {
                var rollbackCypher = await File.ReadAllTextAsync(rollbackPath);
                var statements = rollbackCypher.Split(";", StringSplitOptions.RemoveEmptyEntries);

                await session.ExecuteWriteAsync(async tx =>
                {
                    foreach (var stmt in statements)
                    {
                        var trimmed = stmt.Trim();
                        if (!string.IsNullOrWhiteSpace(trimmed))
                        {
                            if (verbose)
                                WriteColorLine($"\n💬 Executing:\n{trimmed}\n", ConsoleColor.Cyan);

                            await tx.RunAsync(trimmed);
                        }
                    }
                });

                WriteColorLine("✅ Rollback script executed.", ConsoleColor.Green);
            }
        }
        else
        {
            WriteColorLine($"⚠️ No rollback file found at {rollbackPath}, continuing with ScriptRun cleanup only.", ConsoleColor.DarkYellow);
        }

        if (dryRun)
        {
            WriteColorLine($"📝 Dry run: would remove ScriptRun node for '{rollbackTarget}'", ConsoleColor.Magenta);
        }
        else
        {
            var deleted = await session.ExecuteWriteAsync(async tx =>
            {
                var res = await tx.RunAsync("MATCH (s:ScriptRun { name: $name }) DETACH DELETE s RETURN count(*) AS count", new { name = $"{rollbackTarget}.cypher" });
                var record = await res.SingleAsync();
                return record["count"].As<int>();
            });

            if (deleted > 0)
                WriteColorLine($"🧹 Removed ScriptRun node for '{rollbackTarget}'.", ConsoleColor.Magenta);
            else
                WriteColorLine($"⚠️ No ScriptRun node found for '{rollbackTarget}'.", ConsoleColor.DarkYellow);
        }

        await driver.DisposeAsync();
    }
    static async Task<HashSet<string>> GetAppliedScripts(IDriver driver, string db)
    {
        var result = new HashSet<string>();

        await using var session = driver.AsyncSession(o => o.WithDatabase(db));
        await session.ExecuteReadAsync(async tx =>
        {
            var cursor = await tx.RunAsync("MATCH (s:ScriptRun) RETURN s.name AS name");
            while (await cursor.FetchAsync())
            {
                var name = cursor.Current["name"].As<string>();
                result.Add(name);
            }
        });

        return result;
    }
    static void WriteColorLine(string message, ConsoleColor color)
    {
        var prev = Console.ForegroundColor;
        Console.ForegroundColor = color;
        Console.WriteLine(message);
        Console.ForegroundColor = prev;
    }
    static string? GetArg(string[] args, string prefix)
    {
        return args.FirstOrDefault(a => a.StartsWith(prefix))?.Split("=", 2)[1];
    }
    static void PrintHelp()
    {
        Console.WriteLine("Chrono.Graph.Migrations CLI");
        Console.WriteLine("----------------------------");
        Console.WriteLine("Usage:");
        Console.WriteLine("  dotnet run --project Chrono.Graph.Migrations [options]");
        Console.WriteLine();
        Console.WriteLine("Options:");
        Console.WriteLine("  --db=<name>           Target Neo4j database (default: neo4j or $NEO4J_DATABASE)");
        Console.WriteLine("  --uri=<uri>           Neo4j connection URI (default: bolt://localhost:7687 or $NEO4J_URI)");
        Console.WriteLine("  --user=<username>     Username for Neo4j (default: neo4j or $NEO4J_USER)");
        Console.WriteLine("  --only=<filename>     Only run a specific script");
        Console.WriteLine("  --dir=<path>          Directory to load scripts from (default: scripts/init)");
        Console.WriteLine("  --dry-run             Simulate execution without applying changes");
        Console.WriteLine("  --rollback=<script>   Roll back a specific script using optional .rollback.cypher file");
        Console.WriteLine("  --list                Show all scripts and their status (applied or pending)");
        Console.WriteLine("  --plan                Show what would be run, but don’t execute anything");
        Console.WriteLine("  --verbose             Output each Cypher statement being run");
        Console.WriteLine("  --help, -h            Show this help message");
    }


}

