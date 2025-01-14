using MongoDB.Driver;
using MongoDB.Bson;
using System.Text.Json;
using WebAPI.Models;

public class MongoDBService
{
    private readonly IMongoCollection<BsonDocument> _collection;
    private readonly ILogger<MongoDBService>? _logger;

    public MongoDBService()
    {
        var client = new MongoClient("mongodb://localhost:27017");
        var database = client.GetDatabase("WebSocketDB");
        _collection = database.GetCollection<BsonDocument>("Data");
    }
    public MongoDBService(ILogger<MongoDBService> logger) : this()
    {
        _logger = logger;
    }


    public void SaveData(SystemInfo systemInfo)
    {
        try
        {
            // Chuyển đổi đối tượng SystemInfo thành BsonDocument
            var document = new BsonDocument
            {
                { "ClientIp", systemInfo.ClientIp },
                { "DateStamp", systemInfo.DateStamp },
                { "CpuUsage", systemInfo.CpuUsage },
                { "MemoryAvailable", systemInfo.MemoryAvailable },
                { "DiskFreeSpace", systemInfo.DiskFreeSpace },
                { "DiskTotalSpace", systemInfo.DiskTotalSpace},
                { "NetworkSpeed", new BsonArray(
                    systemInfo.NetworkSpeed.ConvertAll(networkSpeed => new BsonDocument
                    {
                        { "NetworkInterface", networkSpeed.NetworkInterface },
                        { "ReceiveSpeed", networkSpeed.ReceiveSpeed },
                        { "SendSpeed", networkSpeed.SendSpeed }
                    }))
                },
                { "ApiStatistics", new BsonArray(
                    systemInfo.ApiStatistics.ConvertAll(apiStat => new BsonDocument
                    {
                        { "Api", apiStat.Api },
                        { "Calls", apiStat.Calls }
                    }))
                },
                { "ListDatabases", new BsonArray(systemInfo.ListDatabases) }
            };

            // Lưu document vào MongoDB
            _collection.InsertOne(document);
            Console.WriteLine("Data saved to MongoDB successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error saving data to MongoDB: {ex.Message}");
        }
    }
}
