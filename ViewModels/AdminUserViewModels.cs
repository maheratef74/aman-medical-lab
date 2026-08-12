namespace DrMohamedWeb.ViewModels
{
    public class CreateAdminUserViewModel
    {
        public string Email { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string Role { get; set; } = "Admin";
        public bool IsActive { get; set; } = true;
    }

    public class EditAdminUserViewModel
    {
        public int Id { get; set; }
        public string Email { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string? Password { get; set; }
        public string Role { get; set; } = "Admin";
        public bool IsActive { get; set; } = true;
        public bool IsCurrentUser { get; set; }
    }
}