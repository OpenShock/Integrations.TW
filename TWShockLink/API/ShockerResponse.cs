namespace ShockLink.Integrations.TW.API;

public class ShockerResponse
{
    public Guid Id { get; set; }
    public ushort RfId { get; set; }
    public string Name { get; set; }
    public bool IsPaused { get; set; }
    public DateTime CreatedOn { get; set; }
    public ShockerModelType Model { get; set; }
}