namespace PluginCore.Interfaces;

/// <summary>
/// Integração com Dynamo — interface para execução de scripts.
/// </summary>
public interface IDynamoIntegration
{
    /// <summary>Executa um script Dynamo por caminho.</summary>
    Task<Common.DynamoPayload> ExecutarScriptAsync(
        string scriptPath, Dictionary<string, object> inputs);

    /// <summary>Verifica se o Dynamo está disponível.</summary>
    bool DynamoDisponivel();

    /// <summary>Obtém a versão do Dynamo instalada.</summary>
    string ObterVersao();

    /// <summary>Lista scripts disponíveis na pasta de scripts.</summary>
    List<string> ListarScripts(string pastaBase);
}
