using Iqbook.Runtime.Data;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Iqbook.Runtime.Core
{
    public class StoryRunner
    {
        private readonly Dictionary<string, StoryNode> _nodes;
        public readonly Dictionary<string, bool> Flags = new();
        public StoryNode Current { get; private set; }

        public StoryRunner(StoryContent content, string startNode = "start")
        {
            _nodes = content.nodes.ToDictionary(n => n.id);
            Current = _nodes[startNode];
        }

        public IReadOnlyList<Choice> GetAvailableChoices()
            => Current.choices.Where(IsChoiceAvailable).ToList();

        public void Choose(Choice choice)
        {
            if (!_nodes.TryGetValue(choice.next_node, out var next))
                throw new InvalidOperationException($"Unknown node: {choice.next_node}");
            Current = next;
        }

        private bool IsChoiceAvailable(Choice choice)
        {
            if (choice.conditions == null || choice.conditions.Count == 0) return true;
            foreach (var cond in choice.conditions)
            {
                var current = Flags.TryGetValue(cond.flag, out var v) && v;
                if (current != cond.value) return false;
            }
            return true;
        }
    }
}
