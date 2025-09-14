using System;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Dapper;
using Npgsql;
using Xunit;

public class AuditLogTests
{
    private static string GetConnString() =>
        Environment.GetEnvironmentVariable("AUDIT_DEMO_CONN")
        ?? "Host=localhost;Port=5442;Username=postgres;Password=postgres;Database=auditdb";

    [Fact]
    public async Task Audit_For_Updates_Captures_Diff()
    {
        await using var conn = new NpgsqlConnection(GetConnString());
        await conn.OpenAsync();

        // Insert
        var payload = JsonSerializer.Serialize(new { name = "t", count = 1 });
        var id = await conn.ExecuteScalarAsync<long>(
            "INSERT INTO public.items(data) VALUES (CAST(@j AS jsonb)) RETURNING id",
            new { j = payload }
        );

        // Update adds a flag
        var upd = JsonSerializer.Serialize(new { flag = true });
        await conn.ExecuteAsync(
            "UPDATE public.items SET data = data || CAST(@j AS jsonb), updated_at = now() WHERE id = @id",
            new { j = upd, id }
        );

        var pkJson = JsonSerializer.Serialize(new { id });
        var diffText = await conn.ExecuteScalarAsync<string>(
            "SELECT diff::text FROM public.audit_log WHERE table_name='public.items' AND pk = CAST(@pk AS jsonb) AND op='UPDATE' ORDER BY ts DESC LIMIT 1",
            new { pk = pkJson }
        );

        Assert.False(string.IsNullOrWhiteSpace(diffText));
        var diff = JsonNode.Parse(diffText!)!.AsObject();
        var set = diff["set"]!.AsObject();
        Assert.True(set.ContainsKey("flag"));
        Assert.Equal(true, set["flag"]!.GetValue<bool>());

        // Snapshot functions should return non-null
        var lastTs = await conn.ExecuteScalarAsync<DateTime>(
            "SELECT max(ts) FROM public.audit_log WHERE table_name='public.items' AND pk = CAST(@pk AS jsonb)",
            new { pk = pkJson }
        );

        var snapAfter = await conn.ExecuteScalarAsync<string>(
            "SELECT public.snapshot_after_at('public.items', CAST(@pk AS jsonb), @ts)::text",
            new { pk = pkJson, ts = lastTs }
        );
        Assert.False(string.IsNullOrWhiteSpace(snapAfter));

        var snapDiffs = await conn.ExecuteScalarAsync<string>(
            "SELECT public.snapshot_from_diffs_at('public.items', CAST(@pk AS jsonb), @ts)::text",
            new { pk = pkJson, ts = lastTs }
        );
        Assert.False(string.IsNullOrWhiteSpace(snapDiffs));
    }
}
