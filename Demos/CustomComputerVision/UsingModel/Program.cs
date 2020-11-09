using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;


namespace UsingModel
{
    class Program
    {
        static async Task Main(string[] args)
        {
            string predictionKey = "a05d08dd2f69414b966a8f6fb979181b";
            string predictionUrl = "https://customvisionrcg.cognitiveservices.azure.com/customvision/v3.0/Prediction/8dbb8cfe-fff5-48a6-b063-fc7295e55af1/classify/iterations/Iteration2/url";
            string imageUrl = "https://image.freepik.com/fotos-gratis/macro-da-carta-do-as-de-copas_58409-8498.jpg";

            HttpClient client = new HttpClient();

            // Request headers.
            client.DefaultRequestHeaders.Add(
                    "Prediction-Key", predictionKey);

            HttpContent content = new StringContent($"{{\"url\":\"{imageUrl}\"}}");
            content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

            HttpResponseMessage response;
            response = await client.PostAsync(predictionUrl, content);

            // Asynchronously get the JSON response.
            string contentString = await response.Content.ReadAsStringAsync();

            // Display the JSON response.
            Console.WriteLine("\nResponse:\n\n{0}\n",
                    JToken.Parse(contentString).ToString());

        }
    }
}
