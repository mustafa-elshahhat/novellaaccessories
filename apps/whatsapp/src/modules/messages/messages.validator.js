export function validateSendBody(req) {
  const { phone, message } = req.body ?? {};
  if (!phone || !message) {
    const err = new Error('phone_and_message_required');
    err.statusCode = 400;
    throw err;
  }
  return { phone, message };
}
