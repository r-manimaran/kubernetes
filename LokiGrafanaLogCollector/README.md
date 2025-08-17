

http://localhost:3100/loki/api/v1/labels

 # LokiGrafanaLogCollector

 ## Troubleshooting

 ### Check Container Status
 ```bash
 # Check all containers
 docker-compose ps

 # Check specific container
 docker-compose ps loki
 ```

 ### Check Container Logs
 ```bash
 # Check logs for all services
 docker-compose logs

 # Check logs for specific service
 docker-compose logs loki
 docker-compose logs grafana
 docker-compose logs prometheus

 # Follow logs in real-time
 docker-compose logs -f loki

 # Show last 20 log entries
 docker-compose logs --tail=20 loki
 ```

 ### Restart Services
 ```bash
 # Restart all services
 docker-compose down && docker-compose up -d

 # Restart specific service
 docker-compose restart loki
 ```

 ### Verify Loki is Working
 ```bash
 # Check if Loki has any labels (indicates logs are present)
 curl -s "http://localhost:3100/loki/api/v1/labels"

 # Check specific label values
 curl -s "http://localhost:3100/loki/api/v1/label/job/values"
 ```

 ### Access Points
 - Grafana: http://localhost:3000
 - Loki API: http://localhost:3100
 - Prometheus: http://localhost:9090