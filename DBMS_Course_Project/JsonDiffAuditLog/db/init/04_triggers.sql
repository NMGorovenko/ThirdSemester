-- Trigger to audit changes in public.items

CREATE OR REPLACE FUNCTION public.audit_items_trigger()
RETURNS TRIGGER
LANGUAGE plpgsql
AS $$
DECLARE
    v_before jsonb;
    v_after  jsonb;
    v_diff   jsonb;
    v_pk     jsonb;
BEGIN
    IF TG_OP = 'INSERT' THEN
        v_before := NULL;
        v_after  := NEW.data;
        v_pk     := jsonb_build_object('id', NEW.id);
    ELSIF TG_OP = 'UPDATE' THEN
        v_before := OLD.data;
        v_after  := NEW.data;
        v_pk     := jsonb_build_object('id', NEW.id);
    ELSIF TG_OP = 'DELETE' THEN
        v_before := OLD.data;
        v_after  := NULL;
        v_pk     := jsonb_build_object('id', OLD.id);
    END IF;

    v_diff := public.compute_json_diff(v_before, v_after);

    INSERT INTO public.audit_log(table_name, pk, ts, op, before, after, diff)
    VALUES (TG_TABLE_SCHEMA || '.' || TG_TABLE_NAME, v_pk, now(), TG_OP, v_before, v_after, v_diff);

    RETURN COALESCE(NEW, OLD);
END;
$$;

DROP TRIGGER IF EXISTS tr_audit_items ON public.items;
CREATE TRIGGER tr_audit_items
AFTER INSERT OR UPDATE OR DELETE ON public.items
FOR EACH ROW EXECUTE FUNCTION public.audit_items_trigger();
