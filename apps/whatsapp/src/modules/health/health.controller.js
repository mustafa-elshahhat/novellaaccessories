import { SERVICE_NAME } from '../../config/constants.js';

export function healthController(client) {
  return (_req, res) => {
    res.json({
      ok: true,
      service: SERVICE_NAME,
      whatsappState: client.status().state,
    });
  };
}
