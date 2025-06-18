// ApiBotService.cs
using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

public class ApiBotService
{
    private readonly TokenAccount _account;

    public ApiBotService(TokenAccount account)
    {
        _account = account;
    }

    public async Task RunAsync()
    {
        using var client = new HttpClient();

        client.DefaultRequestHeaders.Add("Authorization", $"Bearer {_account.BearerToken}");
        client.DefaultRequestHeaders.Add("tenant-id", "1");
        client.DefaultRequestHeaders.Add("origin", "http://www.js-leader.cn:8088");
        client.DefaultRequestHeaders.Add("referer", "http://www.js-leader.cn:8088/");
        client.DefaultRequestHeaders.Add("user-agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64)");

        // 简化后的请求流程

        // 点击“报工”按钮触发的必要请求
        await GetAsync(client, "http://www.js-leader.cn:48080/admin-api/system/user/get?id=635");
        Console.ReadKey();
        await GetAsync(client, "http://www.js-leader.cn:48080/admin-api/cost/dept-type-link/list-report?deptId=128");
        Console.ReadKey();
        await GetAsync(client, "http://www.js-leader.cn:48080/admin-api/common/type/getMapTypeCodes?typeCodes=project_type");
        Console.ReadKey();
        // 进入报工页面
        await GetAsync(client, "http://www.js-leader.cn:48080/admin-api/project/manager/get?id=2335&isTemplate=1");
        Console.ReadKey();
        // 选择任务事项为“软件调试”
        await GetAsync(client, "http://www.js-leader.cn:48080/admin-api/cost/report_work/getDetail?projectId=2335&typeId=1563");
        Console.ReadKey();
        await GetAsync(client, "http://www.js-leader.cn:48080/admin-api/cost/report_work/page?pageSize=50&pageNo=1&reportUser=635&projectId=2335&typeId=1563");
        Console.ReadKey();
        // 提交工时为8小时
        await PostAsync(client, "http://www.js-leader.cn:48080/admin-api/cost/report_work/create", new
        {
            projectId = 2335,
            typeId = 1563,
            reportUser = 635,
            reportType = 10,  // 报工类型
            reportStatus=50,
            reportHour = 8.0 
        });

        Console.ReadKey();
        // 提交后刷新记录
        await GetAsync(client, "http://www.js-leader.cn:48080/admin-api/cost/report_work/page?creator=635&pageSize=50&pageNo=1&deptId=128");
        Console.ReadKey();
        await GetAsync(client, "http://www.js-leader.cn:48080/admin-api/cost/report_work/page?pageSize=50&pageNo=1&reportUser=635&projectId=2335&typeId=1563");
        Console.ReadKey();
    }

    private async Task GetAsync(HttpClient client, string url)
    {
        var response = await client.GetAsync(url);
        var result = await response.Content.ReadAsStringAsync();
        Console.WriteLine($"[{_account.Username}] GET {url} -> 状态: {response.StatusCode} 返回值:{result}");
    }

    private async Task PostAsync(HttpClient client, string url, object data)
    {
        var json = JsonSerializer.Serialize(data);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        var response = await client.PostAsync(url, content);
        var result = await response.Content.ReadAsStringAsync();
        Console.WriteLine($"[{_account.Username}] POST {url} -> 状态: {response.StatusCode}");
    }
}