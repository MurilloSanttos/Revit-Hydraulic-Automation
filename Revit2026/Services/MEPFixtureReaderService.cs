using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using Autodesk.Revit.DB.Plumbing;
using PluginCore.Domain.Enums;
using PluginCore.Interfaces;
using PluginCore.Models;

namespace Revit2026.Services
{
    /// <summary>
    /// Serviço responsável pela leitura de MEP Fixtures (equipamentos hidráulicos)
    /// existentes no modelo Revit, associando-os aos ambientes detectados.
    /// Coleta dados de famílias, posição, parâmetros e realiza matching com Rooms.
    /// </summary>
    public class MEPFixtureReaderService
    {
        private readonly ILogService _log;

        private const string ETAPA = "03_Equipamentos";
        private const string COMPONENTE = "MEPFixtureReader";
        private const string MATCHING = "Matching";

        // Mapeamento de keywords em FamilyName → EquipmentType
        private static readonly List<(string Keyword, EquipmentType Tipo)> _mapeamento = new()
        {
            // Vaso sanitário
            ("toilet", EquipmentType.Toilet),
            ("vaso", EquipmentType.Toilet),
            ("bacia", EquipmentType.Toilet),
            ("closet", EquipmentType.Toilet),
            ("w.c.", EquipmentType.Toilet),

            // Lavatório / Pia de banheiro
            ("lavatorio", EquipmentType.Sink),
            ("sink", EquipmentType.Sink),
            ("basin", EquipmentType.Sink),
            ("cuba banheiro", EquipmentType.Sink),

            // Chuveiro
            ("shower", EquipmentType.Shower),
            ("chuveiro", EquipmentType.Shower),
            ("ducha", EquipmentType.Shower),

            // Banheira
            ("bathtub", EquipmentType.Bathtub),
            ("banheira", EquipmentType.Bathtub),

            // Pia de cozinha
            ("kitchen sink", EquipmentType.KitchenSink),
            ("pia cozinha", EquipmentType.KitchenSink),
            ("pia", EquipmentType.KitchenSink),
            ("cuba cozinha", EquipmentType.KitchenSink),

            // Tanque
            ("laundry", EquipmentType.LaundryTub),
            ("tanque", EquipmentType.LaundryTub),

            // Máquina de lavar
            ("washing", EquipmentType.WashingMachine),
            ("maquina lavar", EquipmentType.WashingMachine),
            ("lava roupa", EquipmentType.WashingMachine),

            // Lava-louças
            ("dishwasher", EquipmentType.Dishwasher),
            ("lava louca", EquipmentType.Dishwasher),

            // Ralo
            ("floor drain", EquipmentType.FloorDrain),
            ("ralo", EquipmentType.FloorDrain),
            ("drain", EquipmentType.FloorDrain),
            ("sifonada", EquipmentType.FloorDrain),

            // Bidê
            ("bidet", EquipmentType.Bidet),
            ("bide", EquipmentType.Bidet),

            // Mictório
            ("urinal", EquipmentType.Urinal),
            ("mictorio", EquipmentType.Urinal),
        };

        public MEPFixtureReaderService(ILogService log)
        {
            _log = log ?? throw new ArgumentNullException(nameof(log));
        }

        // ══════════════════════════════════════════════════════════
        //  LEITURA PRINCIPAL
        // ══════════════════════════════════════════════════════════

        /// <summary>
        /// Lê todos os equipamentos hidráulicos do modelo e associa-os
        /// aos ambientes detectados. Retorna lista completa de equipamentos
        /// e preenche AmbienteInfo.EquipamentosExistentes.
        /// </summary>
        public List<EquipamentoHidraulico> LerEquipamentos(
            Document doc, List<AmbienteInfo> ambientes)
        {
            if (doc == null) throw new ArgumentNullException(nameof(doc));
            if (ambientes == null) throw new ArgumentNullException(nameof(ambientes));

            _log.Info(ETAPA, COMPONENTE,
                "Iniciando leitura de MEP Fixtures do modelo...");

            var equipamentos = new List<EquipamentoHidraulico>();

            // ── 1. Índice de ambientes por ElementId ──────────
            var ambientePorRoomId = new Dictionary<long, AmbienteInfo>();
            foreach (var amb in ambientes)
            {
                ambientePorRoomId[amb.ElementId] = amb;
            }

            // ── 2. Coletar Plumbing Fixtures ──────────────────
            var plumbingFixtures = ColetarInstancias(doc, BuiltInCategory.OST_PlumbingFixtures);
            _log.Info(ETAPA, COMPONENTE,
                $"Coletados {plumbingFixtures.Count} PlumbingFixtures.");

            // ── 3. Coletar Speciality Equipment ───────────────
            var specialEquipment = ColetarInstancias(doc, BuiltInCategory.OST_SpecialityEquipment);
            _log.Info(ETAPA, COMPONENTE,
                $"Coletados {specialEquipment.Count} SpecialityEquipment.");

            // ── 4. Processar todos ────────────────────────────
            int semRoom = 0;
            int semLocation = 0;
            int associados = 0;

            var todasInstancias = new List<(FamilyInstance Instance, string Categoria)>();
            foreach (var fi in plumbingFixtures)
                todasInstancias.Add((fi, "PlumbingFixture"));
            foreach (var fi in specialEquipment)
                todasInstancias.Add((fi, "SpecialityEquipment"));

            foreach (var (instance, categoria) in todasInstancias)
            {
                // 4a. Validar localização
                if (instance.Location is not LocationPoint locPoint)
                {
                    semLocation++;
                    _log.Critico(ETAPA, COMPONENTE,
                        $"Equipamento sem localização: '{instance.Symbol.FamilyName}' " +
                        $"(Id={instance.Id.Value}). Ignorado.",
                        instance.Id.Value);
                    continue;
                }

                var posicao = locPoint.Point;

                // 4b. Encontrar Room
                Room? room = null;
                try
                {
                    room = doc.GetRoomAtPoint(posicao);
                }
                catch { /* GetRoomAtPoint pode falhar em pontos fora do modelo */ }

                if (room == null)
                {
                    semRoom++;
                    _log.Medio(ETAPA, MATCHING,
                        $"Equipamento fora de qualquer Room: " +
                        $"'{instance.Symbol.FamilyName}' ({categoria}) " +
                        $"Id={instance.Id.Value}, " +
                        $"Pos=({ConverterComprimento(posicao.X):F3}, " +
                        $"{ConverterComprimento(posicao.Y):F3}, " +
                        $"{ConverterComprimento(posicao.Z):F3}) m",
                        instance.Id.Value);
                    continue;
                }

                // 4c. Construir EquipamentoHidraulico
                var equipamento = CriarEquipamento(instance, room, doc, categoria);
                equipamentos.Add(equipamento);

                // 4d. Associar ao AmbienteInfo
                if (ambientePorRoomId.TryGetValue(room.Id.Value, out var ambiente))
                {
                    ambiente.EquipamentosExistentes.Add(equipamento);
                    associados++;

                    _log.Info(ETAPA, MATCHING,
                        $"Equipamento associado: '{equipamento.FamilyName}' " +
                        $"({equipamento.Tipo}) → Room '{room.Name}' " +
                        $"(Id={room.Id.Value})",
                        instance.Id.Value);
                }
                else
                {
                    _log.Leve(ETAPA, MATCHING,
                        $"Equipamento '{equipamento.FamilyName}' está no Room " +
                        $"'{room.Name}' (Id={room.Id.Value}) que não foi detectado " +
                        $"pelo RoomReaderService.",
                        instance.Id.Value);
                }
            }

            // ── 5. Resumo ────────────────────────────────────
            _log.Info(ETAPA, COMPONENTE,
                $"Leitura concluída: {equipamentos.Count} equipamentos lidos, " +
                $"{associados} associados a ambientes, " +
                $"{semRoom} fora de Rooms, " +
                $"{semLocation} sem localização.");

            // Log por ambiente
            foreach (var amb in ambientes)
            {
                if (amb.EquipamentosExistentes.Count > 0)
                {
                    var nomes = string.Join(", ",
                        amb.EquipamentosExistentes.Select(e => $"{e.Tipo}"));
                    _log.Info(ETAPA, COMPONENTE,
                        $"'{amb.NomeOriginal}': {amb.EquipamentosExistentes.Count} equip. → [{nomes}]");
                }
            }

            return equipamentos;
        }

        // ══════════════════════════════════════════════════════════
        //  COLETA DE INSTÂNCIAS
        // ══════════════════════════════════════════════════════════

        /// <summary>
        /// Coleta todas as FamilyInstance de uma categoria.
        /// </summary>
        private static List<FamilyInstance> ColetarInstancias(
            Document doc, BuiltInCategory categoria)
        {
            return new FilteredElementCollector(doc)
                .OfCategory(categoria)
                .OfClass(typeof(FamilyInstance))
                .WhereElementIsNotElementType()
                .Cast<FamilyInstance>()
                .Where(fi => fi.Location is LocationPoint)
                .ToList();
        }

        // ══════════════════════════════════════════════════════════
        //  CRIAÇÃO DO MODELO
        // ══════════════════════════════════════════════════════════

        /// <summary>
        /// Constrói EquipamentoHidraulico a partir de uma FamilyInstance.
        /// Extrai família, tipo, posição, parâmetros e classifica automaticamente.
        /// </summary>
        private EquipamentoHidraulico CriarEquipamento(
            FamilyInstance instance, Room room, Document doc, string categoria)
        {
            var familyName = instance.Symbol?.FamilyName ?? "Desconhecido";
            var typeName = instance.Symbol?.Name ?? "Desconhecido";
            var posicao = (instance.Location as LocationPoint)!.Point;
            var nivel = doc.GetElement(instance.LevelId) as Level;

            // Classificar tipo
            var tipo = ClassificarEquipamento(familyName, typeName);

            // Obter parâmetros MEP (se disponíveis)
            var diametroAF = LerParametroDouble(instance, "Diameter") ??
                             LerParametroDouble(instance, "Nominal Diameter") ??
                             LerParametroDouble(instance, "Size") ?? 0;
            var diametroES = LerParametroDouble(instance, "Drain Size") ??
                             LerParametroDouble(instance, "Waste Size") ?? 0;

            // Mark
            var mark = instance.get_Parameter(BuiltInParameter.ALL_MODEL_MARK)
                ?.AsString() ?? string.Empty;

            return new EquipamentoHidraulico
            {
                Id = $"EQ-{instance.Id.Value}",
                RevitElementId = instance.Id.Value,
                Tipo = tipo,
                FamilyName = familyName,
                TypeName = typeName,
                AmbienteId = room.Id.Value.ToString(),
                AmbienteNome = room.Name ?? string.Empty,
                Nivel = nivel?.Name ?? "Sem Nível",
                Mark = mark,
                PosX = ConverterComprimento(posicao.X),
                PosY = ConverterComprimento(posicao.Y),
                PosZ = ConverterComprimento(posicao.Z),
                DiametroAF = (int)Math.Round(diametroAF),
                DiametroES = (int)Math.Round(diametroES),
                Processado = false
            };
        }

        // ══════════════════════════════════════════════════════════
        //  CLASSIFICAÇÃO AUTOMÁTICA
        // ══════════════════════════════════════════════════════════

        /// <summary>
        /// Classifica o tipo de equipamento analisando FamilyName e TypeName.
        /// Usa matching por keywords normalizadas.
        /// </summary>
        private static EquipmentType ClassificarEquipamento(
            string familyName, string typeName)
        {
            var textoCombinadoNorm = Normalizar($"{familyName} {typeName}");

            foreach (var (keyword, tipo) in _mapeamento)
            {
                if (textoCombinadoNorm.Contains(keyword))
                    return tipo;
            }

            return EquipmentType.Sink; // Fallback genérico
        }

        /// <summary>
        /// Normaliza texto para matching: lowercase, sem acentos.
        /// </summary>
        private static string Normalizar(string texto)
        {
            if (string.IsNullOrWhiteSpace(texto))
                return string.Empty;

            texto = texto.ToLowerInvariant();

            // Remoção simplificada de acentos comuns
            texto = texto
                .Replace("á", "a").Replace("à", "a").Replace("ã", "a").Replace("â", "a")
                .Replace("é", "e").Replace("ê", "e")
                .Replace("í", "i")
                .Replace("ó", "o").Replace("ô", "o").Replace("õ", "o")
                .Replace("ú", "u").Replace("ü", "u")
                .Replace("ç", "c");

            return texto;
        }

        // ══════════════════════════════════════════════════════════
        //  LEITURA DE PARÂMETROS
        // ══════════════════════════════════════════════════════════

        /// <summary>
        /// Tenta ler um parâmetro double de uma instância pelo nome.
        /// Converte de unidades internas para milímetros.
        /// Retorna null se não encontrado.
        /// </summary>
        private static double? LerParametroDouble(FamilyInstance instance, string nomeParam)
        {
            try
            {
                // Buscar por nome
                var param = instance.LookupParameter(nomeParam);
                if (param != null && param.HasValue &&
                    param.StorageType == StorageType.Double)
                {
                    var valorInterno = param.AsDouble();
                    // Converter pés → milímetros
                    var valorMm = UnitUtils.ConvertFromInternalUnits(
                        valorInterno, UnitTypeId.Millimeters);
                    return valorMm;
                }
            }
            catch { /* parâmetro não disponível */ }

            return null;
        }

        // ══════════════════════════════════════════════════════════
        //  CONVERSÃO DE UNIDADES
        // ══════════════════════════════════════════════════════════

        private static double ConverterComprimento(double valorInterno)
        {
            return UnitUtils.ConvertFromInternalUnits(valorInterno, UnitTypeId.Meters);
        }
    }
}
