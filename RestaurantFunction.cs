using System.Net;
using System.Net.Http;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using MongoDB.Driver;
using MongoDB.Bson;
using System.Threading.Tasks;
using MongoDB.Bson.Serialization;
using Newtonsoft.Json;

namespace MongoDB.Tutorials.AzureFunctions
{
    public static class RestaurantFunction
    {
        [FunctionName("Restaurant")]
        public static async Task<HttpResponseMessage> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", "patch", "delete", Route = "Restaurant/id/{restaurantId}")]HttpRequestMessage req, string restaurantId, TraceWriter log)
        {
            log.Info("Restaurant function processed a request.");

            try
            {
                string strMongoDBAtlasUri = System.Environment.GetEnvironmentVariable("MongoDBAtlasURI");
                log.Info("The Atlas connection string is: " + strMongoDBAtlasUri);

                MongoUrl mongoUrl = new MongoUrl(strMongoDBAtlasUri);
                var settings = MongoClientSettings.FromUrl(mongoUrl);
                //for more on why we're using ServerSelectionTimeout, read https://scalegrid.io/blog/understanding-mongodb-client-timeout-options/
                settings.ServerSelectionTimeout = new System.TimeSpan(0, 0, 5);

                var client = new MongoClient(settings);
                var db = client.GetDatabase("travel");
                var collection = db.GetCollection<BsonDocument>("restaurants");

                //use the same restaurant_id filter for all get/update/delete queries
                var filter = Builders<BsonDocument>.Filter.Eq("restaurant_id", restaurantId);
                var result = new BsonDocument();
                string returnValue = string.Empty;
                HttpStatusCode returnStatusCode = HttpStatusCode.Forbidden;

                switch (req.Method.Method)
                {
                    case "GET":
                        var results = await collection.Find(filter).ToListAsync();
                        if (results.Count > 0)
                        {
                            result = results[0];
                            returnStatusCode = HttpStatusCode.OK;
                        }

                        else
                        {
                            returnValue = string.Format("A restaurant with id {0} could not be found", restaurantId);
                            returnStatusCode = HttpStatusCode.NotFound;
                        }
                        break;
                    case "PATCH":
                        string jsonContent = req.Content.ReadAsStringAsync().Result;
                        try
                        {
                            BsonDocument updateDoc = BsonSerializer.Deserialize<BsonDocument>(jsonContent);
                        }
                        catch (System.FormatException)
                        {
                            log.Info(string.Format("The JSON content {0} is invalid", jsonContent));
                        }

                        var update = Builders<BsonDocument>.Update
                            .Set("cuisine", "American (New)")
                            .CurrentDate("lastModified");
                        if (jsonContent != null && !string.IsNullOrEmpty(jsonContent))
                        {
                            result = await collection.FindOneAndUpdateAsync(filter, update);
                            returnStatusCode = HttpStatusCode.OK;
                        }
                        if (result == null)
                        {
                            returnValue = string.Format("A restaurant with id {0} could not be updated", restaurantId);
                            returnStatusCode = HttpStatusCode.NotFound;
                        }
                        break;
                    case "DELETE":
                        result = await collection.FindOneAndDeleteAsync(filter);
                        if (result == null)
                        {
                            returnValue = string.Format("A restaurant with id {0} could not be deleted", restaurantId);
                            returnStatusCode = HttpStatusCode.NotFound;
                        }
                        else
                        {
                            returnStatusCode = HttpStatusCode.OK;
                        }
                        break;
                    default:
                        break;
                }
                if (result != null && result.ElementCount > 0)
                    returnValue = result.ToJson();

                return req.CreateResponse(returnStatusCode, returnValue);
            }
            catch (System.Exception ex)
            {
                log.Error("An error occurred", ex);
                throw;
            }
        }
    }
}