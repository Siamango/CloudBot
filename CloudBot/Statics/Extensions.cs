using Newtonsoft.Json.Linq;

namespace CloudBot.Statics;

public static class Extensions
{
    public static bool TryReadProperty<T>(this JObject json, string key, out T? elem)
    {
        if (json == null)
        {
            elem = default;
            return false;
        }

        if (json.TryGetValue(key, out JToken? token))
        {
            elem = token.ToObject<T>();
            return true;
        }
        else
        {
            elem = default;
            return false;
        }
    }
}