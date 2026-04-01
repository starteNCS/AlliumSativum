package com.rest.connector;

import com.facebook.presto.spi.ConnectorSession;
import com.facebook.presto.spi.ConnectorSplitSource;
import com.facebook.presto.spi.ConnectorTableLayoutHandle;
import com.facebook.presto.spi.FixedSplitSource;
import com.facebook.presto.spi.connector.ConnectorSplitManager;
import com.facebook.presto.spi.connector.ConnectorTransactionHandle;

import java.util.Collections;

public class RestSplitManager implements ConnectorSplitManager {
    private final String baseUrl;

    public RestSplitManager(String baseUrl) {
        this.baseUrl = baseUrl;
    }

    @Override
    public ConnectorSplitSource getSplits(ConnectorTransactionHandle transactionHandle, ConnectorSession session, ConnectorTableLayoutHandle layout, SplitSchedulingContext splitSchedulingContext) {
        RestTableLayoutHandle layoutHandle = (RestTableLayoutHandle) layout;
        RestTableHandle tableHandle = layoutHandle.getTableHandle();
        
        RestSplit split = new RestSplit(tableHandle.getTableName(), baseUrl);
        return new FixedSplitSource(Collections.singletonList(split));
    }
}
