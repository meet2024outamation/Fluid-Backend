namespace Fluid.API.Models.User;



public class UserBasicDetailsDto
{
    public int Id { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public int? ClientId { get; set; }
    public int? AbstractorId { get; set; }
    public string? AbstractorName { get; set; }
    public string? Email { get; set; }
    public int UserType { get; set; }
    public int? TeamId { get; set; } = null;
    public int RoleId { get; set; }
}
