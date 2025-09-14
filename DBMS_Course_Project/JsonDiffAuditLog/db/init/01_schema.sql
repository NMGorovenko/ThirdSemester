-- Example application schema
CREATE SCHEMA IF NOT EXISTS public;

-- Demo table we audit
CREATE TABLE IF NOT EXISTS public.items (
    id          BIGSERIAL PRIMARY KEY,
    data        JSONB NOT NULL DEFAULT '{}'::jsonb,
    updated_at  TIMESTAMPTZ NOT NULL DEFAULT now()
);

CREATE INDEX IF NOT EXISTS idx_items_updated_at ON public.items (updated_at DESC);

