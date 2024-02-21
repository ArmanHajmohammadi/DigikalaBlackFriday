using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

class Program
{
    static async Task Main()
    {
        using (HttpClient client = new HttpClient())
        {
            Console.WriteLine("Process started!");
            // ##############################################################
            // YOU HAVE TO CHANGE THIS PART: 
            Console.WriteLine("fromPage? ");
            int fromPage = Convert.ToInt32(Console.ReadLine());
            Console.WriteLine("toPage? ");
            int toPage = Convert.ToInt32(Console.ReadLine());
            Console.WriteLine("Category? ");
            string category = Console.ReadLine();
            // ##############################################################
            // THE REST OF CODE
            int[] sortList = new int[] {7,1,4,22,20,21,25,27,29};
            List<int> productIds = new List<int>();
            List<string> validImages = new List<string>();
            List<Task> downloadTasks = new List<Task>();

            while (true){
               foreach (int sort in sortList)
                {
                    for (int pageNumber = fromPage; pageNumber <= toPage; pageNumber++)
                    {
                        string url = $"https://api.digikala.com/v1/categories/{category}/search/?page={pageNumber}&sort={sort}";
                        downloadTasks.Add(ProcessPageAsync(client, url, productIds, validImages));
                    }

                    await Task.WhenAll(downloadTasks);


                    Console.WriteLine($"####### one round of sort = {sort} finished #######");
               }
                Console.WriteLine("*****************************************************************");
                Console.WriteLine("*****************************************************************");
                Console.WriteLine("*****************************************************************");
                Console.WriteLine("***********************  ROUND FINISHED  ************************");
                Console.WriteLine("*****************************************************************");
                Console.WriteLine("*****************************************************************");
                Console.WriteLine("*****************************************************************");
            }
            
        }
    }

    static async Task ProcessPageAsync(HttpClient client, string url, List<int> productIds, List<string> validImages)
    {
        try
        {
            HttpResponseMessage response = await client.GetAsync(url);
            response.EnsureSuccessStatusCode();
            string responseBody = await response.Content.ReadAsStringAsync();
            JObject json = JObject.Parse(responseBody);
            JArray products = (JArray)json["data"]["products"];

            List<Task> imageDownloadTasks = new List<Task>();

            foreach (JToken product in products)
            {
                int productId = (int)product["id"];
                productIds.Add(productId);

                string productUrl = $"https://api.digikala.com/v2/product/{productId}/";
                HttpResponseMessage productResponse = await client.GetAsync(productUrl);
                productResponse.EnsureSuccessStatusCode();
                string productBody = await productResponse.Content.ReadAsStringAsync();
                JObject productJson = JObject.Parse(productBody);

                JArray images = (JArray)productJson["data"]["product"]["images"]["list"];

                foreach (JToken image in images)
                {
                    string imageUrl = (string)image["url"][0];

                    if (!validImages.Contains(imageUrl))
                    {
                        try
                        {
                            Console.WriteLine(imageUrl);
                            Uri uri = new Uri(imageUrl);
                            string fullPath = uri.AbsoluteUri.Split('?')[0];
                            string filename = System.IO.Path.GetFileName(fullPath);
                            validImages.Add(imageUrl);
                            imageDownloadTasks.Add(DownloadAndSaveImageAsync(client, imageUrl, $"./images/{filename}"));
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine(e);
                            throw;
                        }
                    }
                }
            }

            await Task.WhenAll(imageDownloadTasks);
        }
        catch (Exception ex) { }
    }

    static async Task DownloadAndSaveImageAsync(HttpClient client, string imageUrl, string filePath)
    {
        byte[] imageData = await client.GetByteArrayAsync(imageUrl);
        await File.WriteAllBytesAsync(filePath, imageData);
    }
}
