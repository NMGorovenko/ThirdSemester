using System;
using System.Text.Json;
using System.Threading.Tasks;
using Dapper;
using Npgsql;

class Program
{
    static async Task Main()
    {
        var connString = Environment.GetEnvironmentVariable("AUDIT_DEMO_CONN")
            ?? "Host=localhost;Port=5442;Username=postgres;Password=postgres;Database=auditdb";

        Console.WriteLine($"Connecting: {connString}");
        await using var conn = new NpgsqlConnection(connString);
        await conn.OpenAsync();

        // Ensure DB is ready
        await conn.ExecuteAsync("SELECT 1");

        // 1) Insert a row
        var payload1 = JsonSerializer.Serialize(new { name = "alpha", count = 1 });
        var id = await conn.ExecuteScalarAsync<long>(
            "INSERT INTO public.items(data) VALUES (CAST(@json AS jsonb)) RETURNING id",
            new { json = payload1 }
        );
        Console.WriteLine($"Inserted id={id}");

        // 2) Update #1: change count, add flag
        var upd1 = JsonSerializer.Serialize(new { count = 2, flag = true });
        await conn.ExecuteAsync(
            "UPDATE public.items SET data = data || CAST(@j AS jsonb), updated_at = now() WHERE id = @id",
            new { j = upd1, id }
        );

        // 3) Update #2: remove flag, rename
        var upd2 = JsonSerializer.Serialize(new { name = "beta" });
        await conn.ExecuteAsync(
            "UPDATE public.items SET data = (data - 'flag') || CAST(@j AS jsonb), updated_at = now() WHERE id = @id",
            new { j = upd2, id }
        );

        // 4) Update #3: add nested
        var upd3 = JsonSerializer.Serialize(new { meta = new { rev = 3 } });
        await conn.ExecuteAsync(
            "UPDATE public.items SET data = data || CAST(@j AS jsonb), updated_at = now() WHERE id = @id",
            new { j = upd3, id }
        );

        // Print audit
        var pkJson = JsonSerializer.Serialize(new { id });
        var rows = await conn.QueryAsync(
            "SELECT id, ts, op, diff::text AS diff FROM public.audit_log WHERE table_name = 'public.items' AND pk = CAST(@pk AS jsonb) ORDER BY ts ASC",
            new { pk = pkJson }
        );

        Console.WriteLine("\nAudit log diffs:");
        foreach (var r in rows)
        {
            Console.WriteLine($"#{r.id}: {r.ts:o} {r.op} diff={r.diff}");
        }

        // Pick latest ts
        var lastTs = await conn.ExecuteScalarAsync<DateTime>(
            "SELECT max(ts) FROM public.audit_log WHERE table_name='public.items' AND pk = CAST(@pk AS jsonb)",
            new { pk = pkJson }
        );

        // Snapshot using 'after'
        var snapAfter = await conn.ExecuteScalarAsync<string>(
            "SELECT public.snapshot_after_at('public.items', CAST(@pk AS jsonb), @ts)::text",
            new { pk = pkJson, ts = lastTs }
        );
        Console.WriteLine($"\nSnapshot (after) at {lastTs:o}: {snapAfter}");

        // Snapshot from diffs only
        var snapDiffs = await conn.ExecuteScalarAsync<string>(
            "SELECT public.snapshot_from_diffs_at('public.items', CAST(@pk AS jsonb), @ts)::text",
            new { pk = pkJson, ts = lastTs }
        );
        Console.WriteLine($"Snapshot (diffs) at {lastTs:o}: {snapDiffs}");
    }
}
