# Modelo de Comunicação Plugin ↔ Dynamo — Contrato JSON

> Especificação completa do protocolo de comunicação entre o plugin C# e os scripts Dynamo, usando JSON como contrato de dados.

---

## 1. Estratégia Geral de Comunicação

### 1.1 Modelo adotado: Request/Response via arquivo JSON

```
┌──────────┐    escreve input.json    ┌────────────┐
│  Plugin   │───────────────────────→ │  Dynamo    │
│  (C#)     │                         │  Script    │
│           │←───────────────────────│  (.dyn)    │
└──────────┘    lê output.json        └────────────┘
```

### 1.2 Forma de transporte: Arquivo `.json` em diretório temporário

| Aspecto | Decisão | Justificativa |
|---------|---------|---------------|
| **Meio** | Arquivo JSON no filesystem | Funciona em todas as versões do Revit; debug simples (abrir arquivo e ler); sem dependência de memória compartilhada |
| **Diretório** | `%TEMP%/HidraulicoPlugin/` | Limpeza automática pelo SO; sem conflito com projeto |
| **Naming** | `{modulo}_input_{timestamp}.json` / `{modulo}_output_{timestamp}.json` | Único por execução; permite auditoria |
| **Encoding** | UTF-8 sem BOM | Compatível com Dynamo e C# |
| **Formato** | JSON indentado (2 espaços) | Legível para debug |

### 1.3 Fluxo completo

```
1. PLUGIN escreve {modulo}_input_{ts}.json
2. PLUGIN chama DynamoRevit.RunScript(path_do_dyn)
3. DYNAMO lê input.json via nó FileSystem.ReadText
4. DYNAMO executa operações
5. DYNAMO escreve {modulo}_output_{ts}.json via nó FileSystem.WriteText
6. PLUGIN lê output.json
7. PLUGIN valida resultado
8. PLUGIN deleta ambos os JSONs (cleanup)
```

### 1.4 Por que NÃO usar alternativas

| Alternativa | Por que não |
|------------|-------------|
| Memória compartilhada | Dynamo não expõe API de memória compartilhada |
| Named Pipes | Complexidade desnecessária para dev solo |
| Ambiente Variables | Limite de tamanho, não estruturado |
| Revit Extensible Storage | Persiste no modelo — não queremos isso |
| Shared Parameters | Limitado por tipo de elemento |

---

## 2. Estrutura do JSON de Entrada (Plugin → Dynamo)

### 2.1 Schema

```json
{
  "metadata": {
    "versao_contrato": "1.0",
    "plugin_versao": "0.5.0-alpha",
    "modulo": "string (id do módulo)",
    "timestamp": "ISO 8601",
    "request_id": "GUID"
  },
  "contexto": {
    "modelo_nome": "string",
    "nivel_ativo": "string",
    "niveis": ["string"],
    "unidade_comprimento": "meters"
  },
  "parametros": {
    "material": "string",
    "sistema": "string (ColdWater|Sewer|Ventilation)",
    "dn_padrao_mm": 0,
    "declividade": 0.0,
    "timeout_sec": 60
  },
  "instrucoes": [
    {
      "id": "string (id da instrução)",
      "acao": "string (create_pipe|insert_equipment|apply_slope|create_sheet)",
      "dados": {}
    }
  ]
}
```

### 2.2 Campos obrigatórios vs. opcionais

| Campo | Obrigatório | Tipo | Descrição |
|-------|------------|------|-----------|
| `metadata.versao_contrato` | ✅ | string | Versão do schema JSON |
| `metadata.plugin_versao` | ✅ | string | Versão do plugin (SemVer) |
| `metadata.modulo` | ✅ | string | ID do módulo chamador |
| `metadata.timestamp` | ✅ | string | ISO 8601 com timezone |
| `metadata.request_id` | ✅ | string | GUID único por execução |
| `contexto.modelo_nome` | ✅ | string | Nome do arquivo .rvt |
| `contexto.nivel_ativo` | ⬜ | string | Level ativo (se relevante) |
| `contexto.niveis` | ⬜ | string[] | Lista de Levels |
| `contexto.unidade_comprimento` | ✅ | string | Sempre "meters" |
| `parametros.*` | Depende do módulo | variado | Parâmetros da operação |
| `instrucoes` | ✅ | array | Lista de ações a executar |
| `instrucoes[].id` | ✅ | string | ID único da instrução |
| `instrucoes[].acao` | ✅ | string | Tipo de ação |
| `instrucoes[].dados` | ✅ | object | Dados específicos da ação |

### 2.3 Tipos de ação (`instrucoes[].acao`)

| Ação | Módulo | Dados esperados |
|------|--------|----------------|
| `insert_equipment` | M04 | equipment_type, position, level, family_name |
| `create_pipe` | M07/M08 | start, end, diameter_mm, system |
| `create_fitting` | M07/M08 | type (tee/elbow/valve), position, diameter_mm |
| `apply_slope` | M09 | element_id, slope, direction |
| `adjust_elevation` | M09 | element_id, new_z |
| `create_schedule` | M12 | schedule_type, system_filter, fields |
| `create_sheet` | M13 | views, title_block, number |

---

## 3. Estrutura do JSON de Saída (Dynamo → Plugin)

### 3.1 Schema

```json
{
  "metadata": {
    "versao_contrato": "1.0",
    "request_id": "GUID (mesmo do input)",
    "timestamp": "ISO 8601",
    "duracao_ms": 0
  },
  "status": "success | partial | failure",
  "resumo": {
    "total_instrucoes": 0,
    "executadas": 0,
    "falhas": 0,
    "ignoradas": 0
  },
  "resultados": [
    {
      "instrucao_id": "string (id da instrução original)",
      "status": "success | failure | skipped",
      "element_ids": [0],
      "dados": {}
    }
  ],
  "erros": [
    {
      "instrucao_id": "string",
      "codigo": "string",
      "mensagem": "string",
      "nivel": "critical | medium | light"
    }
  ],
  "alertas": [
    {
      "instrucao_id": "string",
      "mensagem": "string"
    }
  ],
  "logs": [
    {
      "timestamp": "ISO 8601",
      "nivel": "info | warning | error",
      "mensagem": "string"
    }
  ]
}
```

### 3.2 Campos obrigatórios

| Campo | Obrigatório | Descrição |
|-------|------------|-----------|
| `metadata.versao_contrato` | ✅ | Deve ser compatível com input |
| `metadata.request_id` | ✅ | Mesmo GUID do input (rastreabilidade) |
| `metadata.timestamp` | ✅ | Quando a execução terminou |
| `metadata.duracao_ms` | ✅ | Tempo total de execução |
| `status` | ✅ | Status geral da execução |
| `resumo` | ✅ | Contagem de resultados |
| `resultados` | ✅ | Ao menos array vazio |
| `erros` | ✅ | Ao menos array vazio |

### 3.3 Valores de `status`

| Status | Significado | Ação do plugin |
|--------|-----------|----------------|
| `success` | Todas as instruções executadas com sucesso | Validar elementos criados, avançar |
| `partial` | Algumas instruções falharam | Validar o que foi criado, alertar usuário |
| `failure` | Nenhuma instrução executada ou erro fatal | Log Crítico, fallback para Revit API |

---

## 4. Contrato de Dados

### 4.1 Tipos de dados permitidos no JSON

| Tipo JSON | Uso | Exemplo |
|-----------|-----|---------|
| `string` | Nomes, IDs, enums | `"Banheiro"`, `"ColdWater"` |
| `number` | Coordenadas, diâmetros, valores | `25`, `3.048`, `0.02` |
| `boolean` | Flags | `true`, `false` |
| `array` | Listas | `[12345, 12346]` |
| `object` | Dados compostos | `{"x": 1.0, "y": 2.0, "z": 3.0}` |
| `null` | Ausência de valor | `null` |

### 4.2 Convenções de nomenclatura no JSON

| Regra | Exemplo |
|-------|---------|
| snake_case para todas as chaves | `element_ids`, `versao_contrato` |
| Sufixo de unidade obrigatório | `diameter_mm`, `slope_pct`, `length_m`, `pressure_mca` |
| Português para termos normativos | `declividade`, `vazao_ls` |
| Inglês para termos técnicos genéricos | `status`, `timestamp`, `element_ids` |
| IDs como inteiros (ElementId do Revit) | `12345` |
| Coordenadas sempre em metros | `"x": 3.048` (não em feet) |

### 4.3 Estrutura de coordenadas

```json
{
  "position": {
    "x": 3.048,
    "y": 5.120,
    "z": 0.000
  }
}
```

**Sempre em metros.** Conversão ft→m é feita pelo plugin ANTES de escrever o JSON.

### 4.4 Estrutura de elemento referenciado

```json
{
  "element_id": 12345,
  "element_type": "Pipe",
  "system": "ColdWater"
}
```

---

## 5. Controle de Execução

### 5.1 Como o plugin chama o Dynamo

```csharp
public class DynamoExecutorAdapter : IDynamoExecutor
{
    private readonly string _scriptsFolder;
    private readonly string _tempFolder;

    public DynamoResult Execute(string scriptName, string inputJson)
    {
        // 1. Gerar paths
        string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss_fff");
        string requestId = Guid.NewGuid().ToString();
        string inputPath = Path.Combine(_tempFolder, $"{scriptName}_input_{timestamp}.json");
        string outputPath = Path.Combine(_tempFolder, $"{scriptName}_output_{timestamp}.json");

        // 2. Escrever input
        File.WriteAllText(inputPath, inputJson, Encoding.UTF8);

        // 3. Chamar Dynamo
        string dynPath = Path.Combine(_scriptsFolder, $"{scriptName}.dyn");
        // DynamoRevit.RunScript(dynPath) ou via journal file

        // 4. Aguardar output (polling)
        var result = WaitForOutput(outputPath, timeoutSec: 60);

        // 5. Cleanup
        TryDelete(inputPath);
        TryDelete(outputPath);

        return result;
    }

    private DynamoResult WaitForOutput(string outputPath, int timeoutSec)
    {
        var deadline = DateTime.Now.AddSeconds(timeoutSec);

        while (DateTime.Now < deadline)
        {
            if (File.Exists(outputPath))
            {
                Thread.Sleep(500); // esperar escrita completar
                string json = File.ReadAllText(outputPath, Encoding.UTF8);
                return DynamoResult.Parse(json);
            }
            Thread.Sleep(1000); // polling a cada 1s
        }

        return DynamoResult.Timeout(timeoutSec);
    }
}
```

### 5.2 Timeout

| Parâmetro | Valor padrão | Configurável |
|-----------|-------------|-------------|
| Timeout por script | 60 segundos | Sim, via `parametros.timeout_sec` |
| Polling interval | 1 segundo | Fixo |
| Espera pós-detecção de arquivo | 500ms | Fixo |

### 5.3 Ação em caso de timeout

```
SE timeout:
  1. Log Crítico: "Script {nome} excedeu timeout de {s}s"
  2. Verificar se Dynamo criou elementos parciais
  3. Se criou parciais → status = "partial", alertar usuário
  4. Se não criou nada → status = "failure", fallback para Revit API
  5. Deletar input.json (cleanup)
  6. Não deletar output.json (pode aparecer depois — cleanup na próxima execução)
```

### 5.4 Reexecução

```
Reexecução permitida quando:
  - status = "partial" e usuário solicita
  - status = "failure" por timeout (pode ser temporário)

Reexecução proibida quando:
  - status = "failure" por erro de dados (input inválido)
  - Mesmo input geraria mesmo erro

PROCEDIMENTO de reexecução:
  1. Gerar novo request_id
  2. Filtrar instruções já executadas com sucesso
  3. Enviar apenas instruções pendentes
  4. Merge de resultados parciais
```

---

## 6. Tratamento de Erros

### 6.1 Tipos de erro

| Código | Tipo | Exemplo | Nível |
|--------|------|---------|-------|
| `DYN-EXE-001` | Execução | Script não encontrado | critical |
| `DYN-EXE-002` | Execução | Timeout | critical |
| `DYN-EXE-003` | Execução | Dynamo crash | critical |
| `DYN-DAT-001` | Dados | JSON de input malformado | critical |
| `DYN-DAT-002` | Dados | Campo obrigatório ausente | critical |
| `DYN-DAT-003` | Dados | Tipo de dado incorreto | critical |
| `DYN-DAT-004` | Dados | element_id não encontrado no modelo | medium |
| `DYN-GEO-001` | Geométrica | Pipe com comprimento zero | medium |
| `DYN-GEO-002` | Geométrica | Colisão com elemento existente | light |
| `DYN-GEO-003` | Geométrica | Fitting não conectou | medium |
| `DYN-GEO-004` | Geométrica | Família não encontrada | critical |
| `DYN-GEO-005` | Geométrica | Level não encontrado | critical |

### 6.2 Estrutura do erro no JSON

```json
{
  "instrucao_id": "pipe_003",
  "codigo": "DYN-GEO-003",
  "mensagem": "Fitting não conectou no endpoint (5.2, 3.1, 0.0)",
  "nivel": "medium",
  "detalhes": {
    "position": {"x": 5.2, "y": 3.1, "z": 0.0},
    "element_id": 45678,
    "tipo_fitting": "tee"
  }
}
```

### 6.3 Como o plugin trata cada nível

| Nível | Ação do plugin |
|-------|---------------|
| `critical` | Log Crítico. Marcar instrução como falha. Se > 30% falhas → status geral = failure. |
| `medium` | Log Médio. Destacar elemento no modelo. Pedir decisão do usuário. |
| `light` | Log Leve. Registrar para auditoria. Não parar execução. |

---

## 7. Logs

### 7.1 Estrutura

```json
{
  "timestamp": "2026-03-18T20:30:15.123",
  "nivel": "info",
  "mensagem": "Criando pipe DN25 de (1.0, 2.0, 3.0) até (4.0, 2.0, 3.0)"
}
```

### 7.2 Níveis

| Nível JSON | Mapeamento no plugin | Cor |
|-----------|---------------------|-----|
| `info` | ValidationLevel.Info | #9E9E9E |
| `warning` | ValidationLevel.Light | #2196F3 |
| `error` | ValidationLevel.Medium ou Critical | #FF9800 / #F44336 |

### 7.3 Como o plugin consome logs do Dynamo

```csharp
// Após ler output.json:
foreach (var log in dynamoResult.Logs)
{
    var nivel = log.Nivel switch
    {
        "info" => ValidationLevel.Info,
        "warning" => ValidationLevel.Light,
        "error" => ValidationLevel.Medium,
        _ => ValidationLevel.Info
    };

    _logService.Log(nivel, $"[Dynamo/{scriptName}] {log.Mensagem}");
}
```

---

## 8. Versionamento do JSON

### 8.1 Regra de versão

```
versao_contrato: "MAJOR.MINOR"

MAJOR incrementa quando:
  - Campo obrigatório adicionado ou removido
  - Tipo de campo muda
  - Estrutura incompatível

MINOR incrementa quando:
  - Campo opcional adicionado
  - Novo valor de enum adicionado
  - Melhoria sem quebra
```

### 8.2 Compatibilidade

| Plugin v | Contrato v | Dynamo contrato v | Compatível |
|----------|-----------|-------------------|-----------|
| 0.5.0 | 1.0 | 1.0 | ✅ |
| 0.6.0 | 1.1 | 1.0 | ✅ (minor compat) |
| 0.6.0 | 1.1 | 1.1 | ✅ |
| 1.0.0 | 2.0 | 1.0 | ❌ (major break) |

### 8.3 Validação de versão

```csharp
public DynamoResult ValidateOutput(string json)
{
    var output = JsonConvert.DeserializeObject<DynamoOutput>(json);

    // Verificar compatibilidade de versão
    var inputMajor = int.Parse(_currentVersion.Split('.')[0]);
    var outputMajor = int.Parse(output.Metadata.VersaoContrato.Split('.')[0]);

    if (outputMajor != inputMajor)
    {
        return DynamoResult.Error("DYN-DAT-001",
            $"Versão incompatível: plugin={_currentVersion}, dynamo={output.Metadata.VersaoContrato}");
    }

    // Verificar request_id
    if (output.Metadata.RequestId != _currentRequestId)
    {
        return DynamoResult.Error("DYN-DAT-002",
            "Request ID não confere — output de outra execução");
    }

    return DynamoResult.Valid(output);
}
```

---

## 9. Segurança e Validação

### 9.1 Validação do input (antes de escrever)

```csharp
public void ValidateInput(DynamoInput input)
{
    // Campos obrigatórios
    if (string.IsNullOrEmpty(input.Metadata.Modulo))
        throw new ArgumentException("Módulo obrigatório");

    if (string.IsNullOrEmpty(input.Metadata.RequestId))
        throw new ArgumentException("RequestId obrigatório");

    if (!input.Instrucoes.Any())
        throw new ArgumentException("Nenhuma instrução para executar");

    // Instruções válidas
    foreach (var instr in input.Instrucoes)
    {
        if (string.IsNullOrEmpty(instr.Id))
            throw new ArgumentException("Instrução sem ID");

        if (string.IsNullOrEmpty(instr.Acao))
            throw new ArgumentException($"Instrução {instr.Id} sem ação");
    }

    // Coordenadas em metros (sanity check)
    foreach (var instr in input.Instrucoes.Where(i => i.Dados.ContainsKey("position")))
    {
        var pos = instr.Dados["position"];
        if (Math.Abs(pos.X) > 1000 || Math.Abs(pos.Y) > 1000)
            _log.Log(ValidationLevel.Light,
                $"Coordenada muito grande em {instr.Id} — verificar unidade (esperado: metros)");
    }
}
```

### 9.2 Validação do output (depois de ler)

```
VERIFICAÇÕES OBRIGATÓRIAS:

1. Arquivo existe e não está vazio
2. JSON parseia sem erro
3. versao_contrato é compatível (mesmo MAJOR)
4. request_id confere com o input enviado
5. status é um dos valores válidos (success|partial|failure)
6. Se status=success: resultados não pode estar vazio
7. Se status=failure: erros não pode estar vazio
8. Cada resultado.instrucao_id existe no input original
```

### 9.3 Tratamento de dados inconsistentes

| Cenário | Ação |
|---------|------|
| Output JSON vazio | Tratar como failure com código DYN-EXE-003 |
| Output JSON malformado | Tratar como failure; log do JSON raw para debug |
| element_id no resultado não existe no modelo | Alerta Médio; provavelmente Transaction foi revertida |
| Mais resultados que instruções | Ignorar extras; log de Alerta |
| Menos resultados que instruções | Instruções faltantes = skipped |

---

## 10. Exemplos Completos

### 10.1 Exemplo: Inserção de equipamentos (M04)

#### Input (Plugin → Dynamo)

```json
{
  "metadata": {
    "versao_contrato": "1.0",
    "plugin_versao": "0.4.0-alpha",
    "modulo": "M04_insercao_equipamentos",
    "timestamp": "2026-03-18T20:30:00-03:00",
    "request_id": "a1b2c3d4-e5f6-7890-abcd-ef1234567890"
  },
  "contexto": {
    "modelo_nome": "Residencia_Teste_01.rvt",
    "nivel_ativo": "Térreo",
    "niveis": ["Térreo", "Superior"],
    "unidade_comprimento": "meters"
  },
  "parametros": {
    "material": "pvc_soldavel",
    "familia_vaso": "M_Toilet-Commercial-Wall Mounted-3D",
    "familia_lavatorio": "M_Lavatory-Round-3D",
    "familia_chuveiro": "M_Shower Head-Wall Mounted-3D",
    "timeout_sec": 60
  },
  "instrucoes": [
    {
      "id": "equip_001",
      "acao": "insert_equipment",
      "dados": {
        "equipment_type": "vaso_caixa_acoplada",
        "familia": "M_Toilet-Commercial-Wall Mounted-3D",
        "position": {"x": 2.50, "y": 1.20, "z": 0.00},
        "rotation_deg": 180,
        "level": "Térreo",
        "room_name": "Banheiro Social"
      }
    },
    {
      "id": "equip_002",
      "acao": "insert_equipment",
      "dados": {
        "equipment_type": "lavatorio",
        "familia": "M_Lavatory-Round-3D",
        "position": {"x": 1.50, "y": 2.80, "z": 0.60},
        "rotation_deg": 0,
        "level": "Térreo",
        "room_name": "Banheiro Social"
      }
    },
    {
      "id": "equip_003",
      "acao": "insert_equipment",
      "dados": {
        "equipment_type": "chuveiro",
        "familia": "M_Shower Head-Wall Mounted-3D",
        "position": {"x": 0.60, "y": 0.60, "z": 2.00},
        "rotation_deg": 0,
        "level": "Térreo",
        "room_name": "Banheiro Social"
      }
    }
  ]
}
```

#### Output (Dynamo → Plugin)

```json
{
  "metadata": {
    "versao_contrato": "1.0",
    "request_id": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
    "timestamp": "2026-03-18T20:30:08-03:00",
    "duracao_ms": 7850
  },
  "status": "partial",
  "resumo": {
    "total_instrucoes": 3,
    "executadas": 2,
    "falhas": 1,
    "ignoradas": 0
  },
  "resultados": [
    {
      "instrucao_id": "equip_001",
      "status": "success",
      "element_ids": [456789],
      "dados": {
        "familia_usada": "M_Toilet-Commercial-Wall Mounted-3D",
        "has_connectors": true,
        "connector_count": 2
      }
    },
    {
      "instrucao_id": "equip_002",
      "status": "success",
      "element_ids": [456790],
      "dados": {
        "familia_usada": "M_Lavatory-Round-3D",
        "has_connectors": true,
        "connector_count": 2
      }
    },
    {
      "instrucao_id": "equip_003",
      "status": "failure",
      "element_ids": [],
      "dados": {}
    }
  ],
  "erros": [
    {
      "instrucao_id": "equip_003",
      "codigo": "DYN-GEO-004",
      "mensagem": "Família 'M_Shower Head-Wall Mounted-3D' não encontrada no modelo",
      "nivel": "critical",
      "detalhes": {
        "familia_solicitada": "M_Shower Head-Wall Mounted-3D",
        "familias_disponiveis": ["M_Shower Stall-Square-3D"]
      }
    }
  ],
  "alertas": [
    {
      "instrucao_id": "equip_001",
      "mensagem": "Vaso posicionado a 15cm da parede — verificar acesso para manutenção"
    }
  ],
  "logs": [
    {"timestamp": "2026-03-18T20:30:01", "nivel": "info", "mensagem": "Iniciando inserção de 3 equipamentos"},
    {"timestamp": "2026-03-18T20:30:03", "nivel": "info", "mensagem": "equip_001: Vaso inserido com sucesso (ID: 456789)"},
    {"timestamp": "2026-03-18T20:30:05", "nivel": "info", "mensagem": "equip_002: Lavatório inserido com sucesso (ID: 456790)"},
    {"timestamp": "2026-03-18T20:30:07", "nivel": "error", "mensagem": "equip_003: Família não encontrada no modelo"},
    {"timestamp": "2026-03-18T20:30:08", "nivel": "info", "mensagem": "Execução finalizada: 2/3 sucesso"}
  ]
}
```

### 10.2 Exemplo: Geração de rede de esgoto (M08)

#### Input

```json
{
  "metadata": {
    "versao_contrato": "1.0",
    "plugin_versao": "0.6.0-beta",
    "modulo": "M08_rede_esgoto",
    "timestamp": "2026-04-10T14:20:00-03:00",
    "request_id": "b2c3d4e5-f6a7-8901-bcde-f12345678901"
  },
  "contexto": {
    "modelo_nome": "Residencia_Teste_02.rvt",
    "nivel_ativo": "Térreo",
    "niveis": ["Térreo", "Superior"],
    "unidade_comprimento": "meters"
  },
  "parametros": {
    "material": "pvc_esgoto",
    "sistema": "Sewer",
    "declividade_padrao": 0.02,
    "inserir_cx_sifonada": true,
    "inserir_cx_gordura": true,
    "timeout_sec": 90
  },
  "instrucoes": [
    {
      "id": "pipe_es_001",
      "acao": "create_pipe",
      "dados": {
        "start": {"x": 2.50, "y": 1.20, "z": 0.20},
        "end": {"x": 2.50, "y": 3.00, "z": 0.16},
        "diameter_mm": 100,
        "system": "Sewer",
        "level": "Térreo",
        "descricao": "Ramal de descarga - vaso"
      }
    },
    {
      "id": "pipe_es_002",
      "acao": "create_pipe",
      "dados": {
        "start": {"x": 1.50, "y": 2.80, "z": 0.45},
        "end": {"x": 1.50, "y": 3.00, "z": 0.44},
        "diameter_mm": 40,
        "system": "Sewer",
        "level": "Térreo",
        "descricao": "Ramal de descarga - lavatório → CX sifonada"
      }
    },
    {
      "id": "fitting_001",
      "acao": "create_fitting",
      "dados": {
        "type": "tee",
        "position": {"x": 2.50, "y": 3.00, "z": 0.16},
        "diameter_mm": 100,
        "connect_to": ["pipe_es_001"]
      }
    }
  ]
}
```

#### Output

```json
{
  "metadata": {
    "versao_contrato": "1.0",
    "request_id": "b2c3d4e5-f6a7-8901-bcde-f12345678901",
    "timestamp": "2026-04-10T14:20:12-03:00",
    "duracao_ms": 11500
  },
  "status": "success",
  "resumo": {
    "total_instrucoes": 3,
    "executadas": 3,
    "falhas": 0,
    "ignoradas": 0
  },
  "resultados": [
    {
      "instrucao_id": "pipe_es_001",
      "status": "success",
      "element_ids": [567001],
      "dados": {"comprimento_m": 1.80, "declividade_aplicada": 0.022}
    },
    {
      "instrucao_id": "pipe_es_002",
      "status": "success",
      "element_ids": [567002],
      "dados": {"comprimento_m": 0.20, "declividade_aplicada": 0.050}
    },
    {
      "instrucao_id": "fitting_001",
      "status": "success",
      "element_ids": [567003],
      "dados": {"tipo": "tee", "connected": true}
    }
  ],
  "erros": [],
  "alertas": [],
  "logs": [
    {"timestamp": "2026-04-10T14:20:01", "nivel": "info", "mensagem": "Iniciando criação de 3 elementos ES"},
    {"timestamp": "2026-04-10T14:20:12", "nivel": "info", "mensagem": "Todos os 3 elementos criados com sucesso"}
  ]
}
```

---

## 11. Limitações e Riscos

### 11.1 Limitações do Dynamo

| Limitação | Impacto | Mitigação |
|-----------|---------|-----------|
| **Sem retorno programático** | Plugin não recebe callback quando script termina | Polling do arquivo output + timeout |
| **Sem tratamento de exceção nativo** | Script pode falhar sem gerar output | Validar delta de elementos no modelo pós-execução |
| **Limite de performance** | Scripts lentos em modelos grandes (> 100 elementos) | Dividir em batches de 50 instruções |
| **Formato .dyn instável entre versões** | Script pode não abrir em versão diferente | Fixar versão do Dynamo; testar antes de release |
| **Node deprecation** | Nós podem ser removidos em atualizações | Usar apenas nós estáveis e documentados |
| **Sem acesso a clipboard/memória** | Comunicação apenas via filesystem | Aceitar e usar JSON via arquivo |

### 11.2 Problemas comuns na troca de dados

| Problema | Causa | Prevenção |
|---------|-------|-----------|
| **JSON corrompido** | Escrita parcial (crash durante write) | Escrever em arquivo temp, depois renomear (atomic) |
| **Encoding errado** | BOM, charset misturado | Sempre UTF-8 sem BOM |
| **Unidades misturadas** | Plugin em metros, Dynamo em feet | Plugin SEMPRE envia metros; Dynamo converte se necessário |
| **ElementId inválido** | Elemento criado mas Transaction reverteu | Verificar existência do ElementId pós-output |
| **Output de outra execução** | Timestamp/requestId não batem | Verificar request_id no output |
| **Arquivo locked** | Dynamo ainda está escrevendo | Sleep de 500ms após detectar arquivo |

### 11.3 Estratégias de mitigação

```
1. ESCRITA ATÔMICA
   Escrever em arquivo .tmp, depois renomear para .json
   → Evita arquivo parcial

2. VALIDAÇÃO PÓS-EXECUÇÃO
   Depois de ler output.json, verificar no modelo:
   - Elementos com ElementId retornado existem?
   - Connectors estão conectados?
   - DNs estão corretos?
   → Não confiar cegamente no output do Dynamo

3. FALLBACK PARA REVIT API
   Se Dynamo falha em > 30% das instruções:
   - Tentar via Revit API direta
   - Se API também falha: marcar para resolução manual
   → Sistema nunca trava completamente

4. BATCH DE INSTRUÇÕES
   Se > 50 instruções:
   - Dividir em batches de 50
   - Executar sequencialmente
   - Merge de resultados
   → Evitar timeout e problemas de memória

5. CLEANUP DE ARQUIVOS
   Na inicialização do plugin:
   - Limpar todos os JSONs do diretório temp
   - Evitar acúmulo de arquivos de execuções anteriores
```

### 11.4 Fluxo de fallback

```
TENTAR execução via Dynamo:
  SE sucesso (100% instruções) → prosseguir
  SE parcial (< 30% falhas) → validar, alertar, prosseguir
  SE parcial (≥ 30% falhas) → FALLBACK

FALLBACK — Revit API direta:
  PARA cada instrução falhada no Dynamo:
    TENTAR executar via Revit API (PipeCreator, etc.)
    SE sucesso → registrar como "fallback_api"
    SE falha → marcar como "manual" para usuário

RELATÓRIO FINAL:
  X instruções via Dynamo
  Y instruções via fallback API
  Z instruções para resolução manual
```

---

## 12. Resumo do Protocolo

```
┌─────────────────────────────────────────────────────┐
│ PLUGIN (C#)                                         │
│                                                     │
│ 1. Montar DynamoInput com instruções                │
│ 2. Validar input (campos obrigatórios)              │
│ 3. Serializar para JSON (UTF-8, 2 espaços)          │
│ 4. Escrever em %TEMP%/HidraulicoPlugin/             │
│ 5. Chamar DynamoRevit.RunScript()                   │
│ 6. Polling para output.json (1s interval, 60s max)  │
│ 7. Ler e deserializar output                        │
│ 8. Validar: request_id, versão, status              │
│ 9. Processar resultados por instrução               │
│ 10. Importar logs do Dynamo para LogService          │
│ 11. Validar elementos criados no modelo              │
│ 12. Cleanup de arquivos temp                         │
│ 13. Retornar DynamoResult para o Orchestrator       │
└─────────────────────────────────────────────────────┘
```
