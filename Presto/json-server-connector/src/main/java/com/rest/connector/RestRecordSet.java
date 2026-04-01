package com.rest.connector;

import com.facebook.presto.spi.RecordCursor;
import com.facebook.presto.spi.RecordSet;
import com.facebook.presto.common.type.Type;

import java.util.List;
import java.util.stream.Collectors;

public class RestRecordSet implements RecordSet {
    private final RestSplit split;
    private final List<RestColumnHandle> columnHandles;

    public RestRecordSet(RestSplit split, List<RestColumnHandle> columnHandles) {
        this.split = split;
        this.columnHandles = columnHandles;
    }

    @Override
    public List<Type> getColumnTypes() {
        return columnHandles.stream()
                .map(RestColumnHandle::getColumnType)
                .collect(Collectors.toList());
    }

    @Override
    public RecordCursor cursor() {
        return new RestRecordCursor(split, columnHandles);
    }
}
