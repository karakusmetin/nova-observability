# Nova Grafana Development Stack

This environment provides a local OpenTelemetry backend for Nova
development and integration testing.

Components:

- Grafana
- OpenTelemetry Collector
- Loki
- Tempo
- Mimir

## Start

```powershell
docker compose -f deploy/grafana/docker-compose.yml up -d