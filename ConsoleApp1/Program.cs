﻿using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

class Program
{
    static Timer _timer;
    static List<TimeSpan> _runTimes = new List<TimeSpan>
    {
        new TimeSpan(17, 20, 00)
    };

    static void Main(string[] args)
    {
        Console.WriteLine("输入1 立即报工  输入2 倒计时报工");
        var read = Console.ReadKey();
        if (read.KeyChar == '1')
        {
            WecomNotifier.SendToWeCom("立即报工");
            RunTask().Wait();
            return;
        }
        else if (read.KeyChar == '2')
        {
            Console.WriteLine("倒计时报工");
        }
        else
        {
            Console.WriteLine("输入错误，默认立即报工");
            RunTask().Wait();
            return;
        }
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
        WecomNotifier.SendToWeCom($"下次执行报工时间: {next}").Wait();
    }

    static async Task RunTask()
    {
        Console.WriteLine("加载 Token 文件...");
        var accounts = TokenLoader.LoadFromFile("Tokens.json");
        ApiBotService.SendToWecomList.Clear();
        ApiBotService.SendToWecomList.Add("开始报工");
        foreach (var account in accounts)
        {
            Console.WriteLine($"开始执行账号：{account.ChineseName}");
            var bot = new ApiBotService(account);
            await bot.RunAsync();
        }
        Console.WriteLine("全部账号执行完毕。");
        await WecomNotifier.SendToWeCom(string.Join("\n", ApiBotService.SendToWecomList) + "\n" + "帮你们都报工了，不用谢", atAll: true);

        // 重新安排下一次
        ScheduleNextRun();
    }
}