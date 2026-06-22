import { MongoClient } from 'mongodb';

import { AUTH_STATE_COLLECTION, MONGODB_DATABASE } from '../config/constants.js';

export class WhatsappAuthRepository {
  constructor(mongodbUri, dbName = MONGODB_DATABASE) {
    this.mongodbUri = mongodbUri;
    this.dbName = dbName;
    this.mongo = null;
    this.collection = null;
  }

  requireUri() {
    if (!this.mongodbUri) {
      const err = new Error('mongodb_uri_required');
      err.statusCode = 503;
      throw err;
    }
  }

  getCollection() {
    this.requireUri();
    if (!this.collection) {
      this.mongo ??= new MongoClient(this.mongodbUri, { maxPoolSize: 5 });
      this.collection = this.mongo.db(this.dbName).collection(AUTH_STATE_COLLECTION);
    }
    return this.collection;
  }

  async ensureConnection() {
    if (!this.mongodbUri) return;
    if (this.mongo) return;
    this.mongo = new MongoClient(this.mongodbUri, { maxPoolSize: 5 });
    await this.mongo.connect();
    this.collection = this.mongo.db(this.dbName).collection(AUTH_STATE_COLLECTION);
  }

  /**
   * Connect, verify the link with a server `ping`, and ensure the required
   * collections and indexes exist. Idempotent — safe to call on every
   * (re)connect. Returns metadata describing what was provisioned, for
   * startup logging.
   */
  async bootstrap() {
    this.requireUri();
    await this.ensureConnection();
    const db = this.mongo.db(this.dbName);
    await db.command({ ping: 1 });
    const collections = await this.ensureCollections(db);
    return { database: db.databaseName, collections };
  }

  async ensureCollections(db) {
    const existing = await db.listCollections({}, { nameOnly: true }).toArray();
    const names = new Set(existing.map((c) => c.name));

    if (!names.has(AUTH_STATE_COLLECTION)) {
      try {
        await db.createCollection(AUTH_STATE_COLLECTION);
      } catch (err) {
        // Tolerate a concurrent creator winning the race.
        if (err?.codeName !== 'NamespaceExists' && err?.code !== 48) throw err;
      }
    }

    await db
      .collection(AUTH_STATE_COLLECTION)
      .createIndex({ updatedAt: 1 }, { name: 'updatedAt_1' });

    return [AUTH_STATE_COLLECTION];
  }

  async clearAll() {
    const collection = this.getCollection();
    await collection.deleteMany({});
  }

  async close() {
    await this.mongo?.close();
    this.mongo = null;
    this.collection = null;
  }
}
