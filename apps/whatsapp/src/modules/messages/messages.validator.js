export function validateSendBody(req) {
  const { phone, message } = req.body ?? {};
  const normalizedPhone = typeof phone === 'string' ? phone.trim() : '';
  const normalizedMessage = typeof message === 'string' ? message.trim() : '';
  if (!normalizedPhone || !normalizedMessage) {
    const err = new Error('phone_and_message_required');
    err.statusCode = 400;
    throw err;
  }
  if (normalizedMessage.length > 4096) {
    const err = new Error('message_too_long');
    err.statusCode = 400;
    throw err;
  }
  return { phone: normalizedPhone, message: normalizedMessage };
}
