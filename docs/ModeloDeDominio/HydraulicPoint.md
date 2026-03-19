    # Modelo de Domínio — HydraulicPoint (PontoHidráulico)

    > Especificação completa do modelo que representa a conexão entre ambiente e equipamento hidráulico, servindo como base para geração de redes.

    ---

    ## 1. Definição do Modelo

    ### 1.1 O que é HydraulicPoint

    `HydraulicPoint` é a representação de um **ponto físico de conexão hidráulica** dentro de um ambiente. É o local exato onde uma tubulação precisa chegar (água fria) ou de onde uma tubulação precisa partir (esgoto/ventilação).

    ### 1.2 Papel no sistema

    ```
    RoomInfo (ambiente)
        │
        ├── HydraulicPoint (ponto de conexão)    ← ESTE MODELO
        │       │
        │       └── EquipmentInfo (aparelho)
        │
        └── HydraulicPoint (outro ponto)
                │
                └── (independente — ralo, por exemplo)
    ```

    | Módulo | Uso do HydraulicPoint |
    |--------|----------------------|
    | E03 — Identificação | Gerado: lista de pontos necessários por ambiente |
    | E04 — Inserção | Referência de posição para inserir equipamentos |
    | E05 — Validação | Status de cada ponto (existe? correto?) |
    | E07 — Rede AF | Endpoint de destino dos sub-ramais de água fria |
    | E08 — Rede ES | Endpoint de origem dos ramais de descarga |
    | E09 — Inclinações | Cota Z dos pontos define a declividade necessária |
    | E11 — Dimensionamento | Fonte de ΣP e ΣUHC para cálculo |

    ### 1.3 Por que é separado de EquipmentInfo

    ```
    EquipmentInfo = O APARELHO (vaso, lavatório, chuveiro)
    - Possui múltiplas conexões (AF entrada + ES saída)
    - Tem família Revit, parâmetros de inserção
    - Pode existir ou ser planejado

    HydraulicPoint = O PONTO DE CONEXÃO COM A REDE
    - UM sistema específico (AF ou ES ou VE)
    - UMA direção específica (In ou Out)
    - UMA posição exata de onde a tubulação sai/chega
    - Pode existir sem equipamento (ralo independente, CX sifonada)

    POR QUE SEPARAR:
    1. Um equipamento gera MÚLTIPLOS pontos (vaso → ponto AF + ponto ES)
    2. Alguns pontos NÃO têm equipamento (ralo, CX sifonada)
    3. Geração de rede trabalha com PONTOS, não com equipamentos
    4. Simplifica o grafo da rede: ponto → trecho → ponto → trecho
    ```

    ---

    ## 2. Estrutura de Dados — Código C# Completo

    ```csharp
    namespace HidraulicoPlugin.Core.Models
    {
        /// <summary>
        /// Representa um ponto de conexão hidráulica em um ambiente.
        /// É o endpoint de onde uma tubulação chega (AF) ou parte (ES/VE).
        /// Base para geração de redes.
        /// </summary>
        public class HydraulicPoint
        {
            // ── Identidade ──────────────────────────────────────────────

            /// <summary>
            /// ID único do ponto hidráulico.
            /// Formato: "hp_{sistema}_{seq}" (ex: "hp_af_001", "hp_es_003").
            /// </summary>
            public string Id { get; set; }

            /// <summary>
            /// Tipo do ponto (alimentação, descarga, ventilação, dreno, acessório).
            /// </summary>
            public PointType Type { get; set; }

            /// <summary>
            /// Sistema hidráulico ao qual este ponto pertence.
            /// </summary>
            public HydraulicSystem System { get; set; }

            /// <summary>
            /// Direção do fluxo neste ponto.
            /// In = água entrando no aparelho (AF).
            /// Out = esgoto saindo do aparelho (ES).
            /// </summary>
            public FlowDirection Direction { get; set; }

            // ── Relações ────────────────────────────────────────────────

            /// <summary>
            /// ID do ambiente (RoomInfo.Id) ao qual este ponto pertence.
            /// SEMPRE preenchido — todo ponto está em um ambiente.
            /// </summary>
            public string RoomId { get; set; }

            /// <summary>
            /// ID do equipamento (EquipmentInfo.Id) associado.
            /// NULL para pontos independentes (ralo avulso, CX sifonada, CX gordura).
            /// </summary>
            public string EquipmentId { get; set; }

            /// <summary>
            /// Tipo do equipamento associado (para consulta rápida sem buscar EquipmentInfo).
            /// Unknown se ponto é independente.
            /// </summary>
            public EquipmentType EquipmentType { get; set; } = EquipmentType.Unknown;

            // ── Localização ─────────────────────────────────────────────

            /// <summary>
            /// Posição do ponto de conexão em metros (coordenada absoluta).
            /// Para AF: ponto onde o sub-ramal chega ao aparelho.
            /// Para ES: ponto onde o ramal de descarga sai do aparelho.
            /// </summary>
            public Point3D Position { get; set; }

            /// <summary>
            /// Nome do Level.
            /// </summary>
            public string LevelName { get; set; }

            /// <summary>
            /// Elevação do ponto de conexão em metros (Z relativo ao piso).
            /// Útil para calcular declividade e desnível.
            /// AF lavatório: ~0.60m. ES vaso: ~0.00m. AF chuveiro: ~2.00m.
            /// </summary>
            public double ElevationFromFloorM { get; set; }

            // ── Parâmetros Hidráulicos ──────────────────────────────────

            /// <summary>
            /// Peso relativo para dimensionamento de AF.
            /// Copiado do EquipmentInfo para acesso direto.
            /// 0 para pontos que não consomem água fria.
            /// </summary>
            public double WeightColdWater { get; set; }

            /// <summary>
            /// Unidades de Hunter de Contribuição para ES.
            /// Copiado do EquipmentInfo para acesso direto.
            /// 0 para pontos que não geram esgoto.
            /// </summary>
            public int ContributionUnitsES { get; set; }

            /// <summary>
            /// Vazão de projeto no ponto em L/s.
            /// </summary>
            public double DesignFlowRateLs { get; set; }

            /// <summary>
            /// Pressão dinâmica mínima requerida neste ponto em mca.
            /// Relevante apenas para pontos AF.
            /// </summary>
            public double MinDynamicPressureMca { get; set; }

            /// <summary>
            /// DN mínimo da tubulação que deve chegar/sair deste ponto em mm.
            /// AF: DN do sub-ramal. ES: DN do ramal de descarga.
            /// </summary>
            public int MinDiameterMm { get; set; }

            // ── Comportamento de Rede ───────────────────────────────────

            /// <summary>
            /// Se o ramal deste ponto deve ser independente (não passar pela CX sifonada).
            /// True para: vaso sanitário.
            /// </summary>
            public bool RequiresIndependentBranch { get; set; }

            /// <summary>
            /// Se este ponto deve conectar via caixa sifonada.
            /// True para: lavatório, chuveiro, bidê, ralo (DN ≤ 50).
            /// </summary>
            public bool ConnectsViaSiphonBox { get; set; }

            /// <summary>
            /// Se este ponto requer ventilação individual.
            /// True para: vaso sanitário.
            /// </summary>
            public bool RequiresVentilation { get; set; }

            /// <summary>
            /// ID do ponto de acessório ao qual este ponto conecta.
            /// Ex: ponto ES do lavatório conecta ao ponto da CX sifonada.
            /// NULL se conecta diretamente ao subcoletor/prumada.
            /// </summary>
            public string ConnectsToPointId { get; set; }

            /// <summary>
            /// ID do trecho de rede que conecta este ponto à rede.
            /// Preenchido em E07/E08 após geração de rede.
            /// </summary>
            public string ConnectedSegmentId { get; set; }

            // ── Status ──────────────────────────────────────────────────

            /// <summary>
            /// Se o ponto é obrigatório para o ambiente.
            /// Obrigatório = sem ele, o projeto está incompleto.
            /// </summary>
            public bool IsRequired { get; set; }

            /// <summary>
            /// Status de validação do ponto.
            /// </summary>
            public PointStatus Status { get; set; } = PointStatus.Planned;

            /// <summary>
            /// Se o ponto está conectado à rede (preenchido pós E07/E08).
            /// </summary>
            public bool IsConnectedToNetwork { get; set; }
        }
    }
    ```

    ---

    ## 3. Enums

    ### 3.1 PointType

    ```csharp
    namespace HidraulicoPlugin.Core.Models.Enums
    {
        /// <summary>
        /// Tipo funcional do ponto hidráulico.
        /// </summary>
        public enum PointType
        {
            /// <summary>
            /// Ponto de alimentação — água fria chegando no aparelho.
            /// Direção: In. Sistema: ColdWater.
            /// Exemplos: sub-ramal do lavatório, sub-ramal do chuveiro.
            /// </summary>
            WaterSupply = 1,

            /// <summary>
            /// Ponto de descarga — esgoto saindo do aparelho.
            /// Direção: Out. Sistema: Sewer.
            /// Exemplos: ramal do vaso, ramal do lavatório.
            /// </summary>
            SewerDischarge = 2,

            /// <summary>
            /// Ponto de ventilação — conexão com coluna de ventilação.
            /// Direção: Out. Sistema: Ventilation.
            /// Exemplos: ventilação do ramal do vaso.
            /// </summary>
            Ventilation = 3,

            /// <summary>
            /// Ponto de dreno — coleta de piso sem equipamento associado.
            /// Direção: Out. Sistema: Sewer.
            /// Exemplos: ralo sifonado, ralo seco.
            /// </summary>
            FloorDrain = 4,

            /// <summary>
            /// Ponto de acessório — CX sifonada, CX gordura.
            /// Funciona como concentrador de ramais.
            /// Tem entrada (de vários aparelhos) e saída (para subcoletor).
            /// </summary>
            Accessory = 5,

            /// <summary>
            /// Ponto de prumada — conexão com tubo de queda ou coluna AF.
            /// Destino final dos ramais do pavimento.
            /// </summary>
            Riser = 6
        }
    }
    ```

    ### 3.2 PointStatus

    ```csharp
    namespace HidraulicoPlugin.Core.Models.Enums
    {
        /// <summary>
        /// Status do ponto hidráulico no ciclo de vida do pipeline.
        /// </summary>
        public enum PointStatus
        {
            /// <summary>Ponto planejado, ainda não inserido no modelo.</summary>
            Planned = 0,

            /// <summary>Equipamento do ponto inserido no modelo (pós E04).</summary>
            Inserted = 1,

            /// <summary>Ponto validado com sucesso (pós E05).</summary>
            Validated = 2,

            /// <summary>Ponto conectado à rede (pós E07/E08).</summary>
            Connected = 3,

            /// <summary>Ponto com problema identificado.</summary>
            Error = 4,

            /// <summary>Ponto ignorado pelo usuário.</summary>
            Skipped = 5
        }
    }
    ```

    ---

    ## 4. Relação com Equipamento

    ### 4.1 Cardinalidade

    ```
    1 EquipmentInfo → N HydraulicPoints

    Vaso c/ caixa acoplada:
        → hp_af_001 (WaterSupply, ColdWater, In, DN 20)
        → hp_es_001 (SewerDischarge, Sewer, Out, DN 100)

    Lavatório:
        → hp_af_002 (WaterSupply, ColdWater, In, DN 20)
        → hp_es_002 (SewerDischarge, Sewer, Out, DN 40)

    Chuveiro:
        → hp_af_003 (WaterSupply, ColdWater, In, DN 20)
        → hp_es_003 (SewerDischarge, Sewer, Out, DN 40)

    Ralo sifonado (SEM equipamento AF):
        → hp_es_004 (FloorDrain, Sewer, Out, DN 40)

    CX sifonada (acessório, SEM equipamento de consumo):
        → hp_acc_001 (Accessory, Sewer, Out, DN 75)
    ```

    ### 4.2 Pontos com equipamento vs. independentes

    | Situação | EquipmentId | Exemplo |
    |----------|------------|---------|
    | **Com equipamento** | Preenchido | Ponto AF do lavatório (lavatório é o equipment) |
    | **Independente (dreno)** | NULL | Ralo sifonado no piso (ponto existe sozinho) |
    | **Independente (acessório)** | NULL | CX sifonada (concentrador de ramais) |
    | **Prumada** | NULL | Ponto de conexão com tubo de queda (gerado em E06) |

    ### 4.3 Geração de pontos a partir de equipamento

    ```csharp
    /// <summary>
    /// Gera HydraulicPoints a partir de um EquipmentInfo.
    /// Chamado na etapa E03.
    /// </summary>
    public static class HydraulicPointGenerator
    {
        private static int _seqAf = 0;
        private static int _seqEs = 0;
        private static int _seqVe = 0;

        public static List<HydraulicPoint> GenerateFromEquipment(
            EquipmentInfo equipment, RoomInfo room)
        {
            var points = new List<HydraulicPoint>();

            // Ponto de água fria (se aplicável)
            if (equipment.RequiresColdWater())
            {
                var afConn = equipment.GetColdWaterConnection();
                points.Add(new HydraulicPoint
                {
                    Id = $"hp_af_{++_seqAf:D3}",
                    Type = PointType.WaterSupply,
                    System = HydraulicSystem.ColdWater,
                    Direction = FlowDirection.In,
                    RoomId = room.Id,
                    EquipmentId = equipment.Id,
                    EquipmentType = equipment.Type,
                    Position = afConn?.Position ?? equipment.Position,
                    LevelName = room.LevelName,
                    ElevationFromFloorM = GetDefaultElevation(equipment.Type, HydraulicSystem.ColdWater),
                    WeightColdWater = equipment.WeightColdWater,
                    DesignFlowRateLs = equipment.DesignFlowRateLs,
                    MinDynamicPressureMca = equipment.MinDynamicPressureMca,
                    MinDiameterMm = equipment.MinSubBranchDiameterAfMm,
                    IsRequired = true,
                    RequiresIndependentBranch = false,
                    ConnectsViaSiphonBox = false,
                    RequiresVentilation = false
                });
            }

            // Ponto de esgoto (se aplicável)
            if (equipment.RequiresSewer())
            {
                var esConn = equipment.GetSewerConnection();
                bool isToilet = equipment.HasIndependentDischargeBranch();

                points.Add(new HydraulicPoint
                {
                    Id = $"hp_es_{++_seqEs:D3}",
                    Type = PointType.SewerDischarge,
                    System = HydraulicSystem.Sewer,
                    Direction = FlowDirection.Out,
                    RoomId = room.Id,
                    EquipmentId = equipment.Id,
                    EquipmentType = equipment.Type,
                    Position = esConn?.Position ?? equipment.Position,
                    LevelName = room.LevelName,
                    ElevationFromFloorM = GetDefaultElevation(equipment.Type, HydraulicSystem.Sewer),
                    ContributionUnitsES = equipment.ContributionUnitsES,
                    MinDiameterMm = equipment.MinDischargeBranchDiameterEsMm,
                    IsRequired = true,
                    RequiresIndependentBranch = isToilet,
                    ConnectsViaSiphonBox = equipment.CanConnectViaSiphonBox(),
                    RequiresVentilation = equipment.RequiresVentilation()
                });
            }

            // Ponto de ventilação (se vaso sanitário)
            if (equipment.RequiresVentilation())
            {
                points.Add(new HydraulicPoint
                {
                    Id = $"hp_ve_{++_seqVe:D3}",
                    Type = PointType.Ventilation,
                    System = HydraulicSystem.Ventilation,
                    Direction = FlowDirection.Out,
                    RoomId = room.Id,
                    EquipmentId = equipment.Id,
                    EquipmentType = equipment.Type,
                    Position = equipment.Position,
                    LevelName = room.LevelName,
                    ElevationFromFloorM = 0.15, // acima da geratriz do ramal
                    MinDiameterMm = equipment.MinVentDiameterMm,
                    IsRequired = true,
                    RequiresIndependentBranch = false,
                    ConnectsViaSiphonBox = false,
                    RequiresVentilation = false
                });
            }

            return points;
        }

        /// <summary>
        /// Gera ponto de dreno independente (ralo, CX sifonada, CX gordura).
        /// </summary>
        public static HydraulicPoint GenerateAccessoryPoint(
            EquipmentType accessoryType, RoomInfo room, Point3D position)
        {
            return new HydraulicPoint
            {
                Id = $"hp_es_{++_seqEs:D3}",
                Type = accessoryType switch
                {
                    EquipmentType.FloorDrain => PointType.FloorDrain,
                    EquipmentType.FloorDrainDry => PointType.FloorDrain,
                    EquipmentType.SiphonBox => PointType.Accessory,
                    EquipmentType.GreaseBox => PointType.Accessory,
                    _ => PointType.FloorDrain
                },
                System = HydraulicSystem.Sewer,
                Direction = FlowDirection.Out,
                RoomId = room.Id,
                EquipmentId = null, // independente
                EquipmentType = accessoryType,
                Position = position,
                LevelName = room.LevelName,
                ElevationFromFloorM = 0.0,
                ContributionUnitsES = accessoryType switch
                {
                    EquipmentType.FloorDrain => 1,
                    EquipmentType.SiphonBox => 0, // CX sifonada não tem UHC próprio
                    EquipmentType.GreaseBox => 0,
                    _ => 1
                },
                MinDiameterMm = accessoryType switch
                {
                    EquipmentType.SiphonBox => 75,
                    EquipmentType.GreaseBox => 75,
                    _ => 40
                },
                IsRequired = true,
                RequiresIndependentBranch = false,
                ConnectsViaSiphonBox = false,
                RequiresVentilation = false
            };
        }

        /// <summary>
        /// Gera ponto de prumada (destino final dos ramais).
        /// </summary>
        public static HydraulicPoint GenerateRiserPoint(
            HydraulicSystem system, string levelName, Point3D position, int diameterMm)
        {
            string prefix = system switch
            {
                HydraulicSystem.ColdWater => "af",
                HydraulicSystem.Sewer => "es",
                HydraulicSystem.Ventilation => "ve",
                _ => "xx"
            };

            return new HydraulicPoint
            {
                Id = $"hp_riser_{prefix}_{++_seqEs:D3}",
                Type = PointType.Riser,
                System = system,
                Direction = system == HydraulicSystem.ColdWater
                    ? FlowDirection.Out  // prumada AF fornece água
                    : FlowDirection.In,  // prumada ES recebe esgoto
                RoomId = null,  // prumada pode não estar em Room
                EquipmentId = null,
                Position = position,
                LevelName = levelName,
                MinDiameterMm = diameterMm,
                IsRequired = true
            };
        }

        private static double GetDefaultElevation(EquipmentType type, HydraulicSystem system)
        {
            if (system == HydraulicSystem.ColdWater)
            {
                return type switch
                {
                    EquipmentType.Shower => 2.00,
                    EquipmentType.Sink => 0.60,
                    EquipmentType.KitchenSink => 1.00,
                    EquipmentType.LaundryTub => 0.90,
                    EquipmentType.ToiletCoupledTank => 0.20,
                    EquipmentType.WashingMachine => 0.80,
                    EquipmentType.Dishwasher => 0.80,
                    EquipmentType.GardenFaucet => 0.60,
                    _ => 0.60
                };
            }
            else // Sewer
            {
                return type switch
                {
                    EquipmentType.ToiletCoupledTank => 0.00,
                    EquipmentType.ToiletFlushValve => 0.00,
                    EquipmentType.FloorDrain => 0.00,
                    EquipmentType.SiphonBox => 0.00,
                    EquipmentType.GreaseBox => 0.00,
                    EquipmentType.Sink => 0.50,
                    EquipmentType.KitchenSink => 0.85,
                    EquipmentType.Shower => 0.00,
                    EquipmentType.Bathtub => 0.10,
                    EquipmentType.LaundryTub => 0.80,
                    _ => 0.00
                };
            }
        }
    }
    ```

    ---

    ## 5. Métodos do Modelo

    ```csharp
    public class HydraulicPoint
    {
        // ... propriedades acima ...

        // ── Consultas ───────────────────────────────────────────────

        /// <summary>
        /// Verifica se o ponto está conectado à rede.
        /// </summary>
        public bool IsConnected() => IsConnectedToNetwork && !string.IsNullOrEmpty(ConnectedSegmentId);

        /// <summary>
        /// Retorna demanda de água fria (peso) para dimensionamento.
        /// 0 se o ponto não é AF.
        /// </summary>
        public double GetWaterDemand() => System == HydraulicSystem.ColdWater ? WeightColdWater : 0;

        /// <summary>
        /// Retorna carga de esgoto (UHC) para dimensionamento.
        /// 0 se o ponto não é ES.
        /// </summary>
        public int GetSewageLoad() => System == HydraulicSystem.Sewer ? ContributionUnitsES : 0;

        /// <summary>
        /// Verifica se é um ponto que tem equipamento associado.
        /// </summary>
        public bool HasEquipment() => !string.IsNullOrEmpty(EquipmentId);

        /// <summary>
        /// Verifica se é um ponto independente (sem equipamento).
        /// </summary>
        public bool IsIndependent() => string.IsNullOrEmpty(EquipmentId);

        /// <summary>
        /// Verifica se é um ponto de acessório (CX sifonada, CX gordura).
        /// </summary>
        public bool IsAccessory() => Type == PointType.Accessory;

        /// <summary>
        /// Verifica se é um ponto de prumada.
        /// </summary>
        public bool IsRiser() => Type == PointType.Riser;

        /// <summary>
        /// Verifica se o ponto está no piso (Z ≈ 0).
        /// </summary>
        public bool IsAtFloorLevel() => ElevationFromFloorM <= 0.10;

        /// <summary>
        /// Verifica se a posição está dentro do bounding box do ambiente.
        /// </summary>
        public bool ValidatePosition(RoomInfo room)
        {
            if (room == null) return false;
            if (IsRiser()) return true; // prumadas podem não estar em Room
            return room.ContainsPoint(Position);
        }

        /// <summary>
        /// Calcula distância 2D até outro ponto (para estimar comprimento do ramal).
        /// </summary>
        public double DistanceTo(HydraulicPoint other)
        {
            return Position.DistanceTo2D(other.Position);
        }

        /// <summary>
        /// Calcula desnível vertical até outro ponto (para declividade).
        /// </summary>
        public double VerticalDifferenceTo(HydraulicPoint other)
        {
            return Position.Z - other.Position.Z;
        }

        // ── Display ─────────────────────────────────────────────────

        /// <summary>
        /// Nome curto para UI.
        /// </summary>
        public string GetDisplayName()
        {
            string equip = EquipmentType != EquipmentType.Unknown
                ? EquipmentType.ToString()
                : Type.ToString();

            string sys = System switch
            {
                HydraulicSystem.ColdWater => "AF",
                HydraulicSystem.Sewer => "ES",
                HydraulicSystem.Ventilation => "VE",
                _ => "??"
            };

            string status = Status switch
            {
                PointStatus.Connected => "✅",
                PointStatus.Validated => "🔵",
                PointStatus.Inserted => "🟡",
                PointStatus.Planned => "⬜",
                PointStatus.Error => "❌",
                PointStatus.Skipped => "⏭️",
                _ => "⬜"
            };

            return $"{status} {sys} — {equip} (DN {MinDiameterMm})";
        }

        public override string ToString()
        {
            return $"HP[{Id}] {System}/{Type} — Equip:{EquipmentId ?? "independente"}, " +
                $"Pos:{Position}, DN:{MinDiameterMm}, Status:{Status}";
        }
    }
    ```

    ---

    ## 6. Validação

    ### 6.1 HydraulicPointValidator

    ```csharp
    namespace HidraulicoPlugin.Core.Validation
    {
        public class HydraulicPointValidator
        {
            public ValidationReport Validate(HydraulicPoint point, RoomInfo room = null)
            {
                var report = new ValidationReport();

                // ── Identidade ──────────────────────────────────────
                if (string.IsNullOrWhiteSpace(point.Id))
                    report.Add(ValidationLevel.Critical, "Ponto sem ID");

                if (point.Type == 0)
                    report.Add(ValidationLevel.Critical,
                        $"Ponto {point.Id}: tipo não definido");

                // ── Sistema/Direção consistentes ────────────────────
                if (point.System == HydraulicSystem.ColdWater && point.Direction != FlowDirection.In)
                    report.Add(ValidationLevel.Medium,
                        $"Ponto {point.Id}: AF deve ser Direction=In");

                if (point.System == HydraulicSystem.Sewer && point.Direction != FlowDirection.Out
                    && point.Type != PointType.Riser) // prumada ES recebe
                    report.Add(ValidationLevel.Light,
                        $"Ponto {point.Id}: ES normalmente é Direction=Out");

                // ── Ambiente ────────────────────────────────────────
                if (string.IsNullOrWhiteSpace(point.RoomId) && !point.IsRiser())
                    report.Add(ValidationLevel.Medium,
                        $"Ponto {point.Id}: sem ambiente associado");

                // ── Posição ─────────────────────────────────────────
                if (point.Position.X == 0 && point.Position.Y == 0 && point.Position.Z == 0)
                    report.Add(ValidationLevel.Light,
                        $"Ponto {point.Id}: posição na origem (0,0,0)");

                if (room != null && !point.ValidatePosition(room))
                    report.Add(ValidationLevel.Medium,
                        $"Ponto {point.Id}: fora do BBox do ambiente {room.Id}");

                // ── Parâmetros hidráulicos ──────────────────────────
                if (point.System == HydraulicSystem.ColdWater && point.WeightColdWater <= 0 
                    && point.Type != PointType.Riser)
                    report.Add(ValidationLevel.Medium,
                        $"Ponto {point.Id}: peso AF = 0");

                if (point.System == HydraulicSystem.Sewer && point.ContributionUnitsES <= 0
                    && point.Type != PointType.Riser && point.Type != PointType.Accessory)
                    report.Add(ValidationLevel.Medium,
                        $"Ponto {point.Id}: UHC = 0");

                if (point.MinDiameterMm <= 0)
                    report.Add(ValidationLevel.Medium,
                        $"Ponto {point.Id}: DN mínimo = 0");

                // ── Regras normativas ───────────────────────────────
                if (point.RequiresIndependentBranch && point.ConnectsViaSiphonBox)
                    report.Add(ValidationLevel.Critical,
                        $"Ponto {point.Id}: ramal independente E via CX sifonada (contradição)");

                if (point.EquipmentType == EquipmentType.ToiletCoupledTank
                    && point.MinDiameterMm < 100
                    && point.System == HydraulicSystem.Sewer)
                    report.Add(ValidationLevel.Critical,
                        $"Ponto {point.Id}: vaso c/ DN ES < 100mm");

                return report;
            }
        }
    }
    ```

    ---

    ## 7. Exemplo — Banheiro completo

    ### 7.1 Pontos gerados para 1 banheiro social

    ```
    Banheiro Social (room_102):
    Equipamentos: Vaso, Lavatório, Chuveiro, Ralo

    Pontos AF (3):
        hp_af_001 → Vaso (WaterSupply, DN 20, Z 0.20m)
        hp_af_002 → Lavatório (WaterSupply, DN 20, Z 0.60m)
        hp_af_003 → Chuveiro (WaterSupply, DN 20, Z 2.00m)

    Pontos ES (4):
        hp_es_001 → Vaso (SewerDischarge, DN 100, INDEPENDENTE, requer ventilação)
        hp_es_002 → Lavatório (SewerDischarge, DN 40, via CX sifonada)
        hp_es_003 → Chuveiro (SewerDischarge, DN 40, via CX sifonada)
        hp_es_004 → Ralo (FloorDrain, DN 40, via CX sifonada)

    Ponto Acessório (1):
        hp_acc_001 → CX sifonada (Accessory, DN 75, independente)

    Ponto Ventilação (1):
        hp_ve_001 → Ventilação do vaso (Ventilation, DN 50)

    Total: 9 pontos para 1 banheiro
    ```

    ### 7.2 Topologia de conexão ES

    ```
    Lavatório (hp_es_002, DN 40) ─┐
    Chuveiro  (hp_es_003, DN 40) ─┼─→ CX sifonada (hp_acc_001, DN 75) → Subcoletor
    Ralo      (hp_es_004, DN 40) ─┘

    Vaso (hp_es_001, DN 100) ────────────────────────────────────────→ Subcoletor
                                                                        (INDEPENDENTE)
    ```

    ---

    ## 8. JSON de Exemplo

    ```json
    [
    {
        "id": "hp_af_001",
        "type": "WaterSupply",
        "system": "ColdWater",
        "direction": "In",
        "room_id": "423567",
        "equipment_id": "eq_001",
        "equipment_type": "ToiletCoupledTank",
        "position": { "x": 5.800, "y": 3.200, "z": 0.200 },
        "level_name": "Térreo",
        "elevation_from_floor_m": 0.20,
        "weight_cold_water": 0.3,
        "contribution_units_es": 0,
        "design_flow_rate_ls": 0.15,
        "min_dynamic_pressure_mca": 0.5,
        "min_diameter_mm": 20,
        "requires_independent_branch": false,
        "connects_via_siphon_box": false,
        "requires_ventilation": false,
        "connects_to_point_id": null,
        "connected_segment_id": null,
        "is_required": true,
        "status": "Planned",
        "is_connected_to_network": false
    },
    {
        "id": "hp_es_001",
        "type": "SewerDischarge",
        "system": "Sewer",
        "direction": "Out",
        "room_id": "423567",
        "equipment_id": "eq_001",
        "equipment_type": "ToiletCoupledTank",
        "position": { "x": 5.800, "y": 3.050, "z": 0.000 },
        "level_name": "Térreo",
        "elevation_from_floor_m": 0.00,
        "weight_cold_water": 0.0,
        "contribution_units_es": 6,
        "design_flow_rate_ls": 0.0,
        "min_dynamic_pressure_mca": 0.0,
        "min_diameter_mm": 100,
        "requires_independent_branch": true,
        "connects_via_siphon_box": false,
        "requires_ventilation": true,
        "connects_to_point_id": null,
        "connected_segment_id": null,
        "is_required": true,
        "status": "Planned",
        "is_connected_to_network": false
    },
    {
        "id": "hp_es_002",
        "type": "SewerDischarge",
        "system": "Sewer",
        "direction": "Out",
        "room_id": "423567",
        "equipment_id": "eq_002",
        "equipment_type": "Sink",
        "position": { "x": 4.600, "y": 4.500, "z": 0.500 },
        "level_name": "Térreo",
        "elevation_from_floor_m": 0.50,
        "weight_cold_water": 0.0,
        "contribution_units_es": 2,
        "design_flow_rate_ls": 0.0,
        "min_dynamic_pressure_mca": 0.0,
        "min_diameter_mm": 40,
        "requires_independent_branch": false,
        "connects_via_siphon_box": true,
        "requires_ventilation": false,
        "connects_to_point_id": "hp_acc_001",
        "connected_segment_id": null,
        "is_required": true,
        "status": "Planned",
        "is_connected_to_network": false
    },
    {
        "id": "hp_acc_001",
        "type": "Accessory",
        "system": "Sewer",
        "direction": "Out",
        "room_id": "423567",
        "equipment_id": null,
        "equipment_type": "SiphonBox",
        "position": { "x": 5.200, "y": 3.800, "z": 0.000 },
        "level_name": "Térreo",
        "elevation_from_floor_m": 0.00,
        "weight_cold_water": 0.0,
        "contribution_units_es": 0,
        "design_flow_rate_ls": 0.0,
        "min_dynamic_pressure_mca": 0.0,
        "min_diameter_mm": 75,
        "requires_independent_branch": false,
        "connects_via_siphon_box": false,
        "requires_ventilation": false,
        "connects_to_point_id": null,
        "connected_segment_id": null,
        "is_required": true,
        "status": "Planned",
        "is_connected_to_network": false
    }
    ]
    ```

    ---

    ## 9. Resumo Visual

    ```
    HydraulicPoint
    ├── Identidade
    │   ├── Id (string — "hp_{sys}_{seq}")
    │   ├── Type (PointType — 6 valores)
    │   ├── System (HydraulicSystem — AF|ES|VE)
    │   └── Direction (FlowDirection — In|Out)
    ├── Relações
    │   ├── RoomId (string → RoomInfo.Id)
    │   ├── EquipmentId (string? → EquipmentInfo.Id)
    │   ├── EquipmentType (enum)
    │   ├── ConnectsToPointId (string? → outro HP)
    │   └── ConnectedSegmentId (string? → PipeSegment)
    ├── Localização
    │   ├── Position (Point3D, metros)
    │   ├── LevelName (string)
    │   └── ElevationFromFloorM (double)
    ├── Parâmetros Hidráulicos
    │   ├── WeightColdWater (double)
    │   ├── ContributionUnitsES (int)
    │   ├── DesignFlowRateLs (double)
    │   ├── MinDynamicPressureMca (double)
    │   └── MinDiameterMm (int)
    ├── Comportamento de Rede
    │   ├── RequiresIndependentBranch (bool)
    │   ├── ConnectsViaSiphonBox (bool)
    │   └── RequiresVentilation (bool)
    ├── Status
    │   ├── IsRequired (bool)
    │   ├── Status (PointStatus — 6 valores)
    │   └── IsConnectedToNetwork (bool)
    └── Métodos
        ├── IsConnected()
        ├── GetWaterDemand()
        ├── GetSewageLoad()
        ├── HasEquipment()
        ├── IsIndependent()
        ├── IsAccessory()
        ├── IsRiser()
        ├── IsAtFloorLevel()
        ├── ValidatePosition(RoomInfo)
        ├── DistanceTo(HydraulicPoint)
        ├── VerticalDifferenceTo(HydraulicPoint)
        ├── GetDisplayName()
        └── ToString()
    ```
