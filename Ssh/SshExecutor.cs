using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Renci.SshNet;

namespace NetstatSshParsing.Ssh;

public interface ISshExecutor
{
    Task Connect(string addr, string login, string password);

    Task<string> ExecuteCommand(string command);
}

public abstract class SshExecutor : ISshExecutor, IDisposable
{
    protected const string ALLOWED_PROGRAM = "netstat -atn | grep ESTABLISHED";

    protected record SshContext
    {
        public required string Address { get; init; }

        public required string Password { get; init; }

        public required string Login { get; init; }
    }

    protected SshContext? _context { get; private set; }

    public async Task Connect(string address, string login, string password)
    {
        this._context = new SshContext
        {
            Address = address,
            Password = password,
            Login = login,
        };

        await Connect_Internal();
    }

    protected abstract Task Connect_Internal();

    protected abstract void Dispose_Internal();

    /// <summary>
    /// Should work fine only with simple commands. Cancellation token is 
    /// provided for cancelling programs like "top"
    /// </summary>
    public abstract Task<string> ExecuteCommand(string command);

    public void Dispose()
    {
        Dispose_Internal();
    }
}

public class SshNetExecutor : SshExecutor
{
    public const string LOGNAME = "SSH NET EXECUTOR";
    private readonly ILogger<SshNetExecutor> logger;
    private SshClient? _client;

    public SshNetExecutor(
            ILogger<SshNetExecutor> logger
            )
    {
        this.logger = logger;
    }

    public override Task<string> ExecuteCommand(string command)
    {
        Debug.Assert(this._client is not null);
        Debug.Assert(this._client.IsConnected is true);
        Debug.Assert(ALLOWED_PROGRAM == command);

        logger.LogTrace("{logName}: executing command: \"{command}\" for context {context}",
                LOGNAME,
                command,
                this._context);

        using var cmd = this._client.RunCommand(command);

        return Task.FromResult(cmd.Result);
    }

    protected override async Task Connect_Internal()
    {
        Debug.Assert(this._context is not null);

        var host = this._context.Address;
        var login = this._context.Login;
        var pass = this._context.Password;

        this._client = new SshClient(host, login, pass);
        await this._client.ConnectAsync(CancellationToken.None);
    }

    protected override void Dispose_Internal()
    {
        this._client?.Dispose();
    }

}

public class MockSshExecutor : SshExecutor
{
    private const string LOGNAME = "[DEBUG] MOCK SSH EXECUTOR";
    private const string PATH = "./test-src.txt";

    private readonly ILogger<MockSshExecutor> logger;

    public MockSshExecutor(
            ILogger<MockSshExecutor> logger
            )
    {
        this.logger = logger;
    }

    /// <summary>
    ///	works only for "netstat -atn | grep ESTABLISHED"
    /// </summary>
    public override async Task<string> ExecuteCommand(string command)
    {
        Debug.Assert(this._context is not null);
        Debug.Assert(ALLOWED_PROGRAM == command);
        Debug.Assert(File.Exists(PATH));

        logger.LogTrace("{logName}: executing command: \"{command}\" for context {context}",
                LOGNAME,
                command,
                this._context);

        var result = await File.ReadAllTextAsync(PATH);

        logger.LogTrace("{logName}: executed command successfully: \"{command}\" for context {context}, result: \n{result}",
                LOGNAME,
                command,
                this._context,
                result);

        return result;
    }

    protected override Task Connect_Internal()
    {
        return Task.CompletedTask;
    }

    protected override void Dispose_Internal()
    {

    }
}
