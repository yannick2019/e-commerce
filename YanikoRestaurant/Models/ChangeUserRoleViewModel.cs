namespace YanikoRestaurant.Models;

public class ChangeUserRoleViewModel
{
    public string UserId { get; set; } = null!;
    public string CurrentRole { get; set; } = null!;
    public string NewRole { get; set; } = null!;
}
