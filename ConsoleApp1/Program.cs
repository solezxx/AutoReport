using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

class Program
{
    static Timer _timer;
    static List<TimeSpan> _runTimes = new List<TimeSpan>
    {
        new TimeSpan(17, 30, 0), // 18:00
        new TimeSpan(20, 0, 0)  // 20:00
    };

    static void Main(string[] args)
    {
        ScheduleNextRun();
        Console.WriteLine("定时任务已启动，按任意键退出...");
        Console.ReadKey();
    }

    static void ScheduleNextRun()
    {
        var now = DateTime.Now;
        DateTime? next = null;
        foreach (var time in _runTimes)
        {
            var candidate = now.Date.Add(time);
            if (candidate > now)
            {
                next = candidate;
                break;
            }
        }
        if (next == null)
        {
            // 今天都过了，安排到明天的第一个时间点
            next = now.Date.AddDays(1).Add(_runTimes[0]);
        }

        var dueTime = next.Value - now;
        _timer = new Timer(async _ => await RunTask(), null, dueTime, Timeout.InfiniteTimeSpan);
        Console.WriteLine($"下次执行时间: {next}");
    }

    static async Task RunTask()
    {
        Console.WriteLine("加载 Token 文件...");
        var accounts = TokenLoader.LoadFromFile("Tokens.json");
        foreach (var account in accounts)
        {
            Console.WriteLine($"开始执行账号：{account.Username}");
            await WecomNotifier.SendToWeCom($"开始执行账号：{account.Username}");
            var bot = new ApiBotService(account);
            await bot.RunAsync();
        }
        Console.WriteLine("全部账号执行完毕。");
        await WecomNotifier.SendToWeCom($"帮你们都报工了，不用谢", atAll:true);

        // 重新安排下一次
        ScheduleNextRun();
    }
}