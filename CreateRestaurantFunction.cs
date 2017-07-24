using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;

using MongoDB.Driver;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;

namespace MongoDB.Tutorials.AzureFunctions
{
    public static class CreateRestaurantFunction
    {
        [FunctionName("CreateRestaurant")]
        public static async Task<HttpResponseMessage> Run([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = null)]HttpRequestMessage req, TraceWriter log)
        {
            log.Info("CreateRestaurant function processed a request.");       
            ObjectId itemId = ObjectId.Empty;
            string jsonContent = string.Empty;
            try
            {
                //retrieve the content from the request's body
                jsonContent = req.Content.ReadAsStringAsync().Result;
                //assuming we have valid JSON content, convert to BSON
                BsonDocument doc = BsonSerializer.Deserialize<BsonDocument>(jsonContent);
                var collection = MongoDBConnection.GetCollection(log);
                //store new document in MongoDB collection
                await collection.InsertOneAsync(doc);
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