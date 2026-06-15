namespace VetAnesthesiaApp.Services.Workflow;

public sealed class SessionCompletionSummary
{
    public SessionCompletionSummary(IReadOnlyList<SessionCompletionItem> items)
    {
        Items = items;
    }

    public IReadOnlyList<SessionCompletionItem> Items { get; }

    public bool IsReadyForHandoff => Items.All(x => !x.BlocksHandoff || x.IsComplete);

    public int CompleteCount => Items.Count(x => x.IsComplete);

    public int IncompleteCount => Items.Count(x => !x.IsComplete);
}
