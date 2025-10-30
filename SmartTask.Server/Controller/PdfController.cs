using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace SmartTask.Server.Controllers
{
    [ApiController]
    [Route("api/pdf")]
    public class PdfController : ControllerBase
    {
        private readonly IHttpClientFactory _clientFactory;
        private readonly IConfiguration _config;

        public PdfController(IHttpClientFactory clientFactory, IConfiguration config)
        {
            _clientFactory = clientFactory;
            _config = config;
        }

        [HttpPost("analyze")]
        public async Task<IActionResult> AnalyzePdf(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("No file uploaded");

            var apiKey = _config["OpenAI:ApiKey"];
            if (string.IsNullOrEmpty(apiKey))
                return StatusCode(500, "Missing OpenAI API key");

            // try {
            //     using var ms = new MemoryStream();
            //     await file.CopyToAsync(ms);
            //     ms.Position = 0;
            // } catch (exception e) {
            //     Console.WriteLine("Mistake 1");
            // }

            using var ms = new MemoryStream();
            await file.CopyToAsync(ms);
            ms.Position = 0;

            var httpClient = _clientFactory.CreateClient();
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

            // 🔹 OpenAI file upload + prompt request
            using var content = new MultipartFormDataContent();
            content.Add(new StreamContent(ms), "file", file.FileName);
            content.Add(new StringContent("assistants"), "purpose");

            var uploadResponse = await httpClient.PostAsync("https://api.openai.com/v1/files", content);

            var uploadJson = await uploadResponse.Content.ReadAsStringAsync();
            var fileData = JsonSerializer.Deserialize<JsonElement>(uploadJson);

            // 🧩 Check if the response contains an error
            if (fileData.TryGetProperty("error", out var errorProp))
            {
                var msg = errorProp.GetProperty("message").GetString();
                return StatusCode((int)uploadResponse.StatusCode, $"File upload failed: {msg}");
            }

            // ✅ Extract the file ID safely
            if (!fileData.TryGetProperty("id", out var idProp))
            {
                return StatusCode(500, $"Unexpected upload response: {uploadJson}");
            }
            var fileId = idProp.GetString();

            // 🧩 Read prompt text from external file
            string promptPath = Path.Combine(Directory.GetCurrentDirectory(), "prompt.txt");
            string prompt = await System.IO.File.ReadAllTextAsync(promptPath);


            // STEP 3️⃣ Send JSON request referencing uploaded file
            var body = new
            {
                model = "gpt-4.1-mini",
                input = new[]
                {
                    new
                    {
                        role = "user",
                        content = new object[]
                        {
                            new { type = "input_text", text = prompt },
                            new { type = "input_file", file_id = fileId }
                        }
                    }
                }
            };

            var json = JsonSerializer.Serialize(body);
            var stringContent = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await httpClient.PostAsync("https://api.openai.com/v1/responses", stringContent);

            if (!response.IsSuccessStatusCode)
            {
                var err = await response.Content.ReadAsStringAsync();
                return StatusCode((int)response.StatusCode, err+"Second Request");
            }

            var result = await response.Content.ReadAsStringAsync();
            return Ok(result);
        }
    }
}
