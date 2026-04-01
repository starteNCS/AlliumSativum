package com.rest.connector;

import com.facebook.presto.spi.ConnectorTableHandle;
import com.fasterxml.jackson.annotation.JsonCreator;
import com.fasterxml.jackson.annotation.JsonProperty;
import java.util.Objects;

public class RestTableHandle implements ConnectorTableHandle {
    private final String schemaName;
    private final String tableName;

    @JsonCreator
    public RestTableHandle(
            @JsonProperty("schemaName") String schemaName,
            @JsonProperty("tableName") String tableName) {
        this.schemaName = Objects.requireNonNull(schemaName, "schemaName is null");
        this.tableName = Objects.requireNonNull(tableName, "tableName is null");
    }

    @JsonProperty
    public String getSchemaName() {
        return schemaName;
    }

    @JsonProperty
    public String getTableName() {
        return tableName;
    }

    @Override
    public int hashCode() {
        return Objects.hash(schemaName, tableName);
    }

    @Override
    public boolean equals(Object obj) {
        if (this == obj) {
            return true;
        }
        if (obj == null || getClass() != obj.getClass()) {
            return false;
        }
        RestTableHandle other = (RestTableHandle) obj;
        return Objects.equals(this.schemaName, other.schemaName) &&
                Objects.equals(this.tableName, other.tableName);
    }

    @Override
    public String toString() {
        return schemaName + ":" + tableName;
    }
}
