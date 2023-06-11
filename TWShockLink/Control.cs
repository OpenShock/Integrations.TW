namespace ShockLink.Integrations.TW;

public class Control
{
    public Guid Id { get; set; }
    public ControlType Type { get; set; }
    public byte Intensity { get; set; }
    public uint Duration { get; set; }
}