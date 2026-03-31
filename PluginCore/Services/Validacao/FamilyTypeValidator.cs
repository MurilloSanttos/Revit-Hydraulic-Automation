using System.Globalization;
using System.Text;
using PluginCore.Domain.Enums;
using PluginCore.Interfaces;
using PluginCore.Models;

namespace PluginCore.Services.Validacao
{
    /// <summary>
    /// Valida se cada equipamento hidráulico tem família e símbolo
    /// MEP compatível carregado no modelo Revit.
    ///
    /// Usa mapa de alternativas de nome por EquipmentType com busca
    /// normalizada (case-insensitive, sem acentos, contains parcial).
    /// </summary>
    public class FamilyTypeValidator
    {
        private const string ETAPA = "03_Equipamentos";
        private const string COMPONENTE = "FamilyTypeValidator";

        /// <summary>
        /// Mapa de alternativas: EquipmentType → possíveis termos de busca (PT-BR + EN).
        /// A busca usa contains normalizado — qualquer nome de família
        /// que contenha um destes termos será considerado compatível.
        /// </summary>
        private static readonly Dictionary<EquipmentType, string[]> MapaAlternativas = new()
        {
            [EquipmentType.Toilet] = new[]
            {
                "vaso", "bacia", "toilet", "wc", "sanitario", "sanitária",
                "bacia sanitaria", "bacia sanitária", "closet"
            },
            [EquipmentType.Sink] = new[]
            {
                "lavatorio", "lavatório", "cuba", "sink", "wash basin",
                "lavabo", "pia de banheiro", "bathroom sink"
            },
            [EquipmentType.Shower] = new[]
            {
                "chuveiro", "ducha", "shower", "ducha higienica",
                "ducha higiênica", "shower head"
            },
            [EquipmentType.Bathtub] = new[]
            {
                "banheira", "bathtub", "tub", "hidromassagem",
                "spa", "bath"
            },
            [EquipmentType.KitchenSink] = new[]
            {
                "pia", "pia de cozinha", "kitchen sink", "cuba cozinha",
                "pia cozinha", "torneira cozinha", "kitchen"
            },
            [EquipmentType.LaundryTub] = new[]
            {
                "tanque", "tanque de lavar", "laundry tub", "laundry sink",
                "tanque lavanderia", "utility sink", "tanque de roupa"
            },
            [EquipmentType.WashingMachine] = new[]
            {
                "maquina de lavar", "máquina de lavar", "lavadora",
                "washing machine", "mlr", "lava roupa", "lava-roupa"
            },
            [EquipmentType.Dishwasher] = new[]
            {
                "lava loucas", "lava-louças", "lava louça", "dishwasher",
                "maquina de lavar louca", "máquina de lavar louça"
            },
            [EquipmentType.FloorDrain] = new[]
            {
                "ralo", "floor drain", "ralo seco", "ralo sifonado",
                "caixa sifonada", "drain", "sifao", "sifão"
            },
            [EquipmentType.Bidet] = new[]
            {
                "bide", "bidê", "bidet"
            },
            [EquipmentType.Urinal] = new[]
            {
                "mictorio", "mictório", "urinal"
            },
        };

        // ══════════════════════════════════════════════════════════
        //  VALIDAÇÃO PRINCIPAL
        // ══════════════════════════════════════════════════════════

        /// <summary>
        /// Valida se existe família e símbolo compatível no catálogo
        /// para o tipo de equipamento informado.
        /// </summary>
        public FamilyTypeValidationResult ValidarTipoDeFamilia(
            EquipamentoHidraulico equipamento,
            MepFamilyCatalog catalogo,
            ILogService log)
        {
            if (equipamento == null) throw new ArgumentNullException(nameof(equipamento));
            if (catalogo == null) throw new ArgumentNullException(nameof(catalogo));
            if (log == null) throw new ArgumentNullException(nameof(log));

            var resultado = new FamilyTypeValidationResult();

            try
            {
                // 1. Obter termos de busca para o tipo
                if (!MapaAlternativas.TryGetValue(equipamento.Tipo, out var termos))
                {
                    resultado.Valido = false;
                    resultado.MotivoFalha =
                        $"Tipo de equipamento '{equipamento.Tipo}' não possui mapa de famílias.";
                    log.Medio(ETAPA, COMPONENTE,
                        $"Família inexistente para equipamento {equipamento.Id}: " +
                        $"tipo '{equipamento.Tipo}' sem mapa.",
                        equipamento.RevitElementId);
                    return resultado;
                }

                // 2. Buscar família no catálogo
                var familiaEncontrada = BuscarFamilia(catalogo, termos, resultado);

                if (familiaEncontrada == null)
                {
                    resultado.Valido = false;
                    resultado.MotivoFalha =
                        $"Família não encontrada para o tipo de equipamento '{equipamento.Tipo}'. " +
                        $"Verificados {resultado.AlternativasVerificadas} alternativas: " +
                        $"[{string.Join(", ", termos)}]";
                    log.Medio(ETAPA, COMPONENTE,
                        $"Família inexistente para equipamento {equipamento.Id} " +
                        $"('{equipamento.FamilyName}', Tipo={equipamento.Tipo}). " +
                        $"Verificados: [{string.Join(", ", termos)}]",
                        equipamento.RevitElementId);
                    return resultado;
                }

                // 3. Verificar símbolos
                var simbolos = catalogo.ObterSimbolos(familiaEncontrada);

                if (simbolos.Count == 0)
                {
                    resultado.Valido = false;
                    resultado.FamiliaEscolhida = familiaEncontrada;
                    resultado.MotivoFalha =
                        $"Família encontrada '{familiaEncontrada}', mas sem símbolos carregados.";
                    log.Medio(ETAPA, COMPONENTE,
                        $"Família encontrada, mas sem símbolos: {familiaEncontrada} " +
                        $"(equipamento {equipamento.Id})",
                        equipamento.RevitElementId);
                    return resultado;
                }

                // 4. Selecionar primeiro símbolo
                resultado.Valido = true;
                resultado.FamiliaEscolhida = familiaEncontrada;
                resultado.SimboloEscolhido = simbolos[0];

                log.Info(ETAPA, COMPONENTE,
                    $"Família válida para equipamento {equipamento.Id}: " +
                    $"{familiaEncontrada} / {simbolos[0]} " +
                    $"(Tipo={equipamento.Tipo}, {simbolos.Count} símbolos disponíveis)",
                    equipamento.RevitElementId);

                return resultado;
            }
            catch (Exception ex)
            {
                resultado.Valido = false;
                resultado.MotivoFalha = $"Erro interno: {ex.Message}";
                log.Critico(ETAPA, COMPONENTE,
                    $"Erro ao validar tipo de família para equipamento {equipamento.Id}: " +
                    $"{ex.Message}",
                    equipamento.RevitElementId,
                    detalhes: ex.StackTrace);
                return resultado;
            }
        }

        // ══════════════════════════════════════════════════════════
        //  VALIDAÇÃO EM LOTE
        // ══════════════════════════════════════════════════════════

        /// <summary>
        /// Valida todos os equipamentos de uma lista contra o catálogo.
        /// Retorna dicionário: EquipamentoId → Resultado.
        /// </summary>
        public Dictionary<string, FamilyTypeValidationResult> ValidarLote(
            List<EquipamentoHidraulico> equipamentos,
            MepFamilyCatalog catalogo,
            ILogService log)
        {
            var resultados = new Dictionary<string, FamilyTypeValidationResult>();

            log.Info(ETAPA, COMPONENTE,
                $"Validando {equipamentos.Count} equipamentos contra catálogo " +
                $"({catalogo.TotalFamilias} famílias, {catalogo.TotalSimbolos} símbolos)...");

            int validos = 0, invalidos = 0;

            foreach (var equipamento in equipamentos)
            {
                var resultado = ValidarTipoDeFamilia(equipamento, catalogo, log);
                resultados[equipamento.Id] = resultado;

                if (resultado.Valido)
                    validos++;
                else
                    invalidos++;
            }

            log.Info(ETAPA, COMPONENTE,
                $"Validação em lote concluída: {validos} válidos, {invalidos} inválidos " +
                $"(de {equipamentos.Count} total).");

            return resultados;
        }

        // ══════════════════════════════════════════════════════════
        //  BUSCA DE FAMÍLIA
        // ══════════════════════════════════════════════════════════

        /// <summary>
        /// Busca uma família no catálogo usando os termos de busca.
        /// Busca normalizada: case-insensitive, sem acentos, contains parcial.
        /// Retorna o nome exato da família no catálogo, ou null.
        /// </summary>
        private static string? BuscarFamilia(
            MepFamilyCatalog catalogo,
            string[] termos,
            FamilyTypeValidationResult resultado)
        {
            var chaves = catalogo.FamiliaParaSimbolos.Keys.ToList();
            resultado.AlternativasVerificadas = termos.Length;

            foreach (var termo in termos)
            {
                var termoNorm = Normalizar(termo);

                foreach (var chave in chaves)
                {
                    var chaveNorm = Normalizar(chave);

                    if (chaveNorm.Contains(termoNorm) || termoNorm.Contains(chaveNorm))
                    {
                        resultado.PadraoBusca = termo;
                        return chave;
                    }
                }
            }

            return null;
        }

        // ══════════════════════════════════════════════════════════
        //  NORMALIZAÇÃO
        // ══════════════════════════════════════════════════════════

        /// <summary>
        /// Normaliza texto removendo acentos e convertendo para minúscula.
        /// </summary>
        private static string Normalizar(string texto)
        {
            if (string.IsNullOrWhiteSpace(texto))
                return string.Empty;

            var normalized = texto.Normalize(NormalizationForm.FormD);
            var sb = new StringBuilder(normalized.Length);

            foreach (var c in normalized)
            {
                var category = CharUnicodeInfo.GetUnicodeCategory(c);
                if (category != UnicodeCategory.NonSpacingMark)
                    sb.Append(c);
            }

            return sb.ToString()
                .Normalize(NormalizationForm.FormC)
                .ToLowerInvariant()
                .Trim();
        }
    }
}
