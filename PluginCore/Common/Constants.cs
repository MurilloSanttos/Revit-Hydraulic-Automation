namespace PluginCore.Common;

/// <summary>
/// Constantes globais do sistema.
/// </summary>
public static class Constants
{
    /// <summary>Nome do plugin.</summary>
    public const string PluginName = "Hydraulic Automation";

    /// <summary>Versão atual.</summary>
    public const string Version = "0.1.0";

    /// <summary>Vendor ID para registro no Revit.</summary>
    public const string VendorId = "HidraulicaAutomacao";

    /// <summary>
    /// Nomes das etapas do pipeline.
    /// </summary>
    public static class StepIds
    {
        public const string DetectRooms = "E01";
        public const string ClassifyRooms = "E02";
        public const string IdentifyEquipment = "E03";
        public const string InsertEquipment = "E04";
        public const string ValidateModel = "E05";
        public const string CreateRisers = "E06";
        public const string BuildColdWater = "E07";
        public const string BuildSewer = "E08";
        public const string BuildVentilation = "E09";
        public const string ExportToRevit = "E10";
        public const string SizeNetworks = "E11";
        public const string GenerateSheets = "E12";
    }

    /// <summary>
    /// Caminhos padrão.
    /// </summary>
    public static class Paths
    {
        public const string LogSubfolder = "HidraulicaRevit\\Logs";
        public const string ConfigSubfolder = "HidraulicaRevit\\Config";
    }
}
