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
    public static class CreateRestaurantFunction
    {
        [FunctionName("CreateRestaurant")]
        public static async Task<HttpResponseMessage> Run([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = null)]HttpRequestMessage req, TraceWriter log)
        {
            log.Info("CreateRestaurant function processed a request.");

            var strMongoDBAtlasUri = System.Environment.GetEnvironmentVariable("MongoDBAtlasURI");
            log.Info($"Atlas connection string is {strMongoDBAtlasUri}");

            var mongoUrl = new MongoUrl(strMongoDBAtlasUri);
            var settings = MongoClientSettings.FromUrl(mongoUrl);
            //for more on why we're using ServerSelectionTimeout, read https://scalegrid.io/blog/understanding-mongodb-client-timeout-options/
            settings.ServerSelectionTimeout = TimeSpan.FromSeconds(5);

            var client = new MongoClient(settings);           
            var db = client.GetDatabase("travel");

            var itemId = ObjectId.Empty;
            var jsonContent = string.Empty;

            try
            {
                //retrieving the content from the request's body
                jsonContent = await req.Content.ReadAsStringAsync().ConfigureAwait(false);
                //assuming we have valid JSON content, convert to BSON
                var doc = BsonSerializer.Deserialize<BsonDocument>(jsonContent);
                var collection = db.GetCollection<BsonDocument>("restaurants");
                //store new document in MongoDB collection
                await collection.InsertOneAsync(doc).ConfigureAwait(false);
                //retrieve the _id property created document
                itemId = (ObjectId)doc["_id"];
            }
            catch (System.FormatException fex)
            {
                //thrown if there's an error in the parsed JSON
                log.Error($"A format exception occurred, check the JSON document is valid: {jsonContent}", fex);
            }
            catch (System.TimeoutException tex)
            {
                log.Error("A timeout error occurred", tex);
            }
            catch (MongoException mdbex)
            {
                log.Error("A MongoDB error occurred", mdbex);
            }
            catch (System.Exception ex)
            {
                log.Error("An error occurred", ex);
            }

            return itemId == ObjectId.Empty
                ? req.CreateResponse(HttpStatusCode.BadRequest, "An error occurred, please check the function log")
                : req.CreateResponse(HttpStatusCode.OK, $"The created item's _id is  {itemId}");
        }
    }
}
