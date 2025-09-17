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

    [Fact]
    public async Task Audit_For_Insert_Has_Full_Set_Empty_Unset()
    {
        await using var conn = new NpgsqlConnection(GetConnString());
        await conn.OpenAsync();

        // Insert a fresh row
        var payload = JsonSerializer.Serialize(new { name = "alpha", count = 10, active = true });
        var id = await conn.ExecuteScalarAsync<long>(
            "INSERT INTO public.items(data) VALUES (CAST(@j AS jsonb)) RETURNING id",
            new { j = payload }
        );

        var pkJson = JsonSerializer.Serialize(new { id });
        var rec = await conn.QueryFirstOrDefaultAsync<(string diff, string? before, string? after, string op)>(
            @"SELECT diff::text, before::text, after::text, op
              FROM public.audit_log
              WHERE table_name='public.items' AND pk = CAST(@pk AS jsonb) AND op='INSERT'
              ORDER BY ts DESC LIMIT 1",
            new { pk = pkJson }
        );

        Assert.Equal("INSERT", rec.op);
        Assert.Null(rec.before);
        Assert.NotNull(rec.after);
        var diff = JsonNode.Parse(rec.diff)!.AsObject();
        var set = diff["set"]!.AsObject();
        var unset = diff["unset"]!.AsArray();
        Assert.True(set.ContainsKey("name") && set.ContainsKey("count") && set.ContainsKey("active"));
        Assert.Equal(0, unset.Count);
    }

    [Fact]
    public async Task Audit_Update_Removing_Key_Populates_Unset()
    {
        await using var conn = new NpgsqlConnection(GetConnString());
        await conn.OpenAsync();

        // Start with object that has removable key
        var payload = JsonSerializer.Serialize(new { name = "beta", removable = 123, keep = "yes" });
        var id = await conn.ExecuteScalarAsync<long>(
            "INSERT INTO public.items(data) VALUES (CAST(@j AS jsonb)) RETURNING id",
            new { j = payload }
        );

        // Remove key 'removable'
        await conn.ExecuteAsync(
            "UPDATE public.items SET data = data - 'removable', updated_at = now() WHERE id = @id",
            new { id }
        );

        var pkJson = JsonSerializer.Serialize(new { id });
        var diffText = await conn.ExecuteScalarAsync<string>(
            "SELECT diff::text FROM public.audit_log WHERE table_name='public.items' AND pk = CAST(@pk AS jsonb) AND op='UPDATE' ORDER BY ts DESC LIMIT 1",
            new { pk = pkJson }
        );

        var diff = JsonNode.Parse(diffText!)!.AsObject();
        var unset = diff["unset"]!.AsArray();
        Assert.Contains("removable", unset.ToString());
    }

    [Fact]
    public async Task Snapshot_From_Diffs_Equals_Snapshot_After()
    {
        await using var conn = new NpgsqlConnection(GetConnString());
        await conn.OpenAsync();

        // Insert and two updates
        var payload = JsonSerializer.Serialize(new { name = "gamma", x = 1 });
        var id = await conn.ExecuteScalarAsync<long>(
            "INSERT INTO public.items(data) VALUES (CAST(@j AS jsonb)) RETURNING id",
            new { j = payload }
        );

        await conn.ExecuteAsync(
            "UPDATE public.items SET data = jsonb_set(data, '{x}', '2'::jsonb), updated_at = now() WHERE id = @id",
            new { id }
        );

        await conn.ExecuteAsync(
            "UPDATE public.items SET data = data || CAST('{\"y\":3}' AS jsonb), updated_at = now() WHERE id = @id",
            new { id }
        );

        var pkJson = JsonSerializer.Serialize(new { id });
        var lastTs = await conn.ExecuteScalarAsync<DateTime>(
            "SELECT max(ts) FROM public.audit_log WHERE table_name='public.items' AND pk = CAST(@pk AS jsonb)",
            new { pk = pkJson }
        );

        var snapAfter = await conn.ExecuteScalarAsync<string>(
            "SELECT public.snapshot_after_at('public.items', CAST(@pk AS jsonb), @ts)::text",
            new { pk = pkJson, ts = lastTs }
        );
        var snapDiffs = await conn.ExecuteScalarAsync<string>(
            "SELECT public.snapshot_from_diffs_at('public.items', CAST(@pk AS jsonb), @ts)::text",
            new { pk = pkJson, ts = lastTs }
        );

        Assert.Equal(
            JsonNode.Parse(snapAfter!)!.ToJsonString(new JsonSerializerOptions { WriteIndented = false }),
            JsonNode.Parse(snapDiffs!)!.ToJsonString(new JsonSerializerOptions { WriteIndented = false })
        );
    }

    [Fact]
    public async Task Audit_Delete_Has_Before_And_Null_After_And_Snapshot_Null()
    {
        await using var conn = new NpgsqlConnection(GetConnString());
        await conn.OpenAsync();

        var payload = JsonSerializer.Serialize(new { name = "delta", z = 100 });
        var id = await conn.ExecuteScalarAsync<long>(
            "INSERT INTO public.items(data) VALUES (CAST(@j AS jsonb)) RETURNING id",
            new { j = payload }
        );

        await conn.ExecuteAsync("DELETE FROM public.items WHERE id=@id", new { id });

        var pkJson = JsonSerializer.Serialize(new { id });
        var rec = await conn.QueryFirstOrDefaultAsync<(string? before, string? after, string diff, string op)>(
            @"SELECT before::text, after::text, diff::text, op
              FROM public.audit_log
              WHERE table_name='public.items' AND pk = CAST(@pk AS jsonb)
              ORDER BY ts DESC LIMIT 1",
            new { pk = pkJson }
        );

        Assert.Equal("DELETE", rec.op);
        Assert.NotNull(rec.before);
        Assert.Null(rec.after);

        var lastTs = await conn.ExecuteScalarAsync<DateTime>(
            "SELECT max(ts) FROM public.audit_log WHERE table_name='public.items' AND pk = CAST(@pk AS jsonb)",
            new { pk = pkJson }
        );
        var snapDiffs = await conn.ExecuteScalarAsync<string?>(
            "SELECT public.snapshot_from_diffs_at('public.items', CAST(@pk AS jsonb), @ts)::text",
            new { pk = pkJson, ts = lastTs }
        );
        Assert.Null(snapDiffs);
    }

    [Fact]
    public async Task Diff_On_Nested_Object_Replaces_Top_Level_Key()
    {
        await using var conn = new NpgsqlConnection(GetConnString());
        await conn.OpenAsync();

        var payload = JsonSerializer.Serialize(new { obj = new { a = 1, b = 2 } });
        var id = await conn.ExecuteScalarAsync<long>(
            "INSERT INTO public.items(data) VALUES (CAST(@j AS jsonb)) RETURNING id",
            new { j = payload }
        );

        var updated = JsonSerializer.Serialize(new { obj = new { a = 3, b = 2 } });
        await conn.ExecuteAsync(
            "UPDATE public.items SET data = CAST(@j AS jsonb), updated_at = now() WHERE id = @id",
            new { j = updated, id }
        );

        var pkJson = JsonSerializer.Serialize(new { id });
        var diffText = await conn.ExecuteScalarAsync<string>(
            "SELECT diff::text FROM public.audit_log WHERE table_name='public.items' AND pk = CAST(@pk AS jsonb) AND op='UPDATE' ORDER BY ts DESC LIMIT 1",
            new { pk = pkJson }
        );

        var diff = JsonNode.Parse(diffText!)!.AsObject();
        var set = diff["set"]!.AsObject();
        // Entire 'obj' replaced, not deep-diffed
        Assert.True(set.ContainsKey("obj"));
        Assert.Equal(3, set["obj"]!["a"]!.GetValue<int>());
        // No top-level 'a' key should appear
        Assert.False(set.ContainsKey("a"));
    }
}
