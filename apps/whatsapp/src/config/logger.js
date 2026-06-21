import pino from 'pino';
import { SERVICE_NAME } from './constants.js';

export const logger = pino({
  name: SERVICE_NAME,
  level: process.env.LOG_LEVEL ?? 'info',
});
