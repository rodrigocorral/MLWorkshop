using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace UseDomainModel
{
    class Program
    {
        static async Task Main(string[] args)
        {
            // Add your Computer Vision subscription key and endpoint to your environment variables.
            string subscriptionKey = "2c1485c9f1fb49fb910fa69e94168465";

            // An endpoint should have a format like "https://westus.api.cognitive.microsoft.com"
            string endpoint = "https://computervisionrcg.cognitiveservices.azure.com/";

            // the Batch Read method endpoint
            string uriBase = endpoint + "vision/v3.1/models/landmarks/analyze?model=landmarks";

            //Set the URL of an image that you want to analyze.
            string imageUrl = "https://miviaje.com/wp-content/uploads/2018/03/fuente-cibeles-madrid.jpg";

            HttpClient client = new HttpClient();

            // Request headers.
            client.DefaultRequestHeaders.Add(
                "Ocp-Apim-Subscription-Key", subscriptionKey);

            HttpContent content = new StringContent($"{{\"url\":\"{imageUrl}\"}}");
            content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

            HttpResponseMessage response;
            response = await client.PostAsync(uriBase, content);

            // Asynchronously get the JSON response.
            string contentString = await response.Content.ReadAsStringAsync();

            // Display the JSON response.
            Console.WriteLine("\nResponse:\n\n{0}\n",
                JToken.Parse(contentString).ToString());

        }
    }
}
