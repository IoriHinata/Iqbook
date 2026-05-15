namespace Iqbook.Core;

public sealed class StoryEngine
{
    private readonly Dictionary<string, Node> _nodes;
    private readonly Dictionary<string, bool> _flags = new();
    public Node CurrentNode { get; private set; }

    public StoryEngine(StoryContent story)
    {
        _nodes = story.Nodes.ToDictionary(x => x.Id, StringComparer.Ordinal);
        CurrentNode = _nodes[story.StartNodeId];
    }

    public IReadOnlyDictionary<string, bool> Flags => _flags;

    public IReadOnlyList<Choice> AvailableChoices() => CurrentNode.Choices
        .Where(c => c.ConditionFlag is null || (_flags.TryGetValue(c.ConditionFlag, out var v) && v == c.ConditionValue))
        .ToList();

    public void Choose(int index)
    {
        var choices = AvailableChoices();
        if (index < 0 || index >= choices.Count) throw new ArgumentOutOfRangeException(nameof(index));
        var choice = choices[index];
        if (choice.SetFlag is not null) _flags[choice.SetFlag] = choice.SetValue;
        CurrentNode = _nodes[choice.NextNode];
    }
}
