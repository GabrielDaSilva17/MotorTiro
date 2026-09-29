using System;
using System.IO;
using System.Numerics;
using System.Text.Json;

namespace JogoDeTiro.EditorDeLevel
{
    public static class GerenciadorMapa
    {
        private static readonly JsonSerializerOptions opcoesJson = new JsonSerializerOptions
        {
            WriteIndented = true,
            IncludeFields = true,
            PropertyNameCaseInsensitive = true
        };

        // Garante que o arquivo seja salvo na pasta fixa do projeto e não seja apagado no build
        public static string ObterCaminhoPersistente()
        {
            string pastaBase = AppDomain.CurrentDomain.BaseDirectory;
            DirectoryInfo? dir = new DirectoryInfo(pastaBase);

            // Se estiver dentro de bin/Debug/net..., sobe para a raiz real do projeto
            while (dir != null && (dir.Name.Equals("bin", StringComparison.OrdinalIgnoreCase) || 
                                   dir.Name.Equals("Debug", StringComparison.OrdinalIgnoreCase) || 
                                   dir.Name.StartsWith("net", StringComparison.OrdinalIgnoreCase)))
            {
                dir = dir.Parent;
            }

            string pastaProjeto = dir != null ? dir.FullName : Directory.GetCurrentDirectory();
            string pastaMapas = Path.Combine(pastaProjeto, "mapas");

            if (!Directory.Exists(pastaMapas))
            {
                Directory.CreateDirectory(pastaMapas);
            }

            return Path.Combine(pastaMapas, "mapa_teste.json");
        }

        public static bool SalvarMapa(MapaData mapa, string caminhoArquivo)
        {
            try
            {
                string pasta = Path.GetDirectoryName(caminhoArquivo) ?? "";
                if (!string.IsNullOrEmpty(pasta) && !Directory.Exists(pasta))
                {
                    Directory.CreateDirectory(pasta);
                }

                string json = JsonSerializer.Serialize(mapa, opcoesJson);
                File.WriteAllText(caminhoArquivo, json);
                Console.WriteLine($"[EDITOR] Mapa salvo com sucesso em: {caminhoArquivo}");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERRO] Falha ao salvar: {ex.Message}");
                return false;
            }
        }

        public static MapaData CarregarMapa(string caminhoArquivo)
        {
            try
            {
                if (!File.Exists(caminhoArquivo))
                {
                    var novo = CriarMapaPadrao();
                    SalvarMapa(novo, caminhoArquivo);
                    return novo;
                }

                string json = File.ReadAllText(caminhoArquivo);
                MapaData? mapa = JsonSerializer.Deserialize<MapaData>(json, opcoesJson);
                return mapa ?? CriarMapaPadrao();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERRO] Falha ao ler mapa: {ex.Message}. Carregando padrão.");
                return CriarMapaPadrao();
            }
        }

        public static MapaData CriarMapaPadrao()
        {
            var mapa = new MapaData
            {
                NomeMapa = "Fase 1 - Laboratório PB2",
                SpawnPlayerX = 250.0f,
                SpawnPlayerY = 380.0f
            };

            // Chão e plataformas
            mapa.Paredes.Add(new ParedeData { X = -300.0f, Y = 460.0f, Largura = 1800.0f, Altura = 80.0f });
            mapa.Paredes.Add(new ParedeData { X = 200.0f, Y = 340.0f, Largura = 160.0f, Altura = 24.0f });
            mapa.Paredes.Add(new ParedeData { X = 460.0f, Y = 260.0f, Largura = 180.0f, Altura = 24.0f });

            // Inimigos de teste iniciais
            mapa.SpawnsInimigos.Add(new InimigoSpawnData(540.0f, 260.0f));
            mapa.SpawnsInimigos.Add(new InimigoSpawnData(780.0f, 460.0f));

            return mapa;
        }
    }
}