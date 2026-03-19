# Fluxo Manual de Projeto Hidráulico Residencial

> Mapeamento detalhado e realista do processo manual de desenvolvimento de projetos hidráulicos residenciais no Brasil, conforme praticado no mercado.

---

## 1. Visão Geral do Processo

### 1.1 Como o projeto é desenvolvido na prática

O projeto hidráulico residencial é uma disciplina complementar que depende integralmente do projeto arquitetônico. O projetista recebe o modelo ou plantas do arquiteto e desenvolve o projeto hidráulico sobre essa base, sem alterar a arquitetura.

Na prática brasileira, o processo é predominantemente manual, mesmo em escritórios que utilizam Revit. O projetista:

1. Recebe o modelo arquitetônico (frequentemente incompleto ou desatualizado)
2. Interpreta os ambientes visualmente (não há classificação automática)
3. Posiciona equipamentos com base em experiência pessoal
4. Traça tubulações manualmente, elemento por elemento
5. Dimensiona usando planilhas paralelas (Excel ou software específico)
6. Monta pranchas manualmente no Revit

O tempo médio para um projeto residencial de 2 pavimentos com 3 banheiros: **3 a 5 dias úteis** para um projetista experiente, incluindo revisões.

### 1.2 Dependência do modelo arquitetônico

O projeto hidráulico é 100% dependente da qualidade do modelo arquitetônico:

- Paredes devem estar fechadas (para que Rooms existam)
- Rooms devem estar nomeados (para identificar ambientes)
- Portas e janelas devem estar posicionadas (para posicionar equipamentos)
- Levels devem estar corretos (para calcular alturas e pressão)
- O modelo deve estar atualizado (alterações arquitetônicas invalidam o projeto hidráulico)

**Na realidade:** o modelo frequentemente chega com Rooms sem nome, paredes não fechadas, portas faltando, ou em revisão constante — gerando retrabalho significativo.

### 1.3 Nível de intervenção manual

Em um escritório típico usando Revit para hidráulica:

| Etapa | Nível de automação atual |
|-------|-------------------------|
| Interpretar ambientes | 100% manual |
| Posicionar equipamentos | 100% manual |
| Traçar tubulações | 100% manual (elemento por elemento) |
| Dimensionar | Parcialmente automatizado (planilha Excel) |
| Criar sistemas MEP | 80% manual |
| Montar pranchas | 95% manual |

Estimativa: **menos de 10% do processo é automatizado** na maioria dos escritórios brasileiros.

---

## 2. Fluxo Completo do Projeto (Passo a Passo)

---

### Etapa 01 — Recebimento e Análise do Modelo Arquitetônico

| Campo | Descrição |
|-------|-----------|
| **Descrição** | O projetista recebe o modelo Revit do arquiteto (ou plantas em DWG/PDF) e faz uma análise inicial para entender o projeto, identificar ambientes e verificar a qualidade do modelo. |
| **Objetivo** | Compreender o programa de necessidades e avaliar se o modelo está apto para iniciar o projeto hidráulico. |
| **Entradas** | Modelo Revit (.rvt) ou plantas (DWG/PDF). Programa de necessidades do cliente. Memorial descritivo arquitetônico (quando disponível). |
| **Ações manuais** | 1. Abrir o modelo no Revit. 2. Navegar por todos os pavimentos. 3. Identificar visualmente cada ambiente (banheiros, cozinhas, áreas de serviço). 4. Verificar se Rooms existem e estão nomeados. 5. Verificar se paredes estão fechadas. 6. Identificar quantidade de banheiros, cozinhas, lavanderias. 7. Verificar pé-direito e alturas de laje. 8. Anotar problemas no modelo (Rooms faltando, paredes abertas, etc.). 9. Solicitar correções ao arquiteto (quando necessário). |
| **Ferramentas** | Revit (navegação). Bloco de notas ou planilha para anotações. E-mail para comunicação com arquiteto. |
| **Saídas** | Lista de ambientes identificados. Lista de problemas no modelo. Solicitação de correções ao arquiteto (se necessário). Decisão de prosseguir ou aguardar modelo corrigido. |
| **Decisões técnicas** | O modelo está apto? Precisa de correções? Há ambientes ambíguos? O programa de necessidades bate com o modelo? |

---

### Etapa 02 — Definição do Sistema e Parâmetros

| Campo | Descrição |
|-------|-----------|
| **Descrição** | O projetista define os parâmetros do projeto hidráulico: tipo de sistema, pressão, reservatório, normas aplicáveis e padrões de materiais. |
| **Objetivo** | Estabelecer as premissas técnicas que guiarão todo o projeto. |
| **Entradas** | Dados do projeto (número de pavimentos, população estimada). Localização (para verificar pressão da rede pública). Preferências do cliente (tipo de material, marca de louças). |
| **Ações manuais** | 1. Definir tipo de abastecimento (direto, indireto, misto) — residencial quase sempre indireto (reservatório superior). 2. Definir altura do reservatório (geralmente sobre a laje de cobertura, ~6m acima do ponto mais alto). 3. Calcular consumo diário (população × 150 L/dia). 4. Dimensionar reservatório (consumo diário + reserva de incêndio se aplicável). 5. Definir material das tubulações (PVC soldável para AF, PVC esgoto para ES). 6. Definir diâmetros mínimos de referência. 7. Anotar parâmetros em documento ou planilha. |
| **Ferramentas** | Planilha Excel com fórmulas de consumo. Normas NBR 5626 e NBR 8160 (PDF ou livro). Catálogos de fabricantes. |
| **Saídas** | Premissas do projeto documentadas. Volume do reservatório definido. Material e padrão de tubulação definidos. |
| **Decisões técnicas** | Tipo de sistema (indireto com reservatório). Necessidade de pressurizador. Material das tubulações. Posição do reservatório. |

---

### Etapa 03 — Locação de Equipamentos Sanitários

| Campo | Descrição |
|-------|-----------|
| **Descrição** | O projetista posiciona manualmente cada aparelho sanitário (vaso, lavatório, chuveiro, pia, tanque, ralos) em cada ambiente, considerando layout, paredes, portas e ergonomia. |
| **Objetivo** | Definir a posição exata de cada ponto de consumo e descarga no modelo. |
| **Entradas** | Modelo arquitetônico com ambientes identificados (Etapa 01). Lista de equipamentos por ambiente. Catálogo de famílias MEP do Revit. |
| **Ações manuais** | 1. Para cada ambiente hidráulico, abrir a vista de planta no Revit. 2. Verificar quais equipamentos são necessários (banheiro: vaso, lavatório, chuveiro, ralo). 3. Verificar se o arquiteto já colocou equipamentos (frequentemente são famílias de arquitetura sem connectors MEP). 4. Se já existe: avaliar se a posição é viável hidraulicamente. Se não é viável: solicitar alteração ao arquiteto ou adaptar. 5. Se não existe: inserir família MEP manualmente. 6. Posicionar considerando: distância da parede (vaso: 20cm lateral, 60cm frontal), proximidade de prumada prevista, acessibilidade, ergonomia. 7. Rotacionar equipamento conforme orientação da parede. 8. Repetir para todos os ambientes de todos os pavimentos. 9. Inserir ralos nos pontos mais baixos. 10. Verificar visualmente se não há colisões. |
| **Ferramentas** | Revit (inserção de famílias, posicionamento manual). Famílias MEP com connectors (biblioteca própria ou fornecedor). |
| **Saídas** | Todos os equipamentos posicionados no modelo. Famílias MEP com connectors prontos para conexão. |
| **Decisões técnicas** | Posição de cada equipamento (experiência do projetista). Qual família usar (com connectors corretos). Resolução de conflitos (equipamento vs. porta, equipamento vs. janela). Agrupamento de equipamentos para otimizar tubulação. |

---

### Etapa 04 — Definição de Prumadas (Colunas Verticais)

| Campo | Descrição |
|-------|-----------|
| **Descrição** | O projetista define a posição e quantidade das prumadas (colunas verticais) de água fria, esgoto e ventilação, buscando atender todos os pavimentos com o menor número possível de colunas. |
| **Objetivo** | Estabelecer a infraestrutura vertical que servirá como tronco de distribuição e coleta. |
| **Entradas** | Equipamentos posicionados (Etapa 03). Planta de todos os pavimentos. Posição de shafts (se existirem no projeto arquitetônico). |
| **Ações manuais** | 1. Sobrepor mentalmente as plantas de todos os pavimentos. 2. Identificar ambientes alinhados verticalmente (banheiros sobre banheiros, cozinhas sobre cozinhas). 3. Definir pontos de prumada buscando: menor distância média dos equipamentos, posição em shaft quando existe, não interferir com estrutura (vigas, pilares). 4. Definir prumada de AF (1 geralmente basta para residência). 5. Definir tubos de queda de esgoto (1 por grupo de banheiros alinhados). 6. Definir colunas de ventilação (paralelas aos tubos de queda). 7. Marcar as posições na planta. 8. No Revit: criar Pipe vertical do nível mais baixo ao mais alto. 9. Repetir para cada prumada. |
| **Ferramentas** | Revit (criação de Pipes verticais). Lápis e papel (rascunho da locação — muitos projetistas ainda fazem esboço à mão). |
| **Saídas** | Prumadas de AF, ES e VE posicionadas no modelo. Esquema vertical definido. |
| **Decisões técnicas** | Quantidade de prumadas (equilíbrio entre custo e funcionalidade). Posição de cada prumada (experiência + análise de layout). Diâmetro preliminar (será refinado no dimensionamento). |

---

### Etapa 05 — Traçado da Rede de Água Fria

| Campo | Descrição |
|-------|-----------|
| **Descrição** | O projetista traça manualmente toda a rede de distribuição de água fria: barrilete, ramais de distribuição e sub-ramais até cada ponto de consumo, trecho por trecho no Revit. |
| **Objetivo** | Conectar o reservatório superior a todos os pontos de consumo através de tubulação dimensionada. |
| **Entradas** | Prumadas definidas (Etapa 04). Equipamentos posicionados (Etapa 03). Parâmetros do sistema (Etapa 02). |
| **Ações manuais** | 1. No nível mais alto, traçar barrilete de distribuição (saída do reservatório até as prumadas). 2. Inserir registro de gaveta na saída do reservatório. 3. Para cada pavimento: a. Traçar ramal do ponto de derivação da prumada até cada ambiente. b. Traçar sub-ramal dentro do ambiente até o connector de cada equipamento. c. Inserir registro de gaveta na entrada de cada ambiente. d. Conectar cada trecho usando fittings (tees, curvas 90°, reduções). 4. Cada trecho é criado individualmente no Revit: selecionar Pipe, definir diâmetro, clicar ponto inicial, clicar ponto final. 5. Ajustar elevação de cada trecho (geralmente embutido na parede ou sobre o forro). 6. Inserir fittings manualmente em cada conexão. 7. Verificar visualmente se todos os connectors estão ligados. 8. Repetir para todos os pavimentos. |
| **Ferramentas** | Revit (ferramenta Pipe, Pipe Fitting). Muito uso de Snap e alinhamento manual. |
| **Saídas** | Rede de AF completa no modelo (ainda sem dimensionamento final). Todos os equipamentos conectados. |
| **Decisões técnicas** | Traçado da rota (menor comprimento vs. facilidade de execução em obra). Altura de instalação dos ramais. Onde posicionar registros. Como desviar de vigas e outros obstáculos (frequentemente por tentativa e erro). |

---

### Etapa 06 — Traçado da Rede de Esgoto Sanitário

| Campo | Descrição |
|-------|-----------|
| **Descrição** | O projetista traça a rede de esgoto desde cada aparelho sanitário até os tubos de queda e subcoletor, incluindo caixas sifonadas, caixas de gordura e caixas de inspeção. |
| **Objetivo** | Coletar efluentes de todos os aparelhos sanitários e conduzi-los ao tubo de queda e daí à caixa de inspeção externa. |
| **Entradas** | Equipamentos posicionados (Etapa 03). Prumadas de esgoto (Etapa 04). Planta do pavimento térreo com locação da caixa de inspeção. |
| **Ações manuais** | 1. Para cada pavimento, para cada ambiente: a. Traçar ramal de descarga: vaso sanitário → ramal de esgoto (DN mínimo 100mm). b. Traçar ramal de descarga: lavatório → caixa sifonada (DN 40mm). c. Traçar ramal de descarga: chuveiro → caixa sifonada (DN 40mm). d. Traçar ramal de esgoto: caixa sifonada → tubo de queda. 2. Na cozinha: inserir caixa de gordura na saída da pia. 3. No térreo: traçar subcoletor do tubo de queda até a caixa de inspeção externa. 4. Posicionar caixas de inspeção em mudanças de direção (máximo ~15m entre CIs). 5. Todos os trechos são embutidos no piso (abaixo da laje). 6. Cada trecho é criado individualmente no Revit com diâmetro preliminar. 7. Inserir fittings: junções, curvas 45° (evitar 90° em esgoto), reduções. |
| **Ferramentas** | Revit (Pipe, Fittings). Atenção especial à elevação (esgoto é gravitacional). |
| **Saídas** | Rede de ES completa (sem inclinação ainda). Caixas sifonadas, de gordura e de inspeção posicionadas. |
| **Decisões técnicas** | Posição da caixa sifonada (centralizada no banheiro). Rota dos ramais (menor comprimento, sem subir). Posição das caixas de inspeção (acessíveis, em mudanças de direção). Caixa de gordura na cozinha (obrigatória por norma). |

---

### Etapa 07 — Aplicação de Inclinações no Esgoto

| Campo | Descrição |
|-------|-----------|
| **Descrição** | O projetista aplica inclinação (declividade) em cada trecho horizontal de esgoto para garantir escoamento gravitacional, ajustando a elevação de cada Pipe manualmente. |
| **Objetivo** | Garantir que o esgoto escoe por gravidade em toda a rede. |
| **Entradas** | Rede de esgoto traçada (Etapa 06). Regras de declividade: 2% para DN ≤ 75mm, 1% para DN ≥ 100mm. |
| **Ações manuais** | 1. Para cada trecho horizontal de esgoto: a. Identificar o diâmetro do trecho. b. Determinar inclinação (2% ou 1%). c. Calcular desnível: comprimento × inclinação. d. Selecionar o endpoint do trecho no Revit. e. Ajustar a elevação (Z) manualmente, movendo o ponto para baixo. f. Verificar se a conexão com o trecho seguinte ainda está alinhada. g. Ajustar fittings reconectados. 2. Trabalhar de montante (equipamento) para jusante (tubo de queda). 3. Verificar que não há trechos subindo (contra a gravidade). 4. Verificar que a inclinação não faz a tubulação furar a laje inferior. |
| **Ferramentas** | Revit (edição de elevação de Pipes). Calculadora (comprimento × %). |
| **Saídas** | Rede de esgoto com inclinação aplicada. Elevações ajustadas. |
| **Decisões técnicas** | Direção do escoamento (sempre em direção do tubo de queda). Resolução de conflitos de elevação (quando não há espaço para a inclinação). |

**NOTA:** Esta é uma das etapas mais tediosas e propensas a erro. Ajustar elevação trecho por trecho no Revit é extremamente lento e qualquer erro gera desconexão de fittings.

---

### Etapa 08 — Rede de Ventilação

| Campo | Descrição |
|-------|-----------|
| **Descrição** | O projetista cria a rede de ventilação conectando os ramais de esgoto à coluna de ventilação, garantindo a equalização de pressão nos desconectores. |
| **Objetivo** | Impedir o sifonamento de desconectores (caixas sifonadas, sifões) através da ventilação adequada. |
| **Entradas** | Rede de esgoto completa (Etapas 06–07). Coluna de ventilação (Etapa 04). Regras da NBR 8160 para ventilação. |
| **Ações manuais** | 1. Identificar pontos que necessitam ventilação (ramais de esgoto distantes do tubo de queda). 2. Traçar ramal de ventilação do ponto de esgoto até a coluna de ventilação. 3. Ventilação sobe verticalmente a partir do ramal de esgoto (mínimo 15cm acima da borda do lavatório mais alto). 4. Conectar à coluna de ventilação ou diretamente ao tubo de queda (acima do nível da cobertura). 5. A coluna de ventilação sai pela cobertura (terminal de ventilação). |
| **Ferramentas** | Revit (Pipes na disciplina de ventilação). |
| **Saídas** | Rede de ventilação completa. Terminal de ventilação na cobertura. |
| **Decisões técnicas** | Quais pontos precisam de ventilação individual vs. ventilação primária (pelo tubo de queda). Rota da ventilação (deve subir, nunca descer). |

---

### Etapa 09 — Dimensionamento Hidráulico

| Campo | Descrição |
|-------|-----------|
| **Descrição** | O projetista calcula vazões, seleciona diâmetros, verifica velocidade e pressão para toda a rede de água fria, e dimensiona esgoto por UHC. Este cálculo é feito majoritariamente FORA do Revit (em planilha Excel). |
| **Objetivo** | Garantir que todos os trechos da rede estão dimensionados conforme norma. |
| **Entradas** | Rede traçada no Revit (comprimentos de cada trecho). Tabela de pesos dos aparelhos (NBR 5626). Tabela de UHC (NBR 8160). Parâmetros do sistema (altura do reservatório, material). |
| **Ações manuais** | **Água Fria:** 1. Numerar cada trecho da rede (de jusante para montante). 2. Para cada trecho: listar aparelhos atendidos a jusante, somar pesos. 3. Calcular vazão: Q = 0.3 × √(ΣP). 4. Selecionar diâmetro comercial onde V ≤ 3 m/s. 5. Calcular perda de carga unitária (Fair-Whipple-Hsiao ou tabela). 6. Calcular perda de carga no trecho: J × L × 1.20 (20% para perdas localizadas). 7. Somar perdas de carga no caminho crítico (trecho mais desfavorável). 8. Verificar pressão: P = H_geométrica - ΣJ ≥ 3 m.c.a. 9. Se pressão insuficiente: aumentar diâmetro ou considerar pressurizador. **Esgoto:** 1. Para cada trecho: somar UHCs dos aparelhos. 2. Consultar tabela NBR 8160: UHC → diâmetro mínimo. 3. Verificar que diâmetro nunca diminui no sentido do escoamento. 4. Depois de dimensionar em planilha: voltar ao Revit e alterar o diâmetro de cada trecho manualmente. |
| **Ferramentas** | Planilha Excel (cálculos). Tabelas das normas (PDF ou livro). Revit (alterar diâmetros). |
| **Saídas** | Planilha de dimensionamento preenchida. Diâmetros finais de cada trecho. Verificação de pressão no ponto mais desfavorável. Diâmetros atualizados no modelo Revit. |
| **Decisões técnicas** | Escolha do caminho crítico (trecho mais desfavorável). Decisão sobre diâmetro (mínimo normativo vs. margem de segurança). Necessidade de pressurizador. |

**NOTA:** Esta é a etapa com maior desconexão entre ferramenta (Revit) e cálculo (Excel). O projetista calcula em planilha e depois transcrevem diâmetros de volta ao Revit manualmente — processo lento e sujeito a erro de transcrição.

---

### Etapa 10 — Criação de Sistemas MEP no Revit

| Campo | Descrição |
|-------|-----------|
| **Descrição** | O projetista cria os sistemas lógicos no Revit (PipingSystems) e atribui cada tubulação e acessório ao sistema correto, permitindo filtragem, quantificação e visualização por sistema. |
| **Objetivo** | Organizar logicamente os elementos para documentação e verificação. |
| **Entradas** | Rede completa com diâmetros finais (Etapa 09). |
| **Ações manuais** | 1. Criar sistema de Água Fria (tipo DomesticColdWater). 2. Selecionar todos os Pipes e Fittings de AF e atribuir ao sistema. 3. Criar sistema de Esgoto (tipo Sanitary). 4. Selecionar todos os elementos de ES e atribuir. 5. Criar sistema de Ventilação (tipo Vent). 6. Atribuir elementos de ventilação. 7. Verificar se há elementos "soltos" (sem sistema). 8. Verificar conectividade (o Revit mostra warnings quando há desconexão). 9. Aplicar Override de cores por sistema (para visualização). |
| **Ferramentas** | Revit (MEP Systems, Selection, Graphic Override). |
| **Saídas** | Systemas MEP criados e populados. Elementos organizados por sistema. Visualização por cores. |
| **Decisões técnicas** | Nomenclatura dos sistemas. Como tratar elementos em fronteira (tee de derivação: AF ou ES?). |

---

### Etapa 11 — Verificação e Compatibilização

| Campo | Descrição |
|-------|-----------|
| **Descrição** | O projetista revisa todo o projeto visualmente, verifica interferências com outras disciplinas (estrutura, elétrica) e corrige problemas encontrados. |
| **Objetivo** | Garantir que o projeto está correto e sem conflitos antes de documentar. |
| **Entradas** | Projeto hidráulico completo (Etapas 01–10). Modelo com outras disciplinas (estrutura, elétrica). |
| **Ações manuais** | 1. Navegar por todos os pavimentos vista por vista. 2. Verificar visualmente interferências: tubulação passando por vigas, tubulação cruzando eletrodutos, prumada colidindo com pilar. 3. Verificar se todos os equipamentos estão conectados. 4. Verificar se todas as tubulações possuem sistema atribuído. 5. Verificar se os diâmetros estão corretos (comparar com planilha). 6. Rodar o Revit "Interference Check" (quando disponível). 7. Corrigir todos os problemas encontrados (o que frequentemente requer refazer partes do traçado). |
| **Ferramentas** | Revit (navegação, Interference Check). Navisworks (para clash detection mais avançado — quando usado). |
| **Saídas** | Projeto revisado e corrigido. Lista de interferências resolvidas. |
| **Decisões técnicas** | Como resolver cada interferência (desviar para cima? para o lado? mudar rota?). |

**NOTA:** Na prática, muitos escritórios não fazem compatibilização formal. O projetista confia na verificação visual, o que resulta em problemas descobertos apenas na obra.

---

### Etapa 12 — Geração de Tabelas Quantitativas

| Campo | Descrição |
|-------|-----------|
| **Descrição** | O projetista cria tabelas de quantitativos (schedules) no Revit para tubulações, conexões e equipamentos, e/ou exporta dados para planilha Excel. |
| **Objetivo** | Produzir lista de materiais para orçamento e compra. |
| **Entradas** | Projeto completo com sistemas atribuídos (Etapa 10). |
| **Ações manuais** | 1. Criar ViewSchedule para Pipes: filtrar por sistema, agrupar por diâmetro, somar comprimento. 2. Criar ViewSchedule para Fittings: agrupar por tipo e diâmetro, contar. 3. Criar ViewSchedule para equipamentos: listar por ambiente. 4. Formatar cabeçalhos, unidades, totais. 5. Frequentemente: exportar para Excel para formatação mais refinada. 6. Verificar se totais fazem sentido (verificação de sanidade). |
| **Ferramentas** | Revit (Schedules). Excel (formatação final). |
| **Saídas** | Tabelas de quantitativo. Lista de materiais. |
| **Decisões técnicas** | Critérios de agrupamento. Inclusão de margem para perdas (geralmente 10–15% sobre tubulação). |

---

### Etapa 13 — Montagem de Pranchas

| Campo | Descrição |
|-------|-----------|
| **Descrição** | O projetista cria as pranchas finais (ViewSheets) no Revit, posiciona views de planta, cortes, isométricos, tabelas e legendas, e configura tudo para impressão. |
| **Objetivo** | Produzir a documentação final pronta para entrega ao cliente e à obra. |
| **Entradas** | Projeto completo (Etapa 11). Tabelas (Etapa 12). Titleblock do escritório. |
| **Ações manuais** | 1. Duplicar Floor Plan views para cada sistema (AF, ES) por pavimento. 2. Aplicar View Template ou configurar manualmente Visibility/Graphics para cada view: esconder arquitetura desnecessária, mostrar apenas hidráulica do sistema. 3. Configurar escala (1:50 para residencial). 4. Configurar Crop Region. 5. Criar ViewSheet com titleblock. 6. Arrastar view para a prancha. 7. Posicionar manualmente. 8. Adicionar schedules (tabelas). 9. Adicionar legendas (desenhar manualmente ou inserir de biblioteca). 10. Adicionar notas e textos explicativos. 11. Numerar pranchas (HID-01, HID-02...). 12. Preencher campos do carimbo (nome do projeto, data, revisão, projetista). 13. Revisar todas as pranchas visualmente. 14. Exportar para PDF. |
| **Ferramentas** | Revit (Sheets, Views, Annotations). PDF printer. |
| **Saídas** | Pranchas finais em PDF. Arquivo Revit com pranchas montadas. |
| **Decisões técnicas** | Escala adequada (caber na folha vs. legibilidade). Quantas pranchas. Quais views incluir (planta de AF separada de ES? ou juntas?). |

---

### Etapa 14 — Memorial e Entrega

| Campo | Descrição |
|-------|-----------|
| **Descrição** | O projetista redige o memorial descritivo do projeto hidráulico e faz a entrega final ao cliente. |
| **Objetivo** | Documentar premissas, parâmetros e especificações técnicas. |
| **Entradas** | Projeto completo. Planilha de dimensionamento. Premissas (Etapa 02). |
| **Ações manuais** | 1. Redigir memorial descritivo em Word: descrição do sistema, normas utilizadas, parâmetros adotados, especificação de materiais. 2. Anexar planilha de dimensionamento. 3. Compilar documentação: pranchas PDF + memorial + planilha. 4. Enviar ao cliente para aprovação. 5. Iterar com correções solicitadas pelo cliente ou coordenador. |
| **Ferramentas** | Word. Excel. E-mail. |
| **Saídas** | Memorial descritivo. Documentação completa entregue. |
| **Decisões técnicas** | Nível de detalhe do memorial. Formato de entrega. |

---

## 3. Decisões do Projetista

### 3.1 Posicionamento de Equipamentos

| Decisão | Critérios utilizados | Base |
|---------|---------------------|------|
| Onde posicionar o vaso sanitário | Parede oposta à porta, ≥ 15cm de parede lateral, ≥ 60cm frontal livre | Ergonomia + experiência |
| Onde posicionar o lavatório | Parede adjacente à porta, muitas vezes sob janela basculante | Layout arquitetônico |
| Onde posicionar o chuveiro | Canto oposto à porta, longe do vaso sanitário | Experiência + layout |
| Onde posicionar ralos | Ponto mais distante do box ou centro do banheiro | Caimento do piso |
| Onde posicionar pia de cozinha | Sob janela (quando existe) ou parede definida pelo arquiteto | Layout + ventilação |

### 3.2 Definição de Prumadas

| Decisão | Critérios | Base |
|---------|-----------|------|
| Quantidade de prumadas | 1 grupo de prumadas por shaft ou coluna de banheiros alinhados | Experiência + layout |
| Posição | Próximo ao maior agrupamento de aparelhos sanitários | Análise visual + experiência |
| Diâmetro | Dimensionamento por carga acumulada (definido na Etapa 09) | Cálculo normativo |

### 3.3 Traçado de Tubulações

| Decisão | Critérios | Base |
|---------|-----------|------|
| Rota dos ramais AF | Menor comprimento, embutido na parede quando possível, evitar cruzar portas | Experiência + construtibilidade |
| Rota dos ramais ES | Convergir para tubo de queda, manter inclinação possível, embutir no piso | Gravidade + experiência |
| Desvio de obstáculos | Subir por cima de vigas (AF) ou desviar lateralmente (ES) | Caso a caso |
| Onde inserir registros | Entrada de cada ambiente (AF), derivações principais | Norma + manutenibilidade |

### 3.4 Resolução de Interferências

| Problema | Solução típica | Frequência |
|----------|----------------|-----------|
| Tubulação cruza viga | Desviar por cima (AF) ou redirecionar rota (ES) | Muito frequente |
| Prumada no pilar | Mover prumada 50cm lateralmente | Frequente |
| Esgoto sem espaço para inclinação | Reduzir comprimento do trecho ou usar bomba | Ocasional |
| Pressão insuficiente | Aumentar diâmetro, reposicionar reservatório ou pressurizador | Comum em 2+ pavimentos |

---

## 4. Pontos Críticos do Processo

| Etapa | Por que é crítica | Consequência de erro |
|-------|------------------|---------------------|
| 03 — Locação de equipamentos | Posição errada gera retrabalho em todas as etapas seguintes | Refazer tubulações inteiras |
| 04 — Definição de prumadas | Prumada mal posicionada = rotas longas e diâmetros grandes | Ineficiência, custo, problemas de pressão |
| 05/06 — Traçado de redes | Maior volume de trabalho manual. Erros frequentes de conexão | Rede desconectada, leaks no modelo |
| 07 — Inclinação de esgoto | Extremamente tedioso. Erro = esgoto não escoa | Problema em obra, entupimento |
| 09 — Dimensionamento | Cálculo errado = subdimensionamento ou superdimensionamento | Falta de água, pressão insuficiente, custo excessivo |
| 11 — Compatibilização | Interferências não detectadas aparecem na obra | Custo de correção em obra (10× mais caro) |

---

## 5. Gargalos e Retrabalho

### 5.1 Etapas que consomem mais tempo

| Etapa | Tempo estimado (% do total) | Motivo |
|-------|---------------------------|--------|
| 05 — Rede de AF | 20% | Traçar trecho por trecho manualmente no Revit |
| 06 — Rede de ES | 20% | Idem + complexidade de gravitacional |
| 07 — Inclinação | 10% | Ajustar elevação de cada trecho individualmente |
| 09 — Dimensionamento | 15% | Calcular em Excel e transcrever para Revit |
| 13 — Pranchas | 15% | Configurar views, filtros, layout manualmente |

### 5.2 Situações que geram retrabalho

| Situação | Frequência | Impacto |
|----------|-----------|---------|
| Arquiteto altera layout após início do projeto hidráulico | Muito alta | Refazer posição de equipamentos + tubulações |
| Diâmetro calculado diferente do traçado | Alta | Alterar diâmetro de cada trecho no Revit |
| Interferência com estrutura descoberta tarde | Alta | Refazer rota da tubulação |
| Erro em inclinação de esgoto | Média | Reconectar fittings desconectados |
| Família sem connector | Média | Trocar família e reconectar |
| Erro na transcrição Excel → Revit | Alta | Verificar e corrigir cada trecho |

### 5.3 Problemas comuns no modelo arquitetônico

| Problema | Frequência | Impacto no projeto hidráulico |
|----------|-----------|------------------------------|
| Rooms sem nome | Muito alta | Projetista não sabe que ambiente é |
| Paredes não fechadas | Alta | Rooms não existem, áreas incorretas |
| Modelo desatualizado (versão antiga) | Muito alta | Projeto hidráulico sobre planta errada |
| Nível faltando ou incorreto | Média | Altura errada, cálculo de pressão incorreto |
| Sem famílias de equipamentos | Alta | Projetista precisa inserir do zero |
| Pé-direito inconsistente | Média | Espaço insuficiente para tubulação |

---

## 6. Interações com o Modelo

### 6.1 Como o projetista usa Rooms

Na prática, o projetista hidráulico **raramente interage com Rooms do Revit programaticamente**. O uso é visual:

- Olha para a planta e identifica "este é o banheiro" pelo nome ou contexto
- Não usa Spaces MEP (a grande maioria dos projetistas hidráulicos brasileiros não sabe o que é Space)
- Não consulta parâmetros de Rooms (área, perímetro)
- Rooms servem apenas como referência visual de nome/número

### 6.2 Como interpreta espaços

- O projetista interpreta ambientes **visualmente**, não por metadados
- Se o Room se chama "BWC" ou "WC", o projetista entende como banheiro por experiência
- Ambientes ambíguos (ex: "Circulação Serviço") são interpretados no contexto da planta
- O projetista mentalmente categoriza: "este é molhado" vs "este é seco"

### 6.3 Como adapta o modelo

- O projetista geralmente NÃO modifica o modelo arquitetônico
- Trabalha em arquivo separado (linked model) ou copia o modelo
- Quando precisa de correção no modelo: solicita ao arquiteto e aguarda
- Em casos urgentes: faz correções mínimas por conta própria (fechar parede, nomear Room)

---

## 7. Limitações do Processo Atual

| Limitação | Descrição |
|-----------|-----------|
| **Dependência do projetista** | Qualidade do projeto depende inteiramente da experiência individual. Projetistas juniores cometem mais erros e levam 2–3× mais tempo. |
| **Falta de padronização** | Cada projetista tem seu método. Mesmo dentro do mesmo escritório, projetos ficam inconsistentes. |
| **Desconexão cálculo–modelo** | Dimensionamento em Excel, diâmetros no Revit. Transcrição manual gera erros. |
| **Ausência de validação automática** | Erros só são descobertos por revisão visual ou em obra. Não há checagem automatizada. |
| **Retrabalho por modelo desatualizado** | Alterações arquitetônicas frequentes invalidam o projeto hidráulico parcial ou totalmente. |
| **Processo lento** | Traçar tubulação trecho por trecho é ineficiente. Um banheiro pode levar 30–60 minutos. |
| **Sem rastreabilidade** | Se um diâmetro está errado, não há log de por que foi escolhido. Decisões são implícitas. |
| **Baixa reutilização** | Pouca reutilização entre projetos. Cada projeto começa praticamente do zero. |
| **Inclinação é pesadelo** | Aplicar inclinação trecho por trecho é a tarefa mais tediosa e propensa a erro. |
| **Pranchas são artesanais** | Montar pranchas consome 15% do tempo total e é trabalho mecânico. |

---

## 8. Oportunidades de Automação

| Etapa | Automatizável | Dificuldade | Tipo de Automação | Ganho Estimado |
|-------|--------------|------------|-------------------|---------------|
| 01 — Análise do modelo | Sim | Baixa | Plugin lê Rooms, Levels, valida modelo | 90% do tempo |
| 02 — Definição de parâmetros | Sim | Baixa | Plugin carrega configuração JSON | 80% do tempo |
| 03 — Locação de equipamentos | Parcial | Alta | Plugin calcula posição, Dynamo insere | 50% do tempo |
| 04 — Definição de prumadas | Parcial | Média | Plugin calcula centroide de clusters | 60% do tempo |
| 05 — Rede de água fria | Parcial | Alta | Plugin define topologia, Dynamo/unMEP traça | 70% do tempo |
| 06 — Rede de esgoto | Parcial | Alta | Plugin define topologia, Dynamo/unMEP traça | 70% do tempo |
| 07 — Inclinação | Sim | Média | Dynamo ajusta elevação em massa | 95% do tempo |
| 08 — Ventilação | Parcial | Média | Plugin define pontos, Dynamo traça | 60% do tempo |
| 09 — Dimensionamento | Sim | Média | Plugin calcula internamente (elimina Excel) | 95% do tempo |
| 10 — Sistemas MEP | Sim | Baixa | Plugin cria e atribui via API | 90% do tempo |
| 11 — Compatibilização | Parcial | Alta | Plugin faz clash básico, projetista valida | 40% do tempo |
| 12 — Tabelas | Sim | Baixa | Plugin cria Schedules via API | 90% do tempo |
| 13 — Pranchas | Parcial | Média | Plugin/Dynamo monta, projetista ajusta | 70% do tempo |
| 14 — Memorial | Não | — | Fora do escopo (redação textual) | — |

### Resumo de ganho potencial

| Métrica | Valor |
|---------|-------|
| Etapas totais do projetista | 14 |
| Etapas automatizáveis (total ou parcial) | 13 de 14 (93%) |
| Tempo médio manual | 3–5 dias úteis |
| Tempo estimado com automação | 0.5–1.5 dias úteis |
| **Redução estimada de tempo** | **60–80%** |
| **Eliminação de erros de transcrição** | ~100% |
| **Padronização entre projetos** | ~100% |
