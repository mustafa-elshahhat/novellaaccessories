import { describe, it, beforeEach, afterEach } from 'node:test';
import assert from 'node:assert/strict';

import { loadConfig } from '../src/config/env.js';
import { CircuitBreaker } from '../src/services/circuitBreaker.service.js';

const ENV_KEYS = ['INTERNAL_API_KEY', 'CIRCUIT_BREAKER_THRESHOLD', 'CIRCUIT_BREAKER_COOLDOWN_MS'];
let savedEnv;

beforeEach(() => {
  savedEnv = {};
  for (const key of ENV_KEYS) {
    savedEnv[key] = process.env[key];
    delete process.env[key];
  }
  process.env.INTERNAL_API_KEY = 'x'.repeat(32);
});

afterEach(() => {
  for (const key of ENV_KEYS) {
    if (savedEnv[key] === undefined) delete process.env[key];
    else process.env[key] = savedEnv[key];
  }
});

describe('loadConfig circuit breaker settings', () => {
  it('defaults to threshold 3 and cooldown 30000ms when unset', () => {
    const config = loadConfig();
    assert.equal(config.circuitBreakerThreshold, 3);
    assert.equal(config.circuitBreakerCooldownMs, 30_000);
  });

  it('reads CIRCUIT_BREAKER_THRESHOLD and CIRCUIT_BREAKER_COOLDOWN_MS from env', () => {
    process.env.CIRCUIT_BREAKER_THRESHOLD = '5';
    process.env.CIRCUIT_BREAKER_COOLDOWN_MS = '60000';

    const config = loadConfig();
    assert.equal(config.circuitBreakerThreshold, 5);
    assert.equal(config.circuitBreakerCooldownMs, 60_000);
  });

  it('falls back to defaults for invalid values', () => {
    process.env.CIRCUIT_BREAKER_THRESHOLD = 'not-a-number';
    process.env.CIRCUIT_BREAKER_COOLDOWN_MS = '-1';

    const config = loadConfig();
    assert.equal(config.circuitBreakerThreshold, 3);
    assert.equal(config.circuitBreakerCooldownMs, 30_000);
  });

  it('configured values are passed through to the CircuitBreaker', () => {
    process.env.CIRCUIT_BREAKER_THRESHOLD = '2';
    process.env.CIRCUIT_BREAKER_COOLDOWN_MS = '60000';

    const config = loadConfig();
    const breaker = new CircuitBreaker(config.circuitBreakerThreshold, config.circuitBreakerCooldownMs);

    breaker.recordFailure();
    assert.equal(breaker.isOpen(), false);
    breaker.recordFailure();
    assert.equal(breaker.isOpen(), true);
    assert.equal(breaker.threshold, 2);
    assert.equal(breaker.cooldownMs, 60_000);
  });
});
