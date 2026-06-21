export function statusController(client) {
  return (_req, res) => {
    res.json(client.status());
  };
}
