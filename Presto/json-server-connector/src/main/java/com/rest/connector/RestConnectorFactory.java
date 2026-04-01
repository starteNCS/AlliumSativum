package com.rest.connector;

import com.facebook.presto.spi.connector.Connector;
import com.facebook.presto.spi.connector.ConnectorContext;
import com.facebook.presto.spi.connector.ConnectorFactory;
import com.facebook.presto.spi.ConnectorHandleResolver;

import java.util.Map;

public class RestConnectorFactory implements ConnectorFactory {
    @Override
    public String getName() {
        return "rest";
    }

    @Override
    public ConnectorHandleResolver getHandleResolver() {
        return new RestHandleResolver();
    }

    @Override
    public Connector create(String catalogName, Map<String, String> config, ConnectorContext context) {
        String baseUrl = config.getOrDefault("rest.base.url", "https://allium-sativum.honsel.dev");
        return new RestConnector(baseUrl);
    }
}
