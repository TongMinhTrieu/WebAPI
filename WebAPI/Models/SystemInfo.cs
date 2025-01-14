namespace WebAPI.Models
{
    // Model classes
    public class SystemInfo
    {
        public string ClientIp { get; set; }
        public string DateStamp { get; set; }
        public string CpuUsage { get; set; }
        public string MemoryAvailable { get; set; }
        public string DiskFreeSpace { get; set; }
        public string DiskTotalSpace { get; set; }
        public List<NetworkSpeedInfo> NetworkSpeed { get; set; }
        public List<ApiStatisticsInfo> ApiStatistics { get; set; }
        public List<string> ListDatabases { get; set; }
    }

    public class NetworkSpeedInfo
    {
        public string NetworkInterface { get; set; }
        public string ReceiveSpeed { get; set; }
        public string SendSpeed { get; set; }
    }

    public class ApiStatisticsInfo
    {
        public string Api { get; set; }
        public int Calls { get; set; }
    }
}
