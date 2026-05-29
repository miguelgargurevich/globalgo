using System.ComponentModel.DataAnnotations;

namespace GlobalGo.Application.DTOs;

public sealed record EvaluateCreditRequest
{
    [Required]
    [StringLength(8, MinimumLength = 8)]
    public string Dni { get; init; } = string.Empty;

    [Required]
    [StringLength(120)]
    public string FullName { get; init; } = string.Empty;

    [Range(1, 100_000)]
    public decimal AmountRequested { get; init; }

    [Range(300, 100_000)]
    public decimal MonthlyIncome { get; init; }
}