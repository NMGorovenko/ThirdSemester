-- Create C function that computes shallow JSONB diff
CREATE OR REPLACE FUNCTION public.compute_json_diff(before_json jsonb, after_json jsonb)
RETURNS jsonb
AS 'jsondiff', 'compute_json_diff'
LANGUAGE C IMMUTABLE STRICT;

