# Nova Grafana Development Stack

Nova Observability local development/demo Grafana stack.

## Start

```powershell
docker compose -f deploy\grafana\docker-compose.yml up -d
```

## Stop

```powershell
docker compose -f deploy\grafana\docker-compose.yml down
```

## Endpoints

- Grafana: http://localhost:3000
- OTLP gRPC: http://localhost:4317
- OTLP HTTP/protobuf: http://localhost:4318

## Dashboard

`Dashboards -> Nova Observability -> Nova Operations`

### 1. Operasyon Sağlığı

Prometheus/Nova metrics kullanır:

- Online Servis
- Aktif Operation
- Operation
- Başarılı
- Başarısız
- Başarı Oranı
- P95 Süre
- Son Heartbeat
- Servis Bazında Operation
- Servis Bazında Başarısız
- Servis Bazında Uptime
- Servis Bazında P95

Gerçek metric adları:

- `nova_operation_active`
- `nova_operation_duration_seconds_bucket`
- `nova_operation_executions_total`
- `nova_service_alive`
- `nova_service_heartbeat_timestamp_seconds`
- `nova_service_uptime_seconds`

### 2. Log Sağlığı

Loki üzerinden:

- Toplam Log
- Bilgi
- Uyarı
- Hata
- Log Hacmi / Zaman
- Uyarı / Hata Trendi

### 3. Kayıt / Entity İnceleme

- Service
- Environment
- Level
- Entity ID
- Message Search
- Entity Timeline
- Hatalar

`Entity ID`, Loki'deki `nova_entity_id` structured metadata alanını kullanır.

`Message Search` literal aramadır. Boş bırakılırsa tüm mesajlar gösterilir.

V4'te variable adı `messageText` olarak değiştirilmiştir. Böylece eski dashboard URL'lerinde
kalabilecek `var-search=.*` değeri yeni Entity Timeline'ı etkilemez.

Log detayında aşağıdaki alanlar görülebilir:

- `MessageId`
- `nova_entity_id`
- `nova_entity_type`
- `nova_operation_name`
- `trace_id`
- `span_id`
- `service_instance_id`

Grafana Loki datasource üzerindeki trace derived-field yapılandırması kullanılarak logdan Tempo trace detayına geçilebilir.

## Tasarım

Bu dashboard generic Nova operasyon ekranıdır.
Yeni Nova servisi eklendiğinde dashboard JSON değiştirilmemelidir; servis `service_name`
üzerinden Service filtresine otomatik gelir.

Servise özel business ihtiyaçları varsa ayrı domain dashboard oluşturulmalıdır.

Bu stack development/demo içindir. Production'ta uygulama sunucularına Grafana kurulması hedeflenmez;
uygulamalar merkezi OTLP endpoint'ine telemetry gönderir.
