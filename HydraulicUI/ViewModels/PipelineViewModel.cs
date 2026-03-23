using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace HidraulicoPlugin.UI.ViewModels;

/// <summary>
/// ViewModel do painel de pipeline.
/// Controla exibição das etapas, progresso e ações do usuário.
/// </summary>
public partial class PipelineViewModel : BaseViewModel
{
    // ══════════════════════════════════════════════════════════
    //  PROPRIEDADES OBSERVÁVEIS
    // ══════════════════════════════════════════════════════════

    [ObservableProperty]
    private string _sessionId = string.Empty;

    [ObservableProperty]
    private string _currentStepName = "Aguardando início";

    [ObservableProperty]
    private string _currentStepDescription = string.Empty;

    [ObservableProperty]
    private double _overallProgress;

    [ObservableProperty]
    private double _stepProgress;

    [ObservableProperty]
    private string _stepDetail = string.Empty;

    [ObservableProperty]
    private string _statusLine = "⚪ Pipeline não iniciado";

    [ObservableProperty]
    private bool _isRunning;

    [ObservableProperty]
    private bool _isWaitingApproval;

    [ObservableProperty]
    private bool _isComplete;

    [ObservableProperty]
    private int _completedSteps;

    [ObservableProperty]
    private int _totalSteps = 12;

    [ObservableProperty]
    private string _approvalComment = string.Empty;

    [ObservableProperty]
    private string _lastError = string.Empty;

    // ══════════════════════════════════════════════════════════
    //  COLEÇÕES
    // ══════════════════════════════════════════════════════════

    public ObservableCollection<StepDisplayModel> Steps { get; } = new();

    public ObservableCollection<LogEntryModel> LogEntries { get; } = new();

    // ══════════════════════════════════════════════════════════
    //  CONSTRUTOR
    // ══════════════════════════════════════════════════════════

    public PipelineViewModel()
    {
        Title = "Pipeline de Execução";
        InitializeMockSteps();
    }

    private void InitializeMockSteps()
    {
        Steps.Add(new StepDisplayModel("E01", "Detectar Ambientes", "Leitura de Rooms/Spaces"));
        Steps.Add(new StepDisplayModel("E02", "Classificar Ambientes", "Classificação por tipo"));
        Steps.Add(new StepDisplayModel("E03", "Identificar Equipamentos", "Definição por ambiente"));
        Steps.Add(new StepDisplayModel("E04", "Inserir Equipamentos", "Posicionamento automático"));
        Steps.Add(new StepDisplayModel("E05", "Validar Modelo", "Verificação de consistência"));
        Steps.Add(new StepDisplayModel("E06", "Criar Prumadas", "Prumadas verticais por sistema"));
        Steps.Add(new StepDisplayModel("E07", "Rede Água Fria", "Geração da rede AF"));
        Steps.Add(new StepDisplayModel("E08", "Rede Esgoto", "Geração da rede ES"));
        Steps.Add(new StepDisplayModel("E09", "Rede Ventilação", "Geração da rede VE"));
        Steps.Add(new StepDisplayModel("E10", "Exportar p/ Revit", "Materialização no modelo"));
        Steps.Add(new StepDisplayModel("E11", "Dimensionar", "Cálculo hidráulico completo"));
        Steps.Add(new StepDisplayModel("E12", "Tabelas e Pranchas", "Geração de entregáveis"));

        TotalSteps = Steps.Count;
    }

    // ══════════════════════════════════════════════════════════
    //  COMMANDS
    // ══════════════════════════════════════════════════════════

    [RelayCommand(CanExecute = nameof(CanStart))]
    private void StartPipeline()
    {
        IsRunning = true;
        IsComplete = false;
        CompletedSteps = 0;
        StatusLine = "🔄 Pipeline iniciado";
        SessionId = $"sess_{DateTime.UtcNow:yyyyMMdd_HHmmss}";

        LogEntries.Insert(0, LogEntryModel.Info("Pipeline iniciado"));

        // Simular início da primeira etapa
        if (Steps.Count > 0)
        {
            Steps[0].Status = "Running";
            CurrentStepName = Steps[0].Name;
            CurrentStepDescription = Steps[0].Description;
        }
    }

    private bool CanStart() => !IsRunning;

    [RelayCommand(CanExecute = nameof(CanApprove))]
    private void Approve()
    {
        var step = Steps.FirstOrDefault(s => s.Status == "WaitingApproval");
        if (step == null) return;

        step.Status = "Completed";
        CompletedSteps++;
        IsWaitingApproval = false;
        OverallProgress = (double)CompletedSteps / TotalSteps * 100;

        LogEntries.Insert(0, LogEntryModel.Success($"Etapa {step.Id} aprovada"));

        // Avançar para próxima etapa
        var nextStep = Steps.FirstOrDefault(s => s.Status == "Pending");
        if (nextStep != null)
        {
            nextStep.Status = "Running";
            CurrentStepName = nextStep.Name;
            CurrentStepDescription = nextStep.Description;
            StatusLine = $"🔄 Executando: {nextStep.Name}";
        }
        else
        {
            IsRunning = false;
            IsComplete = true;
            StatusLine = "✅ Pipeline concluído";
            CurrentStepName = "Concluído";
            LogEntries.Insert(0, LogEntryModel.Success("Pipeline concluído com sucesso"));
        }

        ApprovalComment = string.Empty;
    }

    private bool CanApprove() => IsWaitingApproval;

    [RelayCommand(CanExecute = nameof(CanApprove))]
    private void Reject()
    {
        var step = Steps.FirstOrDefault(s => s.Status == "WaitingApproval");
        if (step == null) return;

        step.Status = "Rejected";
        IsWaitingApproval = false;
        StatusLine = $"👎 Etapa {step.Id} rejeitada";

        LogEntries.Insert(0, LogEntryModel.Warning($"Etapa {step.Id} rejeitada: {ApprovalComment}"));
        ApprovalComment = string.Empty;
    }

    [RelayCommand]
    private void SimulateStep()
    {
        var runningStep = Steps.FirstOrDefault(s => s.Status == "Running");
        if (runningStep == null) return;

        runningStep.Status = "WaitingApproval";
        IsWaitingApproval = true;
        StatusLine = $"⏸ Aguardando aprovação: {runningStep.Name}";

        LogEntries.Insert(0, LogEntryModel.Info($"Etapa {runningStep.Id} concluída — aguardando aprovação"));
    }
}

// ══════════════════════════════════════════════════════════
//  DISPLAY MODELS
// ══════════════════════════════════════════════════════════

public partial class StepDisplayModel : ObservableObject
{
    public string Id { get; }
    public string Name { get; }
    public string Description { get; }

    [ObservableProperty]
    private string _status = "Pending";

    [ObservableProperty]
    private string _summary = string.Empty;

    [ObservableProperty]
    private string _duration = string.Empty;

    public StepDisplayModel(string id, string name, string description)
    {
        Id = id;
        Name = name;
        Description = description;
    }
}

public class LogEntryModel
{
    public DateTime Timestamp { get; set; } = DateTime.Now;
    public string Level { get; set; } = "Info";
    public string Message { get; set; } = string.Empty;

    public string TimeFormatted => Timestamp.ToString("HH:mm:ss");

    public string Icon => Level switch
    {
        "Info" => "ℹ️",
        "Success" => "✅",
        "Warning" => "⚠️",
        "Error" => "❌",
        _ => "📌"
    };

    public static LogEntryModel Info(string msg) => new() { Level = "Info", Message = msg };
    public static LogEntryModel Success(string msg) => new() { Level = "Success", Message = msg };
    public static LogEntryModel Warning(string msg) => new() { Level = "Warning", Message = msg };
    public static LogEntryModel Error(string msg) => new() { Level = "Error", Message = msg };
}
