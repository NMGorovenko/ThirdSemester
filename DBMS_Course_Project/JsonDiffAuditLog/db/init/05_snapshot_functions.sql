-- Snapshot utilities

-- Fast path: use the last 'after' before timestamp
CREATE OR REPLACE FUNCTION public.snapshot_after_at(
    p_table_name TEXT,
    p_pk         JSONB,
    p_ts         TIMESTAMPTZ
) RETURNS JSONB
LANGUAGE sql
AS $$
    SELECT al.after
    FROM public.audit_log al
    WHERE al.table_name = p_table_name
      AND al.pk = p_pk
      AND al.ts <= p_ts
    ORDER BY al.ts DESC
    LIMIT 1;
$$;

-- Rebuild from diffs only, ignoring stored 'after'
CREATE OR REPLACE FUNCTION public.snapshot_from_diffs_at(
    p_table_name TEXT,
    p_pk         JSONB,
    p_ts         TIMESTAMPTZ
) RETURNS JSONB
LANGUAGE plpgsql
AS $$
DECLARE
    rec RECORD;
    state JSONB;
    s JSONB;
    unset_keys JSONB;
    key TEXT;
BEGIN
    -- Initialize from the earliest 'before' we see in the range
    SELECT before INTO state
    FROM public.audit_log
    WHERE table_name = p_table_name AND pk = p_pk AND ts <= p_ts
    ORDER BY ts ASC
    LIMIT 1;

    state := COALESCE(state, '{}'::jsonb);

    FOR rec IN
        SELECT ts, op, diff
        FROM public.audit_log
        WHERE table_name = p_table_name AND pk = p_pk AND ts <= p_ts
        ORDER BY ts ASC
    LOOP
        IF rec.op = 'DELETE' THEN
            state := NULL;
        ELSE
            -- apply set
            s := COALESCE(rec.diff -> 'set', '{}'::jsonb);
            IF state IS NULL THEN
                state := '{}'::jsonb;
            END IF;
            state := state || s;

            -- apply unset
            unset_keys := rec.diff -> 'unset';
            IF unset_keys IS NOT NULL THEN
                FOR key IN SELECT jsonb_array_elements_text(unset_keys)
                LOOP
                    state := state - key;
                END LOOP;
            END IF;
        END IF;
    END LOOP;

    RETURN state;
END;
$$;

