using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using System.Text.Json;
using JogoDeTiro.EditorDeLevel;

namespace JogoDeTiro
{
    public enum DificuldadeJogo
    {
        Facil,
        Normal,
        Dificil,
        Impossivel
    }

    public class DefinicaoLevel
    {
        public int Numero { get; set; }
        public string Titulo { get; set; } = "";
        public string Capitulo { get; set; } = "";
        public string Briefing { get; set; } = "";
        public int QtdInimigos { get; set; } = 3;
        public bool Desbloqueado { get; set; } = false;
        public bool Concluido { get; set; } = false;
        public float MelhorTempo { get; set; } = 0.0f;
        public CorpoBot FaccaoInimiga { get; set; } = CorpoBot.CivilSecurity;
        public TipoArmaBot ArmaDestaque { get; set; } = TipoArmaBot.M4A1;

        public DefinicaoLevel() { }

        public DefinicaoLevel(int numero, string titulo, string capitulo, string briefing, int qtdInimigos, CorpoBot faccao, TipoArmaBot arma)
        {
            Numero = numero;
            Titulo = titulo;
            Capitulo = capitulo;
            Briefing = briefing;
            QtdInimigos = qtdInimigos;
            FaccaoInimiga = faccao;
            ArmaDestaque = arma;
            Desbloqueado = (numero == 1);
        }
    }

    public class ProgressoCampanha
    {
        public int UltimoLevelDesbloqueado { get; set; } = 1;
        public DificuldadeJogo UltimaDificuldade { get; set; } = DificuldadeJogo.Normal;
        public Dictionary<int, bool> FasesConcluidas { get; set; } = new Dictionary<int, bool>();
        public Dictionary<int, float> MelhoresTempos { get; set; } = new Dictionary<int, float>();
    }

    public class CampanhaPB2
    {
        public const int TOTAL_LEVELS = 45;
        public List<DefinicaoLevel> Levels { get; private set; } = new List<DefinicaoLevel>();
        public DificuldadeJogo DificuldadeAtual { get; set; } = DificuldadeJogo.Normal;
        public int LevelSelecionadoIndex { get; set; } = 0; // 0-indexed (Level 1)
        public DefinicaoLevel LevelAtual => Levels[Math.Clamp(LevelSelecionadoIndex, 0, Levels.Count - 1)];

        public CampanhaPB2()
        {
            InicializarCampanha45Levels();
            CarregarProgresso();
        }

        private void InicializarCampanha45Levels()
        {
            Levels.Clear();

            string[] nomesCapitulos = new string[]
            {
                "Capítulo 1: O Despertar da IA",
                "Capítulo 2: Linha de Frente Falkok",
                "Capítulo 3: Complexo de Pesquisa C-01",
                "Capítulo 4: Fortaleza Usurpation",
                "Capítulo 5: O Confronto Final"
            };

            for (int i = 1; i <= TOTAL_LEVELS; i++)
            {
                int capIdx = (i - 1) / 9;
                string capNome = nomesCapitulos[Math.Clamp(capIdx, 0, nomesCapitulos.Length - 1)];

                string titulo = i switch
                {
                    1 => "Reativação do Sistema",
                    2 => "Posto Avançado Sul",
                    3 => "Armazém Abandonado",
                    4 => "Setor de Quarentena",
                    5 => "Ponte Elevada",
                    6 => "Refinaria Subterrânea",
                    7 => "Emboscada nos Trilhos",
                    8 => "Laboratório de Plasma",
                    9 => "Fuga dos Dutos Centrais",

                    10 => "Território Hostil",
                    11 => "Patrulha Falkok",
                    12 => "Canyon Metálico",
                    13 => "Torre de Transmissão",
                    14 => "Campo Minado e Barricadas",
                    15 => "Silo de Combustível",
                    16 => "Plataforma de Lançamento",
                    17 => "Bunker Alienígena",
                    18 => "Comandante Falkok",

                    19 => "Infiltração Noturna",
                    20 => "Gerador de Energia C-01",
                    21 => "Câmara Criogênica",
                    22 => "Hangar dos Caças",
                    23 => "Corredores Blindados",
                    24 => "Setor de Manutenção Pesada",
                    25 => "Cúpula de Testes Balísticos",
                    26 => "Linha de Montagem de Androides",
                    27 => "Sala de Servidores Centrais",

                    28 => "Cerco à Cidade Alta",
                    29 => "Ruas Devastadas",
                    30 => "Estação Monotrilho",
                    31 => "QG da Usurpation",
                    32 => "Telhados em Chamas",
                    33 => "Avenida dos Snipers",
                    34 => "Fosso de Elevação",
                    35 => "Arsenal Pesado CS",
                    36 => "General Usurpation",

                    37 => "Entrada da Cidadela",
                    38 => "Labirinto de Escudos",
                    39 => "Sala do Reator de Fusão",
                    40 => "Gantry do Canhão Orbital",
                    41 => "Última Trincheira",
                    42 => "Plataforma de Evacuação",
                    43 => "Santuário Cibernético",
                    44 => "Guarda de Elite do Mestre",
                    45 => "Plazma Burst - Batalha Final",

                    _ => $"Missão Tática #{i}"
                };

                string briefing = i switch
                {
                    1 => "Infiltre-se no complexo inicial e neutralize as sentinelas de segurança civil.",
                    9 => "Elimine todos os agentes de reforço e alcance o ponto de evacuação.",
                    18 => "Derrube o esquadrão tático alienígena Falkok com armas de alto calibre.",
                    27 => "Acesse o mainframe principal e impeça a propagação do protocolo inimigo.",
                    36 => "Combate pesado em ambiente urbano. Use granadas e snipers contra as posições fortificadas.",
                    45 => "A batalha decisiva pelo destino do planeta. Derrote todas as forças de elite!",
                    _ => $"Elimine todas as forças inimigas no setor e garanta o avanço seguro para o Level {i + 1}."
                };

                int inimigos = 2 + (i / 4);
                if (inimigos > 8) inimigos = 8;

                CorpoBot faccao = capIdx switch
                {
                    0 => CorpoBot.CivilSecurity,
                    1 => CorpoBot.FalkokAlien,
                    2 => CorpoBot.AndroidHeavy,
                    3 => CorpoBot.UsurpationElite,
                    _ => CorpoBot.NoirMercenary
                };

                TipoArmaBot arma = (i % 4) switch
                {
                    0 => TipoArmaBot.M4A1,
                    1 => TipoArmaBot.Plasma,
                    2 => TipoArmaBot.Shotgun,
                    _ => TipoArmaBot.Pistol
                };

                Levels.Add(new DefinicaoLevel(i, titulo, capNome, briefing, inimigos, faccao, arma));
            }
        }

        public float ObterMultiplicadorDanoInimigo() => DificuldadeAtual switch
        {
            DificuldadeJogo.Facil => 0.65f,
            DificuldadeJogo.Normal => 1.0f,
            DificuldadeJogo.Dificil => 1.35f,
            DificuldadeJogo.Impossivel => 1.80f,
            _ => 1.0f
        };

        public float ObterMultiplicadorReacaoInimigo() => DificuldadeAtual switch
        {
            DificuldadeJogo.Facil => 1.45f,      // Reage mais devagar
            DificuldadeJogo.Normal => 1.0f,       // Padrão PB2
            DificuldadeJogo.Dificil => 0.70f,     // Reação rápida
            DificuldadeJogo.Impossivel => 0.40f,  // Quase instantâneo (Havoc)
            _ => 1.0f
        };

        public MapaData GerarOuCarregarMapaLevel(int numeroLevel, string caminhoArquivoCustomizado = "")
        {
            // Se houver arquivo customizado salvo para essa fase, carrega ele
            if (!string.IsNullOrEmpty(caminhoArquivoCustomizado) && File.Exists(caminhoArquivoCustomizado))
            {
                return GerenciadorMapa.CarregarMapa(caminhoArquivoCustomizado);
            }

            var def = Levels[Math.Clamp(numeroLevel - 1, 0, Levels.Count - 1)];
            MapaData mapa = new MapaData();
            mapa.NomeMapa = $"Campanha PB2 - Level {numeroLevel}: {def.Titulo}";
            mapa.SpawnPlayerX = 220.0f;
            mapa.SpawnPlayerY = 380.0f;

            const float chaoY = 480.0f;

            // Chão base extenso
            mapa.Paredes.Add(new ParedeData { X = -600, Y = chaoY, Largura = 3200, Altura = 300 });

            // Parede esquerda do mapa
            mapa.Paredes.Add(new ParedeData { X = -600, Y = -400, Largura = 120, Altura = 1200 });

            // Layout arquitetural procedural inteligente baseado no número do level e capítulo
            int variante = (numeroLevel % 5);
            switch (variante)
            {
                case 0:
                    // Arena multinível com torre central
                    mapa.Paredes.Add(new ParedeData { X = 320, Y = chaoY - 80, Largura = 160, Altura = 80 });
                    mapa.Paredes.Add(new ParedeData { X = 540, Y = chaoY - 160, Largura = 220, Altura = 25 });
                    mapa.Paredes.Add(new ParedeData { X = 840, Y = chaoY - 240, Largura = 280, Altura = 25 });
                    mapa.Paredes.Add(new ParedeData { X = 1200, Y = chaoY - 110, Largura = 180, Altura = 110 });
                    mapa.Paredes.Add(new ParedeData { X = 1450, Y = -200, Largura = 120, Altura = 1000 }); // Parede final
                    break;

                case 1:
                    // Complexo industrial com passadiços e caixas de cobertura
                    mapa.Paredes.Add(new ParedeData { X = 400, Y = chaoY - 70, Largura = 60, Altura = 70 });
                    mapa.Paredes.Add(new ParedeData { X = 520, Y = chaoY - 120, Largura = 200, Altura = 24 });
                    mapa.Paredes.Add(new ParedeData { X = 780, Y = chaoY - 70, Largura = 60, Altura = 70 });
                    mapa.Paredes.Add(new ParedeData { X = 900, Y = chaoY - 150, Largura = 220, Altura = 24 });
                    mapa.Paredes.Add(new ParedeData { X = 1180, Y = chaoY - 90, Largura = 70, Altura = 90 });
                    mapa.Paredes.Add(new ParedeData { X = 1350, Y = -200, Largura = 120, Altura = 1000 });
                    break;

                case 2:
                    // Canyon / Andares elevados estilo PB2
                    mapa.Paredes.Add(new ParedeData { X = 350, Y = chaoY - 140, Largura = 240, Altura = 25 });
                    mapa.Paredes.Add(new ParedeData { X = 660, Y = chaoY - 220, Largura = 200, Altura = 25 });
                    mapa.Paredes.Add(new ParedeData { X = 930, Y = chaoY - 140, Largura = 240, Altura = 25 });
                    mapa.Paredes.Add(new ParedeData { X = 600, Y = chaoY - 80, Largura = 320, Altura = 80 });
                    mapa.Paredes.Add(new ParedeData { X = 1250, Y = -200, Largura = 120, Altura = 1000 });
                    break;

                case 3:
                    // Fortaleza com pilares de sustentação
                    mapa.Paredes.Add(new ParedeData { X = 380, Y = chaoY - 100, Largura = 50, Altura = 100 });
                    mapa.Paredes.Add(new ParedeData { X = 480, Y = chaoY - 130, Largura = 250, Altura = 25 });
                    mapa.Paredes.Add(new ParedeData { X = 800, Y = chaoY - 190, Largura = 260, Altura = 25 });
                    mapa.Paredes.Add(new ParedeData { X = 1120, Y = chaoY - 130, Largura = 250, Altura = 25 });
                    mapa.Paredes.Add(new ParedeData { X = 1440, Y = -200, Largura = 120, Altura = 1000 });
                    break;

                default:
                    // Bunker subterrâneo simétrico
                    mapa.Paredes.Add(new ParedeData { X = 320, Y = chaoY - 60, Largura = 70, Altura = 60 });
                    mapa.Paredes.Add(new ParedeData { X = 450, Y = chaoY - 150, Largura = 350, Altura = 26 });
                    mapa.Paredes.Add(new ParedeData { X = 860, Y = chaoY - 100, Largura = 80, Altura = 100 });
                    mapa.Paredes.Add(new ParedeData { X = 1000, Y = chaoY - 170, Largura = 260, Altura = 26 });
                    mapa.Paredes.Add(new ParedeData { X = 1320, Y = -200, Largura = 120, Altura = 1000 });
                    break;
            }

            // Distribuição tática dos Bots no cenário
            int countInimigos = def.QtdInimigos;
            float espacamento = 180.0f;
            float posXInicial = 560.0f;

            for (int i = 0; i < countInimigos; i++)
            {
                float posX = posXInicial + (i * espacamento);
                float posY = chaoY;

                // Coloca alguns inimigos nas plataformas elevadas
                if (i == 1 && mapa.Paredes.Count > 3)
                {
                    var plat = mapa.Paredes[3];
                    posX = plat.X + (plat.Largura * 0.5f);
                    posY = plat.Y;
                }
                else if (i == 2 && mapa.Paredes.Count > 4)
                {
                    var plat = mapa.Paredes[4];
                    posX = plat.X + (plat.Largura * 0.5f);
                    posY = plat.Y;
                }

                string tipoArma = ((TipoArmaBot)((i + numeroLevel) % 4)).ToString();
                mapa.SpawnsInimigos.Add(new InimigoSpawnData(posX, posY) { Tipo = tipoArma });
            }

            return mapa;
        }

        public void ConcluirLevel(int numeroLevel, float tempoGasto)
        {
            if (numeroLevel >= 1 && numeroLevel <= TOTAL_LEVELS)
            {
                var lvl = Levels[numeroLevel - 1];
                lvl.Concluido = true;
                if (lvl.MelhorTempo <= 0.0f || tempoGasto < lvl.MelhorTempo)
                {
                    lvl.MelhorTempo = tempoGasto;
                }

                // Desbloqueia o próximo level
                if (numeroLevel < TOTAL_LEVELS)
                {
                    Levels[numeroLevel].Desbloqueado = true;
                }

                SalvarProgresso();
            }
        }

        public void DesbloquearTodosLevels()
        {
            for (int i = 0; i < Levels.Count; i++)
            {
                Levels[i].Desbloqueado = true;
            }
            SalvarProgresso();
        }

        private string ObterCaminhoProgresso()
        {
            string pastaMapas = Path.GetDirectoryName(GerenciadorMapa.ObterCaminhoPersistente()) ?? "";
            return Path.Combine(pastaMapas, "campanha_save.json");
        }

        public void SalvarProgresso()
        {
            try
            {
                ProgressoCampanha p = new ProgressoCampanha
                {
                    UltimaDificuldade = DificuldadeAtual,
                    UltimoLevelDesbloqueado = 1
                };

                for (int i = 0; i < Levels.Count; i++)
                {
                    if (Levels[i].Desbloqueado) p.UltimoLevelDesbloqueado = Math.Max(p.UltimoLevelDesbloqueado, Levels[i].Numero);
                    if (Levels[i].Concluido) p.FasesConcluidas[Levels[i].Numero] = true;
                    if (Levels[i].MelhorTempo > 0) p.MelhoresTempos[Levels[i].Numero] = Levels[i].MelhorTempo;
                }

                string json = JsonSerializer.Serialize(p, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(ObterCaminhoProgresso(), json);
            }
            catch { }
        }

        public void CarregarProgresso()
        {
            try
            {
                string caminho = ObterCaminhoProgresso();
                if (File.Exists(caminho))
                {
                    string json = File.ReadAllText(caminho);
                    var p = JsonSerializer.Deserialize<ProgressoCampanha>(json);
                    if (p != null)
                    {
                        DificuldadeAtual = p.UltimaDificuldade;
                        for (int i = 0; i < Levels.Count; i++)
                        {
                            int num = Levels[i].Numero;
                            if (num <= p.UltimoLevelDesbloqueado) Levels[i].Desbloqueado = true;
                            if (p.FasesConcluidas.TryGetValue(num, out bool c) && c) Levels[i].Concluido = true;
                            if (p.MelhoresTempos.TryGetValue(num, out float t)) Levels[i].MelhorTempo = t;
                        }
                    }
                }
            }
            catch { }
        }
    }
}
