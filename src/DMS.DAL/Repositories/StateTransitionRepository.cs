using DMS.DAL.Data;
using DMS.DAL.Entities;
using Microsoft.EntityFrameworkCore;

namespace DMS.DAL.Repositories;

public class StateTransitionRuleRepository : IStateTransitionRuleRepository
{
    private readonly DmsDbContext _context;

    public StateTransitionRuleRepository(DmsDbContext context) => _context = context;

    public async Task<StateTransitionRule?> GetRuleAsync(DocumentState fromState, DocumentState toState)
    {
        return await _context.StateTransitionRules
            .FirstOrDefaultAsync(r => r.FromState == fromState && r.ToState == toState);
    }

    public async Task<List<StateTransitionRule>> GetRulesFromStateAsync(DocumentState fromState)
    {
        return await _context.StateTransitionRules
            .Where(r => r.FromState == fromState)
            .ToListAsync();
    }

    public async Task<List<StateTransitionRule>> GetAllActiveRulesAsync()
    {
        return await _context.StateTransitionRules.ToListAsync();
    }
}

public class StateTransitionLogRepository : IStateTransitionLogRepository
{
    private readonly DmsDbContext _context;

    public StateTransitionLogRepository(DmsDbContext context) => _context = context;

    public async Task CreateAsync(StateTransitionLog log)
    {
        log.Id = Guid.NewGuid();
        _context.StateTransitionLogs.Add(log);
        await _context.SaveChangesAsync();
    }

    public async Task<List<StateTransitionLog>> GetByDocumentIdAsync(Guid documentId)
    {
        return await _context.StateTransitionLogs
            .Where(l => l.DocumentId == documentId)
            .OrderByDescending(l => l.TransitionedAt)
            .ToListAsync();
    }

    public async Task<StateTransitionLog?> GetLatestByDocumentIdAsync(Guid documentId)
    {
        return await _context.StateTransitionLogs
            .Where(l => l.DocumentId == documentId)
            .OrderByDescending(l => l.TransitionedAt)
            .FirstOrDefaultAsync();
    }
}
