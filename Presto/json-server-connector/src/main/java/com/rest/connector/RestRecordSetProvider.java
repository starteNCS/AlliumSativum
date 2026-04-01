package com.rest.connector;

import com.facebook.presto.spi.ColumnHandle;
import com.facebook.presto.spi.ConnectorSession;
import com.facebook.presto.spi.ConnectorSplit;
import com.facebook.presto.spi.RecordSet;
import com.facebook.presto.spi.connector.ConnectorRecordSetProvider;
import com.facebook.presto.spi.connector.ConnectorTransactionHandle;

import java.util.List;
import java.util.stream.Collectors;

public class RestRecordSetProvider implements ConnectorRecordSetProvider {

    @Override
    public RecordSet getRecordSet(ConnectorTransactionHandle transactionHandle, ConnectorSession session, ConnectorSplit split, List<? extends ColumnHandle> columns) {
        RestSplit restSplit = (RestSplit) split;
        
        List<RestColumnHandle> restColumns = columns.stream()
                .map(c -> (RestColumnHandle) c)
                .collect(Collectors.toList());

        return new RestRecordSet(restSplit, restColumns);
    }
}
