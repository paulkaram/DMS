using DMS.BL.DTOs;
using DMS.DAL.Entities;

namespace DMS.BL.Interfaces;

/// <summary>
/// Manages document lifecycle state transitions with data-driven rules,
/// immutability enforcement, and legal hold integration.
/// </summary>
public interface IDocumentStateService
{
    /// <summary>
    /// Transition a document to a new state, validating against transition rules.
    /// </summary>
    Task<ServiceResult<DocumentDto>> TransitionAsync(Guid documentId, StateTransitionRequestDto dto, Guid userId);

    /// <summary>
    /// Place a document on legal hold, saving its current state for later restoration.
    /// </summary>
    Task<ServiceResult> PlaceOnHoldAsync(Guid documentId, Guid legalHoldId, Guid userId);

    /// <summary>
    /// Release a document from legal hold, restoring its previous state.
    /// </summary>
    Task<ServiceResult> ReleaseFromHoldAsync(Guid documentId, Guid userId);

    /// <summary>
    /// Initiate pending disposal for a document (used by retention engine).
    /// </summary>
    Task<ServiceResult> InitiatePendingDisposalAsync(Guid documentId, Guid userId, string reason);

    /// <summary>
    /// Get allowed transitions for the current document state and user role.
    /// </summary>
    Task<ServiceResult<List<AllowedTransitionDto>>> GetAllowedTransitionsAsync(Guid documentId, Guid userId, IEnumerable<string> userRoles);

    /// <summary>
    /// Get the full transition history for a document.
    /// </summary>
    Task<ServiceResult<List<StateTransitionLogDto>>> GetTransitionHistoryAsync(Guid documentId);

    /// <summary>
    /// Check if a document state is immutable (no edits/deletes allowed).
    /// </summary>
    bool IsImmutable(DocumentState state);
}
