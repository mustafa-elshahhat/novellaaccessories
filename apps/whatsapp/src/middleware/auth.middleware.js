function bearerToken(req) {
  const header = req.get('authorization') ?? '';
  const match = /^Bearer\s+(.+)$/i.exec(header);
  return match?.[1] ?? null;
}

function constantTimeEquals(a, b) {
  // Reject empty/non-string inputs up front. Without this, two empty strings
  // would compare equal (the XOR loop never runs), letting an unset secret
  // (e.g. PAIRING_ADMIN_TOKEN === '') be matched by an empty caller token.
  if (typeof a !== 'string' || typeof b !== 'string') return false;
  if (a.length === 0 || b.length === 0 || a.length !== b.length) return false;
  let result = 0;
  for (let i = 0; i < a.length; i += 1) result |= a.charCodeAt(i) ^ b.charCodeAt(i);
  return result === 0;
}

export function requireInternalApiKey(config) {
  return (req, res, next) => {
    const token = req.get('x-internal-api-key') ?? bearerToken(req);
    if (!constantTimeEquals(token, config.internalApiKey)) {
      return res.status(401).json({ error: 'unauthorized' });
    }
    return next();
  };
}

export function requirePairingToken(config) {
  // PAIRING_ADMIN_TOKEN is optional; only honor token comparisons against it when set.
  const pairingTokenSet =
    typeof config.pairingAdminToken === 'string' && config.pairingAdminToken.length > 0;

  return (req, res, next) => {
    // Header-based auth: accepts both PAIRING_ADMIN_TOKEN (when set) and INTERNAL_API_KEY.
    const headerToken = req.get('x-pairing-admin-token') ??
                        bearerToken(req) ??
                        req.get('x-internal-api-key');

    if ((pairingTokenSet && constantTimeEquals(headerToken, config.pairingAdminToken)) ||
        constantTimeEquals(headerToken, config.internalApiKey)) {
      return next();
    }

    // URL-based auth (query/path): accepts PAIRING_ADMIN_TOKEN only, and only when set.
    // INTERNAL_API_KEY must never appear in URLs (browser history, access logs, referrers).
    if (pairingTokenSet) {
      const urlToken = req.query.token ?? req.params.token;
      if (constantTimeEquals(urlToken, config.pairingAdminToken)) {
        return next();
      }
    }

    return res.status(401).json({ error: 'unauthorized' });
  };
}
