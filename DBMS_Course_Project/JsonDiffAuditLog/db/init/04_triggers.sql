-- Generic audit trigger: diffs either whole row or a JSON column.
-- Usage:
--   EXECUTE FUNCTION public.audit_generic_trigger('jsoncol=data', 'id'); -- diff only JSON column 'data', PK is 'id'
--   EXECUTE FUNCTION public.audit_generic_trigger('id', 'other_pk');     -- diff whole row, PK is ('id','other_pk')

CREATE OR REPLACE FUNCTION public.audit_generic_trigger()
RETURNS TRIGGER
LANGUAGE plpgsql
AS $$
DECLARE
    v_before jsonb;
    v_after  jsonb;
    v_diff   jsonb;
    v_pk     jsonb := '{}'::jsonb;
    arg text;
    jsoncol text := NULL;
    i int;
    col text;
    val jsonb;
BEGIN
    -- Parse trigger arguments: optional 'jsoncol=colname', others are PK column names
    FOR i IN 0..TG_NARGS-1 LOOP
        arg := TG_ARGV[i];
        IF arg LIKE 'jsoncol=%' THEN
            jsoncol := substring(arg from 9);
        ELSE
            -- collect PK values into JSON object
            IF TG_OP = 'DELETE' THEN
                EXECUTE format('SELECT to_jsonb(($1).%I)', arg) INTO val USING OLD;
            ELSE
                EXECUTE format('SELECT to_jsonb(($1).%I)', arg) INTO val USING NEW;
            END IF;
            v_pk := v_pk || jsonb_build_object(arg, val);
        END IF;
    END LOOP;

    IF TG_OP = 'INSERT' THEN
        IF jsoncol IS NULL THEN
            v_before := NULL;
            v_after  := to_jsonb(NEW);
        ELSE
            EXECUTE format('SELECT to_jsonb(($1).%I)', jsoncol) INTO v_after USING NEW;
            v_before := NULL;
        END IF;
    ELSIF TG_OP = 'UPDATE' THEN
        IF jsoncol IS NULL THEN
            v_before := to_jsonb(OLD);
            v_after  := to_jsonb(NEW);
        ELSE
            EXECUTE format('SELECT to_jsonb(($1).%I)', jsoncol) INTO v_before USING OLD;
            EXECUTE format('SELECT to_jsonb(($1).%I)', jsoncol) INTO v_after  USING NEW;
        END IF;
    ELSIF TG_OP = 'DELETE' THEN
        IF jsoncol IS NULL THEN
            v_before := to_jsonb(OLD);
            v_after  := NULL;
        ELSE
            EXECUTE format('SELECT to_jsonb(($1).%I)', jsoncol) INTO v_before USING OLD;
            v_after  := NULL;
        END IF;
    END IF;

    v_diff := public.compute_json_diff(v_before, v_after);

    INSERT INTO public.audit_log(table_name, pk, ts, op, before, after, diff)
    VALUES (TG_TABLE_SCHEMA || '.' || TG_TABLE_NAME, v_pk, now(), TG_OP, v_before, v_after, v_diff);

    RETURN COALESCE(NEW, OLD);
END;
$$;

-- Attach to items: diff only JSON column 'data'; PK = id
DROP TRIGGER IF EXISTS tr_audit_items ON public.items;
CREATE TRIGGER tr_audit_items
AFTER INSERT OR UPDATE OR DELETE ON public.items
FOR EACH ROW EXECUTE FUNCTION public.audit_generic_trigger('jsoncol=data', 'id');
