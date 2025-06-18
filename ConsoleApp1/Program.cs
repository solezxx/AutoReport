// Program.cs
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text;
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
            Console.ReadLine();
            var bot = new ApiBotService(account);
            await bot.RunAsync();
            Console.WriteLine($"账号 {account.Username} 完成\n");
        }

        Console.WriteLine("全部账号执行完毕。");
    }
}