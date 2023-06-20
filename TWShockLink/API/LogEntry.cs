namespace ShockLink.Integrations.TW.API;

public class LogEntry
{
    public Guid Id { get; set; }

    public DateTime CreatedOn { get; set; }

    public ControlType Type { get; set; }

    public GenericIni ControlledBy { get; set; }

    public byte Intensity { get; set; }

    public uint Duration { get; set; }
}