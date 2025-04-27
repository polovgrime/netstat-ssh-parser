using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace NetstatSshParsing.Net;

public class NamesStorage
{
    private const string PATH = "./ip-table.json";
    private const string LOGNAME = "NAMES STORAGE";

    private readonly ILogger<NamesStorage> logger;
    // address to name model relation
    private ConcurrentDictionary<string, NameAddressModel> _names = new();

    public NamesStorage(
            ILogger<NamesStorage> logger
            )
    {
        this.logger = logger;
    }

    public async Task LoadTable()
    {
        try
        {
            Debug.Assert(File.Exists(PATH));

            logger.LogCritical("{logName}: started loading name table into memory",
                    LOGNAME);
            var serializerOptions = new JsonSerializerOptions();
            serializerOptions.PropertyNameCaseInsensitive = true;

            var stream = File.OpenRead(PATH);
            try
            {
                var deserializedArray = await JsonSerializer.DeserializeAsync<NameAddressModel[]>(stream, serializerOptions);
                logger.LogCritical("{logName}: finished loading name table",
                        LOGNAME);
                if (deserializedArray is not null)
                {
                    WriteModels(deserializedArray);
                }
                else
                {
                    logger.LogCritical("{logName}: empty table", LOGNAME);
                }
            }
            finally
            {
                stream.Close();
                stream.Dispose();
            }

        }
        catch (Exception ex)
        {
            logger.LogError(ex, "{logName}: error when loading table", LOGNAME);
        }
    }

    private void WriteModels(NameAddressModel[] models)
    {
        foreach (var model in models)
        {
            this._names.AddOrUpdate(model.Address, (_, added) => added, (_, _, added) => added, model);
        }
    }

    public NameAddressModel? TryFindNameInTable(string address)
    {
        if (this._names.TryGetValue(address, out var model))
        {
            return model;
        }

        return null;
    }
}

public record NameAddressModel
{
    public required string Address { get; set; }

    public required string Name { get; set; }
}
