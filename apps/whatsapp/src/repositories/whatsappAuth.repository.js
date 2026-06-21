import { MongoClient } from 'mongodb';

export class WhatsappAuthRepository {
  constructor(mongodbUri) {
    this.mongodbUri = mongodbUri;
    this.mongo = null;
    this.collection = null;
  }

  getCollection() {
    if (!this.mongodbUri) {
      const err = new Error('mongodb_uri_required');
      err.statusCode = 503;
      throw err;
    }
    if (!this.collection) {
      this.mongo ??= new MongoClient(this.mongodbUri, { maxPoolSize: 5 });
      this.collection = this.mongo.db().collection('baileys_auth_state');
    }
    return this.collection;
  }

  async ensureConnection() {
    if (!this.mongodbUri) return;
    if (this.mongo) return;
    this.mongo = new MongoClient(this.mongodbUri, { maxPoolSize: 5 });
    await this.mongo.connect();
    this.collection = this.mongo.db().collection('baileys_auth_state');
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
