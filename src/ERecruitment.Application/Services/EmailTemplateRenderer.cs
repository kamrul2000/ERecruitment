namespace ERecruitment.Application.Services;

public static class EmailTemplateRenderer
{
    public static string Render(string template, IDictionary<string, string> values)
    {
        var result = template;
        foreach (var kv in values)
            result = result.Replace("{" + kv.Key + "}", kv.Value ?? "");
        return result;
    }
}
