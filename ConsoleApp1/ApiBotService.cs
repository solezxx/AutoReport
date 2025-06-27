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
using ConsoleApp1;

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

        int hasReport = await CheckReportWorkJustNowAsync(client, _account.DeptId, _account.UserId, (int)(DateTime.Now - DateTime.Today).TotalMinutes);
        if (hasReport == 1)
        {
            await WecomNotifier.SendToWeCom($"[{_account.Username}] 今天已经报过工了，跳过本次报工");
            Console.WriteLine($"[{_account.Username}] 今天已经报过工了，跳过本次报工");
            return;
        }
        else if (hasReport == 3)
        {
            return;
        }
        // 获取项目列表，提取其中一个项目ID
        var getProject = await GetProjectIdAsync(client);
        if (getProject == null)
        {
            await WecomNotifier.SendToWeCom($"[{_account.Username}] 未获取到任何项目，无法继续报工", _account.Username);
            return;
        }

        // 进入报工页面
        await GetAsync(client, $"http://www.js-leader.cn:48080/admin-api/project/manager/get?id={getProject.Item1}&isTemplate=1");
        await GetAsync(client, $"http://www.js-leader.cn:48080/admin-api/cost/report_work/page?pageSize=50&pageNo=1&reportUser={_account.UserId}&projectId={getProject.Item1}&typeId=0");

        // 提交工时为8小时
        await PostAsync(client, "http://www.js-leader.cn:48080/admin-api/cost/report_work/create", new ReportData()
        {
            workReportId = null,
            reportType = "10",
            typeId = 1563, // 软件调试
            standardHours = null,
            reportHour = 8, // 报工8小时
            abnormalReportType = null,
            remark = "<p><br></p>",
            reportTime = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"), // 使用UTC时间
            totalHours = null,
            reportUser = _account.UserId,
            creator = _account.UserId.ToString(),
            createTime = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"), // 使用UTC时间
            projectId = getProject.Item1, // 使用获取的项目ID
            reportedHour = 0,
            estimateHour = 0,
            challengeName = null,
            challengeId = new List<long>(),
            machineTool = null,
            gelId = null,
            reportStatus = 50, // 状态为50表示已完成
            taskId = getProject.Item2 // 使用获取的任务ID
        });

        int justNow = await CheckReportWorkJustNowAsync(client, _account.DeptId, _account.UserId);
        if (justNow == 1)
        {
            Console.WriteLine($"[{_account.Username}] 报工完成！");
            await WecomNotifier.SendToWeCom($"账号 {_account.Username} 报工完成 ✅");
        }
        else
        {
            await WecomNotifier.SendToWeCom($"[{_account.Username}] 报工完成，但未在5分钟内完成，可能存在问题。请检查！", _account.Username);
        }
    }
    /// <summary>
    /// 检查有无报工记录，有为1，没有为2，报错为3
    /// </summary>
    /// <param name="client"></param>
    /// <param name="deptId"></param>
    /// <param name="creatorUserId"></param>
    /// <param name="minutesThreshold"></param>
    /// <returns></returns>
    public async Task<int> CheckReportWorkJustNowAsync(HttpClient client, int deptId, int creatorUserId, int minutesThreshold = 5)
    {
        var url = $"http://www.js-leader.cn:48080/admin-api/cost/report_work/page?pageSize=50&pageNo=1&deptId={deptId}&creator={creatorUserId}";
        var response = await client.GetAsync(url);
        if (!response.IsSuccessStatusCode)
        {
            Console.WriteLine($"[{_account.Username}] 检查报工失败: {response.StatusCode}");
            return 3;
        }
        var content = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(content);

        if (content.Contains("未登录"))
        {
            await WecomNotifier.SendToWeCom($"[{_account.Username}] 报工失败: 账号未登录", _account.Username);
            return 3;
        }
        var root = doc.RootElement;

        if (!root.TryGetProperty("data", out var data) || !data.TryGetProperty("list", out var list) || list.GetArrayLength() == 0)
        {
            Console.WriteLine($"[{_account.Username}] 没有报工记录。");
            return 3;
        }

        var first = list[0];
        if (!first.TryGetProperty("reportTime", out var reportTimeProp))
        {
            Console.WriteLine($"[{_account.Username}] 报工记录中无reportTime字段。");
            return 3;
        }

        var reportTime = reportTimeProp.GetInt64();
        var reportDateTime = DateTimeOffset.FromUnixTimeMilliseconds(reportTime).ToLocalTime();
        var now = DateTimeOffset.Now;

        var diff = now - reportDateTime;
        Console.WriteLine($"[{_account.Username}] 最近报工时间: {reportDateTime:yyyy-MM-dd HH:mm:ss}，与当前时间相差 {diff.TotalMinutes:F1} 分钟");

        return Math.Abs(diff.TotalMinutes) <= minutesThreshold ? 1 : 2;
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