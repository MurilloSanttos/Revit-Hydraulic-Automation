namespace PluginCore.Domain.Enums;

/// <summary>
/// Tipo de ambiente — classificação funcional.
/// </summary>
public enum RoomType
{
    Unknown = 0,
    Bathroom,
    MasterBathroom,
    Lavatory,
    Kitchen,
    Laundry,
    Balcony,
    Garage,
    ServiceArea,
    DryArea
}

/// <summary>
/// Sistema hidráulico.
/// </summary>
public enum HydraulicSystem
{
    ColdWater = 1,
    HotWater = 2,
    Sewer = 3,
    Ventilation = 4,
    Rainwater = 5
}

/// <summary>
/// Material da tubulação.
/// </summary>
public enum PipeMaterial
{
    PVC = 1,
    CPVC = 2,
    PPR = 3,
    Copper = 4,
    PEX = 5
}

/// <summary>
/// Tipo de equipamento hidráulico.
/// </summary>
public enum EquipmentType
{
    Toilet = 1,
    Sink = 2,
    Shower = 3,
    Bathtub = 4,
    KitchenSink = 5,
    LaundryTub = 6,
    WashingMachine = 7,
    Dishwasher = 8,
    FloorDrain = 9,
    Bidet = 10,
    Urinal = 11
}

/// <summary>
/// Tipo de conexão hidráulica.
/// </summary>
public enum ConnectionType
{
    Elbow90 = 1,
    Elbow45 = 2,
    Tee = 3,
    Cross = 4,
    Reducer = 5,
    Coupling = 6,
    Cap = 7,
    Valve = 8
}

/// <summary>
/// Status de execução de etapa do pipeline.
/// </summary>
public enum StepStatus
{
    Pending = 0,
    Running = 1,
    Completed = 2,
    Failed = 3,
    WaitingApproval = 4,
    Approved = 5,
    Rejected = 6,
    Skipped = 7,
    RolledBack = 8
}
