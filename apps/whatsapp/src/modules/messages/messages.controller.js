import { validateSendBody } from './messages.validator.js';
import { renderTemplate } from '../../templates/messages.template.js';
import { logger } from '../../config/logger.js';

export function sendMessageController(client, circuitBreaker) {
  return async (req, res) => {
    if (circuitBreaker.isOpen()) {
      return res.status(503).json({ success: false, error: 'circuit_open', retryable: true });
    }
    try {
      const { phone, message } = validateSendBody(req);
      const result = await client.sendMessage(phone, message);
      circuitBreaker.recordSuccess();
      res.json(successPayload(result));
    } catch (err) {
      const statusCode = err?.statusCode ?? 500;
      if (statusCode !== 400 && statusCode !== 429) {
        circuitBreaker.recordFailure();
      }
      respondWithError(res, err);
    }
  };
}

export function sendTemplateController(client, circuitBreaker) {
  return async (req, res) => {
    logger.warn('DEPRECATED: /send-template is not called by the outbox flow; this route will be removed in a future version');
    if (circuitBreaker.isOpen()) {
      return res.status(503).json({ success: false, error: 'circuit_open', retryable: true });
    }
    try {
      const { phone, template, data } = req.body ?? {};
      const message = renderTemplate(template, data);
      const result = await client.sendMessage(phone, message);
      circuitBreaker.recordSuccess();
      res.json(successPayload(result));
    } catch (err) {
      const statusCode = err?.statusCode ?? 500;
      if (statusCode !== 400 && statusCode !== 429) {
        circuitBreaker.recordFailure();
      }
      respondWithError(res, err);
    }
  };
}

function respondWithError(res, err) {
  const statusCode = Number.isInteger(err?.statusCode) ? err.statusCode : 500;
  if (statusCode === 500) {
    logger.error({ err }, 'Unexpected send error');
  }
  const retryable = err?.retryable !== undefined
    ? Boolean(err.retryable)
    : [408, 429, 503].includes(statusCode);
  const error = statusCode === 500
    ? 'send_failed'
    : (err instanceof Error ? err.message : 'send_failed');
  res.status(statusCode).json({ success: false, error, retryable });
}

function successPayload(result) {
  return result?.providerMessageId
    ? { success: true, providerMessageId: result.providerMessageId }
    : { success: true };
}

