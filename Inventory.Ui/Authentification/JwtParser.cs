using System.Security.Claims;
using System.Text.Json;

namespace Inventory.Ui.Authentification
{
    internal static class JwtParser
    {
        public static IEnumerable<Claim> Parse(string jwt)
        {
            var parts = jwt.Split('.');
            if (parts.Length != 3)
                throw new InvalidOperationException("Invalid JWT format");

            var payload = parts[1];
            var jsonBytes = DecodeBase64(payload);

            var data = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(jsonBytes)
                       ?? throw new InvalidOperationException("Invalid JWT payload");

            foreach (var kv in data)
            {
                if (kv.Value.ValueKind == JsonValueKind.Array)
                {
                    foreach (var item in kv.Value.EnumerateArray())
                        yield return new Claim(kv.Key, item.ToString());
                }
                else
                {
                    yield return new Claim(kv.Key, kv.Value.ToString());
                }
            }
        }

        private static byte[] DecodeBase64(string input)
        {
            input = input.Replace('-', '+').Replace('_', '/');
            switch (input.Length % 4)
            {
                case 2: input += "=="; break;
                case 3: input += "="; break;
            }
            return Convert.FromBase64String(input);
        }
    }

}
