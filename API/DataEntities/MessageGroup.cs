namespace API.DataEntities;

using System.ComponentModel.DataAnnotations;

public class MessageGroup
{
    [Key]
    public required string Name { get; set; }
    public ICollection<Connection> Connections { get; set; } = [];
}
