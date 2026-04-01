package com.rest.connector;

import com.facebook.presto.spi.ConnectorTableLayoutHandle;
import com.fasterxml.jackson.annotation.JsonCreator;
import com.fasterxml.jackson.annotation.JsonProperty;
import java.util.Objects;

public class RestTableLayoutHandle implements ConnectorTableLayoutHandle {
    private final RestTableHandle tableHandle;

    @JsonCreator
    public RestTableLayoutHandle(@JsonProperty("tableHandle") RestTableHandle tableHandle) {
        this.tableHandle = Objects.requireNonNull(tableHandle, "tableHandle is null");
    }

    @JsonProperty
    public RestTableHandle getTableHandle() {
        return tableHandle;
    }

    @Override
    public int hashCode() {
        return Objects.hash(tableHandle);
    }

    @Override
    public boolean equals(Object obj) {
        if (this == obj) {
            return true;
        }
        if (obj == null || getClass() != obj.getClass()) {
            return false;
        }
        RestTableLayoutHandle other = (RestTableLayoutHandle) obj;
        return Objects.equals(this.tableHandle, other.tableHandle);
    }

    @Override
    public String toString() {
        return tableHandle.toString();
    }
}
