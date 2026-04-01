package com.rest.connector;

import com.facebook.presto.spi.Plugin;
import com.facebook.presto.spi.connector.ConnectorFactory;
import java.util.Collections;

public class RestPlugin implements Plugin {
    @Override
    public Iterable<ConnectorFactory> getConnectorFactories() {
        return Collections.singletonList(new RestConnectorFactory());
    }
}
