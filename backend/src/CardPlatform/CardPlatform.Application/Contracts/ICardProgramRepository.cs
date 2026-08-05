using AfriWallet.CardPlatform.Domain.Entities;

namespace AfriWallet.CardPlatform.Application.Contracts;

public interface ICardProgramRepository
{
    Task<IReadOnlyList<CardProgram>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<CardProgram?> GetByIdAsync(string programId, CancellationToken cancellationToken = default);
    Task<CardProgram> CreateAsync(CardProgram program, CancellationToken cancellationToken = default);
    Task<CardProgram?> UpdateAsync(CardProgram program, CancellationToken cancellationToken = default);
}
