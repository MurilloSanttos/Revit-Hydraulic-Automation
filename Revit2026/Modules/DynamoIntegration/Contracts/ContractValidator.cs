using Newtonsoft.Json;
using Revit2026.Modules.DynamoIntegration.Contracts;

namespace Revit2026.Modules.DynamoIntegration.Contracts
{
    /// <summary>
    /// Validador de contratos JSON do protocolo Plugin↔Dynamo.
    /// Garante que requests e responses estejam em conformidade.
    /// </summary>
    public static class ContractValidator
    {
        // ══════════════════════════════════════════════════════════
        //  VALIDAR REQUEST
        // ══════════════════════════════════════════════════════════

        /// <summary>
        /// Valida um PluginToDynamoContract antes do envio.
        /// </summary>
        public static ValidationResult ValidarRequest(PluginToDynamoContract request)
        {
            var result = new ValidationResult();

            if (request == null)
            {
                result.Adicionar("Request é null.", ValidationSeverity.Fatal);
                return result;
            }

            // Protocolo
            if (string.IsNullOrEmpty(request.ProtocolVersion))
                result.Adicionar("ProtocolVersion ausente.", ValidationSeverity.Error);

            // RequestId
            if (string.IsNullOrEmpty(request.RequestId))
                result.Adicionar("RequestId ausente.", ValidationSeverity.Error);

            // Timestamp
            if (string.IsNullOrEmpty(request.Timestamp))
                result.Adicionar("Timestamp ausente.", ValidationSeverity.Warning);

            // Command
            if (!Enum.IsDefined(typeof(DynamoCommand), request.Command))
                result.Adicionar(
                    $"Command '{request.Command}' não reconhecido.",
                    ValidationSeverity.Error);

            // Payload
            ValidarPayload(request, result);

            return result;
        }

        private static void ValidarPayload(
            PluginToDynamoContract request,
            ValidationResult result)
        {
            var payload = request.Payload;

            // Commands que exigem workspace
            var requiresWorkspace = request.Command is
                DynamoCommand.RunWorkspace or
                DynamoCommand.LoadWorkspace or
                DynamoCommand.SetInputs or
                DynamoCommand.GetOutputs;

            if (requiresWorkspace)
            {
                if (payload?.Workspace == null)
                    result.Adicionar(
                        $"Command '{request.Command}' exige Workspace.",
                        ValidationSeverity.Error);
                else if (string.IsNullOrEmpty(payload.Workspace.Path))
                    result.Adicionar(
                        "Workspace.Path ausente.",
                        ValidationSeverity.Error);
            }

            // SetInputs exige inputs
            if (request.Command == DynamoCommand.SetInputs)
            {
                if (payload?.Inputs == null || payload.Inputs.Count == 0)
                    result.Adicionar(
                        "SetInputs exige pelo menos um input.",
                        ValidationSeverity.Error);
            }

            // GetOutputs exige outputs
            if (request.Command == DynamoCommand.GetOutputs)
            {
                if (payload?.Outputs == null || payload.Outputs.Count == 0)
                    result.Adicionar(
                        "GetOutputs exige pelo menos um output request.",
                        ValidationSeverity.Error);
            }

            // Validar inputs individuais
            if (payload?.Inputs != null)
            {
                for (int i = 0; i < payload.Inputs.Count; i++)
                {
                    var input = payload.Inputs[i];
                    if (string.IsNullOrEmpty(input.NodeId))
                        result.Adicionar(
                            $"Input[{i}]: NodeId ausente.",
                            ValidationSeverity.Error);
                }
            }

            // Validar outputs individuais
            if (payload?.Outputs != null)
            {
                for (int i = 0; i < payload.Outputs.Count; i++)
                {
                    var output = payload.Outputs[i];
                    if (string.IsNullOrEmpty(output.NodeId))
                        result.Adicionar(
                            $"Output[{i}]: NodeId ausente.",
                            ValidationSeverity.Error);
                }
            }

            // Timeout
            if (payload?.Execution != null && payload.Execution.TimeoutMs <= 0)
                result.Adicionar(
                    "TimeoutMs deve ser positivo.",
                    ValidationSeverity.Warning);
        }

        // ══════════════════════════════════════════════════════════
        //  VALIDAR RESPONSE
        // ══════════════════════════════════════════════════════════

        /// <summary>
        /// Valida uma DynamoToPluginResponse recebida.
        /// </summary>
        public static ValidationResult ValidarResponse(DynamoToPluginResponse response)
        {
            var result = new ValidationResult();

            if (response == null)
            {
                result.Adicionar("Response é null.", ValidationSeverity.Fatal);
                return result;
            }

            if (string.IsNullOrEmpty(response.RequestId))
                result.Adicionar("RequestId ausente na response.", ValidationSeverity.Error);

            if (response.Result == null)
            {
                result.Adicionar("Result ausente.", ValidationSeverity.Error);
                return result;
            }

            // Validar outputs
            if (response.Result.Outputs != null)
            {
                for (int i = 0; i < response.Result.Outputs.Count; i++)
                {
                    var output = response.Result.Outputs[i];
                    if (string.IsNullOrEmpty(output.NodeId))
                        result.Adicionar(
                            $"Output[{i}]: NodeId ausente.",
                            ValidationSeverity.Warning);
                }
            }

            // Validar errors
            if (response.Result.Errors != null)
            {
                foreach (var error in response.Result.Errors)
                {
                    if (string.IsNullOrEmpty(error.Code))
                        result.Adicionar(
                            "DynamoError sem Code.",
                            ValidationSeverity.Warning);
                }
            }

            return result;
        }

        // ══════════════════════════════════════════════════════════
        //  VALIDAR JSON RAW
        // ══════════════════════════════════════════════════════════

        /// <summary>
        /// Valida se um JSON é um request válido.
        /// </summary>
        public static ValidationResult ValidarRequestJson(string json)
        {
            var result = new ValidationResult();

            if (string.IsNullOrWhiteSpace(json))
            {
                result.Adicionar("JSON vazio.", ValidationSeverity.Fatal);
                return result;
            }

            try
            {
                var request = JsonConvert.DeserializeObject<PluginToDynamoContract>(json);
                if (request == null)
                {
                    result.Adicionar("Desserialização retornou null.", ValidationSeverity.Fatal);
                    return result;
                }

                return ValidarRequest(request);
            }
            catch (JsonException ex)
            {
                result.Adicionar(
                    $"JSON inválido: {ex.Message}",
                    ValidationSeverity.Fatal);
                return result;
            }
        }

        /// <summary>
        /// Valida se um JSON é uma response válida.
        /// </summary>
        public static ValidationResult ValidarResponseJson(string json)
        {
            var result = new ValidationResult();

            if (string.IsNullOrWhiteSpace(json))
            {
                result.Adicionar("JSON vazio.", ValidationSeverity.Fatal);
                return result;
            }

            try
            {
                var response = JsonConvert.DeserializeObject<DynamoToPluginResponse>(json);
                if (response == null)
                {
                    result.Adicionar("Desserialização retornou null.", ValidationSeverity.Fatal);
                    return result;
                }

                return ValidarResponse(response);
            }
            catch (JsonException ex)
            {
                result.Adicionar(
                    $"JSON inválido: {ex.Message}",
                    ValidationSeverity.Fatal);
                return result;
            }
        }
    }

    // ══════════════════════════════════════════════════════════════
    //  VALIDATION RESULT
    // ══════════════════════════════════════════════════════════════

    public enum ValidationSeverity
    {
        Info,
        Warning,
        Error,
        Fatal
    }

    public class ValidationIssue
    {
        public string Mensagem { get; set; } = "";
        public ValidationSeverity Severidade { get; set; }

        public override string ToString() =>
            $"[{Severidade}] {Mensagem}";
    }

    public class ValidationResult
    {
        public List<ValidationIssue> Issues { get; set; } = new();

        public bool Valido =>
            !Issues.Any(i =>
                i.Severidade == ValidationSeverity.Error ||
                i.Severidade == ValidationSeverity.Fatal);

        public bool TemAvisos =>
            Issues.Any(i => i.Severidade == ValidationSeverity.Warning);

        public int TotalErros =>
            Issues.Count(i =>
                i.Severidade == ValidationSeverity.Error ||
                i.Severidade == ValidationSeverity.Fatal);

        public void Adicionar(string mensagem, ValidationSeverity severidade)
        {
            Issues.Add(new ValidationIssue
            {
                Mensagem = mensagem,
                Severidade = severidade
            });
        }

        public override string ToString()
        {
            if (Valido && !TemAvisos)
                return "Válido";

            return $"{(Valido ? "Válido" : "Inválido")} " +
                   $"({TotalErros} erros, " +
                   $"{Issues.Count(i => i.Severidade == ValidationSeverity.Warning)} avisos)";
        }
    }
}
