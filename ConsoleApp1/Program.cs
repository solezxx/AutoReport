// Program.cs
using System;
using System.Collections.Generic;
using System.Security.Principal;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;


class Program
{
    static async Task Main(string[] args)
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
        await WecomNotifier.SendToWeCom($"全部账号执行完毕。");
        Console.ReadKey();
    }
}