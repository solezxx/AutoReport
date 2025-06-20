using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

public static class WecomNotifier
{
    public static async Task SendToWeCom(string message)
    {
        using var httpClient = new HttpClient();
        var payload = new
        {
            msgtype = "text",
            text = new { content = message }
        };
        var json = JsonSerializer.Serialize(payload);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await httpClient.PostAsync(
            "https://qyapi.weixin.qq.com/cgi-bin/webhook/send?key=609ec019-5965-45e2-bd33-766a0db3b00c", content);

        Console.WriteLine($"企业微信发送状态: {response.StatusCode}");
    }
    public static async Task SendToWeCom(string content, string userId = null, bool atAll = false)
    {
        using var httpClient = new HttpClient();
        var message = new
        {
            msgtype = "text",
            text = new
            {
                content = atAll ? $"{content}" : (userId == null ? content : $"{content} <@{userId}>"),
                mentioned_list = atAll ? new[]{"@all"}:Array.Empty<string>()
            }
        };
        var json = JsonSerializer.Serialize(message);
        var response = await httpClient.PostAsync(
            "https://qyapi.weixin.qq.com/cgi-bin/webhook/send?key=609ec019-5965-45e2-bd33-766a0db3b00c",
            new StringContent(json, Encoding.UTF8, "application/json"));
        Console.WriteLine($"企业微信发送状态: {response.StatusCode}");
    }
}
