# Modelo de Domínio — SizingResult (ResultadoDimensionamento)

> Especificação completa do modelo agnóstico ao Revit que armazena resultados de cálculos hidráulicos, indicadores de conformidade normativa e contexto de dimensionamento, para uso em trechos, prumadas e sistemas.

---

## 1. Definição do Modelo

### 1.1 O que é SizingResult

`SizingResult` é o registro completo do **resultado de um cálculo de dimensionamento hidráulico** aplicado a um elemento da rede. Contém os valores de entrada resumidos, os valores calculados, os valores adotados (comerciais), e os indicadores de conformidade normativa.

### 1.2 Papel no sistema

```
PipeSegment / Riser / PipeNetwork
        │
        └── SizingResult                ← ESTE MODELO
                ├── Inputs (ΣP, ΣUHC, L, i)
                ├── Calculated (Q, DN calc, V, J)
                ├── Adopted (DN adotado, V real)
                └── Compliance (pressão OK? V OK? DN OK?)
```

| Módulo | Uso do SizingResult |
|--------|---------------------|
| E11 — Dimensionamento | Gerado: 1 SizingResult por trecho/prumada |
| E05 — Validação | Verificar conformidade (IsCompliant) |
| E12 — Tabelas | Exibir resultados em tabelas de dimensionamento |
| E13 — Pranchas | Dados para quadros normativos |
| Pipeline | Decidir se avança ou requer correção |

### 1.3 Por que é separado das entidades físicas

```
PipeSegment = O TRECHO (geometria + propriedades físicas)
  - Sabe seu DN, comprimento, material
  - Muda quando a rede muda

SizingResult = O CÁLCULO (resultado + conformidade)
  - Sabe COMO o DN foi decidido
  - Registra método, inputs, margens
  - Pode ser recalculado sem alterar o trecho
  - Historicável (versões do cálculo)

POR QUE SEPARAR:
  1. Um trecho pode ter MÚLTIPLOS resultados (iterações)
  2. Permite auditar: "por que DN 25 e não DN 20?"
  3. Desacopla cálculo de representação
  4. Facilita testes unitários dos cálculos
  5. Mesmo formato para trechos, prumadas e sistemas
```

---

## 2. Estrutura de Dados — Código C# Completo

```csharp
namespace HidraulicoPlugin.Core.Models
{
    /// <summary>
    /// Resultado do dimensionamento hidráulico de um elemento.
    /// Aplicável a: PipeSegment, Riser, PipeNetwork.
    /// Contém inputs resumidos, valores calculados, valores adotados
    /// e indicadores de conformidade normativa.
    /// </summary>
    public class SizingResult
    {
        // ── Identidade ──────────────────────────────────────────────

        /// <summary>
        /// ID único do resultado.
        /// Formato: "sr_{contexto}_{elementoId}" (ex: "sr_seg_seg_af_001").
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// ID do elemento dimensionado (PipeSegment.Id, Riser.Id, PipeNetwork.Id).
        /// </summary>
        public string ElementId { get; set; }

        /// <summary>
        /// Sistema hidráulico.
        /// </summary>
        public HydraulicSystem System { get; set; }

        /// <summary>
        /// Contexto do cálculo: em qual tipo de elemento este resultado foi gerado.
        /// </summary>
        public SizingContext Context { get; set; }

        /// <summary>
        /// Método de cálculo utilizado.
        /// </summary>
        public CalculationMethod Method { get; set; }

        /// <summary>
        /// Timestamp do cálculo.
        /// </summary>
        public DateTime CalculatedAt { get; set; }

        /// <summary>
        /// Versão do cálculo (incrementa a cada recálculo do mesmo elemento).
        /// </summary>
        public int Version { get; set; } = 1;

        // ── Inputs (Resumo dos dados de entrada) ────────────────────

        /// <summary>
        /// Soma de pesos a montante (AF). ΣP.
        /// </summary>
        public double InputWeightSum { get; set; }

        /// <summary>
        /// Soma de UHCs a montante (ES). ΣUHC.
        /// </summary>
        public int InputUHCSum { get; set; }

        /// <summary>
        /// Comprimento real do trecho em metros.
        /// </summary>
        public double InputLengthM { get; set; }

        /// <summary>
        /// Comprimento equivalente dos fittings em metros.
        /// </summary>
        public double InputFittingsLengthM { get; set; }

        /// <summary>
        /// Comprimento total de cálculo (real + equivalente).
        /// </summary>
        public double InputTotalLengthM => InputLengthM + InputFittingsLengthM;

        /// <summary>
        /// Declividade aplicada em % (apenas ES).
        /// </summary>
        public double InputSlopePercent { get; set; }

        /// <summary>
        /// Desnível geométrico em metros (Z_início - Z_fim).
        /// Positivo = descendo. Usado no cálculo de pressão AF.
        /// </summary>
        public double InputElevationDiffM { get; set; }

        /// <summary>
        /// Material da tubulação (afeta rugosidade e DI).
        /// </summary>
        public PipeMaterial InputMaterial { get; set; }

        /// <summary>
        /// Coeficiente de Hazen-Williams do material.
        /// </summary>
        public double InputHazenC { get; set; }

        /// <summary>
        /// Pressão disponível na entrada do trecho em mca (AF).
        /// </summary>
        public double InputPressureStartMca { get; set; }

        /// <summary>
        /// Pressão mínima requerida no ponto de utilização em mca (AF).
        /// </summary>
        public double InputMinRequiredPressureMca { get; set; }

        // ── Resultados Calculados ───────────────────────────────────

        /// <summary>
        /// Vazão de projeto calculada em L/s.
        /// AF: Q = 0.3 × √ΣP.
        /// ES: tabela UHC → Q (NBR 8160).
        /// </summary>
        public double CalculatedFlowRateLs { get; set; }

        /// <summary>
        /// DN calculado (teórico) em mm.
        /// Resultado direto da fórmula, antes do arredondamento comercial.
        /// </summary>
        public double CalculatedDiameterMm { get; set; }

        /// <summary>
        /// DN adotado (comercial) em mm.
        /// Arredondado para o DN comercial imediatamente superior.
        /// </summary>
        public int AdoptedDiameterMm { get; set; }

        /// <summary>
        /// DI (diâmetro interno) do DN adotado em mm.
        /// Depende do material.
        /// </summary>
        public double AdoptedInternalDiameterMm { get; set; }

        /// <summary>
        /// Velocidade calculada em m/s.
        /// V = Q / A, onde A = π × (DI/2)².
        /// </summary>
        public double CalculatedVelocityMs { get; set; }

        /// <summary>
        /// Perda de carga unitária (J) em m/m.
        /// Calculada por Hazen-Williams ou Fair-Whipple-Hsiao.
        /// </summary>
        public double HeadLossUnitaryMm { get; set; }

        /// <summary>
        /// Perda de carga total no trecho em mca.
        /// J × L_total (unitária × comprimento total de cálculo).
        /// </summary>
        public double HeadLossTotalMca { get; set; }

        /// <summary>
        /// Pressão disponível no final do trecho em mca (AF).
        /// P_fim = P_início - ΔP_perda + ΔZ (desnível a favor).
        /// </summary>
        public double PressureEndMca { get; set; }

        /// <summary>
        /// Declividade real alcançada no trecho em % (ES).
        /// Pode diferir da declividade mínima requerida.
        /// </summary>
        public double ActualSlopePercent { get; set; }

        /// <summary>
        /// Taxa de ocupação da seção do tubo em % (ES).
        /// Lâmina d'água / DI. Máximo: 75% (NBR 8160).
        /// </summary>
        public double OccupancyRatePercent { get; set; }

        // ── Conformidade Normativa ──────────────────────────────────

        /// <summary>
        /// Se a pressão final atende ao mínimo requerido.
        /// True se PressureEndMca ≥ InputMinRequiredPressureMca.
        /// Relevante apenas para AF.
        /// </summary>
        public bool MeetsPressureRequirement { get; set; }

        /// <summary>
        /// Se a velocidade está dentro dos limites.
        /// AF: 0.5 ≤ V ≤ 3.0 m/s (NBR 5626).
        /// </summary>
        public bool MeetsVelocityLimit { get; set; }

        /// <summary>
        /// Se o DN adotado atende ao DN mínimo normativo.
        /// Ex: sub-ramal ≥ DN 20, ramal ES do vaso ≥ DN 100.
        /// </summary>
        public bool MeetsDiameterMinimum { get; set; }

        /// <summary>
        /// Se a declividade atende ao mínimo normativo (ES).
        /// DN ≤ 75 → i ≥ 2%. DN ≥ 100 → i ≥ 1%.
        /// </summary>
        public bool MeetsSlopeRequirement { get; set; }

        /// <summary>
        /// Se a taxa de ocupação está dentro do limite (ES).
        /// Máx 75% para tubos horizontais (NBR 8160).
        /// </summary>
        public bool MeetsOccupancyLimit { get; set; }

        /// <summary>
        /// Se o resultado é normativamente válido (todos os indicadores OK).
        /// </summary>
        public bool IsCompliant { get; set; }

        /// <summary>
        /// Nota de conformidade: "Aprovado", "Aprovado com ressalvas", "Reprovado".
        /// </summary>
        public string ComplianceNote { get; set; }

        // ── Margens ─────────────────────────────────────────────────

        /// <summary>
        /// Margem de pressão em mca (quanto sobra acima do mínimo).
        /// Positivo = folga. Negativo = insuficiente.
        /// </summary>
        public double PressureMarginMca { get; set; }

        /// <summary>
        /// Margem de velocidade em m/s (quanto falta para o limite).
        /// Para AF: 3.0 - V_calculada.
        /// </summary>
        public double VelocityMarginMs { get; set; }

        /// <summary>
        /// Percentual de utilização do DN em %.
        /// Q_real / Q_max_do_DN × 100.
        /// 100% = no limite. >100% = subdimensionado.
        /// </summary>
        public double DiameterUtilizationPercent { get; set; }

        // ── Warnings ────────────────────────────────────────────────

        /// <summary>
        /// Lista de alertas gerados durante o cálculo.
        /// </summary>
        public List<SizingWarning> Warnings { get; set; } = new();
    }
}
```

---

## 3. Enums

### 3.1 SizingContext

```csharp
namespace HidraulicoPlugin.Core.Models.Enums
{
    /// <summary>
    /// Contexto onde o dimensionamento foi aplicado.
    /// </summary>
    public enum SizingContext
    {
        /// <summary>Dimensionamento de um trecho de tubulação.</summary>
        Segment = 1,

        /// <summary>Dimensionamento de uma prumada.</summary>
        Riser = 2,

        /// <summary>Dimensionamento global do sistema.</summary>
        Network = 3,

        /// <summary>Dimensionamento de um ponto específico (verificação pontual).</summary>
        Point = 4
    }
}
```

### 3.2 CalculationMethod

```csharp
namespace HidraulicoPlugin.Core.Models.Enums
{
    /// <summary>
    /// Método de cálculo hidráulico utilizado.
    /// </summary>
    public enum CalculationMethod
    {
        /// <summary>
        /// Hazen-Williams: J = 10.643 × Q^1.85 / (C^1.85 × D^4.87).
        /// Padrão para AF (NBR 5626).
        /// </summary>
        HazenWilliams = 1,

        /// <summary>
        /// Fair-Whipple-Hsiao: J = 8.69 × 10^6 × Q^1.75 / D^4.75.
        /// Alternativa para AF com tubos lisos (PVC).
        /// </summary>
        FairWhippleHsiao = 2,

        /// <summary>
        /// Manning: V = (1/n) × R^(2/3) × i^(1/2).
        /// Padrão para ES (NBR 8160). Escoamento livre (tubo parcialmente cheio).
        /// </summary>
        Manning = 3,

        /// <summary>
        /// Tabela direta: DN lido de tabela normativa (UHC → DN).
        /// Usado para ramais de descarga e subcoletores ES.
        /// </summary>
        NormativeTable = 4,

        /// <summary>
        /// Cálculo simplificado por pesos (Q = 0.3 × √ΣP).
        /// Fórmula principal da NBR 5626 para AF.
        /// </summary>
        ProbableFlow = 5
    }
}
```

### 3.3 SizingWarning

```csharp
namespace HidraulicoPlugin.Core.Models
{
    /// <summary>
    /// Alerta gerado durante o dimensionamento.
    /// </summary>
    public class SizingWarning
    {
        /// <summary>Severidade do alerta.</summary>
        public WarningLevel Level { get; set; }

        /// <summary>Código do alerta (para filtragem).</summary>
        public string Code { get; set; }

        /// <summary>Mensagem descritiva.</summary>
        public string Message { get; set; }

        /// <summary>Valor atual que gerou o alerta.</summary>
        public string ActualValue { get; set; }

        /// <summary>Valor limite que foi violado.</summary>
        public string LimitValue { get; set; }

        /// <summary>Referência normativa (ex: "NBR 5626, item 5.4.2").</summary>
        public string NormReference { get; set; }
    }

    public enum WarningLevel
    {
        /// <summary>Informativo (valor próximo do limite).</summary>
        Info = 0,

        /// <summary>Alerta (margem pequena, mas aceitável).</summary>
        Warning = 1,

        /// <summary>Erro (violação normativa).</summary>
        Error = 2,

        /// <summary>Crítico (projeto inviável sem correção).</summary>
        Critical = 3
    }
}
```

---

## 4. Métodos do Modelo

```csharp
public class SizingResult
{
    // ... propriedades acima ...

    // ── Avaliação de conformidade ───────────────────────────────

    /// <summary>
    /// Executa validação completa e preenche os indicadores de conformidade.
    /// Chamado após o cálculo dos valores.
    /// </summary>
    public void Validate()
    {
        Warnings.Clear();

        // ── Pressão (AF) ────────────────────────────────────────
        if (System == HydraulicSystem.ColdWater)
        {
            MeetsPressureRequirement = PressureEndMca >= InputMinRequiredPressureMca;
            PressureMarginMca = PressureEndMca - InputMinRequiredPressureMca;

            if (!MeetsPressureRequirement)
            {
                Warnings.Add(new SizingWarning
                {
                    Level = WarningLevel.Error,
                    Code = "PRESS_INSUF",
                    Message = $"Pressão insuficiente no ponto de utilização",
                    ActualValue = $"{PressureEndMca:F2} mca",
                    LimitValue = $"≥ {InputMinRequiredPressureMca:F2} mca",
                    NormReference = "NBR 5626, item 5.4.2.1"
                });
            }
            else if (PressureMarginMca < 1.0)
            {
                Warnings.Add(new SizingWarning
                {
                    Level = WarningLevel.Warning,
                    Code = "PRESS_MARGIN",
                    Message = $"Margem de pressão reduzida",
                    ActualValue = $"{PressureMarginMca:F2} mca de folga",
                    LimitValue = "Recomendado ≥ 1.0 mca",
                    NormReference = "NBR 5626, item 5.4.2.1"
                });
            }
        }
        else
        {
            MeetsPressureRequirement = true;
        }

        // ── Velocidade (AF) ─────────────────────────────────────
        if (System == HydraulicSystem.ColdWater)
        {
            MeetsVelocityLimit = CalculatedVelocityMs >= 0.5
                              && CalculatedVelocityMs <= 3.0;
            VelocityMarginMs = 3.0 - CalculatedVelocityMs;

            if (CalculatedVelocityMs > 3.0)
            {
                Warnings.Add(new SizingWarning
                {
                    Level = WarningLevel.Error,
                    Code = "VEL_HIGH",
                    Message = "Velocidade acima do limite máximo",
                    ActualValue = $"{CalculatedVelocityMs:F2} m/s",
                    LimitValue = "≤ 3.0 m/s",
                    NormReference = "NBR 5626, item 5.4.2.3"
                });
            }
            else if (CalculatedVelocityMs < 0.5)
            {
                Warnings.Add(new SizingWarning
                {
                    Level = WarningLevel.Info,
                    Code = "VEL_LOW",
                    Message = "Velocidade abaixo do recomendado (risco de sedimentação)",
                    ActualValue = $"{CalculatedVelocityMs:F2} m/s",
                    LimitValue = "≥ 0.5 m/s",
                    NormReference = "NBR 5626, item 5.4.2.3"
                });
            }
        }
        else
        {
            MeetsVelocityLimit = true;
        }

        // ── DN mínimo ───────────────────────────────────────────
        MeetsDiameterMinimum = AdoptedDiameterMm >= GetMinimumDN();

        if (!MeetsDiameterMinimum)
        {
            Warnings.Add(new SizingWarning
            {
                Level = WarningLevel.Error,
                Code = "DN_MIN",
                Message = "DN abaixo do mínimo normativo",
                ActualValue = $"DN {AdoptedDiameterMm}",
                LimitValue = $"≥ DN {GetMinimumDN()}",
                NormReference = System == HydraulicSystem.ColdWater
                    ? "NBR 5626, Tabela 3" : "NBR 8160, Tabela 3"
            });
        }

        // ── Declividade (ES) ────────────────────────────────────
        if (System == HydraulicSystem.Sewer && Context == SizingContext.Segment)
        {
            double minSlope = AdoptedDiameterMm >= 100 ? 1.0 : 2.0;
            MeetsSlopeRequirement = ActualSlopePercent >= minSlope;

            if (!MeetsSlopeRequirement)
            {
                Warnings.Add(new SizingWarning
                {
                    Level = WarningLevel.Error,
                    Code = "SLOPE_MIN",
                    Message = "Declividade abaixo do mínimo",
                    ActualValue = $"{ActualSlopePercent:F1}%",
                    LimitValue = $"≥ {minSlope:F1}%",
                    NormReference = "NBR 8160, item 5.1.4"
                });
            }
        }
        else
        {
            MeetsSlopeRequirement = true;
        }

        // ── Ocupação (ES) ───────────────────────────────────────
        if (System == HydraulicSystem.Sewer)
        {
            MeetsOccupancyLimit = OccupancyRatePercent <= 75.0;

            if (!MeetsOccupancyLimit)
            {
                Warnings.Add(new SizingWarning
                {
                    Level = WarningLevel.Error,
                    Code = "OCCUP_MAX",
                    Message = "Taxa de ocupação acima do limite",
                    ActualValue = $"{OccupancyRatePercent:F1}%",
                    LimitValue = "≤ 75%",
                    NormReference = "NBR 8160, item 5.1.3"
                });
            }
        }
        else
        {
            MeetsOccupancyLimit = true;
        }

        // ── Resultado global ────────────────────────────────────
        IsCompliant = MeetsPressureRequirement
                   && MeetsVelocityLimit
                   && MeetsDiameterMinimum
                   && MeetsSlopeRequirement
                   && MeetsOccupancyLimit;

        ComplianceNote = IsCompliant
            ? (Warnings.Any(w => w.Level == WarningLevel.Warning)
                ? "Aprovado com ressalvas"
                : "Aprovado")
            : "Reprovado";
    }

    // ── Margens de segurança ────────────────────────────────────

    /// <summary>
    /// Calcula margem percentual de segurança global.
    /// Média das margens individuais (pressão, velocidade, DN).
    /// </summary>
    public double CalculateSafetyMarginPercent()
    {
        var margins = new List<double>();

        // Pressão
        if (System == HydraulicSystem.ColdWater && InputMinRequiredPressureMca > 0)
            margins.Add((PressureMarginMca / InputMinRequiredPressureMca) * 100);

        // Velocidade
        if (CalculatedVelocityMs > 0)
            margins.Add((VelocityMarginMs / 3.0) * 100);

        // DN
        if (AdoptedDiameterMm > 0 && CalculatedDiameterMm > 0)
            margins.Add(((AdoptedDiameterMm - CalculatedDiameterMm) / CalculatedDiameterMm) * 100);

        return margins.Count > 0 ? margins.Average() : 0;
    }

    /// <summary>
    /// Verifica se o DN poderia ser menor (está superdimensionado).
    /// True se um DN comercial menor ainda atenderia todas as regras.
    /// </summary>
    public bool IsOverSized()
    {
        int prevDN = GetPreviousCommercialDN(AdoptedDiameterMm);
        return prevDN > 0 && prevDN >= CalculatedDiameterMm && prevDN >= GetMinimumDN();
    }

    /// <summary>
    /// Retorna os warnings filtrados por nível.
    /// </summary>
    public List<SizingWarning> GetWarnings(WarningLevel minLevel = WarningLevel.Info)
    {
        return Warnings.Where(w => w.Level >= minLevel).ToList();
    }

    /// <summary>
    /// Retorna o número de erros (warnings nível Error ou Critical).
    /// </summary>
    public int GetErrorCount()
    {
        return Warnings.Count(w => w.Level >= WarningLevel.Error);
    }

    // ── Helpers ─────────────────────────────────────────────────

    private int GetMinimumDN()
    {
        return System switch
        {
            HydraulicSystem.ColdWater => 20,   // Sub-ramal mínimo AF
            HydraulicSystem.Sewer => 40,        // Ramal mínimo ES
            HydraulicSystem.Ventilation => 40,  // Ramal ventilação mínimo
            _ => 20
        };
    }

    private int GetPreviousCommercialDN(int currentDN)
    {
        int[] dns = { 20, 25, 32, 40, 50, 60, 75, 100, 150 };
        int idx = Array.IndexOf(dns, currentDN);
        return idx > 0 ? dns[idx - 1] : 0;
    }

    // ── Display ─────────────────────────────────────────────────

    public string GetDisplayName()
    {
        string status = IsCompliant ? "✅" : "❌";
        string sys = System switch
        {
            HydraulicSystem.ColdWater => "AF",
            HydraulicSystem.Sewer => "ES",
            HydraulicSystem.Ventilation => "VE",
            _ => "??"
        };

        return $"{status} {sys} {Context}: DN{AdoptedDiameterMm} " +
               $"Q={CalculatedFlowRateLs:F3}L/s V={CalculatedVelocityMs:F2}m/s " +
               $"[{ComplianceNote}]";
    }

    public override string ToString()
    {
        return $"SR[{Id}] {System}/{Context} → Element:{ElementId}\n" +
               $"  Inputs: ΣP={InputWeightSum:F1} ΣUHC={InputUHCSum} " +
               $"L={InputTotalLengthM:F2}m C={InputHazenC}\n" +
               $"  Calc: Q={CalculatedFlowRateLs:F3}L/s DN_calc={CalculatedDiameterMm:F1}mm\n" +
               $"  Adotado: DN{AdoptedDiameterMm} DI={AdoptedInternalDiameterMm:F1}mm\n" +
               $"  V={CalculatedVelocityMs:F2}m/s J={HeadLossUnitaryMm:F4}m/m " +
               $"ΔP={HeadLossTotalMca:F3}mca\n" +
               $"  Conformidade: {ComplianceNote} ({GetErrorCount()} erros, " +
               $"{Warnings.Count} alertas)";
    }
}
```

---

## 5. Cálculos de Dimensionamento

### 5.1 SizingCalculator — AF (Água Fria)

```csharp
namespace HidraulicoPlugin.Core.Services
{
    /// <summary>
    /// Realiza dimensionamento de trechos de água fria (NBR 5626).
    /// </summary>
    public static class ColdWaterSizingCalculator
    {
        /// <summary>
        /// Dimensiona um trecho de AF.
        /// </summary>
        public static SizingResult Calculate(PipeSegment segment, double pressureStartMca)
        {
            var result = new SizingResult
            {
                Id = $"sr_seg_{segment.Id}",
                ElementId = segment.Id,
                System = HydraulicSystem.ColdWater,
                Context = SizingContext.Segment,
                Method = CalculationMethod.ProbableFlow,
                CalculatedAt = DateTime.Now,

                // Inputs
                InputWeightSum = segment.AccumulatedWeightAF,
                InputLengthM = segment.LengthM,
                InputFittingsLengthM = segment.FittingsEquivalentLengthM,
                InputElevationDiffM = segment.GetElevationDifference(),
                InputMaterial = segment.Material,
                InputHazenC = GetHazenC(segment.Material),
                InputPressureStartMca = pressureStartMca,
                InputMinRequiredPressureMca = 0.5 // mínimo padrão
            };

            // 1. Vazão provável: Q = 0.3 × √ΣP
            result.CalculatedFlowRateLs = 0.3 * Math.Sqrt(result.InputWeightSum);

            // 2. DN teórico: fórmula invertida de Fair-Whipple-Hsiao
            //    D = (8.69e6 × Q^1.75 / J_max)^(1/4.75)
            //    J_max = 8% = 0.08 m/m (critério econômico)
            double Q_m3s = result.CalculatedFlowRateLs / 1000.0;
            double J_max = 0.08;
            result.CalculatedDiameterMm =
                Math.Pow((8.69e6 * Math.Pow(Q_m3s, 1.75)) / J_max, 1.0 / 4.75) * 1000;

            // 3. DN adotado (comercial)
            result.AdoptedDiameterMm = GetNextCommercialDN(
                (int)Math.Ceiling(result.CalculatedDiameterMm));
            result.AdoptedInternalDiameterMm = GetInternalDiameter(
                result.AdoptedDiameterMm, result.InputMaterial);

            // 4. Velocidade: V = Q / A
            double DI_m = result.AdoptedInternalDiameterMm / 1000.0;
            double area = Math.PI * Math.Pow(DI_m / 2.0, 2);
            result.CalculatedVelocityMs = area > 0 ? Q_m3s / area : 0;

            // 5. Perda de carga unitária (Hazen-Williams)
            double C = result.InputHazenC;
            double D = DI_m;
            result.HeadLossUnitaryMm = (D > 0 && C > 0)
                ? (10.643 * Math.Pow(Q_m3s, 1.85)) / (Math.Pow(C, 1.85) * Math.Pow(D, 4.87))
                : 0;

            // 6. Perda de carga total
            result.HeadLossTotalMca = result.HeadLossUnitaryMm * result.InputTotalLengthM;

            // 7. Pressão final
            //    P_fim = P_início - ΔP_perda + ΔZ (desnível positivo = a favor)
            result.PressureEndMca = pressureStartMca
                                  - result.HeadLossTotalMca
                                  + result.InputElevationDiffM;

            // 8. Utilização do DN
            double Q_max = CalculateMaxFlow(result.AdoptedDiameterMm, C);
            result.DiameterUtilizationPercent = Q_max > 0
                ? (result.CalculatedFlowRateLs / Q_max) * 100 : 0;

            // 9. Validar
            result.Validate();

            return result;
        }

        private static double GetHazenC(PipeMaterial material)
        {
            return material switch
            {
                PipeMaterial.PvcSoldavel => 140,
                PipeMaterial.PvcRoscavel => 140,
                PipeMaterial.Ppr => 140,
                PipeMaterial.Cpvc => 140,
                PipeMaterial.FerroFundido => 120,
                _ => 140
            };
        }

        private static int GetNextCommercialDN(int diameterMm)
        {
            int[] dns = { 20, 25, 32, 40, 50, 60, 75, 100, 150 };
            foreach (int dn in dns)
                if (dn >= diameterMm) return dn;
            return dns[^1];
        }

        private static double GetInternalDiameter(int nominalDN, PipeMaterial material)
        {
            // PVC soldável (mais comum)
            return nominalDN switch
            {
                20 => 17.0,
                25 => 21.6,
                32 => 27.8,
                40 => 35.2,
                50 => 44.0,
                60 => 53.0,
                75 => 66.6,
                100 => 97.0,
                150 => 146.0,
                _ => nominalDN * 0.85
            };
        }

        private static double CalculateMaxFlow(int dn, double hazenC)
        {
            // Q máxima com V = 3.0 m/s
            double DI_m = GetInternalDiameter(dn, PipeMaterial.PvcSoldavel) / 1000.0;
            double area = Math.PI * Math.Pow(DI_m / 2.0, 2);
            return area * 3.0 * 1000.0; // L/s
        }
    }
}
```

### 5.2 SizingCalculator — ES (Esgoto Sanitário)

```csharp
namespace HidraulicoPlugin.Core.Services
{
    /// <summary>
    /// Realiza dimensionamento de trechos de esgoto (NBR 8160).
    /// </summary>
    public static class SewerSizingCalculator
    {
        /// <summary>
        /// Dimensiona um trecho de ES.
        /// </summary>
        public static SizingResult Calculate(PipeSegment segment)
        {
            var result = new SizingResult
            {
                Id = $"sr_seg_{segment.Id}",
                ElementId = segment.Id,
                System = HydraulicSystem.Sewer,
                Context = SizingContext.Segment,
                Method = CalculationMethod.NormativeTable,
                CalculatedAt = DateTime.Now,

                // Inputs
                InputUHCSum = segment.AccumulatedUHC,
                InputLengthM = segment.LengthM,
                InputFittingsLengthM = segment.FittingsEquivalentLengthM,
                InputSlopePercent = segment.SlopePercent,
                InputMaterial = segment.Material
            };

            // 1. DN pela tabela UHC → DN (NBR 8160)
            result.AdoptedDiameterMm = GetDNFromUHC(
                result.InputUHCSum, segment.Type);

            result.CalculatedDiameterMm = result.AdoptedDiameterMm;
            result.AdoptedInternalDiameterMm = GetInternalDiameterES(
                result.AdoptedDiameterMm);

            // 2. Vazão (simplificada — Q ∝ √ΣUHC)
            result.CalculatedFlowRateLs = 0.3 * Math.Sqrt(result.InputUHCSum);

            // 3. Declividade
            double minSlope = result.AdoptedDiameterMm >= 100 ? 1.0 : 2.0;
            result.ActualSlopePercent = Math.Max(segment.SlopePercent, minSlope);

            // 4. Velocidade (Manning com n = 0.010 para PVC)
            double DI_m = result.AdoptedInternalDiameterMm / 1000.0;
            double R = DI_m / 4.0; // raio hidráulico (tubo cheio simplificado)
            double n = 0.010;
            double i = result.ActualSlopePercent / 100.0;
            result.CalculatedVelocityMs = (1.0 / n) * Math.Pow(R, 2.0 / 3.0) * Math.Pow(i, 0.5);

            // 5. Taxa de ocupação (simplificada)
            result.OccupancyRatePercent = GetOccupancyRate(
                result.InputUHCSum, result.AdoptedDiameterMm);

            // 6. Validar
            result.Validate();

            return result;
        }

        private static int GetDNFromUHC(int uhc, SegmentType type)
        {
            // Tabela simplificada NBR 8160
            if (type == SegmentType.DischargeBranch || type == SegmentType.SecondaryBranch)
            {
                if (uhc <= 3) return 40;
                if (uhc <= 6) return 50;
                if (uhc <= 10) return 75;
                if (uhc <= 20) return 75;
                return 100;
            }

            // Subcoletor / coletor
            if (uhc <= 6) return 50;
            if (uhc <= 20) return 75;
            if (uhc <= 80) return 100;
            if (uhc <= 300) return 150;
            return 150;
        }

        private static double GetInternalDiameterES(int dn)
        {
            return dn switch
            {
                40 => 37.0,
                50 => 47.0,
                75 => 72.0,
                100 => 97.0,
                150 => 146.0,
                _ => dn * 0.95
            };
        }

        private static double GetOccupancyRate(int uhc, int dn)
        {
            // Simplificação: % de ocupação baseada na relação UHC/capacidade
            double maxUHC = dn switch
            {
                40 => 4,
                50 => 8,
                75 => 24,
                100 => 80,
                150 => 300,
                _ => 100
            };
            return (uhc / maxUHC) * 75.0; // 75% = limite
        }
    }
}
```

---

## 6. Exemplo — Tabela de Dimensionamento (como apareceria em E12)

### 6.1 Rede AF — Banheiro social

| Trecho | ΣP | Q (L/s) | DN calc | DN adotado | V (m/s) | J (m/m) | ΔP (mca) | P_fim (mca) | Status |
|--------|-----|---------|---------|-----------|---------|---------|----------|-------------|--------|
| seg_af_001 (ramal→lav) | 0.3 | 0.164 | 11.2 | 20 | 0.72 | 0.0381 | 0.076 | 7.42 | ✅ |
| seg_af_002 (ramal→chu) | 0.3 | 0.164 | 11.2 | 20 | 0.72 | 0.0381 | 0.053 | 7.45 | ✅ |
| seg_af_003 (ramal→vaso) | 0.3 | 0.164 | 11.2 | 20 | 0.72 | 0.0381 | 0.046 | 7.45 | ✅ |
| seg_af_004 (coluna→ramal) | 0.9 | 0.285 | 14.1 | 20 | 1.25 | 0.1010 | 0.303 | 7.50 | ✅ |

### 6.2 Rede ES — Banheiro social

| Trecho | ΣUHC | DN tab | DN adotado | i (%) | V (m/s) | Ocup (%) | Status |
|--------|------|--------|-----------|-------|---------|---------|--------|
| seg_es_001 (lav→CX) | 2 | 40 | 40 | 2.0 | 0.65 | 37.5 | ✅ |
| seg_es_002 (chu→CX) | 2 | 40 | 40 | 2.0 | 0.65 | 37.5 | ✅ |
| seg_es_003 (ralo→CX) | 1 | 40 | 40 | 2.0 | 0.65 | 18.8 | ✅ |
| seg_es_004 (CX→sub) | 5 | 75 | 75 | 2.0 | 1.02 | 15.6 | ✅ |
| seg_es_005 (vaso→sub) | 6 | 100 | 100 | 1.0 | 0.75 | 5.6 | ✅ |
| seg_es_006 (sub→TQ) | 11 | 100 | 100 | 1.0 | 0.75 | 10.3 | ✅ |

---

## 7. JSON de Exemplo

```json
{
  "id": "sr_seg_seg_af_004",
  "element_id": "seg_af_004",
  "system": "ColdWater",
  "context": "Segment",
  "method": "ProbableFlow",
  "calculated_at": "2026-03-18T22:45:00-03:00",
  "version": 1,
  "input_weight_sum": 0.9,
  "input_uhc_sum": 0,
  "input_length_m": 2.50,
  "input_fittings_length_m": 0.50,
  "input_slope_percent": 0,
  "input_elevation_diff_m": 0.0,
  "input_material": "PvcSoldavel",
  "input_hazen_c": 140,
  "input_pressure_start_mca": 7.80,
  "input_min_required_pressure_mca": 0.50,
  "calculated_flow_rate_ls": 0.285,
  "calculated_diameter_mm": 14.1,
  "adopted_diameter_mm": 20,
  "adopted_internal_diameter_mm": 17.0,
  "calculated_velocity_ms": 1.25,
  "head_loss_unitary_mm": 0.1010,
  "head_loss_total_mca": 0.303,
  "pressure_end_mca": 7.50,
  "actual_slope_percent": 0,
  "occupancy_rate_percent": 0,
  "meets_pressure_requirement": true,
  "meets_velocity_limit": true,
  "meets_diameter_minimum": true,
  "meets_slope_requirement": true,
  "meets_occupancy_limit": true,
  "is_compliant": true,
  "compliance_note": "Aprovado",
  "pressure_margin_mca": 7.00,
  "velocity_margin_ms": 1.75,
  "diameter_utilization_percent": 41.5,
  "warnings": []
}
```

---

## 8. Resumo Visual

```
SizingResult
├── Identidade
│   ├── Id (string)
│   ├── ElementId (string → Segment/Riser/Network)
│   ├── System (HydraulicSystem)
│   ├── Context (SizingContext — 4 valores)
│   ├── Method (CalculationMethod — 5 métodos)
│   ├── CalculatedAt (DateTime)
│   └── Version (int)
├── Inputs
│   ├── InputWeightSum (ΣP)
│   ├── InputUHCSum (ΣUHC)
│   ├── InputLengthM / InputFittingsLengthM
│   ├── InputTotalLengthM (computed)
│   ├── InputSlopePercent
│   ├── InputElevationDiffM
│   ├── InputMaterial / InputHazenC
│   ├── InputPressureStartMca
│   └── InputMinRequiredPressureMca
├── Resultados Calculados
│   ├── CalculatedFlowRateLs (Q)
│   ├── CalculatedDiameterMm (DN teórico)
│   ├── AdoptedDiameterMm (DN comercial)
│   ├── AdoptedInternalDiameterMm (DI)
│   ├── CalculatedVelocityMs (V)
│   ├── HeadLossUnitaryMm (J)
│   ├── HeadLossTotalMca (ΔP)
│   ├── PressureEndMca (P_fim)
│   ├── ActualSlopePercent (i%)
│   └── OccupancyRatePercent (lâmina %)
├── Conformidade
│   ├── MeetsPressureRequirement (bool)
│   ├── MeetsVelocityLimit (bool)
│   ├── MeetsDiameterMinimum (bool)
│   ├── MeetsSlopeRequirement (bool)
│   ├── MeetsOccupancyLimit (bool)
│   ├── IsCompliant (bool — global)
│   └── ComplianceNote (string)
├── Margens
│   ├── PressureMarginMca
│   ├── VelocityMarginMs
│   └── DiameterUtilizationPercent
├── Warnings
│   └── List<SizingWarning>
│       ├── Level (Info|Warning|Error|Critical)
│       ├── Code (string)
│       ├── Message (string)
│       ├── ActualValue / LimitValue
│       └── NormReference (string)
└── Métodos
    ├── Validate()
    ├── CalculateSafetyMarginPercent()
    ├── IsOverSized()
    ├── GetWarnings(minLevel)
    ├── GetErrorCount()
    ├── GetDisplayName()
    └── ToString()
```
