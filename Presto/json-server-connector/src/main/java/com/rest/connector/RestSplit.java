package com.rest.connector;

import com.facebook.presto.spi.ConnectorSplit;
import com.facebook.presto.spi.HostAddress;
import com.facebook.presto.spi.NodeProvider;
import com.facebook.presto.spi.schedule.NodeSelectionStrategy;
import com.fasterxml.jackson.annotation.JsonCreator;
import com.fasterxml.jackson.annotation.JsonProperty;

import java.util.Collections;
import java.util.List;
import java.util.Objects;

public class RestSplit implements ConnectorSplit {
    private final String tableName;
    private final String baseUrl;

    @JsonCreator
    public RestSplit(
            @JsonProperty("tableName") String tableName,
            @JsonProperty("baseUrl") String baseUrl) {
        this.tableName = Objects.requireNonNull(tableName, "tableName is null");
        this.baseUrl = Objects.requireNonNull(baseUrl, "baseUrl is null");
    }

    @JsonProperty
    public String getTableName() {
        return tableName;
    }

    @JsonProperty
    public String getBaseUrl() {
        return baseUrl;
    }

    @Override
    public NodeSelectionStrategy getNodeSelectionStrategy() {
        return NodeSelectionStrategy.NO_PREFERENCE;
    }

    @Override
    public List<HostAddress> getPreferredNodes(NodeProvider nodeProvider) {
        return Collections.emptyList();
    }

    @Override
    public Object getInfo() {
        return this;
    }
}
