export function alreadyConnectedHtml() {
  return '<html><body style="background:#111;color:white;display:flex;flex-direction:column;align-items:center;justify-content:center;height:100vh;font-family:sans-serif;"><h1>Already Connected</h1><p>WhatsApp is linked and ready.</p></body></html>';
}

export function waitingForQrHtml() {
  return '<html><body style="background:#111;color:white;display:flex;flex-direction:column;align-items:center;justify-content:center;height:100vh;font-family:sans-serif;"><h1>Waiting for QR...</h1><p>The service is initializing. Please refresh in 5 seconds.</p><script>setTimeout(()=>location.reload(),5000)</script></body></html>';
}

export function qrPageHtml(qrDataUri) {
  return `
    <html>
      <body style="display:flex; flex-direction:column; align-items:center; justify-content:center; height:100vh; background:#111; color:white; font-family:sans-serif; text-align:center;">
        <h1 style="color:#25D366;">Scan with WhatsApp</h1>
        <div style="background:white; padding:20px; border-radius:15px; box-shadow: 0 10px 25px rgba(0,0,0,0.5);">
          <img src="${qrDataUri}" style="display:block;" />
        </div>
        <p style="margin-top:20px; opacity:0.8;">This QR code refreshes automatically.</p>
        <script>setTimeout(() => window.location.reload(), 30000);</script>
      </body>
    </html>
  `;
}
