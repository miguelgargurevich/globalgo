using GlobalGo.Application.DTOs;

namespace GlobalGo.Application.Interfaces;

public interface ICreditEvaluationService
{
    Task<CreditEvaluationResponse> EvaluateAsync(EvaluateCreditRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<CustomerHistoryItemResponse>> GetCustomerHistoryAsync(string dni, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<GeneralHistoryItemResponse>> GetGeneralHistoryAsync(GeneralHistoryFilterRequest filter, CancellationToken cancellationToken = default);
    Task<PortfolioRiskReportResponse> GetPortfolioRiskReportAsync(CancellationToken cancellationToken = default);
}