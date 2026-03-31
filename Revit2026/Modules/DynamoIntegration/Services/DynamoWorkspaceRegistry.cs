using Revit2026.Modules.DynamoIntegration.Contracts;

namespace Revit2026.Modules.DynamoIntegration.Services
{
    public class WorkspaceInfo
    {
        public string Id { get; set; } = "";
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
        public string RelativePath { get; set; } = "";
        public string Etapa { get; set; } = "";
        public int Ordem { get; set; }
        public List<string> InputNodeNames { get; set; } = new();
        public List<string> OutputNodeNames { get; set; } = new();
        public bool Obrigatorio { get; set; } = true;

        public string FullPath(string basePath) =>
            Path.Combine(basePath, RelativePath);

        public override string ToString() =>
            $"[{Etapa}] {Name} ({RelativePath})";
    }

    public interface IDynamoWorkspaceRegistry
    {
        WorkspaceInfo? GetByName(string name);
        WorkspaceInfo? GetById(string id);
        IList<WorkspaceInfo> GetByEtapa(string etapa);
        IList<WorkspaceInfo> GetAll();
        string ResolveFullPath(string nameOrId);
    }

    public class DynamoWorkspaceRegistry : IDynamoWorkspaceRegistry
    {
        private readonly string _basePath;
        private readonly Dictionary<string, WorkspaceInfo> _byId = new(
            StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, WorkspaceInfo> _byName = new(
            StringComparer.OrdinalIgnoreCase);
        private readonly List<WorkspaceInfo> _all = new();

        public DynamoWorkspaceRegistry(string basePath)
        {
            _basePath = basePath
                ?? throw new ArgumentNullException(nameof(basePath));

            RegistrarScriptsPadrao();
        }

        public WorkspaceInfo? GetByName(string name)
        {
            _byName.TryGetValue(name, out var info);
            return info;
        }

        public WorkspaceInfo? GetById(string id)
        {
            _byId.TryGetValue(id, out var info);
            return info;
        }

        public IList<WorkspaceInfo> GetByEtapa(string etapa)
        {
            return _all
                .Where(w => string.Equals(w.Etapa, etapa,
                    StringComparison.OrdinalIgnoreCase))
                .OrderBy(w => w.Ordem)
                .ToList();
        }

        public IList<WorkspaceInfo> GetAll()
        {
            return _all.OrderBy(w => w.Ordem).ToList();
        }

        public string ResolveFullPath(string nameOrId)
        {
            var info = GetByName(nameOrId) ?? GetById(nameOrId);
            if (info == null)
                throw new KeyNotFoundException(
                    $"Workspace '{nameOrId}' não encontrado no registro.");

            return info.FullPath(_basePath);
        }

        public bool Exists(string nameOrId)
        {
            var info = GetByName(nameOrId) ?? GetById(nameOrId);
            if (info == null) return false;

            return File.Exists(info.FullPath(_basePath));
        }

        public void Registrar(WorkspaceInfo info)
        {
            _byId[info.Id] = info;
            _byName[info.Name] = info;
            _all.Add(info);
        }

        private void RegistrarScriptsPadrao()
        {
            Registrar(new WorkspaceInfo
            {
                Id = "E01-ValidarRooms",
                Name = "ValidarRooms",
                Description = "Validação visual de Rooms — verifica nome, número, área e status",
                RelativePath = @"01_Ambientes\01_ValidarRooms.dyn",
                Etapa = "E01",
                Ordem = 1,
                InputNodeNames = new(),
                OutputNodeNames = new() { "Tabela", "RoomsInvalidos", "Cores" },
                Obrigatorio = true
            });

            Registrar(new WorkspaceInfo
            {
                Id = "E02-ClassificarAmbientes",
                Name = "ClassificarAmbientes",
                Description = "Classificação automática de ambientes por tipo",
                RelativePath = @"02_ClassificarAmbientes\02_ClassificarAmbientes.dyn",
                Etapa = "E02",
                Ordem = 2,
                InputNodeNames = new() { "RoomIds" },
                OutputNodeNames = new() { "Classificacoes", "Status" },
                Obrigatorio = true
            });

            Registrar(new WorkspaceInfo
            {
                Id = "E03-InserirEquipamentos",
                Name = "InserirEquipamentos",
                Description = "Inserção automática de equipamentos hidráulicos",
                RelativePath = @"03_InserirEquipamentos\03_InserirEquipamentos.dyn",
                Etapa = "E03",
                Ordem = 3,
                InputNodeNames = new() { "RoomIds", "FamilyNames", "Positions" },
                OutputNodeNames = new() { "ElementosInseridos", "Status" },
                Obrigatorio = true
            });

            Registrar(new WorkspaceInfo
            {
                Id = "E04-GerarPontos",
                Name = "GerarPontos",
                Description = "Geração de pontos de conexão hidráulica",
                RelativePath = @"04_GerarPontos\04_GerarPontos.dyn",
                Etapa = "E04",
                Ordem = 4,
                InputNodeNames = new() { "ElementIds" },
                OutputNodeNames = new() { "PontosConexao" },
                Obrigatorio = true
            });

            Registrar(new WorkspaceInfo
            {
                Id = "E05-CriarPrumadas",
                Name = "CriarPrumadas",
                Description = "Criação de prumadas verticais por sistema",
                RelativePath = @"05_CriarPrumadas\05_CriarPrumadas.dyn",
                Etapa = "E05",
                Ordem = 5,
                InputNodeNames = new() { "SystemType", "LevelIds" },
                OutputNodeNames = new() { "PrumadasCriadas" },
                Obrigatorio = true
            });

            Registrar(new WorkspaceInfo
            {
                Id = "E06-RedeAguaFria",
                Name = "RedeAguaFria",
                Description = "Geração da rede de água fria",
                RelativePath = @"06_RedeAguaFria\06_RedeAguaFria.dyn",
                Etapa = "E06",
                Ordem = 6,
                InputNodeNames = new() { "PontosAF", "Prumadas" },
                OutputNodeNames = new() { "PipesAF", "Status" },
                Obrigatorio = true
            });

            Registrar(new WorkspaceInfo
            {
                Id = "E07-RedeEsgoto",
                Name = "RedeEsgoto",
                Description = "Geração da rede de esgoto sanitário",
                RelativePath = @"07_RedeEsgoto\07_RedeEsgoto.dyn",
                Etapa = "E07",
                Ordem = 7,
                InputNodeNames = new() { "PontosES", "Declividade" },
                OutputNodeNames = new() { "PipesES", "Status" },
                Obrigatorio = true
            });

            Registrar(new WorkspaceInfo
            {
                Id = "E08-Ventilacao",
                Name = "Ventilacao",
                Description = "Geração da rede de ventilação",
                RelativePath = @"08_Ventilacao\08_Ventilacao.dyn",
                Etapa = "E08",
                Ordem = 8,
                InputNodeNames = new() { "PontosVT" },
                OutputNodeNames = new() { "PipesVT", "Status" },
                Obrigatorio = false
            });

            Registrar(new WorkspaceInfo
            {
                Id = "E09-Dimensionamento",
                Name = "Dimensionamento",
                Description = "Dimensionamento hidráulico completo",
                RelativePath = @"09_Dimensionamento\09_Dimensionamento.dyn",
                Etapa = "E09",
                Ordem = 9,
                InputNodeNames = new() { "PipeIds", "SystemType" },
                OutputNodeNames = new() { "Resultados", "Criticos" },
                Obrigatorio = true
            });

            Registrar(new WorkspaceInfo
            {
                Id = "E10-Tabelas",
                Name = "Tabelas",
                Description = "Geração de tabelas e quantitativos",
                RelativePath = @"10_Tabelas\10_Tabelas.dyn",
                Etapa = "E10",
                Ordem = 10,
                InputNodeNames = new(),
                OutputNodeNames = new() { "ScheduleIds" },
                Obrigatorio = true
            });

            Registrar(new WorkspaceInfo
            {
                Id = "E11-Pranchas",
                Name = "Pranchas",
                Description = "Montagem de pranchas finais",
                RelativePath = @"11_Pranchas\11_Pranchas.dyn",
                Etapa = "E11",
                Ordem = 11,
                InputNodeNames = new() { "ViewIds", "ScheduleIds" },
                OutputNodeNames = new() { "SheetIds" },
                Obrigatorio = true
            });
        }
    }
}
