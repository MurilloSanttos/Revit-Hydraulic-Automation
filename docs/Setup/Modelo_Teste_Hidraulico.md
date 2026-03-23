# Guia de Criação — Modelo de Teste Hidráulico no Revit 2026

> Instruções passo a passo para criar um modelo residencial completo, pronto para validar todas as etapas do plugin de automação hidráulica.

---

## 📋 Índice

- [Criar Projeto](#1-criar-projeto)
- [Configurar Níveis](#2-configurar-níveis)
- [Modelar Paredes](#3-modelar-paredes)
- [Inserir Portas e Janelas](#4-inserir-portas-e-janelas)
- [Criar Pisos e Cobertura](#5-criar-pisos-e-cobertura)
- [Criar Rooms](#6-criar-rooms)
- [Ativar e Criar Spaces MEP](#7-ativar-e-criar-spaces-mep)
- [Inserir Equipamentos Hidráulicos](#8-inserir-equipamentos-hidráulicos)
- [Configurar Vistas](#9-configurar-vistas)
- [Validação Final](#10-validação-final)

---

## 1. Criar Projeto

### 1.1 Novo projeto

1. Abra o **Revit 2026**.
2. **File** → **New** → **Project**.
3. Template: **Mechanical Template** (`Default Mechanical Template.rte`).
   - Se não disponível, use o **Architectural Template** e habilite MEP depois.
4. **Project Name**: `Modelo_Teste_Hidraulico`.
5. Clique **OK**.

### 1.2 Informações do projeto

1. **Manage** → **Project Information**.
2. Preencha:

   | Campo | Valor |
   |-------|-------|
   | **Project Name** | Modelo Teste Hidráulico |
   | **Project Number** | TESTE-001 |
   | **Client Name** | Desenvolvimento Plugin |
   | **Building Name** | Residência Unifamiliar |
   | **Author** | Murillo Santtos |

3. **OK**.

> ✅ **Checkpoint**: Projeto criado com template MEP.

---

## 2. Configurar Níveis

### 2.1 Abrir vista de elevação

1. No **Project Browser**, expanda **Elevations (Building Elevation)**.
2. Clique duplo em **South** (ou qualquer elevação).

### 2.2 Configurar níveis

Edite os níveis existentes e crie novos conforme necessário:

| Nível | Nome | Elevação | Uso |
|-------|------|----------|-----|
| 1 | **Térreo** | 0.000 m | Pavimento principal |
| 2 | **Forro** | 2.800 m | Referência para altura da tubulação |
| 3 | **Cobertura** | 3.200 m | Telhado e caixa d'água |

### 2.3 Passos

1. Clique duplo no texto "Level 1" → renomeie para **Térreo**.
2. Confirme elevação = **0.000**.
3. Clique duplo em "Level 2" → renomeie para **Forro** → ajuste para **2.800 m**.
4. Crie novo nível:
   - **Architecture** → **Level** (ou `LL` shortcut).
   - Clique na elevação **3.200 m** → nomeie como **Cobertura**.
5. Se houver outros níveis, delete-os.

> ✅ **Checkpoint**: 3 níveis configurados (Térreo, Forro, Cobertura).

---

## 3. Modelar Paredes

### 3.1 Planta de referência

O modelo simula uma residência de **~100 m²** com o seguinte layout:

```
    12.00 m
┌────────────────────────────────────┐
│                                    │
│   Quarto 1        Quarto 2        │  
│   (3.5×4.0)       (3.5×4.0)       │  8.50 m
│                                    │
├─────────┬──────────┬───────────────┤
│ Banheiro│          │               │
│ Social  │  Sala    │  Cozinha      │
│ (2.5×2.5)│ (4.0×4.5)│ (3.0×3.5)   │
│         │          │               │
├─────────┤          ├───────────────┤
│ Ár. Serv│          │               │
│ (2.5×2.0)│         │               │
└─────────┴──────────┴───────────────┘
```

### 3.2 Criar paredes externas

1. Vá para a vista **Térreo** (planta baixa).
2. **Architecture** → **Wall** (atalho `WA`).
3. Properties:
   - **Type**: Basic Wall: Generic - 200mm (ou "Parede Externa 200mm").
   - **Base Constraint**: Térreo.
   - **Top Constraint**: Forro (2.800 m).
   - **Location Line**: Finish Face: Exterior.
4. Desenhe o perímetro retangular:
   - Ponto A: (0, 0) → início.
   - Ponto B: (12.00, 0) → parede sul.
   - Ponto C: (12.00, 8.50) → parede leste.
   - Ponto D: (0, 8.50) → parede norte.
   - Feche no ponto A.

### 3.3 Criar paredes internas

1. Altere o tipo para: **Generic - 150mm** (ou "Parede Interna 150mm").
2. Desenhe as divisórias conforme o layout:

**Divisão horizontal principal** (separando quartos da sala):
- De (0, 4.50) a (12.00, 4.50).

**Divisão Quarto 1 / Quarto 2**:
- De (5.50, 4.50) a (5.50, 8.50).

**Divisão Banheiro / Sala**:
- De (2.50, 0) a (2.50, 4.50).

**Divisão Banheiro / Área de Serviço**:
- De (0, 2.50) a (2.50, 2.50).

**Divisão Sala / Cozinha**:
- De (8.00, 0) a (8.00, 4.50).

3. Pressione **Esc** para finalizar.

> ✅ **Checkpoint**: Paredes externas e internas criadas com 6 ambientes definidos.

---

## 4. Inserir Portas e Janelas

### 4.1 Portas

1. **Architecture** → **Door** (atalho `DR`).
2. Tipo: **Single-Flush: 0762 x 2134mm** (ou porta padrão 80cm).
3. Inserir portas nos seguintes locais:

| Ambiente | Para | Posição na parede |
|----------|------|--------------------|
| Sala | Externo (porta principal) | Parede sul, centro |
| Sala | Quarto 1 | Parede divisória, próximo à esquerda |
| Sala | Quarto 2 | Parede divisória, próximo à direita |
| Sala | Cozinha | Parede divisória, centro |
| Sala | Banheiro | Parede divisória, centro |
| Cozinha | Área de Serviço | Parede divisória, centro |

4. Clique na parede, posicione e confirme.

### 4.2 Janelas

1. **Architecture** → **Window** (atalho `WN`).
2. Tipo: **Fixed: 1200 x 1000mm** (ou janela padrão 1.20m).
3. Peitoril: **1.00 m**.
4. Inserir nas paredes externas:

| Ambiente | Parede | Quantidade |
|----------|--------|------------|
| Quarto 1 | Norte | 1 |
| Quarto 2 | Norte | 1 |
| Sala | Sul | 1 (ao lado da porta) |
| Cozinha | Leste | 1 |
| Banheiro | Oeste | 1 (basculante 60×60) |
| Área de Serviço | Oeste | 1 |

> ✅ **Checkpoint**: Portas e janelas inseridas.

---

## 5. Criar Pisos e Cobertura

### 5.1 Piso do Térreo

1. Vá para a vista **Térreo**.
2. **Architecture** → **Floor** (atalho `FL`).
3. Tipo: **Generic 150mm**.
4. Nível: **Térreo**.
5. Desenhe o contorno acompanhando as paredes externas (use **Pick Walls**).
6. Clique em **Finish Edit Mode** (✓).

### 5.2 Cobertura

1. Vá para a vista **Cobertura**.
2. **Architecture** → **Roof** → **Roof by Footprint**.
3. Tipo: **Generic - 300mm**.
4. Nível: **Cobertura**.
5. Contorne as paredes externas (use **Pick Walls**).
6. **Defines Slope**: marque nas 4 bordas.
7. Inclinação: **15%** (ou 8.5°).
8. Clique em **Finish Edit Mode** (✓).

> ✅ **Checkpoint**: Piso e cobertura criados.

---

## 6. Criar Rooms

### 6.1 Inserir Rooms

1. Vá para a vista **Térreo**.
2. **Architecture** → **Room** (atalho `RM`).
3. Clique dentro de cada ambiente para criar o Room.
4. **Imediatamente após clicar**, digite o nome correto:

| Clique em | Nome do Room | Número |
|-----------|-------------|--------|
| Ambiente inferior-esquerdo | **Banheiro Social** | 01 |
| Abaixo do banheiro | **Área de Serviço** | 02 |
| Ambiente central inferior | **Sala de Estar** | 03 |
| Ambiente direito inferior | **Cozinha** | 04 |
| Ambiente superior-esquerdo | **Quarto 1** | 05 |
| Ambiente superior-direito | **Quarto 2** | 06 |

### 6.2 Verificar Rooms

1. Selecione cada Room e confira no **Properties**:
   - **Name**: nome correto.
   - **Number**: número sequencial.
   - **Area**: deve mostrar valor > 0.
   - **Perimeter**: deve mostrar valor > 0.
   - **Level**: Térreo.
   - **Upper Limit**: Forro.
   - **Limit Offset**: 0.000.

### 6.3 Tabela de verificação

| Room | Nome esperado | Área aprox. (m²) |
|------|--------------|-------------------|
| 01 | Banheiro Social | 6.25 |
| 02 | Área de Serviço | 5.00 |
| 03 | Sala de Estar | 18.00 |
| 04 | Cozinha | 10.50 |
| 05 | Quarto 1 | 14.00 |
| 06 | Quarto 2 | 14.00 |

> ⚠️ **IMPORTANTE**: Todos os Rooms devem ter área > 0. Se algum marcar "Not Enclosed", verifique se as paredes estão fechando o ambiente.

> ✅ **Checkpoint**: 6 Rooms criados com nomes padronizados.

---

## 7. Ativar e Criar Spaces MEP

### 7.1 Verificar template

Se você está usando um template Mechanical, Spaces já estão disponíveis. Se usou Architectural:

1. **File** → **New** → verifique se MEP discipline está habilitada.
2. Ou: Em uma vista, vá em **Properties** → **Discipline** → altere para **Mechanical**.

### 7.2 Criar Spaces

1. Vá para a vista **Térreo** (com Discipline = Mechanical).
2. **Analyze** → **Spaces** → **Space** (ou **MEP** → **Space**).
3. Clique dentro de cada ambiente:

| Space | Nome | Tipo sugerido |
|-------|------|---------------|
| 01 | Banheiro Social | Plenum / Occupied |
| 02 | Área de Serviço | Plenum / Occupied |
| 03 | Sala de Estar | Occupied |
| 04 | Cozinha | Occupied |
| 05 | Quarto 1 | Occupied |
| 06 | Quarto 2 | Occupied |

### 7.3 Verificar Spaces

Para cada Space, confirme:
- **Name** corresponde ao Room.
- **Area** > 0.
- **Volume** > 0 (essencial para o plugin).
- **Level**: Térreo.

### 7.4 Habilitar cálculo de volume

Se o volume mostrar 0:
1. **Architecture** → **Room & Area** → **Area and Volume Computations**.
2. Em **Computations**, selecione: **Areas and Volumes**.
3. **OK**.
4. Verifique novamente — os volumes serão calculados.

> ✅ **Checkpoint**: 6 Spaces MEP com volumes calculados.

---

## 8. Inserir Equipamentos Hidráulicos

### 8.1 Carregar famílias

1. **Insert** → **Load Family** (atalho `LF`).
2. Navegue até a biblioteca do Revit:
   ```
   C:\ProgramData\Autodesk\RVT 2026\Libraries\Brasil\
   ```
   Ou:
   ```
   C:\ProgramData\Autodesk\RVT 2026\Libraries\US Metric\
   ```

3. Carregue as seguintes famílias:

| Categoria | Família | Pasta típica |
|-----------|---------|-------------|
| **Plumbing Fixtures** | Toilet (Vaso sanitário) | Plumbing/Fixtures |
| **Plumbing Fixtures** | Lavatory (Lavatório) | Plumbing/Fixtures |
| **Plumbing Fixtures** | Shower (Chuveiro/Box) | Plumbing/Fixtures |
| **Plumbing Fixtures** | Kitchen Sink (Pia) | Plumbing/Fixtures |
| **Plumbing Fixtures** | Laundry Sink (Tanque) | Plumbing/Fixtures |
| **Generic Models** | Washing Machine | Specialty Equipment |

> 💡 **Se não encontrar famílias específicas**: Use **Generic Model** ou **Plumbing Fixtures** genéricas. O plugin identifica por nome e categoria.

### 8.2 Posicionar no Banheiro Social

1. Vá para a vista **Térreo**.
2. **Architecture** → **Component** → **Place a Component** (ou arraste do Project Browser).

**Vaso sanitário**:
- Família: Toilet
- Posição: encostado na parede de fundo do banheiro.
- Distância da parede lateral: ~30 cm.
- Orientação: voltado para a porta.

**Lavatório**:
- Família: Lavatory / Sink Wall Mounted
- Posição: parede lateral do banheiro.
- Altura: ~85 cm (se wall-mounted).
- Distância do vaso: ~60 cm.

**Chuveiro**:
- Família: Shower Head ou marcar posição.
- Posição: canto oposto ao vaso.
- Marcar área de box: ~90×90 cm.

### 8.3 Posicionar na Cozinha

**Pia de cozinha**:
- Família: Kitchen Sink
- Posição: encostada na parede com janela.
- Centralizada na bancada.

### 8.4 Posicionar na Área de Serviço

**Tanque**:
- Família: Laundry Sink / Utility Sink
- Posição: encostado na parede hidráulica (compartilhada com banheiro).

**Ponto para máquina de lavar**:
- Família: Generic Model (ou Specialty Equipment).
- Posição: ao lado do tanque.
- Dimensão: ~60×60 cm.

### 8.5 Resumo dos equipamentos

| Ambiente | Equipamento | Quantidade | Parede |
|----------|------------|------------|--------|
| Banheiro Social | Vaso sanitário | 1 | Fundo |
| Banheiro Social | Lavatório | 1 | Lateral |
| Banheiro Social | Chuveiro | 1 | Canto oposto |
| Cozinha | Pia | 1 | Parede com janela |
| Área de Serviço | Tanque | 1 | Parede hidráulica |
| Área de Serviço | Máquina de lavar | 1 | Ao lado do tanque |

### 8.6 Parede hidráulica

Todos os equipamentos com tubulação devem estar posicionados **próximos ou encostados em uma parede hidráulica comum**, facilitando a criação de prumadas.

O layout recomendado posiciona o **banheiro e a área de serviço** lado a lado, compartilhando a mesma parede:

```
Parede hidráulica (prumada principal)
          │
   ┌──────┤──────┐
   │ Banh │ Á.S. │
   │      │      │
   │ Vaso │Tanque│
   │ Lav  │ MLav │
   │ Chuv │      │
   └──────┴──────┘
```

> ✅ **Checkpoint**: 6 equipamentos posicionados em 3 ambientes hidráulicos.

---

## 9. Configurar Vistas

### 9.1 Planta baixa principal

1. No **Project Browser**, localize **Floor Plans** → **Térreo**.
2. Clique duplo para abrir.
3. Ajuste a escala: **1:50** (Properties → Scale = 1:50).
4. Verifique se todos os Rooms e equipamentos estão visíveis.

### 9.2 Vista 3D

1. **View** → **3D View** → **Default 3D View** (atalho `{3}`).
2. Orbite para verificar:
   - Paredes com altura correta.
   - Cobertura posicionada.
   - Equipamentos no nível correto.

### 9.3 Vista MEP

1. No **Project Browser**, localize **Floor Plans** → **Térreo** (Mechanical).
2. Se não existir: **View** → **Plan Views** → **Floor Plan**.
3. Selecione o nível **Térreo** e discipline **Mechanical**.
4. Verifique se os Spaces são visíveis nesta vista.
5. Se não: **Visibility/Graphics** (`VG`) → marque **Spaces** como visível.

### 9.4 Renomear vistas

No **Project Browser**, renomeie:

| Vista original | Nome novo |
|---------------|-----------|
| Térreo | Térreo - Arquitetura |
| Térreo (Mechanical) | Térreo - MEP |
| {3D} | 3D - Geral |

> ✅ **Checkpoint**: 3 vistas configuradas (Arq, MEP, 3D).

---

## 10. Validação Final

### 10.1 Checklist do modelo

Execute cada verificação:

```
[ ] 3 Níveis (Térreo, Forro, Cobertura) com elevações corretas
[ ] 6 Ambientes com paredes fechadas
[ ] 6 Rooms com nomes padronizados e áreas > 0
[ ] 6 Spaces MEP com volumes calculados
[ ] 6 Equipamentos hidráulicos posicionados
[ ] Portas em todos os ambientes
[ ] Janelas nos ambientes com parede externa
[ ] Piso no Térreo
[ ] Cobertura no nível superior
[ ] 3 Vistas configuradas (Arq, MEP, 3D)
[ ] Arquivo salvo como Modelo_Teste_Hidraulico.rvt
```

### 10.2 Warnings

1. **Manage** → **Review Warnings**.
2. Resolva (se houver):
   - "Room is not in a properly enclosed region" → verifique paredes.
   - "Room separation line" → ajuste limites.
   - "Duplicate room names" → corrija nomes.

### 10.3 Purge

1. **Manage** → **Purge Unused**.
2. Selecione tudo → **OK**.
3. Reduz o tamanho do arquivo removendo famílias não utilizadas.

### 10.4 Salvar

1. **File** → **Save As** → **Project**.
2. Caminho:
   ```
   C:\Users\User\Desktop\PluginRevit\Data\Modelo_Teste_Hidraulico.rvt
   ```
3. Confirme.

> ✅ **Checklist final validado. Modelo pronto para testes do plugin.**

---

## Referência Rápida — O que o Plugin Testa em Cada Etapa

| Etapa | O que precisa no modelo |
|-------|------------------------|
| **E01** Detectar Ambientes | Rooms com nomes padronizados |
| **E02** Classificar Ambientes | Nomes que permitam classificação automática |
| **E03** Identificar Equipamentos | Ambientes classificados corretamente |
| **E04** Inserir Equipamentos | Spaces com volume para posicionamento |
| **E05** Validar Modelo | Todos os itens acima consistentes |
| **E06** Criar Prumadas | Equipamentos próximos a parede hidráulica |
| **E07-E09** Redes | Equipamentos com conectores (se disponíveis) |
| **E10** Exportar p/ Revit | Modelo aberto e editável |
| **E11** Dimensionar | Rede completa gerada |
| **E12** Tabelas e Pranchas | Dados calculados disponíveis |

---

> **Modelo de teste completo.** Siga os passos sequencialmente para ter um ambiente de validação funcional para o plugin de automação hidráulica.
