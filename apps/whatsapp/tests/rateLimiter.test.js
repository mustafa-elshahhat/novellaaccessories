import { describe, it } from 'node:test';
import assert from 'node:assert/strict';

import { SendRateLimiter } from '../src/services/rateLimiter.service.js';

describe('SendRateLimiter', () => {
  describe('cooldown', () => {
    it('first send to a phone passes', () => {
      const limiter = new SendRateLimiter({
        sendDelayMinMs: 10_000,
        sendDelayMaxMs: 20_000,
        dailyPhoneLimit: 10,
        globalSendLimitPerMinute: 60,
      });

      assert.deepEqual(limiter.check('201234567890'), { ok: true });
    });

    it('immediate second send to same phone fails with phone_send_cooldown', () => {
      const limiter = new SendRateLimiter({
        sendDelayMinMs: 10_000,
        sendDelayMaxMs: 20_000,
        dailyPhoneLimit: 10,
        globalSendLimitPerMinute: 60,
      });

      limiter.record('201234567890');
      const result = limiter.check('201234567890');
      assert.equal(result.ok, false);
      assert.equal(result.reason, 'phone_send_cooldown');
    });

    it('different phone is not blocked by first phone cooldown', () => {
      const limiter = new SendRateLimiter({
        sendDelayMinMs: 10_000,
        sendDelayMaxMs: 20_000,
        dailyPhoneLimit: 10,
        globalSendLimitPerMinute: 60,
      });

      limiter.record('201234567890');
      assert.deepEqual(limiter.check('1987654321'), { ok: true });
    });

    it('returns 0 cooldown when SEND_DELAY_MIN_MS=0 and SEND_DELAY_MAX_MS=0', () => {
      const limiter = new SendRateLimiter({
        sendDelayMinMs: 0,
        sendDelayMaxMs: 0,
        dailyPhoneLimit: 10,
        globalSendLimitPerMinute: 60,
      });

      assert.equal(limiter.cooldownMs(), 0);
    });

    it('falls back to min when max < min', () => {
      const limiter = new SendRateLimiter({
        sendDelayMinMs: 100,
        sendDelayMaxMs: 50,
        dailyPhoneLimit: 10,
        globalSendLimitPerMinute: 60,
      });

      assert.equal(limiter.cooldownMs(), 100);
    });
  });

  describe('global rate limit', () => {
    it('blocks when global per-minute limit is exceeded', () => {
      const limiter = new SendRateLimiter({
        sendDelayMinMs: 0,
        sendDelayMaxMs: 0,
        dailyPhoneLimit: 10,
        globalSendLimitPerMinute: 2,
      });

      assert.deepEqual(limiter.check('phone1'), { ok: true });
      limiter.record('phone1');
      assert.deepEqual(limiter.check('phone2'), { ok: true });
      limiter.record('phone2');

      const result = limiter.check('phone3');
      assert.equal(result.ok, false);
      assert.equal(result.reason, 'global_rate_limit');
    });
  });

  describe('daily phone limit', () => {
    it('blocks when daily per-phone limit is exceeded', () => {
      const limiter = new SendRateLimiter({
        sendDelayMinMs: 0,
        sendDelayMaxMs: 0,
        dailyPhoneLimit: 2,
        globalSendLimitPerMinute: 60,
      });

      assert.deepEqual(limiter.check('201234567890'), { ok: true });
      limiter.record('201234567890');
      assert.deepEqual(limiter.check('201234567890'), { ok: true });
      limiter.record('201234567890');

      const result = limiter.check('201234567890');
      assert.equal(result.ok, false);
      assert.equal(result.reason, 'daily_phone_limit');
    });
  });

  describe('phone normalization', () => {
    it('treats +201234567890 and 201234567890 as the same phone for cooldown', () => {
      const limiter = new SendRateLimiter({
        sendDelayMinMs: 10_000,
        sendDelayMaxMs: 20_000,
        dailyPhoneLimit: 10,
        globalSendLimitPerMinute: 60,
      });

      limiter.record('+201234567890');
      const result = limiter.check('201234567890');
      assert.equal(result.ok, false);
      assert.equal(result.reason, 'phone_send_cooldown');
    });
  });

  describe('reserve', () => {
    it('atomically allows only the first of multiple immediate reservations when cooldown is enabled', () => {
      const limiter = new SendRateLimiter({
        sendDelayMinMs: 10_000,
        sendDelayMaxMs: 20_000,
        dailyPhoneLimit: 10,
        globalSendLimitPerMinute: 60,
      });

      const first = limiter.reserve('201234567890');
      assert.equal(first.ok, true);

      const second = limiter.reserve('201234567890');
      assert.equal(second.ok, false);
      assert.equal(second.reason, 'phone_send_cooldown');

      const third = limiter.reserve('201234567890');
      assert.equal(third.ok, false);
      assert.equal(third.reason, 'phone_send_cooldown');
    });

    it('returns global_rate_limit, daily_phone_limit, and phone_send_cooldown reasons like check()', () => {
      const limiter = new SendRateLimiter({
        sendDelayMinMs: 0,
        sendDelayMaxMs: 0,
        dailyPhoneLimit: 1,
        globalSendLimitPerMinute: 1,
      });

      const first = limiter.reserve('201234567890');
      assert.equal(first.ok, true);

      const second = limiter.reserve('201234567890');
      assert.equal(second.ok, false);
      assert.equal(second.reason, 'global_rate_limit');
    });

    it('blocks with daily_phone_limit once the daily quota is reserved', () => {
      const limiter = new SendRateLimiter({
        sendDelayMinMs: 0,
        sendDelayMaxMs: 0,
        dailyPhoneLimit: 1,
        globalSendLimitPerMinute: 60,
      });

      const first = limiter.reserve('201234567890');
      assert.equal(first.ok, true);

      const second = limiter.reserve('1987654321');
      assert.equal(second.ok, true);

      const third = limiter.reserve('201234567890');
      assert.equal(third.ok, false);
      assert.equal(third.reason, 'daily_phone_limit');
    });
  });

  describe('release', () => {
    it('does not consume the daily quota after a rollback', () => {
      const limiter = new SendRateLimiter({
        sendDelayMinMs: 0,
        sendDelayMaxMs: 0,
        dailyPhoneLimit: 1,
        globalSendLimitPerMinute: 60,
      });

      const reservation = limiter.reserve('201234567890');
      assert.equal(reservation.ok, true);

      limiter.release('201234567890', reservation);

      const retry = limiter.reserve('201234567890');
      assert.equal(retry.ok, true);
    });

    it('restores the per-phone cooldown after a rollback', () => {
      const limiter = new SendRateLimiter({
        sendDelayMinMs: 10_000,
        sendDelayMaxMs: 20_000,
        dailyPhoneLimit: 10,
        globalSendLimitPerMinute: 60,
      });

      const reservation = limiter.reserve('201234567890');
      assert.equal(reservation.ok, true);

      limiter.release('201234567890', reservation);

      const retry = limiter.reserve('201234567890');
      assert.equal(retry.ok, true);
    });

    it('does not count toward the global rate limit after a rollback', () => {
      const limiter = new SendRateLimiter({
        sendDelayMinMs: 0,
        sendDelayMaxMs: 0,
        dailyPhoneLimit: 10,
        globalSendLimitPerMinute: 1,
      });

      const reservation = limiter.reserve('201234567890');
      assert.equal(reservation.ok, true);

      limiter.release('201234567890', reservation);

      const retry = limiter.reserve('1987654321');
      assert.equal(retry.ok, true);
    });
  });

  describe('memory pruning', () => {
    it('prunes dailyByPhone entries from previous days on rollover', () => {
      let now = Date.parse('2024-01-01T12:00:00.000Z');
      const limiter = new SendRateLimiter(
        {
          sendDelayMinMs: 0,
          sendDelayMaxMs: 0,
          dailyPhoneLimit: 10,
          globalSendLimitPerMinute: 60,
        },
        () => now,
      );

      const oldKey = `${limiter.dayKey(now)}:201234567890`;
      limiter.reserve('201234567890');
      assert.equal(limiter.dailyByPhone.has(oldKey), true);

      now = Date.parse('2024-01-02T00:00:01.000Z');
      const newKey = `${limiter.dayKey(now)}:1987654321`;
      limiter.reserve('1987654321');

      assert.equal(limiter.dailyByPhone.has(oldKey), false);
      assert.equal(limiter.dailyByPhone.has(newKey), true);
      assert.equal(limiter.dailyByPhone.size, 1);
    });

    it('prunes expired phoneNextAllowed entries', () => {
      let now = Date.parse('2024-01-01T12:00:00.000Z');
      const limiter = new SendRateLimiter(
        {
          sendDelayMinMs: 1_000,
          sendDelayMaxMs: 1_000,
          dailyPhoneLimit: 10,
          globalSendLimitPerMinute: 60,
        },
        () => now,
      );

      limiter.reserve('201234567890');
      assert.equal(limiter.phoneNextAllowed.has('201234567890'), true);

      now += 2_000;
      limiter.reserve('1987654321');

      assert.equal(limiter.phoneNextAllowed.has('201234567890'), false);
    });
  });
});
