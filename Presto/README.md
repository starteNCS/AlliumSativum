# Getting Started

Pull image:

```bash
docker pull prestodb/presto:latest
```

Run container:

```bash
cd /path/to/this/folder

docker run -p 8080:8080 -it -v ./config.properties:/opt/presto-server/etc/config.properties -v ./jvm.config:/opt/presto-server/etc/jvm.config --name presto prestodb/presto:latest
```