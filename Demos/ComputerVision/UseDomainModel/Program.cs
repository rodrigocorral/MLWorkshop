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
            string subscriptionKey = "2d5810c4495d4841ad5e7d63e0f65ca3";

            // An endpoint should have a format like "https://westus.api.cognitive.microsoft.com"
            string endpoint = "https://mlworkshp.cognitiveservices.azure.com/";

            // the Batch Read method endpoint
            string uriBase = endpoint + "vision/v3.1/models/landmarks/analyze?model=landmarks";

            //Set the URL of an image that you want to analyze.
            string imageUrl = "https://upload.wikimedia.org/wikipedia/commons/f/f6/Bunker_Hill_Monument_2005.jpg";

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
