using PluginCore.Domain.Enums;

namespace PluginCore.Common
{
    /// <summary>
    /// Dicionário de padrões de nomenclatura por tipo de ambiente.
    /// Todas as strings já estão normalizadas (lowercase, sem acento).
    /// </summary>
    public static class AmbientePatterns
    {
        // ══════════════════════════════════════════════════════════
        //  DICIONÁRIO PRINCIPAL
        // ══════════════════════════════════════════════════════════

        private static readonly Dictionary<RoomType, List<string>> _patterns = new()
        {
            // ── BATHROOM ───────────────────────────────────────
            [RoomType.Bathroom] = new()
            {
                // Corretos
                "banheiro", "banheiro social", "banheiro servico",
                "banheiro empregada", "banheiro reversivel",
                // Abreviações
                "banh", "banh social", "ban social", "ban",
                // Sinônimos
                "wc", "w.c.", "bwc", "bwc social", "sanitario",
                "banho", "bathroom",
                // Erros de digitação
                "banhero", "banhero social", "bnaherio", "bnaheiro",
                "banhieiro", "baneiro",
                // Com número
                "banheiro 01", "banheiro 02", "banheiro 1", "banheiro 2",
                // Variações
                "banheiro de servico", "banheiro da empregada",
                "banheiro externo", "banheiro comum",
            },

            // ── MASTER BATHROOM ────────────────────────────────
            [RoomType.MasterBathroom] = new()
            {
                // Corretos
                "banheiro suite", "banheiro da suite", "banheiro suite master",
                // Variações
                "banheiro suite casal", "banheiro suite 01",
                "banheiro suite 02", "banheiro master",
                // Abreviações
                "banh suite", "ban suite", "banh ste",
                // Sinônimos
                "master bathroom", "master bath",
                "wc suite", "bwc suite",
                // Erros de digitação
                "banhero suite", "banheiro suit", "banheiro siute",
            },

            // ── LAVATORY ───────────────────────────────────────
            [RoomType.Lavatory] = new()
            {
                // Corretos
                "lavabo", "lavabo social", "lavabo intimo",
                "lavabo externo", "lavabo visitas",
                // Abreviações
                "lav", "lav social",
                // Sinônimos
                "toilette", "toalete", "toilet",
                // Erros de digitação
                "lavbo", "labavo", "labvo", "lavavo",
                // Com número
                "lavabo 01", "lavabo 1",
                // Variações
                "lavabo de visitas",
            },

            // ── KITCHEN ────────────────────────────────────────
            [RoomType.Kitchen] = new()
            {
                // Corretos
                "cozinha", "cozinha principal", "cozinha gourmet",
                "cozinha americana",
                // Abreviações
                "coz", "coz gourmet",
                // Sinônimos
                "copa", "copa cozinha", "copa e cozinha",
                "copa/cozinha", "kitchen",
                // Erros de digitação
                "cozihna", "cosinha", "cozina", "coziha", "cozinnha",
                // Com número
                "cozinha 01", "cozinha 1",
                // Variações
                "cozinha de apoio", "cozinha industrial",
                "cozinha/copa",
            },

            // ── LAUNDRY ────────────────────────────────────────
            [RoomType.Laundry] = new()
            {
                // Corretos
                "area de servico", "area servico", "area de serv",
                // Abreviações
                "a servico", "a serv", "as", "a.s.",
                "a serv",
                // Sinônimos
                "lavanderia", "servico", "laundry",
                // Erros de digitação
                "area servco", "area de servicco", "area de servso",
                "area servico", "aera de servico",
                // Com número
                "area de servico 01", "area de servico 1",
                // Variações
                "area de servico coberta",
                "area de servico externa",
            },

            // ── BALCONY ────────────────────────────────────────
            [RoomType.Balcony] = new()
            {
                // Corretos
                "varanda", "varanda gourmet", "varanda social",
                "varanda intima",
                // Sinônimos
                "sacada", "sacada suite", "terraco", "terraco descoberto",
                "balcao", "balcony",
                // Abreviações
                "var", "var gourmet",
                // Erros de digitação
                "varanda gourme", "varanada", "vranda",
                // Com número
                "varanda 01", "varanda 1",
                // Variações
                "varanda coberta", "varanda descoberta",
            },

            // ── GARAGE ─────────────────────────────────────────
            [RoomType.Garage] = new()
            {
                // Corretos
                "garagem", "garagem coberta", "garagem descoberta",
                // Abreviações
                "gar",
                // Sinônimos
                "vaga", "estacionamento", "garage", "parking",
                // Erros de digitação
                "garagm", "gragem", "gargen",
                // Com número
                "garagem 01", "garagem 02", "garagem 1", "garagem 2",
                "vaga 01", "vaga 1",
                // Variações
                "garagem dupla", "garagem simples",
            },

            // ── SERVICE AREA ───────────────────────────────────
            [RoomType.ServiceArea] = new()
            {
                // Corretos
                "area externa", "piscina", "churrasqueira",
                // Variações
                "area de lazer", "espaco gourmet",
                "area gourmet", "quintal",
                "jardim", "area descoberta",
                // Erros de digitação
                "area esterna", "pisicina",
                // Com número
                "area externa 01",
            },

            // ── DRY AREA ───────────────────────────────────────
            [RoomType.DryArea] = new()
            {
                // Sala
                "sala", "sala de estar", "sala estar", "sala de jantar",
                "sala jantar", "sala de tv", "sala tv", "sala intima",
                "sala estar jantar", "living", "living room", "estar",
                "estar jantar",
                // Quarto
                "quarto", "quarto casal", "quarto solteiro",
                "quarto hospedes", "quarto de hospedes",
                "quarto empregada", "quarto reversivel",
                "dormitorio", "dormitorio casal",
                "suite", "suite master", "suite casal",
                "bedroom",
                // Abreviações quarto
                "qto", "qto casal", "dorm", "dorm casal",
                // Erros de digitação
                "quarot", "qaurto", "quaro",
                // Escritório
                "escritorio", "home office", "home-office", "estudio",
                "biblioteca", "atelie",
                // Closet
                "closet", "rouparia",
                // Circulação
                "corredor", "corredor intimo", "circulacao", "circ",
                "hall", "hall de entrada", "hall social", "hall intimo",
                "escada", "escadaria", "vestibulo",
                // Depósito
                "deposito", "despensa", "desp", "adega",
            },
        };

        // ══════════════════════════════════════════════════════════
        //  NOMES GENÉRICOS (→ Unknown)
        // ══════════════════════════════════════════════════════════

        private static readonly HashSet<string> _genericNames = new()
        {
            "ambiente", "ambiente 01", "espaco", "espaco 01",
            "room", "room 1", "area", "undefined",
            "novo ambiente", "a definir", "???", "teste",
        };

        // ══════════════════════════════════════════════════════════
        //  MÉTODOS PÚBLICOS
        // ══════════════════════════════════════════════════════════

        /// <summary>Retorna o dicionário completo de padrões.</summary>
        public static IReadOnlyDictionary<RoomType, List<string>> Patterns => _patterns;

        /// <summary>Retorna todos os padrões (flatten).</summary>
        public static IEnumerable<string> GetAllPatterns()
        {
            return _patterns.Values.SelectMany(v => v);
        }

        /// <summary>Retorna os padrões de um tipo específico.</summary>
        public static IReadOnlyList<string> GetPatterns(RoomType type)
        {
            return _patterns.TryGetValue(type, out var patterns)
                ? patterns.AsReadOnly()
                : new List<string>().AsReadOnly();
        }

        /// <summary>Verifica se um nome é genérico (→ Unknown).</summary>
        public static bool IsGenericName(string input)
        {
            var normalized = TextNormalizer.NormalizeForClassification(input);
            return _genericNames.Contains(normalized);
        }

        /// <summary>
        /// Busca o RoomType correspondente ao input.
        /// Retorna null se não encontrar match.
        /// </summary>
        public static RoomType? Match(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return null;

            // 1. Normalizar
            var normalized = TextNormalizer.NormalizeForClassification(input);

            if (string.IsNullOrEmpty(normalized))
                return null;

            // 2. Verificar nome genérico
            if (_genericNames.Contains(normalized))
                return RoomType.Unknown;

            // 3. Match exato
            foreach (var (type, patterns) in _patterns)
            {
                if (patterns.Contains(normalized))
                    return type;
            }

            // 4. Match parcial (contém)
            foreach (var (type, patterns) in _patterns)
            {
                foreach (var pattern in patterns)
                {
                    if (normalized.Contains(pattern) || pattern.Contains(normalized))
                        return type;
                }
            }

            // 5. Match por similaridade (erros de digitação)
            var bestMatch = RoomType.Unknown;
            var bestScore = 0.0;
            const double threshold = 0.75;

            foreach (var (type, patterns) in _patterns)
            {
                foreach (var pattern in patterns)
                {
                    var score = TextNormalizer.Similarity(normalized, pattern);
                    if (score > bestScore && score >= threshold)
                    {
                        bestScore = score;
                        bestMatch = type;
                    }
                }
            }

            return bestScore >= threshold ? bestMatch : null;
        }

        /// <summary>
        /// Busca com resultado detalhado (tipo + confiança + padrão matched).
        /// </summary>
        public static MatchResult MatchDetailed(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return new MatchResult(RoomType.Unknown, 0.0, "", "Input vazio");

            var normalized = TextNormalizer.NormalizeForClassification(input);

            if (string.IsNullOrEmpty(normalized))
                return new MatchResult(RoomType.Unknown, 0.0, "", "Input vazio após normalização");

            // Genérico
            if (_genericNames.Contains(normalized))
                return new MatchResult(RoomType.Unknown, 1.0, normalized, "Nome genérico");

            // Match exato
            foreach (var (type, patterns) in _patterns)
            {
                if (patterns.Contains(normalized))
                    return new MatchResult(type, 0.95, normalized, "Match exato");
            }

            // Match parcial — input contém padrão
            foreach (var (type, patterns) in _patterns)
            {
                foreach (var pattern in patterns)
                {
                    if (normalized.StartsWith(pattern))
                        return new MatchResult(type, 0.85, pattern, "StartsWith");
                }
            }

            // Match parcial — padrão contém input
            foreach (var (type, patterns) in _patterns)
            {
                foreach (var pattern in patterns)
                {
                    if (normalized.Contains(pattern))
                        return new MatchResult(type, 0.75, pattern, "Contains");
                }
            }

            // Similaridade
            var bestType = RoomType.Unknown;
            var bestScore = 0.0;
            var bestPattern = "";

            foreach (var (type, patterns) in _patterns)
            {
                foreach (var pattern in patterns)
                {
                    var score = TextNormalizer.Similarity(normalized, pattern);
                    if (score > bestScore)
                    {
                        bestScore = score;
                        bestType = type;
                        bestPattern = pattern;
                    }
                }
            }

            if (bestScore >= 0.75)
                return new MatchResult(bestType, bestScore, bestPattern, "Similaridade");

            return new MatchResult(RoomType.Unknown, bestScore, bestPattern, "Sem match");
        }

        // ══════════════════════════════════════════════════════════
        //  RESULTADO DETALHADO
        // ══════════════════════════════════════════════════════════

        /// <summary>Resultado de um match de classificação.</summary>
        public record MatchResult(
            RoomType Type,
            double Confidence,
            string MatchedPattern,
            string Method
        );

        // Exemplos:
        // AmbientePatterns.Match("Banheiro 02")         → RoomType.Bathroom
        // AmbientePatterns.Match("Cozinha Gourmet")     → RoomType.Kitchen
        // AmbientePatterns.Match("Quarto - Casal")      → RoomType.DryArea
        // AmbientePatterns.Match("Room 1")              → RoomType.Unknown
        // AmbientePatterns.Match("Banhero")             → RoomType.Bathroom (similaridade)
        //
        // var result = AmbientePatterns.MatchDetailed("Cozinha");
        // result.Type       → RoomType.Kitchen
        // result.Confidence → 0.95
        // result.Method     → "Match exato"
    }
}
