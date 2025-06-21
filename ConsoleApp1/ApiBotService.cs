// ApiBotService.cs
using System;
using System;
using System.Net.Http;
using System.Net.Http;
using System.Security.Principal;
using System.Text;
using System.Text;
using System.Text.Json;
using System.Text.Json;
using System.Threading.Tasks;
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

        // 设置请求头
        client.DefaultRequestHeaders.Add("Authorization", $"Bearer {_account.BearerToken}");
        client.DefaultRequestHeaders.Add("tenant-id", "1");
        client.DefaultRequestHeaders.Add("origin", "http://www.js-leader.cn:8088");
        client.DefaultRequestHeaders.Add("referer", "http://www.js-leader.cn:8088/");
        client.DefaultRequestHeaders.Add("user-agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64)");

        bool hasReport = await CheckReportWorkJustNowAsync(client, _account.DeptId, _account.UserId, (int)(DateTime.Now - DateTime.Today).TotalMinutes);
        if (hasReport)
        {
            await WecomNotifier.SendToWeCom($"[{_account.Username}] 今天已经报过工了，跳过本次报工");
            Console.WriteLine($"[{_account.Username}] 今天已经报过工了，跳过本次报工");
            return;
        }
        // 获取项目列表，提取其中一个项目ID
        var getProject = await GetProjectIdAsync(client);
        if (getProject == null)
        {
            await WecomNotifier.SendToWeCom($"[{_account.Username}] 未获取到任何项目，无法继续报工", _account.Username);
            return;
        }

        // 报工必要请求
        await GetAsync(client, $"http://www.js-leader.cn:48080/admin-api/system/user/get?id={_account.UserId}");

        await GetAsync(client, $"http://www.js-leader.cn:48080/admin-api/cost/dept-type-link/list-report?deptId={_account.DeptId}");

        // 进入报工页面
        await GetAsync(client, $"http://www.js-leader.cn:48080/admin-api/project/manager/get?id={getProject.Item1}&isTemplate=1");

        // 选择任务事项为“软件调试”
        await GetAsync(client, $"http://www.js-leader.cn:48080/admin-api/cost/report_work/getDetail?projectId={getProject.Item1}&typeId=1563");

        await GetAsync(client, $"http://www.js-leader.cn:48080/admin-api/cost/report_work/page?pageSize=50&pageNo=1&reportUser={_account.UserId}&projectId={getProject.Item1}&typeId=1563");

        // 提交工时为8小时
        await PostAsync(client, "http://www.js-leader.cn:48080/admin-api/cost/report_work/create", new
        {
            reportType = "10",
            typeId = 1563, // 软件调试
            reportHour = 0.8, // 报工8小时
            reportTime = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
            reportUser = _account.UserId,
            creator = _account.UserId.ToString(),
            createTime = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
            projectId = getProject.Item1,
            reportStatus = 50, // 状态为已报工
            taskId = getProject.Item2 // 使用获取的任务ID
        });

        // 刷新记录
        await GetAsync(client, $"http://www.js-leader.cn:48080/admin-api/cost/report_work/page?creator={_account.UserId}&pageSize=50&pageNo=1&deptId={_account.DeptId}");
        await GetAsync(client, $"http://www.js-leader.cn:48080/admin-api/cost/report_work/page?pageSize=50&pageNo=1&reportUser={_account.UserId}&projectId={getProject.Item1}&typeId=1563");
        bool justNow = await CheckReportWorkJustNowAsync(client, _account.DeptId, _account.UserId);
        if (justNow)
        {
            Console.WriteLine($"[{_account.Username}] 报工完成！");
            await WecomNotifier.SendToWeCom($"账号 {_account.Username} 报工完成 ✅");
        }
        else
        {
            await WecomNotifier.SendToWeCom($"[{_account.Username}] 报工完成，但未在5分钟内完成，可能存在问题。请检查！", _account.Username);
        }
    }
    public async Task<bool> CheckReportWorkJustNowAsync(HttpClient client, int deptId, int creatorUserId, int minutesThreshold = 5)
    {
        var url = $"http://www.js-leader.cn:48080/admin-api/cost/report_work/page?pageSize=50&pageNo=1&deptId={deptId}&creator={creatorUserId}";
        var response = await client.GetAsync(url);
        if (!response.IsSuccessStatusCode)
        {
            Console.WriteLine($"[{_account.Username}] 检查报工失败: {response.StatusCode}");
            return false;
        }
        var content = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(content);
        var root = doc.RootElement;

        if (!root.TryGetProperty("data", out var data) || !data.TryGetProperty("list", out var list) || list.GetArrayLength() == 0)
        {
            Console.WriteLine($"[{_account.Username}] 没有报工记录。");
            return false;
        }

        var first = list[0];
        if (!first.TryGetProperty("reportTime", out var reportTimeProp))
        {
            Console.WriteLine($"[{_account.Username}] 报工记录中无reportTime字段。");
            return false;
        }

        var reportTime = reportTimeProp.GetInt64();
        var reportDateTime = DateTimeOffset.FromUnixTimeMilliseconds(reportTime).ToLocalTime();
        var now = DateTimeOffset.Now;

        var diff = now - reportDateTime;
        Console.WriteLine($"[{_account.Username}] 最近报工时间: {reportDateTime:yyyy-MM-dd HH:mm:ss}，与当前时间相差 {diff.TotalMinutes:F1} 分钟");

        return Math.Abs(diff.TotalMinutes) <= minutesThreshold;
    }
    Random _random = new Random();
    private async Task<Tuple<int, long>?> GetProjectIdAsync(HttpClient client)
    {
        var url = "http://www.js-leader.cn:48080/admin-api/project/task/my-order?pageSize=10&pageNo=1&isFirst=1&type=1";
        var response = await client.GetAsync(url);
        var content = await response.Content.ReadAsStringAsync();

        using var doc = JsonDocument.Parse(content);
        var root = doc.RootElement;

        var data = root.GetProperty("data");
        var list = data.GetProperty("list");

        if (list.GetArrayLength() == 0)
        {
            Console.WriteLine($"[{_account.Username}] 未获取到任何项目ID，list为空。");
            return null;
        }
        //随机获取其中一个项目ID和任务ID
        int randomIndex = _random.Next(0, list.GetArrayLength());
        var firstProjectId = list[randomIndex].GetProperty("projectId").GetInt32();
        var id = list[randomIndex].GetProperty("id").GetInt64();
        Console.WriteLine($"[{_account.Username}] 获取的项目ID：{firstProjectId},任务ID：{id}");
        return new Tuple<int, long>(firstProjectId, id);
    }

    private async Task GetAsync(HttpClient client, string url)
    {
        var response = await client.GetAsync(url);
        var result = await response.Content.ReadAsStringAsync();
        Console.WriteLine($"[{_account.Username}] GET {url} -> 状态: {response.StatusCode}");
        if (!response.IsSuccessStatusCode)
        {
            Console.WriteLine($"[{_account.Username}] GET {url} -> 错误: {result}");
            await WecomNotifier.SendToWeCom($"[{_account.Username}] 获取数据失败: {result}", _account.Username);
        }
    }

    private async Task PostAsync(HttpClient client, string url, object data)
    {
        var json = JsonSerializer.Serialize(data);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        Console.WriteLine(json);
        var response = await client.PostAsync(url, content);
        var result = await response.Content.ReadAsStringAsync();
        Console.WriteLine($"[{_account.Username}] POST {url} -> 状态: {response.StatusCode}");
        if (!response.IsSuccessStatusCode)
        {
            Console.WriteLine($"[{_account.Username}] POST {url} -> 错误: {result}");
            await WecomNotifier.SendToWeCom($"[{_account.Username}] 报工失败: {result}", _account.Username);
        }
    }
}