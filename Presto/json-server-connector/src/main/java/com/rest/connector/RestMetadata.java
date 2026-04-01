package com.rest.connector;

import com.facebook.presto.spi.ColumnHandle;
import com.facebook.presto.spi.ColumnMetadata;
import com.facebook.presto.spi.ConnectorSession;
import com.facebook.presto.spi.ConnectorTableHandle;
import com.facebook.presto.spi.ConnectorTableLayout;
import com.facebook.presto.spi.ConnectorTableLayoutHandle;
import com.facebook.presto.spi.ConnectorTableLayoutResult;
import com.facebook.presto.spi.ConnectorTableMetadata;
import com.facebook.presto.spi.Constraint;
import com.facebook.presto.spi.SchemaTableName;
import com.facebook.presto.spi.SchemaTablePrefix;
import com.facebook.presto.spi.connector.ConnectorMetadata;
import com.facebook.presto.common.type.DateType;
import com.facebook.presto.common.type.DoubleType;
import com.facebook.presto.common.type.IntegerType;
import com.facebook.presto.common.type.TimestampType;
import com.facebook.presto.common.type.VarcharType;
import java.util.Arrays;
import java.util.Collections;
import java.util.HashMap;
import java.util.List;
import java.util.Map;
import java.util.Optional;
import java.util.Set;
import java.util.stream.Collectors;

public class RestMetadata implements ConnectorMetadata {
    private static final String SCHEMA_NAME = "default";
    private static final Map<String, List<ColumnMetadata>> schemas = new HashMap<>();

    static {
        schemas.put("cs_algorithm", Arrays.asList(
                ColumnMetadata.builder().setName("id").setType(IntegerType.INTEGER).build(),
                ColumnMetadata.builder().setName("name").setType(VarcharType.VARCHAR).build(),
                ColumnMetadata.builder().setName("family").setType(VarcharType.VARCHAR).build(),
                ColumnMetadata.builder().setName("task_type").setType(IntegerType.INTEGER).build(),
                ColumnMetadata.builder().setName("paper_doi").setType(VarcharType.VARCHAR).build(),
                ColumnMetadata.builder().setName("repo_url").setType(VarcharType.VARCHAR).build(),
                ColumnMetadata.builder().setName("version").setType(VarcharType.VARCHAR).build()));
        schemas.put("cs_benchmark", Arrays.asList(
                ColumnMetadata.builder().setName("id").setType(IntegerType.INTEGER).build(),
                ColumnMetadata.builder().setName("name").setType(VarcharType.VARCHAR).build(),
                ColumnMetadata.builder().setName("task_type").setType(IntegerType.INTEGER).build(),
                ColumnMetadata.builder().setName("dataset_id").setType(IntegerType.INTEGER).build(),
                ColumnMetadata.builder().setName("train_split_pct").setType(IntegerType.INTEGER).build(),
                ColumnMetadata.builder().setName("eval_metric").setType(VarcharType.VARCHAR).build(),
                ColumnMetadata.builder().setName("created_at").setType(DateType.DATE).build(),
                ColumnMetadata.builder().setName("notes").setType(VarcharType.VARCHAR).build()));
        schemas.put("cs_experiment_run", Arrays.asList(
                ColumnMetadata.builder().setName("id").setType(IntegerType.INTEGER).build(),
                ColumnMetadata.builder().setName("benchmark_id").setType(IntegerType.INTEGER).build(),
                ColumnMetadata.builder().setName("algorithm_id").setType(IntegerType.INTEGER).build(),
                ColumnMetadata.builder().setName("run_at").setType(TimestampType.TIMESTAMP).build(),
                ColumnMetadata.builder().setName("hardware_tag").setType(VarcharType.VARCHAR).build(),
                ColumnMetadata.builder().setName("random_seed").setType(IntegerType.INTEGER).build(),
                ColumnMetadata.builder().setName("train_time_secs").setType(DoubleType.DOUBLE).build(),
                ColumnMetadata.builder().setName("peak_memory_mb").setType(DoubleType.DOUBLE).build(),
                ColumnMetadata.builder().setName("git_commit").setType(VarcharType.VARCHAR).build(),
                ColumnMetadata.builder().setName("status").setType(VarcharType.VARCHAR).build()));
        schemas.put("cs_run_metric", Arrays.asList(
                ColumnMetadata.builder().setName("run_id").setType(IntegerType.INTEGER).build(),
                ColumnMetadata.builder().setName("metric_name").setType(VarcharType.VARCHAR).build(),
                ColumnMetadata.builder().setName("split").setType(VarcharType.VARCHAR).build(),
                ColumnMetadata.builder().setName("value").setType(DoubleType.DOUBLE).build(),
                ColumnMetadata.builder().setName("std_dev").setType(DoubleType.DOUBLE).build(),
                ColumnMetadata.builder().setName("k_folds").setType(IntegerType.INTEGER).build()));
        schemas.put("cs_hyperparameter", Arrays.asList(
                ColumnMetadata.builder().setName("run_id").setType(IntegerType.INTEGER).build(),
                ColumnMetadata.builder().setName("param_name").setType(VarcharType.VARCHAR).build(),
                ColumnMetadata.builder().setName("param_value").setType(VarcharType.VARCHAR).build(),
                ColumnMetadata.builder().setName("param_type").setType(VarcharType.VARCHAR).build()));
        schemas.put("cs_user_study", Arrays.asList(
                ColumnMetadata.builder().setName("id").setType(IntegerType.INTEGER).build(),
                ColumnMetadata.builder().setName("title").setType(VarcharType.VARCHAR).build(),
                ColumnMetadata.builder().setName("system_tested").setType(VarcharType.VARCHAR).build(),
                ColumnMetadata.builder().setName("algorithm_id").setType(IntegerType.INTEGER).build(),
                ColumnMetadata.builder().setName("instrument_id").setType(IntegerType.INTEGER).build(),
                ColumnMetadata.builder().setName("start_date").setType(DateType.DATE).build(),
                ColumnMetadata.builder().setName("end_date").setType(DateType.DATE).build(),
                ColumnMetadata.builder().setName("n_participants").setType(IntegerType.INTEGER).build(),
                ColumnMetadata.builder().setName("protocol").setType(VarcharType.VARCHAR).build()));
        schemas.put("cs_study_observation", Arrays.asList(
                ColumnMetadata.builder().setName("id").setType(IntegerType.INTEGER).build(),
                ColumnMetadata.builder().setName("user_study_id").setType(IntegerType.INTEGER).build(),
                ColumnMetadata.builder().setName("respondent_id").setType(IntegerType.INTEGER).build(),
                ColumnMetadata.builder().setName("task_label").setType(VarcharType.VARCHAR).build(),
                ColumnMetadata.builder().setName("completion_secs").setType(DoubleType.DOUBLE).build(),
                ColumnMetadata.builder().setName("error_count").setType(IntegerType.INTEGER).build(),
                ColumnMetadata.builder().setName("nasa_tlx_score").setType(DoubleType.DOUBLE).build(),
                ColumnMetadata.builder().setName("satisfaction").setType(DoubleType.DOUBLE).build(),
                ColumnMetadata.builder().setName("noted_at").setType(TimestampType.TIMESTAMP).build()));
        schemas.put("cs_task_type", Arrays.asList(
                ColumnMetadata.builder().setName("id").setType(IntegerType.INTEGER).build(),
                ColumnMetadata.builder().setName("name").setType(VarcharType.VARCHAR).build()));
    }

    @Override
    public List<String> listSchemaNames(ConnectorSession session) {
        return Collections.singletonList(SCHEMA_NAME);
    }

    @Override
    public ConnectorTableHandle getTableHandle(ConnectorSession session, SchemaTableName tableName) {
        if (!tableName.getSchemaName().equals(SCHEMA_NAME)) {
            return null;
        }
        if (!schemas.containsKey(tableName.getTableName())) {
            return null;
        }
        return new RestTableHandle(tableName.getSchemaName(), tableName.getTableName());
    }

    @Override
    public List<ConnectorTableLayoutResult> getTableLayouts(ConnectorSession session, ConnectorTableHandle table, Constraint<ColumnHandle> constraint, Optional<Set<ColumnHandle>> desiredColumns) {
        RestTableHandle tableHandle = (RestTableHandle) table;
        RestTableLayoutHandle layoutHandle = new RestTableLayoutHandle(tableHandle);
        return Arrays.asList(new ConnectorTableLayoutResult(getTableLayout(session, layoutHandle), constraint.getSummary()));
    }

    @Override
    public ConnectorTableLayout getTableLayout(ConnectorSession session, ConnectorTableLayoutHandle handle) {
        return new ConnectorTableLayout(handle);
    }

    @Override
    public ConnectorTableMetadata getTableMetadata(ConnectorSession session, ConnectorTableHandle table) {
        RestTableHandle tableHandle = (RestTableHandle) table;
        return new ConnectorTableMetadata(new SchemaTableName(tableHandle.getSchemaName(), tableHandle.getTableName()), schemas.get(tableHandle.getTableName()));
    }

    @Override
    public List<SchemaTableName> listTables(ConnectorSession session, Optional<String> schemaName) {
        if (schemaName.isPresent() && !schemaName.get().equals(SCHEMA_NAME)) {
            return Collections.emptyList();
        }
        return schemas.keySet().stream()
                .map(table -> new SchemaTableName(SCHEMA_NAME, table))
                .collect(Collectors.toList());
    }

    @Override
    public Map<String, ColumnHandle> getColumnHandles(ConnectorSession session, ConnectorTableHandle tableHandle) {
        RestTableHandle restTableHandle = (RestTableHandle) tableHandle;
        List<ColumnMetadata> columns = schemas.get(restTableHandle.getTableName());
        if (columns == null) {
            return Collections.emptyMap();
        }

        Map<String, ColumnHandle> columnHandles = new HashMap<>();
        for (ColumnMetadata column : columns) {
            columnHandles.put(column.getName(), new RestColumnHandle(column.getName(), column.getType()));
        }
        return columnHandles;
    }

    @Override
    public ColumnMetadata getColumnMetadata(ConnectorSession session, ConnectorTableHandle tableHandle, ColumnHandle columnHandle) {
        RestColumnHandle handle = (RestColumnHandle) columnHandle;
        return ColumnMetadata.builder().setName(handle.getColumnName()).setType(handle.getColumnType()).build();
    }

    @Override
    public Map<SchemaTableName, List<ColumnMetadata>> listTableColumns(ConnectorSession session, SchemaTablePrefix prefix) {
        Map<SchemaTableName, List<ColumnMetadata>> columns = new HashMap<>();
        for (SchemaTableName tableName : listTables(session, prefix.getSchemaName())) {
            if (schemas.containsKey(tableName.getTableName())) {
                columns.put(tableName, schemas.get(tableName.getTableName()));
            }
        }
        return columns;
    }
}
