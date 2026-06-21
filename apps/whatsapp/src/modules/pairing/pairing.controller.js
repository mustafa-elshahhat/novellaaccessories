import { alreadyConnectedHtml, waitingForQrHtml, qrPageHtml } from './pairing.html.js';

export function pairingController(config, client) {
  return (req, res) => {
    const pairing = client.pair();

    const wantsHtml = req.get('accept')?.includes('text/html') === true;
    if (wantsHtml && !config.enablePairingUi) {
      return res.status(403).json({ error: 'pairing_ui_disabled' });
    }

    if (wantsHtml) {
      if (pairing.state === 'connected') {
        return res.send(alreadyConnectedHtml());
      }
      if (!pairing.qrDataUri) {
        return res.send(waitingForQrHtml());
      }
      return res.send(qrPageHtml(pairing.qrDataUri));
    }

    if (pairing.state === 'connected') return res.status(409).json({ error: 'already_connected' });
    return res.json(pairing);
  };
}
