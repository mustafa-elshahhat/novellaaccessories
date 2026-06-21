import {
  CIRCUIT_BREAKER_THRESHOLD,
  CIRCUIT_BREAKER_COOLDOWN_MS,
} from '../config/constants.js';

export class CircuitBreaker {
  constructor(threshold = CIRCUIT_BREAKER_THRESHOLD, cooldownMs = CIRCUIT_BREAKER_COOLDOWN_MS) {
    this.threshold = threshold;
    this.cooldownMs = cooldownMs;
    this.failures = 0;
    this.openUntil = 0;
  }

  isOpen() {
    if (this.failures < this.threshold) return false;
    if (Date.now() >= this.openUntil) {
      this.failures = 0;
      return false;
    }
    return true;
  }

  recordFailure() {
    this.failures += 1;
    if (this.failures >= this.threshold) {
      this.openUntil = Date.now() + this.cooldownMs;
    }
  }

  recordSuccess() {
    this.failures = 0;
    this.openUntil = 0;
  }
}

