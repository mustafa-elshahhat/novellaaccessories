export function toWhatsAppJid(phone) {
  const digits = String(phone ?? '').replace(/\D/g, '');
  if (!digits) return null;
  return `${digits}@s.whatsapp.net`;
}

export function normalizePhone(phone) {
  const digits = String(phone ?? '').replace(/\D/g, '');
  if (!digits) return null;
  return digits;
}

export function maskPhone(phone) {
  const value = String(phone ?? '').trim();
  if (!value) return 'unknown';
  const digits = [...value].map((ch, index) => ({ ch, index })).filter((x) => /\d/.test(x.ch));
  if (digits.length <= 4) return '*'.repeat(value.length);
  const chars = [...value];
  for (const { index } of digits.slice(2, -4)) chars[index] = '*';
  return chars.join('');
}
