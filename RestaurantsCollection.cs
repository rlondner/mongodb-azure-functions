using MongoDB.Bson;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MongoDB.Tutorials.AzureFunctions
{
    public sealed class RestaurantsCollection
    {
        private static volatile IMongoCollection<BsonDocument> instance;
        private static object syncRoot = new Object();

        private RestaurantsCollection() { }

        public static IMongoCollection<BsonDocument> Instance
        {
            get
            {
                if (instance == null)
                {
                    lock (syncRoot)
                    {
                        if (instance == null)
                        {
                            string strMongoDBAtlasUri = System.Environment.GetEnvironmentVariable("MongoDBAtlasURI");
                            var client = new MongoClient(strMongoDBAtlasUri);
                            var db = client.GetDatabase("travel");
                            instance = db.GetCollection<BsonDocument>("restaurants");
                        }
                    }
                }
                return instance;
            }
        }
    }
}

