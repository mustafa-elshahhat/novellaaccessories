import { describe, it } from 'node:test';
import assert from 'node:assert/strict';

import { CircuitBreaker } from '../src/services/circuitBreaker.service.js';

describe('CircuitBreaker', () => {
  it('is closed initially', () => {
    const cb = new CircuitBreaker();
    assert.equal(cb.isOpen(), false);
  });

  it('stays closed after fewer failures than threshold', () => {
    const cb = new CircuitBreaker(3);
    cb.recordFailure();
    assert.equal(cb.isOpen(), false);
    cb.recordFailure();
    assert.equal(cb.isOpen(), false);
  });

  it('opens after reaching failure threshold', () => {
    const cb = new CircuitBreaker(3, 60_000);
    cb.recordFailure();
    cb.recordFailure();
    cb.recordFailure();
    assert.equal(cb.isOpen(), true);
  });

  it('resets failure count on success', () => {
    const cb = new CircuitBreaker(3, 60_000);
    cb.recordFailure();
    cb.recordFailure();
    cb.recordSuccess();
    assert.equal(cb.isOpen(), false);
    // Needs 3 fresh failures to open again.
    cb.recordFailure();
    assert.equal(cb.isOpen(), false);
  });

  it('auto-recovers after cooldown expires', () => {
    // 0ms cooldown: breaker closes immediately on next isOpen() check.
    const cb = new CircuitBreaker(2, 0);
    cb.recordFailure();
    cb.recordFailure();
    // Cooldown is 0ms so isOpen() auto-recovers.
    assert.equal(cb.isOpen(), false);
    assert.equal(cb.failures, 0);
  });

  it('uses custom threshold and cooldown', () => {
    const cb = new CircuitBreaker(2, 60_000);
    cb.recordFailure();
    assert.equal(cb.isOpen(), false);
    cb.recordFailure();
    assert.equal(cb.isOpen(), true);
  });

  it('uses default threshold of 3 from constants', () => {
    const cb = new CircuitBreaker();
    cb.recordFailure();
    assert.equal(cb.isOpen(), false);
    cb.recordFailure();
    assert.equal(cb.isOpen(), false);
    cb.recordFailure();
    assert.equal(cb.isOpen(), true);
  });
});
