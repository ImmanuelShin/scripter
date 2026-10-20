namespace Scripter.Models;

public class Script
{
    public string Name { get; set; } = "Untitled";
    public List<MacroAction> Actions { get; set; } = [];
    public int RepeatCount { get; set; } = 1;
    public bool RepeatUntilStopped { get; set; }

    public Script Clone() => new()
    {
        Name = Name,
        RepeatCount = RepeatCount,
        RepeatUntilStopped = RepeatUntilStopped,
        Actions = Actions.Select(a => a.Clone()).ToList()
    };
}
