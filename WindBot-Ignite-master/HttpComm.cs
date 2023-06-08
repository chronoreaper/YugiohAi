using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static WindBot.PlayHistory;
using System.Net.Http;
using Newtonsoft.Json;

namespace WindBot
{
    class HttpComm
    {
        private static readonly HttpClient client = new HttpClient();

        public static async Task<Dictionary<long, double>> GetActionWeightsAsync(List<long> actionIds, List<long> compareIds)
        {
            string actions = "";
            string data = "";

            foreach (var i in actionIds)
            {
                actions += i.ToString() + " ";
            }
            actions = actions.Trim();

            foreach (var i in compareIds)
            {
                data += i.ToString() + " ";
            }
            data = data.Trim();

            var values = new Dictionary<string, string>
              {
                  { "data", data },
                  { "actions", actions }
              };
            var json = JsonConvert.SerializeObject(values);

            var httpContent = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await client.PostAsync("http://localhost:8000/", httpContent);

            var responseString = await response.Content.ReadAsStringAsync();

            var results = JsonConvert.DeserializeObject<Dictionary<string, string>>(responseString);

            var weights = new Dictionary<long, double>();
            foreach(var i in results.Keys)
            {
                weights.Add(long.Parse(i), double.Parse(results[i]));
            }

            return weights;
        }
    }
}
