namespace IdentityService.Dto
{
    public class UserDto
    {
        public string UserName { get; set; }
        public string FirstName { get; set; }
        public string? MiddleName { get; set; }
        public string LastName { get; set; }
        public string Title { get; set; }
        public string Email { get; set; }
        public bool IsActive { get; set; }
        public bool IsStaff { get; set; }
        public string? PhoneNumber { get; set; }
    }
}