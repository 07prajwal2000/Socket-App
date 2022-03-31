namespace Shared.Models;

public class ClientDetails
{
    public int Id { get; set; }
    public Guid UniqueId { get; set; } = Guid.NewGuid();
}