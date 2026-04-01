package com.rest.connector;

import com.facebook.presto.spi.ColumnHandle;
import com.facebook.presto.common.type.Type;
import com.fasterxml.jackson.annotation.JsonCreator;
import com.fasterxml.jackson.annotation.JsonProperty;
import java.util.Objects;

public class RestColumnHandle implements ColumnHandle {
    private final String columnName;
    private final Type columnType;

    @JsonCreator
    public RestColumnHandle(
            @JsonProperty("columnName") String columnName,
            @JsonProperty("columnType") Type columnType) {
        this.columnName = Objects.requireNonNull(columnName, "columnName is null");
        this.columnType = Objects.requireNonNull(columnType, "columnType is null");
    }

    @JsonProperty
    public String getColumnName() {
        return columnName;
    }

    @JsonProperty
    public Type getColumnType() {
        return columnType;
    }

    @Override
    public int hashCode() {
        return Objects.hash(columnName, columnType);
    }

    @Override
    public boolean equals(Object obj) {
        if (this == obj) {
            return true;
        }
        if (obj == null || getClass() != obj.getClass()) {
            return false;
        }
        RestColumnHandle other = (RestColumnHandle) obj;
        return Objects.equals(this.columnName, other.columnName) &&
                Objects.equals(this.columnType, other.columnType);
    }

    @Override
    public String toString() {
        return columnName + ":" + columnType;
    }
}
