# Laravel / PHP Integration Example

## Configuration (.env)

```env
WHATSAPP_ENABLED=true
WHATSAPP_SERVICE_URL=http://localhost:4000
WHATSAPP_INTERNAL_API_KEY=your-32-char-key-here
WHATSAPP_TIMEOUT_SECONDS=10
```

## Service Class

```php
<?php

namespace App\Services;

use Illuminate\Support\Facades\Http;
use Illuminate\Support\Facades\Log;

class WhatsAppService
{
    protected string $serviceUrl;
    protected string $apiKey;
    protected int $timeout;

    public function __construct()
    {
        $this->serviceUrl = config('services.whatsapp.service_url');
        $this->apiKey = config('services.whatsapp.internal_api_key');
        $this->timeout = config('services.whatsapp.timeout_seconds', 10);
    }

    public function sendMessage(string $phone, string $message): bool
    {
        try {
            $response = Http::timeout($this->timeout)
                ->withHeaders(['x-internal-api-key' => $this->apiKey])
                ->post("{$this->serviceUrl}/send-message", [
                    'phone' => $phone,
                    'message' => $message,
                ]);

            $data = $response->json();
            return $data['success'] ?? false;
        } catch (\Exception $e) {
            Log::warning('WhatsApp send failed', [
                'phone' => $this->maskPhone($phone),
                'error' => $e->getMessage(),
            ]);
            return false;
        }
    }

    public function getStatus(): ?array
    {
        try {
            $response = Http::withHeaders(['x-internal-api-key' => $this->apiKey])
                ->get("{$this->serviceUrl}/status");
            return $response->json();
        } catch (\Exception $e) {
            return null;
        }
    }

    public function healthCheck(): bool
    {
        try {
            $response = Http::get("{$this->serviceUrl}/health");
            return $response->json('ok') === true;
        } catch (\Exception $e) {
            return false;
        }
    }

    protected function maskPhone(string $phone): string
    {
        if (strlen($phone) < 6) return '****';
        return substr($phone, 0, 2) . str_repeat('*', strlen($phone) - 6) . substr($phone, -4);
    }
}
```

## Configuration (config/services.php)

```php
'whatsapp' => [
    'service_url' => env('WHATSAPP_SERVICE_URL'),
    'internal_api_key' => env('WHATSAPP_INTERNAL_API_KEY'),
    'timeout_seconds' => env('WHATSAPP_TIMEOUT_SECONDS', 10),
],
```

## Usage

```php
use App\Services\WhatsAppService;

class OrderController extends Controller
{
    public function confirm(Order $order, WhatsAppService $whatsApp)
    {
        // Business logic...
        
        if (config('services.whatsapp.enabled')) {
            $whatsApp->sendMessage(
                $order->customer_phone,
                "Order #{$order->id} has been confirmed!"
            );
        }
    }
}
```
