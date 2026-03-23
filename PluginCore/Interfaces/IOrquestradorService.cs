using PluginCore.Domain.Enums;

namespace PluginCore.Interfaces;

/// <summary>
/// Orquestrador do pipeline de execução.
/// Controla a sequência de etapas, validação humana e rollback.
/// </summary>
public interface IOrquestradorService
{
    /// <summary>Executa o pipeline completo.</summary>
    Task ExecutarPipelineAsync(CancellationToken cancellation = default);

    /// <summary>Executa uma etapa específica.</summary>
    Task ExecutarEtapaAsync(string stepId, CancellationToken cancellation = default);

    /// <summary>Aprova a etapa atual (validação humana).</summary>
    void AprovarEtapa(string stepId, string? comentario = null);

    /// <summary>Rejeita a etapa atual (volta para correção).</summary>
    void RejeitarEtapa(string stepId, string motivo);

    /// <summary>Obtém o status atual de uma etapa.</summary>
    StepStatus ObterStatus(string stepId);

    /// <summary>Obtém o progresso geral (0-100).</summary>
    double ObterProgresso();

    /// <summary>Rollback da última etapa executada.</summary>
    Task RollbackAsync(string stepId);
}
