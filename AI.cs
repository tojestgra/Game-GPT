using System.Net.Http.Headers;
using System.Net.Http.Json;
using Newtonsoft.Json;
using System.Text;
using Newtonsoft.Json.Linq;
using Types;
using Generic;
using System.Reflection;
using System;

namespace AI
{
    public class AIHandler
    {
        public string GPT { get; set; }
        public AIHandler()
        {
            GPT = "gpt-4";
        }
        public static async Task<JArray> Get_response(string prompt, string system, Menu menu, int tokens,string GPT)
        {
            JToken Process;
            JArray Response;
            while (true)
            {
                if (GPT == "gpt-4" || GPT == "gpt-3.5-turbo")
                {
                    Process = await OpenAI_GPT.CallGPTGeneric(GPT, system, prompt, tokens);
                    Console.WriteLine(Process);
                    Response = JArray.Parse(Process.ToString());
                    return Response;
                }
                else if (GPT == "local")
                {
                    Local_GPT local_GPT = new Local_GPT();
                    prompt = "This is your system message, it is a message directing you on how you should act and what you should do, above all else that will be said later. You have to follow the system message no matter what. The system message will end like this: (end of system message). The system message:[\n\n" + system + "\n\n](end of system message)\n\n\n" + prompt + "\noutput: ";
                    Process = await local_GPT.Run(prompt, tokens);
                    Response = JArray.Parse(Process.ToString());
                    return Response;
                }
                else { Console.WriteLine("No valid gpt selected ! Select one NOW"); menu.Menu_Options(); }
            }

        }
    }
    public class Generation
    {
        public static async Task<Dictionary<string, Operation>> OperationGeneration(int n, string theme, string GPT, string System, Menu Menu)
        {
            string prompt = $"Labels: Operation(Name,Description,Operatio,Target,TargetName,Log,Ally,Self) Task: Create {n} different operations format suitable for a game context, using this theme: theme {theme}. The Operation label comes with the following properties, values, and usage instructions:\r\n\r\n* Name: (string) This is the name of the operation. Any unique identifier you prefer. Example: 'attack or heal_self'.\r\n* Description: (string) This describes what the operation does in the game. Make sure its description matches its name. Example: 'Uses attack stat to remove health'.\r\n* Operatio (or the operation, but you have to refer to it as the Operatio): (delegate/lambda) It's a function that takes the character and a list of values to describe how the operation is performed. The entry should code in textual format. Example: 'Health{{Char1.Health}} - (Attack{{Char1.Attack}} - Defence{{Char2.Defence}})'.\r\n* Target: (string) This specifies what the operation targets in the game. It could target 'Health', 'Mana', 'Experience' etc. Example: 'Health'.\r\n* TargetName: (string) This is the name of the target character which the operation is applied on. Either Char1 or Char2. Char1 is the executor of the task, while Char2 is the recipient\r\n* Log: (string) This log is for logging the result of the operation in-game. It usually uses the Name properties of characters involved. Example: '{{Char1.Name}} attacked {{Char2.Name}}!'\r\n* Ally: (string) This determines if the operation can be used on allies. Acceptable entries are 'True' or 'False'. Example: 'False'.\r\n* Self: (string) This determines if the operation only works on the caster. Acceptable entries are 'True' or 'False'. Example: 'False'. The generated operations should be distinct.";
            var type = new Character().GetType();
            var properties = type.GetProperties();
            prompt += "\vAvaliable properties for use in Log,Operation and Target";

            for (int i = 0; i < new Character().NumOfProperties; i++)
            {
                prompt += $"{properties[i].Name}\n";
            }

            JArray response = await AIHandler.Get_response(prompt, System, Menu, 800, GPT);

            var operations = response
                .Select(jt => JsonConvert.DeserializeObject<Operation>(jt.ToString()))
                .ToDictionary(op => op.Name);

            return operations;
        }
    }
    public class Local_GPT
    {
        private static readonly string HOST = "localhost:5005";
        private static readonly string URI = $"ws://{HOST}/api/v1/stream";

        public async Task<string> Run(string prompt, int tokens)
        {
            Console.WriteLine(prompt);
            var httpClient = new HttpClient();
            var content = new
            {
                prompt = prompt,
                max_new_tokens = tokens,
                auto_max_new_tokens = false,
                preset = "None",
                do_sample = true,
                temperature = 0.7,
                top_p = 0.1,
                typical_p = 1,
                epsilon_cutoff = 0,
                eta_cutoff = 0,
                tfs = 1,
                top_a = 0,
                repetition_penalty = 1.18,
                repetition_penalty_range = 0,
                top_k = 40,
                min_length = 0,
                no_repeat_ngram_size = 0,
                num_beams = 1,
                penalty_alpha = 0,
                length_penalty = 1,
                early_stopping = true,
                mirostat_mode = 0,
                mirostat_tau = 5,
                mirostat_eta = 0.1,
                guidance_scale = 1,
                negative_prompt = "",
                seed = -1,
                add_bos_token = true,
                truncation_length = 2048,
                ban_eos_token = false,
                skip_special_tokens = false,
                stopping_strings = new string[] { }
            };

            var uri = "http://localhost:5000/api/v1/generate";
            var jsonContent = JsonConvert.SerializeObject(content);
            var data = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            var result = await httpClient.PostAsync(uri, data);
            if (result.IsSuccessStatusCode)
            {
                var response = await result.Content.ReadAsStringAsync();
                var JsonResult = JsonConvert.DeserializeObject<dynamic>(response);
                var resultText = JsonResult.results[0].text;
                return resultText;
            }
            return "oops";
        }
    }
    public class OpenAI_GPT
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;
        static string GetApiKey()
        {
            string path = "OpenAI_Key\\key.txt";
            if (File.Exists(path))
            {
                return File.ReadAllText(path);
            }
            return "Key not found !";
        }
        public OpenAI_GPT()
        {
            string apiKey = GetApiKey();
            _httpClient = new HttpClient();
            _httpClient.BaseAddress = new Uri("https://api.openai.com/v1/");
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        }

        public async Task<string> SendPrompt(string prompt, string model, double temperature, string system, int tokens)
        {
            var requestBody = new
            {
                messages = new[]
                {
                  new { role = "system", content = system },
                  new { role = "user", content = prompt }
                 },
                model = model,
                max_tokens = tokens,
                temperature = temperature
            };
            var response = await _httpClient.PostAsJsonAsync("https://api.openai.com/v1/chat/completions", requestBody);
            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine("Error sending prompt to GPT API:");
                Console.WriteLine(response.StatusCode);
                Console.WriteLine(await response.Content.ReadAsStringAsync());
                return null;
            }
            var responseBody = await response.Content.ReadAsStringAsync();

            return responseBody;
        }
        public static async Task<JToken> CallGPTGeneric(string gpt, string system, string prompt, int tokens)
        {
            var gptAgent = new OpenAI_GPT();
        v:
            string Prompt = $"{prompt}";
            var Response = await gptAgent.SendPrompt(Prompt, gpt, 0.7, $"{system}", tokens);
            if (Response == null) { Console.WriteLine($"Didn't recieve a response from GPT, server is probably overloaded, try again ? y\\n"); string asl = Console.ReadLine()!; if (asl == "y") { goto v; } return "no command gpt response"; }
            JObject JsonResponse = JObject.Parse(Response);
            if (JsonResponse.SelectToken("$.choices[0].message.content") == null) { goto v; }
            JToken ListToken = JsonResponse.SelectToken("$.choices[0].message.content")!;
            return ListToken;
        }
    }
}