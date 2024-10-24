namespace WebAPI.Models
{
    public class User
    {
        public int Id { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public string EmailAddress { get; set; }
        public DateTime DateRegister { get; set; }
        public string Role { get; set; } // Thêm role cho tài khoản
    }

}
