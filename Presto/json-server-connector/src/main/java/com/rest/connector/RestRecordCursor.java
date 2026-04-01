package com.rest.connector;

import com.facebook.presto.spi.RecordCursor;
import com.facebook.presto.common.type.Type;
import com.fasterxml.jackson.databind.JsonNode;
import com.fasterxml.jackson.databind.ObjectMapper;
import io.airlift.slice.Slice;
import io.airlift.slice.Slices;

import java.io.IOException;
import java.net.URI;
import java.net.http.HttpClient;
import java.net.http.HttpRequest;
import java.net.http.HttpResponse;
import java.time.LocalDate;
import java.time.LocalDateTime;
import java.time.ZoneOffset;
import java.time.format.DateTimeFormatter;
import java.util.Iterator;
import java.util.List;

public class RestRecordCursor implements RecordCursor {
    private static final ObjectMapper MAPPER = new ObjectMapper();
    
    private final List<RestColumnHandle> columnHandles;
    private final Iterator<JsonNode> rows;
    private JsonNode currentRow;

    private long totalBytes = 0;

    public RestRecordCursor(RestSplit split, List<RestColumnHandle> columnHandles) {
        this.columnHandles = columnHandles;
        
        String url = split.getBaseUrl();
        if (url.endsWith("/")) {
            url = url.substring(0, url.length() - 1);
        }
        url += "/" + split.getTableName();

        try {
            HttpClient client = HttpClient.newHttpClient();
            HttpRequest request = HttpRequest.newBuilder()
                    .uri(URI.create(url))
                    .GET()
                    .build();

            HttpResponse<String> response = client.send(request, HttpResponse.BodyHandlers.ofString());
            if (response.statusCode() != 200) {
                throw new RuntimeException("Failed to fetch data from REST endpoint (" + url + "). Status: " + response.statusCode() + " Body: " + response.body());
            }

            String body = response.body();
            this.totalBytes = body.length();
            
            JsonNode rootNode = MAPPER.readTree(body);
            if (!rootNode.isArray()) {
                throw new RuntimeException("Expected a JSON array, got: " + rootNode.getNodeType());
            }

            this.rows = rootNode.elements();
            
        } catch (IOException | InterruptedException e) {
            throw new RuntimeException("Error fetching JSON from " + url, e);
        }
    }

    @Override
    public long getCompletedBytes() {
        return totalBytes;
    }

    @Override
    public long getReadTimeNanos() {
        return 0;
    }

    @Override
    public Type getType(int field) {
        return columnHandles.get(field).getColumnType();
    }

    @Override
    public boolean advanceNextPosition() {
        if (!rows.hasNext()) {
            return false;
        }
        currentRow = rows.next();
        return true;
    }

    private JsonNode getFieldValue(int field) {
        String columnName = columnHandles.get(field).getColumnName();
        return currentRow.get(columnName);
    }

    @Override
    public boolean getBoolean(int field) {
        JsonNode node = getFieldValue(field);
        return node != null && !node.isNull() && node.asBoolean();
    }

    @Override
    public long getLong(int field) {
        JsonNode node = getFieldValue(field);
        if (node == null || node.isNull()) {
            return 0L;
        }
        
        Type type = columnHandles.get(field).getColumnType();
        String typeSignature = type.getTypeSignature().getBase();
        
        if (typeSignature.equals("date")) {
            // DATE type in Presto represents days since epoch
            try {
                LocalDate date = LocalDate.parse(node.asText(), DateTimeFormatter.ISO_LOCAL_DATE);
                return date.toEpochDay();
            } catch (Exception e) {
                return 0L;
            }
        } else if (typeSignature.equals("timestamp")) {
            // TIMESTAMP type in Presto represents milliseconds since epoch
            try {
                LocalDateTime dateTime = LocalDateTime.parse(node.asText(), DateTimeFormatter.ISO_LOCAL_DATE_TIME);
                return dateTime.toInstant(ZoneOffset.UTC).toEpochMilli();
            } catch (Exception e) {
                return 0L;
            }
        }
        
        return node.asLong();
    }

    @Override
    public double getDouble(int field) {
        JsonNode node = getFieldValue(field);
        return (node != null && !node.isNull()) ? node.asDouble() : 0.0;
    }

    @Override
    public Slice getSlice(int field) {
        JsonNode node = getFieldValue(field);
        if (node == null || node.isNull()) {
            return Slices.utf8Slice("");
        }
        return Slices.utf8Slice(node.asText());
    }

    @Override
    public Object getObject(int field) {
        throw new UnsupportedOperationException("getObject not supported by RestRecordCursor");
    }

    @Override
    public boolean isNull(int field) {
        JsonNode node = getFieldValue(field);
        return node == null || node.isNull();
    }

    @Override
    public void close() {
        // No resources to release
    }
}
