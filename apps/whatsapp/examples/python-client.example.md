# Python / FastAPI / Django / Flask Integration Example

## Configuration (.env)

```env
WHATSAPP_ENABLED=true
WHATSAPP_SERVICE_URL=http://localhost:4000
WHATSAPP_INTERNAL_API_KEY=your-32-char-key-here
WHATSAPP_TIMEOUT_SECONDS=10
```

## HTTP Client Wrapper

```python
# services/whatsapp_client.py
import logging
import os
from typing import Optional
import httpx

logger = logging.getLogger(__name__)

SERVICE_URL = os.getenv("WHATSAPP_SERVICE_URL", "http://localhost:4000")
API_KEY = os.getenv("WHATSAPP_INTERNAL_API_KEY", "")
TIMEOUT = int(os.getenv("WHATSAPP_TIMEOUT_SECONDS", "10"))


def mask_phone(phone: str) -> str:
    if not phone or len(phone) < 6:
        return "****"
    return phone[:2] + "*" * (len(phone) - 6) + phone[-4:]


async def send_message(phone: str, message: str) -> tuple[bool, bool]:
    """Returns (success, retryable)."""
    async with httpx.AsyncClient(timeout=TIMEOUT) as client:
        try:
            response = await client.post(
                f"{SERVICE_URL}/send-message",
                headers={
                    "x-internal-api-key": API_KEY,
                    "Content-Type": "application/json",
                },
                json={"phone": phone, "message": message},
            )
            data = response.json()
            if data.get("success"):
                return True, False
            logger.warning(
                "WhatsApp send failed",
                extra={"phone": mask_phone(phone), "error": data.get("error")},
            )
            return False, data.get("retryable", False)
        except Exception as exc:
            logger.warning(
                "WhatsApp send error",
                extra={"phone": mask_phone(phone), "error": str(exc)},
            )
            return False, True


async def get_status() -> Optional[dict]:
    async with httpx.AsyncClient() as client:
        try:
            response = await client.get(
                f"{SERVICE_URL}/status",
                headers={"x-internal-api-key": API_KEY},
            )
            return response.json()
        except Exception:
            return None


async def health_check() -> bool:
    try:
        async with httpx.AsyncClient() as client:
            response = await client.get(f"{SERVICE_URL}/health")
            data = response.json()
            return data.get("ok") is True
    except Exception:
        return False
```

## FastAPI Usage

```python
from fastapi import APIRouter
from services.whatsapp_client import send_message

router = APIRouter()

@router.post("/orders/{order_id}/notify")
async def notify_customer(order_id: str, phone: str):
    success, _ = await send_message(
        phone, f"Order #{order_id} has been confirmed!"
    )
    if not success:
        # Queue for retry
        pass
    return {"ok": success}
```

## Django Usage (sync variant)

```python
import requests

def send_message_sync(phone: str, message: str) -> bool:
    try:
        resp = requests.post(
            f"{SERVICE_URL}/send-message",
            headers={"x-internal-api-key": API_KEY},
            json={"phone": phone, "message": message},
            timeout=TIMEOUT,
        )
        return resp.json().get("success", False)
    except requests.RequestException:
        return False
```
