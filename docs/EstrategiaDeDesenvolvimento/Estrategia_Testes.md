# Estratégia de Testes do Sistema — Plugin Hidráulico Revit

> Estratégia completa de testes unitários, integração e validação real para garantir qualidade, conformidade normativa e estabilidade do plugin.

---

## 1. Estratégia Geral de Testes

### 1.1 Pirâmide de testes

```
                    ╱╲
                   ╱  ╲
                  ╱ VR ╲           Validação Real (Revit)
                 ╱──────╲          5–10 cenários completos
                ╱        ╲         Execução manual
               ╱ INTEGR.  ╲       Integração (módulos + Revit API)
              ╱─────────────╲      20–30 testes
             ╱               ╲     Execução semi-auto
            ╱   UNITÁRIOS     ╲    Unitários (Core)
           ╱───────────────────╲   100–200+ testes
          ╱                     ╲  Execução automática
         ╱─────────────────────── ╲
```

### 1.2 Distribuição de esforço

| Tipo | % do esforço | Quantidade estimada | Execução | Frequência |
|------|-------------|-------------------|----------|-----------|
| **Unitários** | 60% | 150–200 testes | Automática (CI) | A cada commit |
| **Integração** | 25% | 30–40 testes | Semi-automática | A cada PR / merge em develop |
| **Validação Real** | 15% | 8–12 cenários | Manual no Revit | A cada release |

### 1.3 Justificativa técnica

| Decisão | Justificativa |
|---------|--------------|
| Priorizar unitários | Lógica normativa e cálculos são o coração do plugin. Erros aqui propagam para tudo. Testes rápidos (< 1s cada). |
| Integração focada | Revit API exige processo Revit rodando. Custoso. Testar apenas interações críticas. |
| Validação real limitada | Depende de ambiente gráfico. Custoso em tempo. Reservar para milestones. |
| Core sem Revit | Separar lógica de negócio do Revit permite testar 60%+ do código sem abrir o Revit. |

### 1.4 Framework e ferramentas

| Ferramenta | Uso |
|-----------|-----|
| **xUnit** | Framework de testes (unitários e integração) |
| **FluentAssertions** | Assertions legíveis |
| **Moq** | Mocking de interfaces |
| **RevitTestFramework** (Dynamo) | Testes dentro do Revit |
| **NUnit** (alternativa) | Se RevitTestFramework exigir |

---

## 2. Testes Unitários (Core)

### 2.1 O que deve ser testado

| Categoria | Exemplos | Prioridade |
|-----------|---------|-----------|
| **Cálculos hidráulicos** | Vazão, perda de carga, velocidade, pressão | 🔴 Alta |
| **Regras normativas** | DN mínimo, declividade, ventilação, UHC | 🔴 Alta |
| **Classificação de ambientes** | Matching de nomes, cálculo de confiança | 🔴 Alta |
| **Seleção de diâmetro** | Algoritmo de escolha por velocidade e tabela | 🔴 Alta |
| **Validações** | Regras de bloqueio, níveis de erro | 🟡 Média |
| **Configuração** | Leitura de JSON, parsing, defaults | 🟡 Média |
| **Conversão de unidades** | ft→m, ft²→m², mca→kPa | 🟡 Média |
| **Mapeamento ambiente→aparelhos** | Pontos obrigatórios/opcionais | 🟡 Média |
| **Totalização** | Soma de pesos, soma de UHCs | 🟡 Média |

### 2.2 O que NÃO deve ser testado (unitariamente)

| Exclusão | Motivo | Coberto por |
|----------|--------|-------------|
| Criação de elementos no Revit | Requer Revit rodando | Teste de integração |
| Posicionamento visual | Resultado geométrico | Validação real |
| Scripts Dynamo | Ambiente Dynamo | Teste de Dynamo |
| Interface WPF | XAML + binding | Teste manual |
| Comunicação com API nativa | Requires Revit context | Teste de integração |

### 2.3 Testes obrigatórios — Cálculo de vazão

```csharp
public class FlowRateCalculatorTests
{
    private readonly FlowRateCalculator _calculator = new();

    [Theory]
    [InlineData(0.3, 0.30, 0.164)]   // 1 vaso caixa acoplada
    [InlineData(1.3, 0.30, 0.342)]   // banheiro completo (vaso+lav+ch+ralo)
    [InlineData(0.8, 0.30, 0.268)]   // lavabo (vaso+lav)
    [InlineData(0.7, 0.30, 0.251)]   // cozinha (pia)
    [InlineData(2.0, 0.30, 0.424)]   // referência
    [InlineData(5.0, 0.30, 0.671)]   // residência média
    [InlineData(10.0, 0.30, 0.949)]  // residência grande
    public void CalculateFlowRate_ValidWeights_ReturnsCorrectValue(
        double sumWeights, double coefC, double expectedLs)
    {
        var result = _calculator.CalculateFlowRate(sumWeights, coefC);
        result.Should().BeApproximately(expectedLs, 0.001);
    }

    [Fact]
    public void CalculateFlowRate_ZeroWeights_ReturnsZero()
    {
        _calculator.CalculateFlowRate(0, 0.30).Should().Be(0);
    }

    [Fact]
    public void CalculateFlowRate_NegativeWeights_ThrowsException()
    {
        Action act = () => _calculator.CalculateFlowRate(-1, 0.30);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void CalculateFlowRate_WithFlushValve_AddsBaseFlow()
    {
        // Q = 1.70 + 0.30 * √(demais)
        double demais = 1.0;
        double expected = 1.70 + 0.30 * Math.Sqrt(demais);
        
        var result = _calculator.CalculateFlowRateWithFlushValve(demais);
        result.Should().BeApproximately(expected, 0.001);
    }
}
```

### 2.4 Testes obrigatórios — Seleção de diâmetro

```csharp
public class PipeSizingServiceTests
{
    private readonly PipeSizingService _sizer;

    public PipeSizingServiceTests()
    {
        var dataProvider = new NormativeDataProvider("referencia_normativa.json");
        _sizer = new PipeSizingService(dataProvider);
    }

    [Theory]
    [InlineData(0.164, 20)]   // vazão baixa → DN 20
    [InlineData(0.342, 20)]   // banheiro → DN 20
    [InlineData(0.671, 25)]   // residência média → DN 25
    [InlineData(0.949, 25)]   // residência grande → DN 25
    [InlineData(1.70, 50)]    // válvula de descarga → DN 50
    public void SelectDiameter_ByVelocity_ReturnsSmallestValid(
        double flowRateLs, int expectedDnMm)
    {
        int dn = _sizer.SelectDiameterByVelocity(flowRateLs, maxVelocityMs: 3.0);
        dn.Should().Be(expectedDnMm);
    }

    [Theory]
    [InlineData("vaso_caixa_acoplada", 20)]
    [InlineData("vaso_valvula_descarga", 50)]
    [InlineData("lavatorio", 20)]
    [InlineData("tanque", 25)]
    [InlineData("maquina_lavar_roupa", 25)]
    public void GetMinimumDiameter_ColdWater_ReturnsNormativeValue(
        string equipmentId, int expectedDnMm)
    {
        int dn = _sizer.GetMinimumDiameterColdWater(equipmentId);
        dn.Should().Be(expectedDnMm);
    }

    [Fact]
    public void SelectDiameter_NeverBelowMinimum()
    {
        // Mesmo que velocidade permita DN 15, mínimo é 20
        int dn = _sizer.SelectDiameterByVelocity(0.01, maxVelocityMs: 3.0);
        dn.Should().BeGreaterThanOrEqualTo(20);
    }

    [Fact]
    public void SelectDiameter_ReturnsCommercialValue()
    {
        int dn = _sizer.SelectDiameterByVelocity(0.5);
        var commercialDns = new[] { 20, 25, 32, 40, 50, 60, 75, 85, 110 };
        commercialDns.Should().Contain(dn);
    }
}
```

### 2.5 Testes obrigatórios — Perda de carga

```csharp
public class HeadLossCalculatorTests
{
    private readonly HeadLossCalculator _calc = new();

    [Theory]
    [InlineData(0.342, 17.0, 0.0899)]   // Q=0.342 L/s, D_int=17mm → J≈0.09 m/m
    [InlineData(0.671, 21.6, 0.0773)]   // Q=0.671 L/s, D_int=21.6mm
    public void CalculateUnitaryLoss_FWH_ReturnsCorrectValue(
        double flowRateLs, double internalDiameterMm, double expectedJApprox)
    {
        double j = _calc.CalculateUnitaryLossFWH(flowRateLs, internalDiameterMm);
        j.Should().BeApproximately(expectedJApprox, expectedJApprox * 0.10); // ±10%
    }

    [Theory]
    [InlineData(0.0899, 3.0, 1.20, 0.3237)]  // J * L * K
    public void CalculateSegmentLoss_ReturnsCorrectValue(
        double unitaryLoss, double lengthM, double kFactor, double expectedLossM)
    {
        double loss = _calc.CalculateSegmentLoss(unitaryLoss, lengthM, kFactor);
        loss.Should().BeApproximately(expectedLossM, 0.001);
    }
}
```

### 2.6 Testes obrigatórios — Pressão

```csharp
public class PressureVerificationTests
{
    [Theory]
    [InlineData(6.0, 2.0, 0.5, 3.5, true)]    // P_est=4.0, ΔH=0.5 → P_din=3.5 → OK
    [InlineData(6.0, 4.5, 1.0, 0.5, false)]    // P_est=1.5, ΔH=1.0 → P_din=0.5 → FALHA
    [InlineData(6.0, 2.8, 0.1, 3.1, true)]     // Marginal mas OK
    [InlineData(6.0, 3.0, 0.1, 2.9, false)]    // Marginal e FALHA
    public void VerifyPressure_ReturnsCorrectResult(
        double reservoirHeightM, double pointHeightM, 
        double totalHeadLossM, double expectedPressureMca, bool expectedOk)
    {
        var service = new PressureVerificationService(minPressureMca: 3.0);
        var result = service.Verify(reservoirHeightM, pointHeightM, totalHeadLossM);
        
        result.AvailablePressureMca.Should().BeApproximately(expectedPressureMca, 0.01);
        result.IsAdequate.Should().Be(expectedOk);
    }
}
```

### 2.7 Testes obrigatórios — Declividade

```csharp
public class SlopeRulesTests
{
    [Theory]
    [InlineData(40, 0.020)]
    [InlineData(50, 0.020)]
    [InlineData(75, 0.020)]
    [InlineData(100, 0.010)]
    [InlineData(150, 0.0065)]
    [InlineData(200, 0.005)]
    public void GetMinimumSlope_ByDiameter_ReturnsNormativeValue(
        int diameterMm, double expectedSlope)
    {
        var rules = new SlopeRules();
        rules.GetMinimumSlope(diameterMm).Should().Be(expectedSlope);
    }

    [Theory]
    [InlineData(100, 0.008, true, "OK")]         // 0.8% < 1% → inválido
    [InlineData(100, 0.010, true, "OK")]          // exatamente no limite
    [InlineData(100, 0.015, true, "OK")]          // dentro do OK
    [InlineData(100, 0.060, true, "Leve")]        // > 5% → alerta
    [InlineData(100, 0.090, true, "Medio")]       // > 8% → alerta médio
    public void ValidateSlope_ReturnsCorrectLevel(
        int diameterMm, double slope, bool isValid, string expectedLevel)
    {
        var result = new SlopeRules().Validate(diameterMm, slope);
        // Testes detalhados por nível
    }

    [Theory]
    [InlineData(2.0, 0.02, 0.04)]  // 2m, 2% → 4cm
    [InlineData(3.0, 0.01, 0.03)]  // 3m, 1% → 3cm
    [InlineData(8.0, 0.01, 0.08)]  // 8m, 1% → 8cm
    public void CalculateElevationDrop_ReturnsCorrectValue(
        double lengthM, double slope, double expectedDropM)
    {
        var drop = SlopeRules.CalculateElevationDrop(lengthM, slope);
        drop.Should().BeApproximately(expectedDropM, 0.0001);
    }

    [Fact]
    public void ValidateSlope_AgainstGravity_ReturnsCritical()
    {
        // Z_final >= Z_inicial → contra gravidade
        var result = new SlopeRules().ValidateElevations(
            zStartM: 1.0, zEndM: 1.05, lengthM: 2.0, diameterMm: 100);
        result.Level.Should().Be(ValidationLevel.Critical);
    }
}
```

### 2.8 Testes obrigatórios — Ventilação

```csharp
public class VentilationRulesTests
{
    [Theory]
    [InlineData(40, 40)]
    [InlineData(50, 40)]
    [InlineData(75, 50)]
    [InlineData(100, 75)]
    [InlineData(150, 100)]
    public void GetColumnDiameter_ByRiserDn_ReturnsCorrect(
        int riserDnMm, int expectedVentDnMm)
    {
        VentilationRules.GetColumnDiameter(riserDnMm).Should().Be(expectedVentDnMm);
    }

    [Theory]
    [InlineData(40, 40)]
    [InlineData(50, 40)]
    [InlineData(75, 40)]
    [InlineData(100, 50)]
    public void GetBranchDiameter_ByDischargeDn_ReturnsCorrect(
        int dischargeDnMm, int expectedVentDnMm)
    {
        VentilationRules.GetBranchDiameter(dischargeDnMm).Should().Be(expectedVentDnMm);
    }

    [Theory]
    [InlineData(40, 1.0)]
    [InlineData(50, 1.2)]
    [InlineData(75, 1.8)]
    [InlineData(100, 2.4)]
    public void GetMaxDistanceWithoutVent_ByDn_ReturnsCorrect(
        int branchDnMm, double expectedMaxDistM)
    {
        VentilationRules.GetMaxDistanceWithoutVent(branchDnMm)
            .Should().Be(expectedMaxDistM);
    }

    [Theory]
    [InlineData(1, false)]   // 1 pav → não obrigatória
    [InlineData(2, false)]   // 2 pav → não obrigatória
    [InlineData(3, true)]    // 3 pav → obrigatória
    [InlineData(5, true)]    // 5 pav → obrigatória
    public void IsSecondaryVentRequired_ByFloors_ReturnsCorrect(
        int floors, bool expected)
    {
        VentilationRules.IsSecondaryRequired(floors).Should().Be(expected);
    }
}
```

### 2.9 Testes obrigatórios — Classificação

```csharp
public class RoomClassifierTests
{
    private readonly RoomClassifier _classifier = new();

    [Theory]
    [InlineData("Banheiro", RoomType.Bathroom, 1.0)]
    [InlineData("Banheiro Social", RoomType.Bathroom, 0.95)]
    [InlineData("BWC", RoomType.Bathroom, 0.80)]
    [InlineData("WC", RoomType.Bathroom, 0.75)]
    [InlineData("Banho", RoomType.Bathroom, 0.80)]
    [InlineData("Lavabo", RoomType.Lavatory, 1.0)]
    [InlineData("Cozinha", RoomType.Kitchen, 1.0)]
    [InlineData("Cozinha Gourmet", RoomType.GourmetKitchen, 0.95)]
    [InlineData("Lavanderia", RoomType.Laundry, 1.0)]
    [InlineData("Área de Serviço", RoomType.ServiceArea, 0.95)]
    [InlineData("A. Serviço", RoomType.ServiceArea, 0.80)]
    public void Classify_KnownNames_ReturnsCorrectType(
        string name, RoomType expectedType, double minConfidence)
    {
        var result = _classifier.Classify(name);
        result.RoomType.Should().Be(expectedType);
        result.Confidence.Should().BeGreaterThanOrEqualTo(minConfidence);
    }

    [Theory]
    [InlineData("Sala de Estar", RoomType.NonHydraulic)]
    [InlineData("Quarto", RoomType.NonHydraulic)]
    [InlineData("Corredor", RoomType.NonHydraulic)]
    [InlineData("Garagem", RoomType.NonHydraulic)]
    public void Classify_NonHydraulic_ExcludesFromPipeline(
        string name, RoomType expectedType)
    {
        _classifier.Classify(name).RoomType.Should().Be(expectedType);
    }

    [Theory]
    [InlineData("BANHEIRO")]         // maiúsculo
    [InlineData("banheiro")]         // minúsculo
    [InlineData("  Banheiro  ")]     // espaços
    [InlineData("Banheiro 01")]      // com número
    public void Classify_Variations_StillRecognizes(string name)
    {
        _classifier.Classify(name).RoomType.Should().Be(RoomType.Bathroom);
    }
}
```

### 2.10 Testes — Conversão de unidades

```csharp
public class UnitConversionTests
{
    [Theory]
    [InlineData(1.0, 0.0929)]      // 1 ft² ≈ 0.0929 m²
    [InlineData(100.0, 9.2903)]    // 100 ft²
    [InlineData(0, 0)]
    public void SqFeetToSqMeters_ReturnsCorrect(double sqFeet, double expectedSqM)
    {
        UnitConversionHelper.SqFeetToSqMeters(sqFeet)
            .Should().BeApproximately(expectedSqM, 0.001);
    }

    [Theory]
    [InlineData(1.0, 0.3048)]     // 1 ft = 0.3048m
    [InlineData(10.0, 3.048)]
    public void FeetToMeters_ReturnsCorrect(double feet, double expectedM)
    {
        UnitConversionHelper.FeetToMeters(feet)
            .Should().BeApproximately(expectedM, 0.0001);
    }
}
```

### 2.11 Cobertura mínima

| Camada | Cobertura mínima | Justificativa |
|--------|-----------------|---------------|
| `Core.Sizing` | **95%** | Cálculos são inegociáveis |
| `Core.Sizing.Rules` | **95%** | Regras normativas obrigatórias |
| `Core.Classification` | **90%** | NLP pode ter edge cases |
| `Core.Config` | **80%** | Parsing e defaults |
| `Core.Models` | **50%** | POCOs simples, pouca lógica |
| `Core.Diagnostics` | **70%** | LogService básico |
| **Total Core** | **≥ 85%** | — |

---

## 3. Testes de Integração

### 3.1 O que validar

| Categoria | Teste | Ambiente |
|-----------|-------|---------|
| **Leitura do modelo** | RoomReader extrai Rooms do documento | Revit API |
| **Criação de elementos** | PipeCreator gera Pipe com DN e Level corretos | Revit API |
| **Inserção de fixtures** | EquipmentInsertionService insere FamilyInstance | Revit API |
| **Conectividade** | Pipes conectam via Connectors | Revit API |
| **Transactions** | TransactionHelper abre/commita/reverte corretamente | Revit API |
| **Leitura de JSON** | NormativeDataProvider carrega referencia_normativa.json | Filesystem |
| **Comunicação Plugin↔Dynamo** | Plugin escreve JSON, Dynamo lê e executa | Dynamo |

### 3.2 Framework: RevitTestFramework

```
RevitTestFramework (do Dynamo team) permite:
  - Executar testes dentro do processo do Revit
  - Acessar Document, UIDocument
  - Criar e modificar elementos em Transaction
  - Rodar xUnit/NUnit dentro do Revit

Configuração:
  1. Instalar RevitTestFramework via NuGet
  2. Criar projeto HidraulicoPlugin.IntegrationTests
  3. Configurar para Revit 2024 (.NET 4.8)
  4. Referenciar Revit API assemblies
```

### 3.3 Testes de integração obrigatórios

```csharp
// Dentro do Revit (via RevitTestFramework)

[RevitTest]
public void RoomReader_ReadsAllValidRooms(Document doc)
{
    var reader = new RoomReader();
    var rooms = reader.GetValidRooms(doc);
    
    rooms.Should().NotBeEmpty();
    rooms.Should().AllSatisfy(r => r.AreaSqM.Should().BeGreaterThan(0));
    rooms.Should().AllSatisfy(r => r.LocationPoint.Should().NotBeNull());
}

[RevitTest]
public void PipeCreator_CreatesPipeWithCorrectDn(Document doc)
{
    using var tx = new Transaction(doc, "Test");
    tx.Start();
    
    var creator = new PipeCreator();
    var pipe = creator.CreatePipe(doc, 
        startXyz: new XYZ(0, 0, 0), 
        endXyz: new XYZ(10, 0, 0),
        diameterMm: 50,
        systemType: HydraulicSystem.ColdWater);
    
    pipe.Should().NotBeNull();
    pipe.Diameter.Should().BeApproximately(50.0 / 304.8, 0.001); // mm → feet
    
    tx.RollBack(); // limpar
}

[RevitTest]
public void EquipmentInsertion_CreatesInstanceWithConnectors(Document doc)
{
    using var tx = new Transaction(doc, "Test");
    tx.Start();
    
    var service = new EquipmentInsertionService();
    var instance = service.InsertEquipment(doc,
        equipmentType: EquipmentType.Sink,
        locationXyz: new XYZ(5, 5, 0),
        levelId: GetTestLevelId(doc));
    
    instance.Should().NotBeNull();
    var connectors = instance.MEPModel?.ConnectorManager?.Connectors;
    connectors.Should().NotBeNull();
    connectors.Size.Should().BeGreaterThan(0);
    
    tx.RollBack();
}

[RevitTest]
public void TransactionHelper_RollsBackOnException(Document doc)
{
    int elementsBefore = CountPipes(doc);
    
    Action act = () => TransactionHelper.Execute(doc, "Test", d =>
    {
        new PipeCreator().CreatePipe(d, new XYZ(0,0,0), new XYZ(1,0,0), 25);
        throw new InvalidOperationException("Simulated failure");
    });
    
    act.Should().Throw<InvalidOperationException>();
    CountPipes(doc).Should().Be(elementsBefore); // rollback funcionou
}
```

### 3.4 Como simular vs. usar ambiente real

| Estratégia | Quando | Exemplo |
|-----------|--------|---------|
| **Mock (Moq)** | Interface do Revit API em testes unitários | `Mock<IRoomReader>` |
| **Fake Document** | Testes que precisam de Document mas sem elementos | RevitTestFramework com doc vazio |
| **Modelo de teste** | Testes que precisam de Rooms, Levels, Families | `Teste_01_Basico.rvt` |
| **Transaction com RollBack** | Testes que criam/modificam elementos | Sempre reverter ao final |

---

## 4. Testes no Revit (Validação Real)

### 4.1 Cenários de teste

| # | Cenário | Modelo | Complexidade | Valida |
|---|---------|--------|-------------|--------|
| VR-01 | Casa térrea simples | `Teste_01_Basico.rvt` | Baixa | Pipeline básico completo |
| VR-02 | Sobrado 2 pavimentos | `Teste_02_Sobrado.rvt` | Média | Prumadas multi-nível |
| VR-03 | Rooms sem nome padrão | `Teste_03_NomesIrregulares.rvt` | Média | Classificação robusta |
| VR-04 | Modelo com equip. existentes | `Teste_04_ComEquipamentos.rvt` | Média | Validação de existentes |
| VR-05 | Modelo sem Rooms | `Teste_05_SemRooms.rvt` | Baixa | Erro Crítico e bloqueio |
| VR-06 | 3 pavimentos | `Teste_06_3Pavimentos.rvt` | Alta | Ventilação secundária |
| VR-07 | Configuração não padrão | Qualquer | Média | Override de parâmetros |
| VR-08 | Dimensionamento extremo | `Teste_08_GrandePorte.rvt` | Alta | Pressão em ponto distante |

### 4.2 Checklist por cenário

```
PARA cada cenário (VR-NN):

□ E01 Detecção: Rooms lidos = N esperado
□ E02 Classificação: tipos corretos, confiança ≥ 0.70
□ E03 Pontos: aparelhos obrigatórios listados
□ E04 Inserção: taxa ≥ 80% (retangulares ≥ 90%)
□ E05 Validação: existentes classificados corretamente
□ E06 Prumadas: visíveis, DNs corretos
□ E07 Rede AF: todos os pontos conectados
□ E08 Rede ES: CX sifonada, CX gordura, vaso independente
□ E09 Inclinações: 0 trechos contra gravidade
□ E10 Sistemas: 3 PipingSystems criados
□ E11 Dimensionamento: vazão ±1%, pressão ≥ 3.0 mca
□ E12 Tabelas: 4 schedules com dados corretos
□ E13 Pranchas: views filtradas, numeração sequencial
□ Log: 0 erros Críticos não tratados
□ Tempo total: ≤ 5 min para modelo básico
```

### 4.3 Validação de geometria

| Verificação | Como validar | Ferramenta |
|-------------|-------------|-----------|
| Pipes conectados | Verificar connector.IsConnected | Revit API |
| Pipes dentro do building | BoundingBox vs. edificação | Revit API |
| Sem colisão com estrutura | Interference Check | Revit nativo |
| Fittings nos pontos de mudança | Verificar tees/curvas automáticos | Visual + API |
| Equipamentos dentro do Room | LocationPoint dentro do Room boundary | Revit API |

### 4.4 Validação de conformidade normativa

| Regra | Verificação | Critério |
|-------|------------|---------|
| DN vaso ≥ 100mm ES | Filtrar Pipes do vaso, checar DN | 100% |
| DN nunca diminui | Percorrer rede de montante a jusante | 100% |
| Declividade ≥ mínima | Verificar Z endpoints de cada Pipe horizontal | 100% |
| CX sifonada em banheiro | FilteredElementCollector por categoria | 100% |
| CX gordura em cozinha | FilteredElementCollector por família | 100% |
| Pressão ≥ 3.0 mca | Cálculo por caminho crítico | 100% |
| Velocidade ≤ 3.0 m/s | Cálculo por trecho | 100% |

---

## 5. Testes de Regras Normativas

### 5.1 Estratégia: Tabela de decisão

Cada regra normativa é testada com 3 categorias de input:

| Categoria | Descrição | Resultado esperado |
|-----------|-----------|-------------------|
| **Válido** | Dentro dos limites normativos | Pass, sem erro |
| **Limite** | Exatamente no valor limite | Pass (aceitar limite) |
| **Inválido** | Fora dos limites | Fail com código de erro correto |

### 5.2 Matriz de testes normativos

| Regra | Válido | Limite | Inválido |
|-------|--------|--------|----------|
| Pressão mín 3.0 mca | P=5.0 → OK | P=3.0 → OK | P=2.9 → Crítico |
| Pressão máx 40 mca | P=30 → OK | P=40.0 → OK | P=41 → Médio |
| Velocidade máx 3.0 m/s | V=2.0 → OK | V=3.0 → OK | V=3.1 → Crítico |
| DN vaso ES ≥ 100 | DN=100 → OK | DN=100 → OK | DN=75 → Crítico |
| DN nunca diminui | 50→100 → OK | 100→100 → OK | 100→75 → Crítico |
| Decliv DN100 ≥ 1% | 1.5% → OK | 1.0% → OK | 0.8% → Crítico |
| Decliv máx 5% | 3% → OK | 5% → Leve | 6% → Leve |
| Decliv máx 8% | 5% → OK | 8% → Médio | 9% → Médio |
| Contra gravidade | Z↓ → OK | Z=Z → Crítico | Z↑ → Crítico |
| DN vent coluna ≥ 2/3 TQ | 75/100 → OK | 67/100 → OK | 50/100 → Médio |
| Terminal ≥ 30cm cobertura | 50cm → OK | 30cm → OK | 20cm → Médio |
| Terminal ≥ 4m janela | 5m → OK | 4m → OK | 3m → Médio |

### 5.3 Cobertura

**Meta: 100% das regras do `referencia_normativa.json` > `validacoes.regras` com teste.**

Cada `VAL-NNN` deve ter pelo menos 1 teste válido + 1 teste inválido.

---

## 6. Testes de Regressão

### 6.1 Quando executar

| Trigger | Testes executados |
|---------|-----------------|
| Commit em feature/* | Unitários do módulo |
| PR para develop | Todos os unitários + integração básica |
| Merge em develop | Todos os unitários + integração |
| Branch release/* | Tudo + validação real em 2+ modelos |
| Hotfix | Unitários + teste específico do fix |

### 6.2 Como garantir não regressão

```
1. Suite de testes unitários completa (~150+)
   → Executa em < 30s
   → Qualquer falha = PR rejeitado

2. Suite de testes de integração (~30)
   → Executa em < 5 min (com Revit)
   → Qualquer falha Crítica = PR rejeitado

3. Modelos de referência "congelados"
   → Modelos .rvt com resultado esperado documentado
   → Comparar resultado do plugin vs. esperado

4. Testes de fumaça (smoke tests)
   → Pipeline E01→E13 executa sem crash em modelo básico
   → Tempo total < 5 min
```

### 6.3 Testes de regressão por módulo

```
Quando módulo M{NN} é alterado:
  1. Executar TODOS os testes unitários de M{NN}
  2. Executar testes de integração que envolvem M{NN}
  3. Executar testes de módulos que DEPENDEM de M{NN}
     (conforme grafo de dependências)
  4. SE M{NN} é M07, M08, M09 ou M11:
     Executar smoke test completo (alta criticidade)
```

---

## 7. Testes com Dynamo

### 7.1 Como validar scripts .dyn

| Aspecto | Método | Resultado esperado |
|---------|--------|-------------------|
| Script abre sem erro | Abrir no Dynamo Player | Sem erros de nó |
| Inputs aceitos | Fornecer JSON de entrada padrão | Nós recebem dados |
| Outputs gerados | Executar e verificar output | ElementIds no JSON de saída |
| Elementos criados | Contar no modelo antes/depois | Delta > 0 |
| Sem crash | Executar com modelo válido | Sem exceção |

### 7.2 Checklist por script

```
PARA cada script {NN}_{Nome}.dyn:

□ Abre no Dynamo sem erro de nó (sem marcadores "?" ou vermelhos)
□ Aceita JSON de input gerado pelo plugin
□ Executa sem crash no modelo de teste
□ Gera output JSON com status
□ Elementos criados estão na categoria correta
□ Elementos criados têm connectors (se Pipe/Fitting)
□ Tempo de execução ≤ 30s (modelo básico)
□ Funciona na versão do Dynamo alvo
□ Funciona com pacotes listados em packages.json
```

### 7.3 Versionamento de testes

```
Para cada versão do script (ex: 07_GenerateColdWaterNetwork.dyn v2):
  1. Criar JSON de input de referência
  2. Documentar output esperado
  3. Testar ANTES de merge
  4. Registrar no CHANGELOG_DYNAMO.md
```

---

## 8. Ambiente de Testes

### 8.1 Configuração do ambiente

| Componente | Versão | Nota |
|-----------|--------|------|
| Revit | 2024 (principal) | Testar também 2022, 2023 antes de release |
| .NET Framework | 4.8 | Para Revit 2022–2024 |
| Dynamo | 2.x (embutido no Revit) | Compatível com Revit 2024 |
| xUnit | 2.6+ | Framework de testes |
| FluentAssertions | 6.x | Assertions |
| Moq | 4.x | Mocking |
| RevitTestFramework | Último compatível | Testes dentro do Revit |

### 8.2 Ambiente isolado

```
REGRAS DO AMBIENTE DE TESTE:

1. Modelo de teste NUNCA é o modelo real do cliente
2. Sempre usar cópia do modelo (não original)
3. Testes de integração usam Transaction.RollBack()
4. Testes de validação real usam modelo descartável
5. Nenhum teste grava em disco permanente (exceto logs)
6. Variáveis de ambiente separadas (TEST_MODE=true)
```

---

## 9. Dados de Teste

### 9.1 Modelos de teste

| Modelo | Rooms | Levels | Equipamentos | Uso |
|--------|-------|--------|-------------|-----|
| `Teste_01_Basico.rvt` | 8 (1 banh, 1 coz, 1 lav, 1 lavand, 4 não-hid) | 1 | Nenhum | Fluxo básico |
| `Teste_02_Sobrado.rvt` | 15 (3 banh, 1 lavabo, 1 coz, 1 lavand, 9 não-hid) | 2 | Nenhum | Multi-nível |
| `Teste_03_NomesIrregulares.rvt` | 10 (nomes: BWC, WC, A.Serv, etc.) | 1 | Nenhum | Classificação |
| `Teste_04_ComEquipamentos.rvt` | 8 | 1 | 10 fixtures MEP | Validação existentes |
| `Teste_05_SemRooms.rvt` | 0 | 1 | Nenhum | Erro Crítico |
| `Teste_06_3Pavimentos.rvt` | 20 | 3 | Nenhum | Ventilação |
| `Teste_07_AmplasAreas.rvt` | 8 (salas grandes) | 1 | Nenhum | Performance |
| `Teste_08_GrandePorte.rvt` | 30 | 3 | 20 fixtures | Stress test |

### 9.2 Dataset de classificação

```json
// tests/data/nomes_ambientes_corpus.json
{
  "testes_classificacao": [
    { "input": "Banheiro",         "expected": "Bathroom",     "min_confidence": 1.0 },
    { "input": "Banheiro Social",  "expected": "Bathroom",     "min_confidence": 0.95 },
    { "input": "BWC",              "expected": "Bathroom",     "min_confidence": 0.75 },
    { "input": "WC",               "expected": "Bathroom",     "min_confidence": 0.70 },
    { "input": "Banho Suíte",      "expected": "Bathroom",     "min_confidence": 0.80 },
    { "input": "Banheiro 01",      "expected": "Bathroom",     "min_confidence": 0.90 },
    { "input": "BANHEIRO",         "expected": "Bathroom",     "min_confidence": 0.95 },
    { "input": "banheiro",         "expected": "Bathroom",     "min_confidence": 0.95 },
    { "input": "Lavabo",           "expected": "Lavatory",     "min_confidence": 1.0 },
    { "input": "Cozinha",          "expected": "Kitchen",      "min_confidence": 1.0 },
    { "input": "Cozinha Gourmet",  "expected": "GourmetKitchen","min_confidence": 0.90 },
    { "input": "Lavanderia",       "expected": "Laundry",      "min_confidence": 1.0 },
    { "input": "Área de Serviço",  "expected": "ServiceArea",  "min_confidence": 0.90 },
    { "input": "A. Serviço",       "expected": "ServiceArea",  "min_confidence": 0.75 },
    { "input": "Sala de Estar",    "expected": "NonHydraulic", "min_confidence": 0.90 },
    { "input": "Quarto",           "expected": "NonHydraulic", "min_confidence": 0.90 },
    { "input": "Corredor",         "expected": "NonHydraulic", "min_confidence": 0.90 }
  ]
}
```

### 9.3 Dados com erro proposital

| Cenário de erro | Modelo | Erro esperado |
|----------------|--------|---------------|
| Room sem Location | Teste_03 (Room não colocado) | Leve: Room ignorado |
| 0 Rooms | Teste_05 | Crítico: pipeline bloqueado |
| Família MEP ausente | Qualquer (sem familia carregada) | Crítico: inserção falha |
| Reservatório Z=0 | Config: `altura_reservatorio=0` | Pressão negativa → Crítico |
| DN incorreto forçado | Config: `dn_minimo_ramal_vaso=50` | Violação normativa |

---

## 10. Métricas de Qualidade

### 10.1 Métricas obrigatórias

| Métrica | Alvo | Mínimo aceitável | Medido quando |
|---------|------|-----------------|--------------|
| Cobertura unitários (Core) | 90% | **85%** | A cada PR |
| Cobertura unitários (Total) | 80% | **70%** | A cada PR |
| Testes unitários passando | 100% | **100%** | A cada commit |
| Testes integração passando | 100% | **95%** | A cada merge em develop |
| Tempo total unitários | < 15s | **< 30s** | A cada execução |
| Tempo total integração | < 3min | **< 5min** | A cada execução |
| Tempo pipeline completo (modelo básico) | < 3min | **< 5min** | A cada release |
| Precisão de vazão (vs. manual) | ±0.5% | **±1%** | Testes normativos |
| Precisão de pressão (vs. manual) | ±2% | **±5%** | Testes normativos |

### 10.2 Taxa de falha aceitável

| Fase | Taxa aceitável | Ação se exceder |
|------|---------------|----------------|
| Alpha | 5% dos testes de integração | Corrigir antes de avançar |
| Beta | 1% dos testes de integração | Corrigir antes de RC |
| RC | 0% | Corrigir antes de produção |
| Produção | 0% | Hotfix imediato |

---

## 11. Automação de Testes

### 11.1 O que será automatizado

| Tipo | Automação | Ferramenta |
|------|-----------|-----------|
| Unitários (Core) | ✅ Totalmente automatizado | xUnit + CI |
| Compilação | ✅ Totalmente automatizado | MSBuild + CI |
| Lint / formatação | ✅ Automatizado | EditorConfig + analyzers |
| Integração (Revit) | ⚠️ Semi-automático | RevitTestFramework (requer Revit aberto) |
| Validação real | ❌ Manual | Humano no Revit |
| Testes Dynamo | ❌ Manual | Humano no Dynamo Player |

### 11.2 Pipeline de CI

```
TRIGGER: push em qualquer branch

1. BUILD
   → dotnet build HidraulicoPlugin.sln
   → Se falha: ❌ pipeline falha

2. UNIT TESTS
   → dotnet test HidraulicoPlugin.Tests --filter "Category=Unit"
   → Se qualquer falha: ❌ pipeline falha

3. COVERAGE
   → dotnet test --collect:"XPlat Code Coverage"
   → Verificar cobertura ≥ 70% (total), ≥ 85% (Core)
   → Se abaixo: ⚠️ warning (não bloqueia)

4. LINT
   → dotnet format --verify-no-changes
   → Se falha: ⚠️ warning

RESULTADO: ✅ ou ❌ no PR
```

### 11.3 Quando rodar cada tipo

| Evento | Unitários | Integração | Validação Real |
|--------|-----------|------------|---------------|
| Commit em feature/* | ✅ | — | — |
| PR para develop | ✅ | ✅ (se possível) | — |
| Merge em develop | ✅ | ✅ | — |
| Branch release/* | ✅ | ✅ | ✅ (2+ modelos) |
| Tag de produção | ✅ | ✅ | ✅ (todos modelos) |

---

## 12. Critérios de Aprovação

### 12.1 Funcionalidade pronta para merge em develop

```
□ Código compila sem erros
□ Todos os testes unitários passam (100%)
□ Testes unitários do módulo criados (cobertura ≥ 85% Core)
□ Código segue convenções de nomenclatura
□ Commits seguem Conventional Commits
□ XML docs em métodos públicos
□ Funcionalidade testada localmente no Revit (se aplicável)
□ Critérios de validação da etapa atendidos
```

### 12.2 Funcionalidade pronta para release

```
□ Todos os critérios de merge atendidos
□ Testes de integração passam (100%)
□ Validação real em ≥ 2 modelos
□ 0 erros Críticos
□ Erros Médios resolvidos ou aceitos com justificativa
□ CHANGELOG atualizado
□ Versão atualizada em AssemblyInfo
□ Smoke test completo (E01→E13, ≤ 5 min)
```

### 12.3 Pronto para produção (1.0.0)

```
□ Todos os critérios de release atendidos
□ Validação real em ≥ 5 modelos (incluindo complexos)
□ 0 erros Críticos nos últimos 15 RC builds
□ 0 erros Médios não resolvidos
□ ΣUHC e vazões conferem com planilha de referência (±1%)
□ Performance: pipeline < 5 min em modelo médio
□ Documentação do usuário completa
□ Testado em Revit 2022, 2023, 2024
```

---

## 13. Riscos e Limitações

### 13.1 Limitações de testar dentro do Revit

| Limitação | Impacto | Mitigação |
|-----------|---------|-----------|
| Revit precisa estar aberto | Testes de integração não rodam em CI headless | Separar unitários (CI) de integração (local) |
| Transaction necessária para criar elementos | Teste lento (~0.5s por Transaction) | Agrupar operações em 1 Transaction |
| UI thread obrigatória | Testes com UI não rodam em background | Não testar UI automaticamente |
| Modelo pode corromper | Teste falho pode deixar elementos lixo | Sempre usar RollBack |
| Famílias precisam estar carregadas | Teste falha se família ausente | Verificar famílias no setup do teste |
| Licença do Revit | Máquina de teste precisa de licença | Usar licença educacional ou de desenvolvimento |

### 13.2 Dificuldades com Dynamo

| Dificuldade | Impacto | Mitigação |
|------------|---------|-----------|
| .dyn não é testável por xUnit | Sem cobertura automática | Testar manualmente; validar output via JSON |
| Versões de pacotes | Script pode quebrar com update de pacote | Fixar versões em packages.json |
| .dyn como binário | Diff impossível, merge impossível | 1 branch por script, sem merge automático |
| Dynamo Player pode ignorar erros | Script "termina" mas não fez nada | Validar delta de elementos pós-execução |
| Compatibilidade entre versões do Dynamo | Script de Dynamo 2.12 pode não rodar em 2.10 | Testar em versão alvo antes de merge |

### 13.3 Dependência de ambiente gráfico

| Dependência | Impacto | Mitigação |
|------------|---------|-----------|
| Revit é aplicação desktop | Não tem modo headless oficial | Aceitar testes de integração como semi-automáticos |
| Renderização de vistas | Não testável automaticamente | Validação real = visual pelo humano |
| Interação com UI do plugin | Binding WPF não é testável sem UI | Testar ViewModel com mock, não a View |
| Revit pode travar | Teste perde contexto | Timeout por teste (30s unitário, 60s integração) |
| Resolução de tela | Pode afetar posicionamento de janela | Não depender de posição absoluta nos testes |
