using AfriWallet.CardPlatform.Application.Contracts;
using AfriWallet.CardPlatform.Domain.Entities;

namespace AfriWallet.CardPlatform.Infrastructure;

public sealed class InMemoryCardProgramRepository : ICardProgramRepository
{
    private readonly List<CardProgram> _programs;

    public InMemoryCardProgramRepository()
    {
        _programs = SeedData.CreateSeedPrograms();
    }

    public Task<IReadOnlyList<CardProgram>> GetAllAsync(CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<CardProgram>>(_programs.AsReadOnly());

    public Task<CardProgram?> GetByIdAsync(string programId, CancellationToken cancellationToken = default)
        => Task.FromResult(_programs.FirstOrDefault(p => p.ProgramId.Equals(programId, StringComparison.OrdinalIgnoreCase)));

    public Task<CardProgram> CreateAsync(CardProgram program, CancellationToken cancellationToken = default)
    {
        _programs.Add(program);
        return Task.FromResult(program);
    }

    public Task<CardProgram?> UpdateAsync(CardProgram program, CancellationToken cancellationToken = default)
    {
        var existing = _programs.FirstOrDefault(p => p.ProgramId.Equals(program.ProgramId, StringComparison.OrdinalIgnoreCase));
        if (existing is null) { return Task.FromResult<CardProgram?>(null); }

        var index = _programs.IndexOf(existing);
        _programs[index] = program;
        return Task.FromResult<CardProgram?>(program);
    }
}
