export function notFoundMiddleware(_req, res) {
  res.status(404).json({ error: 'not_found' });
}
