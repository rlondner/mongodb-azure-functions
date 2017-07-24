using System.Net;
using System.Net.Http;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using MongoDB.Driver;
using MongoDB.Bson;
using System.Threading.Tasks;
using MongoDB.Bson.Serialization;

namespace MongoDB.Tutorials.AzureFunctions
{
    public static class RestaurantFunction
    {
        static IMongoCollection<BsonDocument> collection;
        static FilterDefinition<BsonDocument> filter;
        static TraceWriter _log;

        [FunctionName("Restaurant")]
        public static async Task<HttpResponseMessage> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", "patch", "delete", Route = "Restaurant/id/{restaurantId}")]HttpRequestMessage req, string restaurantId, TraceWriter log)
        {
            log.Info("Restaurant function processed a request.");
            string returnValue = string.Empty;
            HttpStatusCode returnStatusCode = HttpStatusCode.Forbidden;
            string jsonContent = string.Empty;
            try
            {
                _log = log;
                collection = MongoDBConnection.GetCollection(log);
                //use the same restaurant_id filter for all get/update/delete queries
                filter = Builders<BsonDocument>.Filter.Eq("restaurant_id", restaurantId);
                var result = new BsonDocument();
                switch (req.Method.Method)
                {
                    case "GET":
                        RunGet(restaurantId, out returnStatusCode, out returnValue);
                        break;
                    case "PATCH":
                        jsonContent = await req.Content.ReadAsStringAsync().ConfigureAwait(false);
                        RunPatch(jsonContent, restaurantId, out returnStatusCode, out returnValue);
                        break;
                    case "DELETE":
                        RunDelete(restaurantId, out returnStatusCode, out returnValue);
                        break;
                    default:
                        break;
                }
            }
            catch (System.FormatException)
            {
                _log.Info($"The JSON content {jsonContent} is invalid");
            }
            catch (System.Exception ex)
            {
                log.Error("An error occurred", ex);
            }
            return req.CreateResponse(returnStatusCode, returnValue);
        }

        private static void RunGet(string restaurantId, out HttpStatusCode httpStatusCode, out string retValue)
        {
            var results = collection.Find(filter).ToList();
            var result = new BsonDocument();
            retValue = string.Empty;
            if (results.Count > 0)
            {
                result = results[0];
                if (result != null && result.ElementCount > 0)
                    retValue = result.ToJson();
                httpStatusCode = HttpStatusCode.OK;
            }
            else
            {
                retValue = $"A restaurant with id {restaurantId} could not be found";
                httpStatusCode = HttpStatusCode.NotFound;
            }
        }

        private static void RunPatch(string jsonContent, string restaurantId, out HttpStatusCode httpStatusCode, out string retValue)
        {
            httpStatusCode = HttpStatusCode.NotFound;
            retValue = string.Empty;
            var changesDocument = BsonSerializer.Deserialize<BsonDocument>(jsonContent);
            UpdateDefinition<BsonDocument> update = null;
            foreach (var change in changesDocument)
            {
                if (update == null)
                {
                    var builder = Builders<BsonDocument>.Update;
                    update = builder.Set(change.Name, change.Value);
                }
                else
                {
                    update = update.Set(change.Name, change.Value);
                }
            }
            //you can also use the simpler form below if you're OK with bypassing the UpdateDefinitionBuilder (and trust the JSON string to be fully correct)
            //update = new BsonDocumentUpdateDefinition<BsonDocument>(new BsonDocument("$set", changesDocument));
            
            //The following lines should be commented out for debugging purposes
            //var registry = BsonSerializer.SerializerRegistry;
            //var serializer = registry.GetSerializer<BsonDocument>();
            //var rendered = update.Render(serializer, registry).ToJson();
            var updateResult = collection.UpdateOne(filter, update);
            if (updateResult.ModifiedCount == 1)
            {
                httpStatusCode = HttpStatusCode.OK;
            }
            else
            {
                retValue = $"A restaurant with id {restaurantId} could not be updated";
                httpStatusCode = HttpStatusCode.NotFound;
                if (updateResult.MatchedCount == 1)
                {
                    retValue += " because this update would have left it unchanged";
                    httpStatusCode = HttpStatusCode.NotModified;
                }
            }
        }

        private static void RunDelete(string restaurantId, out HttpStatusCode httpStatusCode, out string retValue)
        {
            var deleteResult = collection.DeleteOne(filter);
            retValue = string.Empty;
            if (deleteResult.DeletedCount == 1)
            {
                httpStatusCode = HttpStatusCode.OK;
            }
            else
            {
                retValue = $"A restaurant with id {restaurantId} could not be deleted";
                httpStatusCode = HttpStatusCode.NotFound;
            }
        }
    }
}