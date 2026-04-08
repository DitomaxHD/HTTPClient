using System.Text;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;

var services = new ServiceCollection();
services.AddHttpClient();

var provider = services.BuildServiceProvider();
var factory = provider.GetRequiredService<IHttpClientFactory>();

Console.WriteLine("HTTP Client – q zum Beenden\n");

while (true)
{
    Console.WriteLine("Methode: GET / POST / PUT / PATCH / DELETE / HEAD");
    Console.Write("> ");
    var method = Console.ReadLine()?.Trim().ToUpper();

    if (method == "Q" || method == null) break;

    var validMethods = new[] { "GET", "POST", "PUT", "PATCH", "DELETE", "HEAD" };
    if (!validMethods.Contains(method))
    {
        Console.WriteLine("Unbekannte Methode.\n");
        continue;
    }

    Console.Write("URL: ");
    var url = Console.ReadLine()?.Trim();

    if (string.IsNullOrEmpty(url)) continue;
    if (!url.StartsWith("http://") && !url.StartsWith("https://")) url = "https://" + url;
    
    var headers = new Dictionary<string, string>();

    Console.WriteLine("Header (Format: Name: Wert) – leere Zeile zum Weiter:");

    while (true)
    {
        Console.Write("  ");

        var line = Console.ReadLine()?.Trim();
        if (string.IsNullOrEmpty(line)) break;

        var colon = line.IndexOf(':');
        if (colon < 1)
        {
            Console.WriteLine("  -> Format: Name: Wert");
            continue;
        }

        var key = line[..colon].Trim();
        var val = line[(colon + 1)..].Trim();

        headers[key] = val;

        Console.WriteLine($"  -> {key} gesetzt");
    }

    string body = null;

    if (method is "POST" or "PUT" or "PATCH")
    {
        Console.WriteLine("Body (mehrzeilig möglich, leere Zeile zum Abschicken):");
        var lines = new List<string>();
        while (true)
        {
            Console.Write("  ");
            var bl = Console.ReadLine();
            if (bl == "" && lines.Count > 0) break;
            if (bl != null) lines.Add(bl);
        }
        if (lines.Count > 0)
            body = string.Join("\n", lines);
    }

    Console.WriteLine($"\n-> {method} {url}");

    try
    {
        var client = factory.CreateClient();
        client.Timeout = TimeSpan.FromSeconds(30);

        var request = new HttpRequestMessage(new HttpMethod(method), url);

        foreach (var (k, v) in headers)
            request.Headers.TryAddWithoutValidation(k, v);

        if (body != null)
        {
            var contentType = headers.GetValueOrDefault("Content-Type", "application/json");
            request.Content = new StringContent(body, Encoding.UTF8, contentType);
        }

        var response = await client.SendAsync(request);

        Console.WriteLine($"Status: {(int)response.StatusCode} {response.StatusCode}");

        Console.WriteLine("\n-- Response-Header --");

        foreach (var h in response.Headers)
            Console.WriteLine($"  {h.Key}: {string.Join(", ", h.Value)}");
        foreach (var h in response.Content.Headers)
            Console.WriteLine($"  {h.Key}: {string.Join(", ", h.Value)}");


        var responseBody = await response.Content.ReadAsStringAsync();
        if (!string.IsNullOrWhiteSpace(responseBody))
        {
            Console.WriteLine("\n-- Body --");

            if (responseBody.TrimStart() is ['{' or '[', ..])
            {
                try
                {
                    var doc = JsonDocument.Parse(responseBody);
                    var pretty = JsonSerializer.Serialize(doc, new JsonSerializerOptions { WriteIndented = true });

                    if (pretty.Length > 5000)
                        Console.WriteLine(pretty[..5000] + "\n[... gekürzt]");
                    else
                        Console.WriteLine(pretty);
                }
                catch
                {
                    Console.WriteLine(responseBody);
                }
            }
            else
            {
                Console.WriteLine(responseBody.Length > 5000 ? responseBody[..5000] + "\n[... gekürzt]" : responseBody);
            }
        }
    }

    catch (HttpRequestException ex)
    {
        Console.WriteLine($"Fehler: {ex.Message}");
    }

    catch (TaskCanceledException)
    {
        Console.WriteLine("Timeout – keine Antwort nach 30s.");
    }

    Console.WriteLine();
}