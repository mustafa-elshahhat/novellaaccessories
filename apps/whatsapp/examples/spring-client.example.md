# Spring Boot / Java Integration Example

## Configuration (application.yml)

```yaml
whatsapp:
  enabled: true
  service-url: http://localhost:4000
  internal-api-key: your-32-char-key-here
  timeout-seconds: 10
```

## HTTP Client Configuration

```java
// config/WhatsAppConfig.java
@Configuration
public class WhatsAppConfig {

    @Bean
    public RestClient whatsAppRestClient(WhatsAppProperties props) {
        return RestClient.builder()
            .baseUrl(props.getServiceUrl())
            .defaultHeader("x-internal-api-key", props.getInternalApiKey())
            .requestInterceptor((request, body, execution) -> {
                // Add timeout
                return execution.execute(request, body);
            })
            .build();
    }
}
```

## Properties Class

```java
// config/WhatsAppProperties.java
@ConfigurationProperties(prefix = "whatsapp")
public record WhatsAppProperties(
    boolean enabled,
    String serviceUrl,
    String internalApiKey,
    int timeoutSeconds
) {}
```

## Service Class

```java
// service/WhatsAppService.java
@Service
@Slf4j
public class WhatsAppService {

    private final RestClient restClient;
    private final WhatsAppProperties props;

    public WhatsAppService(RestClient whatsAppRestClient, WhatsAppProperties props) {
        this.restClient = whatsAppRestClient;
        this.props = props;
    }

    public boolean sendMessage(String phone, String message) {
        try {
            var body = new SendRequest(phone, message);
            var response = restClient.post()
                .uri("/send-message")
                .body(body)
                .retrieve()
                .body(SendResponse.class);
            return response != null && response.success();
        } catch (Exception e) {
            log.warn("WhatsApp send failed for {}: {}", maskPhone(phone), e.getMessage());
            return false;
        }
    }

    public WhatsAppStatus getStatus() {
        try {
            return restClient.get()
                .uri("/status")
                .retrieve()
                .body(WhatsAppStatus.class);
        } catch (Exception e) {
            log.warn("WhatsApp status check failed", e);
            return null;
        }
    }

    public boolean healthCheck() {
        try {
            var response = restClient.get()
                .uri("/health")
                .retrieve()
                .body(HealthResponse.class);
            return response != null && response.ok();
        } catch (Exception e) {
            return false;
        }
    }

    private String maskPhone(String phone) {
        if (phone == null || phone.length() < 6) return "****";
        return phone.substring(0, 2) + "*".repeat(phone.length() - 6) + phone.substring(phone.length() - 4);
    }

    private record SendRequest(String phone, String message) {}
    private record SendResponse(boolean success, String error) {}
    private record HealthResponse(boolean ok, String service, String whatsAppState) {}
    private record WhatsAppStatus(String state, boolean qrAvailable, String lastSentAt, String error) {}
}
```

## Enable Configuration Properties

```java
// Application.java or a config class
@EnableConfigurationProperties(WhatsAppProperties.class)
```

## Usage

```java
@Service
public class OrderService {

    private final WhatsAppService whatsApp;

    public OrderService(WhatsAppService whatsApp) {
        this.whatsApp = whatsApp;
    }

    public void confirmOrder(Order order) {
        // Business logic...
        
        if (whatsApp.sendMessage(
            order.getCustomerPhone(),
            "Order #" + order.getId() + " has been confirmed!"
        )) {
            log.info("Customer notified via WhatsApp");
        }
    }
}
```
