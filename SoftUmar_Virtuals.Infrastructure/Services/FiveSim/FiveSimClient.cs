using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;

namespace SoftUmar_Virtuals.Infrastructure.Services.FiveSim;

public class FiveSimClient(HttpClient httpClient, IOptions<FiveSimSettings> options)
{
    private readonly FiveSimSettings _settings = options.Value;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
    };

    private void SetAuthHeader(HttpRequestMessage request)
    {
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _settings.ApiKey);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
    }

    // GET /v1/guest/products/{country}/{operator} - no auth needed. Lists
    // EVERY available product (service - whatsapp, facebook, etc) for a
    // country, with live price and quantity. Use this to populate a
    // "choose a service" dropdown on the frontend BEFORE the user buys -
    // this is the answer to "is there an endpoint that lists services."
    public async Task<List<FiveSimProductAvailability>> GetAvailableProductsAsync(
        string country, string @operator, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(
            HttpMethod.Get, $"/v1/guest/products/{country}/{@operator}");
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        using var response = await httpClient.SendAsync(request, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
            throw new FiveSimApiException("api_error", $"5SIM products list failed: {response.StatusCode} - {body}");

        // Shape: { "facebook": { "Category": "activation", "Qty": 133, "Price": 21 }, ... }
        // Dictionary keyed by product name, not a flat array, so this is
        // parsed manually instead of straight JsonSerializer.Deserialize.
        using var doc = JsonDocument.Parse(body);
        var products = new List<FiveSimProductAvailability>();

        foreach (var prop in doc.RootElement.EnumerateObject())
        {
            var category = prop.Value.TryGetProperty("Category", out var catEl) ? catEl.GetString() : null;

            // Only "activation" products matter for your platform -
            // "hosting" products (long-term rented numbers) are a
            // different feature, not what your Activations table/flow
            // is built for.
            if (category != "activation") continue;

            products.Add(new FiveSimProductAvailability
            {
                Product = prop.Name,
                Quantity = prop.Value.TryGetProperty("Qty", out var qtyEl) ? qtyEl.GetInt32() : 0,
                Price = prop.Value.TryGetProperty("Price", out var priceEl) ? priceEl.GetDecimal() : 0,
            });
        }

        return products.Where(p => p.Quantity > 0).OrderBy(p => p.Price).ToList();
    }

    // GET /v1/guest/prices?country=X&product=Y - no auth needed. Used to
    // know the price BEFORE buying, so you can check the user's wallet
    // balance against it first rather than finding out after an
    // irreversible purchase.
    public async Task<decimal?> GetCheapestPriceAsync(
        string country, string product, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(
            HttpMethod.Get, $"/v1/guest/prices?country={country}&product={product}");
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        using var response = await httpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
            return null;

        var body = await response.Content.ReadAsStringAsync(cancellationToken);

        // Shape: { "country": { "product": { "operatorName": { "cost": X, "count": Y, "rate": Z } } } }
        // Walk it generically since operator names vary per country/product.
        using var doc = JsonDocument.Parse(body);
        if (!doc.RootElement.TryGetProperty(country, out var countryEl)) return null;
        if (!countryEl.TryGetProperty(product, out var productEl)) return null;

        decimal? cheapest = null;
        foreach (var op in productEl.EnumerateObject())
        {
            if (op.Value.TryGetProperty("count", out var countEl) && countEl.GetInt32() <= 0)
                continue; // skip operators with zero availability

            if (op.Value.TryGetProperty("cost", out var costEl))
            {
                var cost = costEl.GetDecimal();
                if (cheapest == null || cost < cheapest) cheapest = cost;
            }
        }

        return cheapest;
    }

    // GET /v1/user/buy/activation/{country}/{operator}/{product}
    public async Task<FiveSimOrderResult> BuyActivationNumberAsync(
        string country, string @operator, string product, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(
            HttpMethod.Get, $"/v1/user/buy/activation/{country}/{@operator}/{product}");
        SetAuthHeader(request);

        using var response = await httpClient.SendAsync(request, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);

        // 5SIM returns 200 even for "no free phones" in some cases, and
        // the body is the literal string "no free phones" rather than
        // JSON - handle that before trying to deserialize.
        if (body.Trim('"') == "no free phones")
            throw new FiveSimApiException("no_numbers", "No numbers available for this country/operator/product.");

        if (!response.IsSuccessStatusCode)
            throw new FiveSimApiException("api_error", $"5SIM buy failed: {response.StatusCode} - {body}");

        var result = JsonSerializer.Deserialize<FiveSimOrderResult>(body, JsonOptions)
            ?? throw new FiveSimApiException("parse_error", "Could not parse 5SIM buy response.");

        return result;
    }

    // GET /v1/user/check/{id} - poll for SMS / current status
    public async Task<FiveSimOrderResult> CheckOrderAsync(long orderId, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, $"/v1/user/check/{orderId}");
        SetAuthHeader(request);

        using var response = await httpClient.SendAsync(request, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);

        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            throw new FiveSimApiException("order_not_found", "Order not found on 5SIM.");

        if (!response.IsSuccessStatusCode)
            throw new FiveSimApiException("api_error", $"5SIM check failed: {response.StatusCode} - {body}");

        return JsonSerializer.Deserialize<FiveSimOrderResult>(body, JsonOptions)
            ?? throw new FiveSimApiException("parse_error", "Could not parse 5SIM check response.");
    }

    // GET /v1/user/finish/{id} - mark order complete (you got your code,
    // you're done with the number)
    public async Task<FiveSimOrderResult> FinishOrderAsync(long orderId, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, $"/v1/user/finish/{orderId}");
        SetAuthHeader(request);

        using var response = await httpClient.SendAsync(request, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
            throw new FiveSimApiException("api_error", $"5SIM finish failed: {response.StatusCode} - {body}");

        return JsonSerializer.Deserialize<FiveSimOrderResult>(body, JsonOptions)
            ?? throw new FiveSimApiException("parse_error", "Could not parse 5SIM finish response.");
    }

    // GET /v1/user/cancel/{id} - cancel before SMS arrives (refundable).
    // 5SIM rejects this (400) once an SMS has already arrived on the
    // order - "order has sms" - that's expected, not a bug, if your
    // CancelActivation flow hits it.
    public async Task<FiveSimOrderResult> CancelOrderAsync(long orderId, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, $"/v1/user/cancel/{orderId}");
        SetAuthHeader(request);

        using var response = await httpClient.SendAsync(request, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
            throw new FiveSimApiException("api_error", $"5SIM cancel failed: {response.StatusCode} - {body}");

        return JsonSerializer.Deserialize<FiveSimOrderResult>(body, JsonOptions)
            ?? throw new FiveSimApiException("parse_error", "Could not parse 5SIM cancel response.");
    }
}

public class FiveSimApiException(string code, string message) : Exception(message)
{
    public string Code { get; } = code;
}



public class FiveSimProductAvailability
{
    public required string Product { get; set; }
    public int Quantity { get; set; }
    public decimal Price { get; set; }
}




