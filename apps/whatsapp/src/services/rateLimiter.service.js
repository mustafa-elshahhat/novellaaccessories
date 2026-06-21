import { normalizePhone } from '../utils/phone.util.js';

export class SendRateLimiter {
  constructor(config, clock = () => Date.now()) {
    this.config = config;
    this.clock = clock;
    this.globalSends = [];
    this.dailyByPhone = new Map();
    this.phoneNextAllowed = new Map();
  }

  cooldownMs() {
    const min = this.config.sendDelayMinMs;
    const max = this.config.sendDelayMaxMs;
    if (min <= 0 && max <= 0) return 0;
    if (max < min) return min;
    return min + Math.floor(Math.random() * (max - min + 1));
  }

  dayKey(now) {
    return new Date(now).toISOString().slice(0, 10);
  }

  /** Drop stale dailyByPhone entries from previous days and expired cooldowns. */
  prune(now, dayKey) {
    for (const key of this.dailyByPhone.keys()) {
      if (!key.startsWith(`${dayKey}:`)) {
        this.dailyByPhone.delete(key);
      }
    }
    for (const [normalized, nextAllowed] of this.phoneNextAllowed) {
      if (now >= nextAllowed) {
        this.phoneNextAllowed.delete(normalized);
      }
    }
  }

  evaluate(phone, now, dayKey) {
    if (this.globalSends.length >= this.config.globalSendLimitPerMinute) {
      return { ok: false, reason: 'global_rate_limit' };
    }

    const normalized = normalizePhone(phone) ?? phone;
    const phoneKey = `${dayKey}:${normalized}`;
    const count = this.dailyByPhone.get(phoneKey) ?? 0;
    if (count >= this.config.dailyPhoneLimit) {
      return { ok: false, reason: 'daily_phone_limit' };
    }

    const nextAllowed = this.phoneNextAllowed.get(normalized);
    if (nextAllowed !== undefined && now < nextAllowed) {
      return { ok: false, reason: 'phone_send_cooldown' };
    }

    return { ok: true, normalized, phoneKey };
  }

  check(phone) {
    const now = this.clock();
    const dayKey = this.dayKey(now);
    this.globalSends = this.globalSends.filter((timestamp) => now - timestamp < 60_000);
    const result = this.evaluate(phone, now, dayKey);
    if (!result.ok) return result;
    return { ok: true };
  }

  record(phone) {
    const now = this.clock();
    const dayKey = this.dayKey(now);
    const normalized = normalizePhone(phone) ?? phone;
    const phoneKey = `${dayKey}:${normalized}`;
    this.globalSends.push(now);
    this.dailyByPhone.set(phoneKey, (this.dailyByPhone.get(phoneKey) ?? 0) + 1);

    const delay = this.cooldownMs();
    if (delay > 0) {
      this.phoneNextAllowed.set(normalized, now + delay);
    }
  }

  /**
   * Atomically check the global, daily, and per-phone cooldown limits and, if
   * allowed, record the usage in the same synchronous step. This prevents
   * concurrent requests from all passing the checks before any of them
   * records usage.
   */
  reserve(phone) {
    const now = this.clock();
    const dayKey = this.dayKey(now);

    this.globalSends = this.globalSends.filter((timestamp) => now - timestamp < 60_000);
    this.prune(now, dayKey);

    const result = this.evaluate(phone, now, dayKey);
    if (!result.ok) return result;

    const { normalized, phoneKey } = result;
    const previousNextAllowed = this.phoneNextAllowed.get(normalized);

    this.globalSends.push(now);
    this.dailyByPhone.set(phoneKey, (this.dailyByPhone.get(phoneKey) ?? 0) + 1);

    let nextAllowedSet = false;
    const delay = this.cooldownMs();
    if (delay > 0) {
      this.phoneNextAllowed.set(normalized, now + delay);
      nextAllowedSet = true;
    }

    return {
      ok: true,
      reservation: { normalized, phoneKey, timestamp: now, nextAllowedSet, previousNextAllowed },
    };
  }

  /**
   * Roll back a reservation made by `reserve()`. Only safe to call for
   * definitive non-delivery failures (e.g. `not_connected`) - never for
   * `send_timeout`, where the message may still have been delivered.
   */
  release(phone, reserveResult) {
    const reservation = reserveResult?.reservation;
    if (!reservation) return;

    const { normalized, phoneKey, timestamp, nextAllowedSet, previousNextAllowed } = reservation;

    const index = this.globalSends.indexOf(timestamp);
    if (index !== -1) this.globalSends.splice(index, 1);

    const count = this.dailyByPhone.get(phoneKey) ?? 0;
    if (count <= 1) {
      this.dailyByPhone.delete(phoneKey);
    } else {
      this.dailyByPhone.set(phoneKey, count - 1);
    }

    if (nextAllowedSet) {
      if (previousNextAllowed === undefined) {
        this.phoneNextAllowed.delete(normalized);
      } else {
        this.phoneNextAllowed.set(normalized, previousNextAllowed);
      }
    }
  }
}
