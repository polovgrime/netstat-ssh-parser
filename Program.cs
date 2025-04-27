// See https://aka.ms/new-console-template for more information
//
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NetstatSshParsing;
using NetstatSshParsing.Netstat;
using NetstatSshParsing.Ssh;
using NetstatSshParsing.Net;
using System.Text;
using System.Text.Json;
using System.Diagnostics;
using Microsoft.Extensions.Logging;

Debug.Assert(File.Exists("./settings.json"));
var settingsRaw = File.ReadAllText("./settings.json");

var serializerOptions = new JsonSerializerOptions();
serializerOptions.PropertyNameCaseInsensitive = true;
var settings = JsonSerializer.Deserialize<AppSettings[]>(settingsRaw, serializerOptions);
Debug.Assert(settings is not null);


HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);
// debug:
// builder.Services.AddTransient<ISshExecutor, MockSshExecutor>();
builder.Services.AddTransient<ISshExecutor, SshNetExecutor>();
builder.Services.AddTransient<NetstatParser>();
builder.Services.AddSingleton<NamesStorage>();

using IHost host = builder.Build();
Console.WriteLine("executing temp ssh program");

Console.WriteLine("result:");
var parser = host.Services.GetRequiredService<NetstatParser>();
var nameStorage = host.Services.GetRequiredService<NamesStorage>();
var logger = host.Services.GetRequiredService<ILogger<Program>>();
await nameStorage.LoadTable();
var sb = new StringBuilder();
var executors = new Dictionary<string, (ISshExecutor, AppSettings)>();

foreach (var setting in settings)
{
    try
    {
        var sshExecutor = host.Services.GetRequiredService<ISshExecutor>();

        await sshExecutor.Connect(setting.Host, setting.Login, setting.Password);

        if (executors.ContainsKey(setting.Host) is false)
        {
            executors.Add(setting.Host, (sshExecutor, setting));
        }
        else
        {
            logger.LogWarning("server {addr} already added, skipping", setting.Host);
        }
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "couldn't connect to server {server}", setting.Host);
    }
}

while (true)
{
    sb.Clear();
    foreach (var item in executors.ToArray())
    {
        var (executor, setting) = item.Value;
        try
        {
            await HandleReadingServerData(sb, setting.Host, executor);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "couldn't read info from server {server}", setting.Host);
            executors.Remove(item.Key);
        }
    }
    Console.Clear();
    Console.WriteLine(sb.ToString());
    await Task.Delay(1000);
}



async Task HandleReadingServerData(StringBuilder sb, string server, ISshExecutor sshExecutor)
{
    var result = await sshExecutor.ExecuteCommand("netstat -atn | grep ESTABLISHED");
    var rows = result.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);

    sb.AppendLine($"SERVER {server}: ");
    foreach (var row in rows)
    {
        var parsed = parser.ParseLine(row);
        // sb.AppendLine($"")
        if (parsed is not null)
        {
            var nameModel = nameStorage.TryFindNameInTable(parsed.RemoteAddress);

            string remoteAddr;

            if (parsed.PortOnRemote is not null)
            {
                remoteAddr = $"{parsed.RemoteAddress}:{parsed.PortOnRemote}";
            }
            else
            {
                remoteAddr = parsed.RemoteAddress;
            }

            if (nameModel is not null)
            {
                var displayName = $"{nameModel.Name}({remoteAddr})";
                sb.Append(displayName);
            }
            else
            {
                sb.Append(remoteAddr);
            }

            string localAddr;

            if (parsed.PortOnLocal is not null)
            {
                localAddr = $"{parsed.LocalAddress}:{parsed.PortOnLocal}";
            }
            else
            {
                localAddr = parsed.LocalAddress;
            }

            sb
                .Append(" on local ")
                .Append(localAddr);

            sb.AppendLine();
        }
    }
}


