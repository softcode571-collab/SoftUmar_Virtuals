using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;
using SoftUmar_Virtuals.Application.Interfaces;

namespace SoftUmar_Virtuals.Infrastructure.Services.FiveSim
{
    

    // CHANGED: now implements IFiveSimClient (defined in Application). This
    // is what makes Program.cs register it as
    // AddHttpClient<IFiveSimClient, FiveSimClient>(...) instead of just
    // AddHttpClient<FiveSimClient>(...) - see the Program.cs note at the
    // bottom.
    public class FiveSimClient(HttpClient httpClient, IOptions<FiveSimSettings> options) : IFiveSimClient
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

            using var doc = JsonDocument.Parse(body);
            var products = new List<FiveSimProductAvailability>();

            foreach (var prop in doc.RootElement.EnumerateObject())
            {
                var category = prop.Value.TryGetProperty("Category", out var catEl) ? catEl.GetString() : null;
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

        

        public async Task<List<FiveSimCountry>> GetCountriesAsync(CancellationToken cancellationToken)
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, "/v1/guest/countries");
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            using var response = await httpClient.SendAsync(request, cancellationToken);
            var body = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
                throw new FiveSimApiException("api_error", $"5SIM countries list failed: {response.StatusCode} - {body}");

            // Shape: { "afghanistan": { "iso": {"AF": 4}, "prefix": {"93": 4},
            //           "text_en": "Afghanistan", "text_ru": "..." }, ... }
            using var doc = JsonDocument.Parse(body);
            var countries = new List<FiveSimCountry>();

            foreach (var prop in doc.RootElement.EnumerateObject())
            {
                var textEn = prop.Value.TryGetProperty("text_en", out var textEl) ? textEl.GetString() : null;
                if (string.IsNullOrEmpty(textEn)) continue; // skip malformed entries

                string? iso = null;
                if (prop.Value.TryGetProperty("iso", out var isoEl))
                {
                    foreach (var isoProp in isoEl.EnumerateObject())
                    {
                        iso = isoProp.Name; // the key itself is the ISO code
                        break;
                    }
                }

                string? prefix = null;
                if (prop.Value.TryGetProperty("prefix", out var prefixEl))
                {
                    foreach (var prefixProp in prefixEl.EnumerateObject())
                    {
                        prefix = prefixProp.Name;
                        break;
                    }
                }

                countries.Add(new FiveSimCountry
                {
                    Code = prop.Name, // e.g. "england" - what you pass to every other endpoint
                    NameEnglish = textEn,
                    Iso = iso,
                    Prefix = prefix,
                });
            }

            return countries.OrderBy(c => c.NameEnglish).ToList();
        }

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

            using var doc = JsonDocument.Parse(body);
            if (!doc.RootElement.TryGetProperty(country, out var countryEl)) return null;
            if (!countryEl.TryGetProperty(product, out var productEl)) return null;

            decimal? cheapest = null;
            foreach (var op in productEl.EnumerateObject())
            {
                if (op.Value.TryGetProperty("count", out var countEl) && countEl.GetInt32() <= 0)
                    continue;

                if (op.Value.TryGetProperty("cost", out var costEl))
                {
                    var cost = costEl.GetDecimal();
                    if (cheapest == null || cost < cheapest) cheapest = cost;
                }
            }

            return cheapest;
        }

        public async Task<FiveSimOrderResult> BuyActivationNumberAsync(
            string country, string @operator, string product, CancellationToken cancellationToken)
        {
            using var request = new HttpRequestMessage(
                HttpMethod.Get, $"/v1/user/buy/activation/{country}/{@operator}/{product}");
            SetAuthHeader(request);

            using var response = await httpClient.SendAsync(request, cancellationToken);
            var body = await response.Content.ReadAsStringAsync(cancellationToken);

            if (body.Trim('"') == "no free phones")
                throw new FiveSimApiException("no_numbers", "No numbers available for this country/operator/product.");

            if (!response.IsSuccessStatusCode)
                throw new FiveSimApiException("api_error", $"5SIM buy failed: {response.StatusCode} - {body}");

            return JsonSerializer.Deserialize<FiveSimOrderResult>(body, JsonOptions)
                ?? throw new FiveSimApiException("parse_error", "Could not parse 5SIM buy response.");
        }

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
}

// --- Update in Program.cs: change this line ---
//   builder.Services.AddHttpClient<FiveSimClient>(client => ...);
// to:
//   builder.Services.AddHttpClient<IFiveSimClient, FiveSimClient>(client =>
//   {
//       client.BaseAddress = new Uri(builder.Configuration["FiveSim:BaseUrl"]!);
//   });
// Add "using SoftUmar_Virtuals.Application.Interfaces;" to Program.cs if not
// already there (it should be, from other interfaces already used).