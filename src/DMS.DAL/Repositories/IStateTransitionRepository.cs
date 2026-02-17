using DMS.DAL.Entities;

namespace DMS.DAL.Repositories;

public interface IStateTransitionRuleRepository
{
    Task<StateTransitionRule?> GetRuleAsync(DocumentState fromState, DocumentState toState);
    Task<List<StateTransitionRule>> GetRulesFromStateAsync(DocumentState fromState);
    Task<List<StateTransitionRule>> GetAllActiveRulesAsync();
}

public interface IStateTransitionLogRepository
{
    Task CreateAsync(StateTransitionLog log);
    Task<List<StateTransitionLog>> GetByDocumentIdAsync(Guid documentId);
    Task<StateTransitionLog?> GetLatestByDocumentIdAsync(Guid documentId);
}
