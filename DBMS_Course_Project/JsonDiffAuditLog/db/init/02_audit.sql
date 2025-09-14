-- Audit log schema
CREATE TABLE IF NOT EXISTS public.audit_log (
    id          BIGSERIAL PRIMARY KEY,
    table_name  TEXT NOT NULL,
    pk          JSONB NOT NULL,
    ts          TIMESTAMPTZ NOT NULL DEFAULT now(),
    op          TEXT NOT NULL CHECK (op IN ('INSERT', 'UPDATE', 'DELETE')),
    before      JSONB,
    after       JSONB,
    diff        JSONB
);

CREATE INDEX IF NOT EXISTS idx_audit_log_tbl_pk_ts ON public.audit_log(table_name, pk, ts DESC);
CREATE INDEX IF NOT EXISTS idx_audit_log_ts ON public.audit_log(ts DESC);

