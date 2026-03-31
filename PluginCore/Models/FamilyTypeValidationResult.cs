namespace PluginCore.Models
{
    /// <summary>
    /// Resultado da validação de tipo de família para um equipamento hidráulico.
    /// Indica se existe família e símbolo compatível carregado no modelo.
    /// </summary>
    public class FamilyTypeValidationResult
    {
        /// <summary>Se a validação passou (família e símbolo encontrados).</summary>
        public bool Valido { get; set; }

        /// <summary>Nome da família escolhida (se encontrada).</summary>
        public string FamiliaEscolhida { get; set; } = string.Empty;

        /// <summary>Nome do símbolo escolhido (se encontrado).</summary>
        public string SimboloEscolhido { get; set; } = string.Empty;

        /// <summary>Motivo da falha (se inválido).</summary>
        public string MotivoFalha { get; set; } = string.Empty;

        /// <summary>Padrão de busca que deu match (para diagnóstico).</summary>
        public string PadraoBusca { get; set; } = string.Empty;

        /// <summary>Total de alternativas verificadas antes do match.</summary>
        public int AlternativasVerificadas { get; set; }

        public override string ToString()
        {
            if (Valido)
                return $"✅ {FamiliaEscolhida} / {SimboloEscolhido}";
            return $"❌ {MotivoFalha}";
        }
    }
}
