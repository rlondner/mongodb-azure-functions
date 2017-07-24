using Microsoft.Azure.WebJobs.Host;
using MongoDB.Bson;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MongoDB.Tutorials.AzureFunctions
{
    internal class MongoDBConnection
    {
        private MongoDBConnection() { }

        private static IMongoDatabase InitConnection(TraceWriter log)
        {
            string strMongoDBAtlasUri = System.Environment.GetEnvironmentVariable("MongoDBAtlasURI");
            log.Info($"MongoDB connection string is {strMongoDBAtlasUri}");
            var client = new MongoClient(strMongoDBAtlasUri);
            return client.GetDatabase("travel");
        }

        internal static IMongoCollection<BsonDocument> GetCollection(TraceWriter log)
        {
            IMongoDatabase db = InitConnection(log);
            return db.GetCollection<BsonDocument>("restaurants");
        }
    }
}
