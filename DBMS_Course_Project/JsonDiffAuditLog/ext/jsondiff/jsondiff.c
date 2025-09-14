#include "postgres.h"
#include "fmgr.h"
#include "executor/spi.h"
#include "catalog/pg_type.h"
#include "utils/jsonb.h"

PG_MODULE_MAGIC;

PG_FUNCTION_INFO_V1(compute_json_diff);

Datum compute_json_diff(PG_FUNCTION_ARGS);

Datum
compute_json_diff(PG_FUNCTION_ARGS)
{
    Jsonb *before = PG_ARGISNULL(0) ? NULL : PG_GETARG_JSONB_P(0);
    Jsonb *after  = PG_ARGISNULL(1) ? NULL : PG_GETARG_JSONB_P(1);

    if (SPI_connect() != SPI_OK_CONNECT)
        elog(ERROR, "SPI_connect failed");

    static const char *query =
        "WITH keys AS ("
        "  SELECT key FROM jsonb_object_keys(COALESCE($1,'{}'::jsonb)) AS b(key)"
        "  UNION"
        "  SELECT key FROM jsonb_object_keys(COALESCE($2,'{}'::jsonb)) AS a(key)"
        "), set_pairs AS ("
        "  SELECT key, (COALESCE($2,'{}'::jsonb) -> key) AS val"
        "  FROM keys"
        "  WHERE (COALESCE($1,'{}'::jsonb) -> key) IS DISTINCT FROM (COALESCE($2,'{}'::jsonb) -> key)"
        "), unset_keys AS ("
        "  SELECT key FROM jsonb_object_keys(COALESCE($1,'{}'::jsonb)) AS b(key)"
        "  WHERE NOT (COALESCE($2,'{}'::jsonb) ? key)"
        ")"
        "SELECT jsonb_build_object("
        "  'set',   COALESCE((SELECT jsonb_object_agg(key, val) FROM set_pairs), '{}'::jsonb),"
        "  'unset', COALESCE((SELECT jsonb_agg(to_jsonb(key)) FROM unset_keys), '[]'::jsonb)"
        ")::jsonb";

    Oid argtypes[2] = { JSONBOID, JSONBOID };
    SPIPlanPtr plan = SPI_prepare(query, 2, argtypes);
    if (plan == NULL)
    {
        SPI_finish();
        elog(ERROR, "SPI_prepare failed: %s", SPI_result_code_string(SPI_result));
    }

    Datum values[2];
    char nulls[2];
    values[0] = PointerGetDatum(before);
    values[1] = PointerGetDatum(after);
    nulls[0] = (before == NULL) ? 'n' : ' '; 
    nulls[1] = (after  == NULL) ? 'n' : ' ';

    int rc = SPI_execute_plan(plan, values, nulls, true, 1);
    if (rc != SPI_OK_SELECT)
    {
        SPI_finish();
        elog(ERROR, "SPI_execute_plan failed: %s", SPI_result_code_string(rc));
    }

    if (SPI_processed != 1)
    {
        SPI_finish();
        elog(ERROR, "Unexpected rows returned from diff query: %lu", (unsigned long)SPI_processed);
    }

    bool isnull = false;
    Datum d = SPI_getbinval(SPI_tuptable->vals[0], SPI_tuptable->tupdesc, 1, &isnull);

    void *ret = NULL;
    if (!isnull)
        ret = PG_DETOAST_DATUM_COPY(d);

    SPI_finish();

    if (ret == NULL)
        PG_RETURN_NULL();
    PG_RETURN_POINTER(ret);
}

