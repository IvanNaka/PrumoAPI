using Prumo.Application.DTOs.Okr;

namespace Prumo.Application.Interfaces
{
    // OKRs e Key Results (RF14, RF15, RF17, UC8) e associações (RF16, UC9).
    public interface IOkrService
    {
        Task<IEnumerable<OkrDto>> GetAllAsync();
        Task<OkrDto> GetAsync(Guid id);
        Task<OkrDto> CreateAsync(SalvarOkrDto dto);
        Task<OkrDto> UpdateAsync(Guid id, SalvarOkrDto dto);
        Task<KeyResultDto> UpdateKeyResultValueAsync(Guid keyResultId, decimal? valorAtual);
    }
}
