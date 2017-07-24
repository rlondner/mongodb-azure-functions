using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;


namespace MongoDB.Tutorials.AzureFunctions
{
    public static class RestaurantFunction
    {
        static IMongoCollection<BsonDocument> collection;
        static FilterDefinition<BsonDocument> filter;
        static TraceWriter _log;

        [FunctionName("Restaurant")]
        public static Task<HttpResponseMessage> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", "patch", "delete", Route = "Restaurant/id/{restaurantId}")]HttpRequestMessage req, string restaurantId, TraceWriter log)
        {
            log.Info("Restaurant function processed a request.");

            var strMongoDBAtlasUri = Environment.GetEnvironmentVariable("MongoDBAtlasURI");
            log.Info($"Atlas connection string is: {strMongoDBAtlasUri}");

            var mongoUrl = new MongoUrl(strMongoDBAtlasUri);
            var settings = MongoClientSettings.FromUrl(mongoUrl);
            //for more on why we're using ServerSelectionTimeout, read https://scalegrid.io/blog/understanding-mongodb-client-timeout-options/
            settings.ServerSelectionTimeout = TimeSpan.FromSeconds(5);

            var client = new MongoClient(settings);
            var db = client.GetDatabase("travel");
            var collection = db.GetCollection<BsonDocument>("restaurants");

            try
            {
                switch (req.Method.Method)
                {
                    case "GET":
                        return RunGet(req, restaurantId, log, collection);
                    case "PATCH":
                        return RunPatch(req, restaurantId, log, collection);
                    case "DELETE":
                        return RunDelete(req, restaurantId, log, collection);
                    default:
                        return Task.FromResult(req.CreateResponse(HttpStatusCode.MethodNotAllowed));
                }
            }
            catch (System.Exception ex)
            {
                log.Error("An error occurred", ex);
                return Task.FromResult(req.CreateResponse(HttpStatusCode.InternalServerError));
            }
        }

        private static async Task<HttpResponseMessage> RunGet(HttpRequestMessage req, string restaurantId, TraceWriter log, IMongoCollection<BsonDocument> collection)
        {
            var filter = Builders<BsonDocument>.Filter.Eq("restaurant_id", restaurantId);
            var results = await collection.Find(filter).ToListAsync().ConfigureAwait(false);
            if (results.Count > 0)
            {
                return req.CreateResponse(HttpStatusCode.OK, results[0].ToString());
            }

            return req.CreateResponse(HttpStatusCode.NotFound, $"A restaurant with id {restaurantId} could not be found");
        }

        private static async Task<HttpResponseMessage> RunDelete(HttpRequestMessage req, string restaurantId, TraceWriter log, IMongoCollection<BsonDocument> collection)
        {
            var filter = Builders<BsonDocument>.Filter.Eq("restaurant_id", restaurantId);
            var result = await collection.FindOneAndDeleteAsync(filter).ConfigureAwait(false);
            if (result != null)
            {
                return req.CreateResponse(HttpStatusCode.OK);
            }

            return req.CreateResponse(HttpStatusCode.NotFound, $"A restaurant with id {restaurantId} could not be deleted");
        }

        private static async Task<HttpResponseMessage> RunPatch(HttpRequestMessage req, string restaurantId, TraceWriter log, IMongoCollection<BsonDocument> collection)
        {
            var filter = Builders<BsonDocument>.Filter.Eq("restaurant_id", restaurantId);
            string jsonContent = await req.Content.ReadAsStringAsync();
            BsonDocument changesDocument;
            try
            {
                changesDocument = BsonSerializer.Deserialize<BsonDocument>(jsonContent);
            }
            catch (System.FormatException)
            {
                var msg = $"The JSON content is invalid: {jsonContent}";
                log.Info(msg);
                return req.CreateResponse(HttpStatusCode.BadRequest, msg);
            }

            UpdateDefinition<BsonDocument> update = null;
            foreach (var change in changesDocument)
            {
                if (update == null)
                {
                    update = Builders<BsonDocument>.Update.Set(change.Name, change.Value);
                }
                else
                {
                    update = update.Set(change.Name, change.Value);
                }
            }

            //you can also use the simpler form below if you're OK with bypassing the UpdateDefinitionBuilder (and trust the JSON string to be fully correct)
            update = new BsonDocument("$set", changesDocument);

            //The following lines could be uncommented out for debugging purposes
            //var registry = collection.Settings.SerializerRegistry;
            //var serializer = collection.DocumentSerializer;
            //var rendered = update.Render(serializer, registry).ToJson();

            var updateResult = await collection.UpdateOneAsync(filter, update).ConfigureAwait(false);

            if (updateResult.ModifiedCount == 1)
            {
                return req.CreateResponse(HttpStatusCode.OK);
            }
            return req.CreateResponse(HttpStatusCode.NotFound, $"A restaurant with id {restaurantId} could not be updated");
        }
    }
}
