namespace OrderingSystem.Shared;

public interface OrderAnalysisCompleted
{
    Guid OrderId { get; }
    string AnalysisResult { get; }
    DateTime CompletedAt { get; }
}