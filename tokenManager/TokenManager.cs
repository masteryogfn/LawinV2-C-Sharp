using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace RadiumServer.TokenManager;

public static class TokenManager
{
    private static readonly JwtSecurityTokenHandler JwtHandler = new();
    private static readonly string TokenFilePath = Path.Combine(Directory.GetCurrentDirectory(), "tokenManager", "tokens.json");

    public static JsonArray AccessTokens { get; set; } = new();
    public static JsonArray RefreshTokens { get; set; } = new();
    public static JsonArray ClientTokens { get; set; } = new();
    public static List<object> ExchangeCodes { get; set; } = new();

    public static void Initialize()
    {
        if (!File.Exists(TokenFilePath))
        {
            var initial = new JsonObject
            {
                ["accessTokens"] = new JsonArray(),
                ["refreshTokens"] = new JsonArray(),
                ["clientTokens"] = new JsonArray()
            };
            var dir = Path.GetDirectoryName(TokenFilePath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }
            File.WriteAllText(TokenFilePath, initial.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));
        }

        var content = File.ReadAllText(TokenFilePath);
        var jsonNode = JsonNode.Parse(content)?.AsObject() ?? new JsonObject();

        CleanTokens(jsonNode, "accessTokens");
        CleanTokens(jsonNode, "refreshTokens");
        CleanTokens(jsonNode, "clientTokens");

        File.WriteAllText(TokenFilePath, jsonNode.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));

        AccessTokens = jsonNode["accessTokens"]?.AsArray() ?? new JsonArray();
        RefreshTokens = jsonNode["refreshTokens"]?.AsArray() ?? new JsonArray();
        ClientTokens = jsonNode["clientTokens"]?.AsArray() ?? new JsonArray();
        ExchangeCodes = new List<object>();
    }

    private static void CleanTokens(JsonObject obj, string tokenType)
    {
        if (obj[tokenType] is not JsonArray array)
        {
            obj[tokenType] = new JsonArray();
            return;
        }

        for (var i = array.Count - 1; i >= 0; i--)
        {
            var item = array[i]?.AsObject();
            var rawToken = item?["token"]?.ToString();
            if (string.IsNullOrWhiteSpace(rawToken))
            {
                array.RemoveAt(i);
                continue;
            }

            if (IsExpired(rawToken))
            {
                array.RemoveAt(i);
            }
        }
    }

    private static bool IsExpired(string rawToken)
    {
        try
        {
            var token = rawToken.StartsWith("eg1~") ? rawToken[4..] : rawToken;
            if (!JwtHandler.CanReadToken(token))
            {
                return true;
            }

            var jwt = JwtHandler.ReadJwtToken(token);
            var creationClaim = jwt.Claims.FirstOrDefault(c => c.Type == "creation_date")?.Value;
            var expireHoursClaim = jwt.Claims.FirstOrDefault(c => c.Type == "hours_expire")?.Value;

            if (creationClaim != null && expireHoursClaim != null)
            {
                if (DateTime.TryParse(creationClaim, null, DateTimeStyles.RoundtripKind, out var creationDate) &&
                    double.TryParse(expireHoursClaim, CultureInfo.InvariantCulture, out var hoursExpire))
                {
                    return creationDate.AddHours(hoursExpire) <= DateTime.UtcNow;
                }
            }

            if (jwt.ValidTo != DateTime.MinValue)
            {
                return jwt.ValidTo <= DateTime.UtcNow;
            }

            return false;
        }
        catch
        {
            return true;
        }
    }
}
