# Deploying the REST Connector to PrestoDB

Once you build the connector using `mvn clean package`, you will get an uber-JAR (with Jackson shaded) in the `target/` directory:
`target/json-server-connector-1.0-SNAPSHOT.jar`

## Docker Volume Mount Snippet

To run this connector in a `prestodb/presto:0.284` container, you need to:
1. Create a directory for the plugin inside `/opt/presto-server/plugin/rest` (Presto expects each plugin in its own folder).
2. Mount the built JAR into that plugin directory.
3. Mount the `rest.properties` file into `/opt/presto-server/etc/catalog/`.

Here is an example `docker-compose.yml` snippet:

```yaml
version: '3.8'

services:
  presto-coordinator:
    image: prestodb/presto:0.284
    ports:
      - "8080:8080"
    volumes:
      # Mount the compiled JAR directly into the plugin directory
      - ./target/json-server-connector-1.0-SNAPSHOT.jar:/opt/presto-server/plugin/rest/json-server-connector-1.0-SNAPSHOT.jar
      
      # Mount the catalog properties file to register our connector
      - ./catalog/rest.properties:/opt/presto-server/etc/catalog/rest.properties
```

## Running standalone via Docker 

```bash
docker run -p 8080:8080 \\
  -v $(pwd)/target/json-server-connector-1.0-SNAPSHOT.jar:/opt/presto-server/plugin/rest/json-server-connector-1.0-SNAPSHOT.jar \\
  -v $(pwd)/catalog/rest.properties:/opt/presto-server/etc/catalog/rest.properties \\
  prestodb/presto:0.284
```

After the container starts, you can query via Presto CLI:
```bash
docker exec -it <container_id> presto --catalog rest --schema default
presto:default> SELECT * FROM cs_algorithms;
```
