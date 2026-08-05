using AfriWallet.CardPlatform.Application.Contracts;
using AfriWallet.CardPlatform.Domain.Entities;

namespace AfriWallet.CardPlatform.Application.Services;

public sealed class CardProgramService
{
    private readonly ICardProgramRepository _repository;

    public CardProgramService(ICardProgramRepository repository)
    {
        _repository = repository;
    }

    public Task<IReadOnlyList<CardProgram>> GetAllAsync(CancellationToken cancellationToken = default)
        => _repository.GetAllAsync(cancellationToken);

    public Task<CardProgram?> GetByIdAsync(string programId, CancellationToken cancellationToken = default)
        => _repository.GetByIdAsync(programId, cancellationToken);

    public Task<CardProgram> CreateAsync(CardProgram program, CancellationToken cancellationToken = default)
        => _repository.CreateAsync(Normalize(program), cancellationToken);

    public Task<CardProgram?> UpdateAsync(string programId, CardProgram program, CancellationToken cancellationToken = default)
    {
        var updated = Normalize(program, programId);
        return _repository.UpdateAsync(updated, cancellationToken);
    }

    private static CardProgram Normalize(CardProgram program, string? programId = null)
    {
        return new CardProgram
        {
            ProgramId = string.IsNullOrWhiteSpace(programId) ? (string.IsNullOrWhiteSpace(program.ProgramId) ? Guid.NewGuid().ToString("N") : program.ProgramId) : programId,
            ProgramCode = program.ProgramCode,
            DisplayName = program.DisplayName,
            Network = program.Network,
            CardType = program.CardType,
            FundingType = program.FundingType,
            CountryCode = program.CountryCode,
            BaseCurrency = program.BaseCurrency,
            SupportedCurrencies = program.SupportedCurrencies?.ToList() ?? [],
            Environment = program.Environment,
            Status = program.Status,
            Capabilities = program.Capabilities ?? new CardProgramCapabilities(),
            Limits = program.Limits ?? new CardProgramLimits(),
            Fees = program.Fees ?? new CardProgramFees(),
            Priority = program.Priority,
            CreatedAt = program.CreatedAt == default ? DateTimeOffset.UtcNow : program.CreatedAt,
            UpdatedAt = program.UpdatedAt == default ? DateTimeOffset.UtcNow : program.UpdatedAt,
            Version = program.Version > 0 ? program.Version : 1
        };
    }
}
