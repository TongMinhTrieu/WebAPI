namespace WebAPI.Models
{
    public class SystemInfo
    {
        public string ClientIp { get; set; } // IP
        public string CpuUsage { get; set; }      // CPU %
        public string MemoryAvailable { get; set; }   // MB 
        public string DiskFreeSpace { get; set; } // GB
        public string DiskTotalSpace { get; set; } // GB
        public string NetworkSpeed { get; set; } // Mbps
        public List<object> ApiStatistics { get; set; } // Chứa danh sách API và số lượng yêu cầu
        public DateTime DateStamp { get; set; } // Thời gian

        // Phương thức chuyển đổi sang kiểu số nếu cần thiết
        public float GetCpuUsage() => float.TryParse(CpuUsage, out var result) ? result : 0;
        public float GetMemoryAvailable() => float.TryParse(MemoryAvailable, out var result) ? result : 0;
        public float GetDiskFreeSpace() => float.TryParse(DiskFreeSpace, out var result) ? result : 0;
        public float GetDiskTotalSpace() => float.TryParse(DiskTotalSpace, out var result) ? result : 0;
        public float GetNetworkSpeed() => float.TryParse(NetworkSpeed, out var result) ? result : 0;
    }
}
