using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using Raylib_cs;

namespace JogoDeTiro
{
    public enum EstadoIABot
    {
        Patrulhando,
        Alerta,
        Atacando,
        Morto
    }

    public enum TipoArmaBot
    {
        M4A1,
        Plasma,
        Shotgun,
        Pistol
    }

    public enum CorpoBot
    {
        CivilSecurity,   // Tático Azul militar PB2 (Segurança Civil)
        FalkokAlien,     // Exosqueleto Bronze / Âmbar PB2 (Alienígena Falkok)
        AndroidHeavy,    // Blindagem Titânio / Cromo com visor duplo cibernético
        UsurpationElite, // Armadura Carmesim / Bordô escuro PB2
        NoirMercenary    // Traje Escuro Mercenário / Roxo
    }

    public class DefinicaoCorpoBot
    {
        public string NomeFaccao = "Civil Security";
        public Color CorDianteira = new Color(75, 115, 165, 255);
        public Color CorTraseira = new Color(50, 78, 120, 255);
        public Color CorArticulacao = new Color(26, 38, 60, 255);
        public Color CorVisor = new Color(0, 240, 255, 255);
        public Color CorVisorBrilho = Color.White;
        public Color CorSangue = new Color(140, 20, 20, 190);
        public float EscalaCorpo = 1.0f;
        public bool VisorDuplo = false;
        public float OffsetVisorY = -12.0f;
    }

    public class DefinicaoArmaBot
    {
        public string Nome = "Arma";
        public Texture2D Textura;
        public Sound SomTiro;
        public float Cadencia = 0.12f;
        public int Pellets = 1;
        public float Dispersao = 0.03f;
        public float VelocidadeProjetil = 980.0f;
        public Color CorProjetil = Color.Red;
        public string TipoProjetil = "plasma";
        public float RecuoLinear = 6.0f;
        public float RecuoRotacao = 8.5f;
        public float ComprimentoCano = 32.0f;
        public float AlturaCanoY = -1.0f;
        public float DistanciaOmbro = 21.0f;
        public float PivotNormX = 0.25f;
        public float PivotNormY = 0.50f;
        public float OffsetGatilhoX = 0.0f;
        public float OffsetGatilhoY = 3.0f;
        public float OffsetSuporteX = 18.0f;
        public float OffsetSuporteY = 1.5f;
        public float Escala = 0.85f;
        public bool TemLaser = false;
        public Color CorLaser = new Color(255, 40, 40, 220);
        public int TirosPorBurst = 3;
        public float PausaBurstMin = 0.65f;
        public float PausaBurstMax = 1.2f;
        public float DistanciaIdealMin = 140.0f;
        public float DistanciaIdealMax = 320.0f;
    }

    public class BotIAPB2 : IDisposable
    {
        // -------------------------------------------------------------
        // Recursos Compartilhados (Carregados uma única vez na memória)
        // -------------------------------------------------------------
        public static SkinModular? SkinCompartilhada { get; private set; }
        public static Dictionary<TipoArmaBot, DefinicaoArmaBot> ArsenalBot = new Dictionary<TipoArmaBot, DefinicaoArmaBot>();
        public static bool RecursosCarregados { get; private set; } = false;

        public static void InicializarRecursos(string diretorioAssets)
        {
            if (RecursosCarregados) return;

            SkinCompartilhada = SkinModular.CarregarPadrao(diretorioAssets);

            string pastaArmas = Path.Combine(diretorioAssets, "sprites", "weapons");
            string pastaSons = Path.Combine(diretorioAssets, "sounds");

            // 1. M4A1 Assault Rifle (Fuzil de Assalto Militar)
            ArsenalBot[TipoArmaBot.M4A1] = new DefinicaoArmaBot
            {
                Nome = "M4A1 Rifle",
                Textura = CarregarTexturaSegura(Path.Combine(pastaArmas, "gun_m4a1.png")),
                SomTiro = CarregarSomSeguro(Path.Combine(pastaSons, "70_wea_rifle.mp3")),
                Cadencia = 0.095f,
                Pellets = 1,
                Dispersao = 0.035f,
                VelocidadeProjetil = 1180.0f,
                CorProjetil = new Color(255, 215, 60, 255),
                TipoProjetil = "m4a1",
                RecuoLinear = 6.0f,
                RecuoRotacao = 7.5f,
                ComprimentoCano = 32.0f,
                AlturaCanoY = -1.0f,
                DistanciaOmbro = 22.0f,
                PivotNormX = 0.22f,
                PivotNormY = 0.50f,
                OffsetGatilhoX = 0.0f,
                OffsetGatilhoY = 3.0f,
                OffsetSuporteX = 16.0f,
                OffsetSuporteY = 1.5f,
                Escala = 0.85f,
                TirosPorBurst = 3,
                PausaBurstMin = 0.60f,
                PausaBurstMax = 1.10f,
                DistanciaIdealMin = 160.0f,
                DistanciaIdealMax = 330.0f
            };

            // 2. Fuzil de Plasma C-01p
            ArsenalBot[TipoArmaBot.Plasma] = new DefinicaoArmaBot
            {
                Nome = "C-01p Plasma",
                Textura = CarregarTexturaSegura(Path.Combine(pastaArmas, "gun_plasma.png")),
                SomTiro = CarregarSomSeguro(Path.Combine(pastaSons, "84_wea_plasmagun.mp3")),
                Cadencia = 0.11f,
                Pellets = 1,
                Dispersao = 0.040f,
                VelocidadeProjetil = 980.0f,
                CorProjetil = new Color(255, 65, 65, 255),
                TipoProjetil = "plasma",
                RecuoLinear = 5.5f,
                RecuoRotacao = 6.5f,
                ComprimentoCano = 28.0f,
                AlturaCanoY = -2.0f,
                DistanciaOmbro = 20.0f,
                PivotNormX = 0.31f,
                PivotNormY = 0.44f,
                OffsetGatilhoX = 0.0f,
                OffsetGatilhoY = 3.0f,
                OffsetSuporteX = 18.0f,
                OffsetSuporteY = 1.5f,
                Escala = 0.85f,
                TirosPorBurst = 4,
                PausaBurstMin = 0.70f,
                PausaBurstMax = 1.25f,
                DistanciaIdealMin = 140.0f,
                DistanciaIdealMax = 310.0f
            };

            // 3. Escopeta CS-DAZ (Shotgun PB2 de alto impacto em curta distância)
            ArsenalBot[TipoArmaBot.Shotgun] = new DefinicaoArmaBot
            {
                Nome = "CS-DAZ Shotgun",
                Textura = CarregarTexturaSegura(Path.Combine(pastaArmas, "gun_shotgun.png")),
                SomTiro = CarregarSomSeguro(Path.Combine(pastaSons, "66_wea_shotgun.mp3")),
                Cadencia = 0.80f,
                Pellets = 5,
                Dispersao = 0.18f,
                VelocidadeProjetil = 880.0f,
                CorProjetil = new Color(255, 140, 40, 255),
                TipoProjetil = "shotgun",
                RecuoLinear = 13.0f,
                RecuoRotacao = 18.0f,
                ComprimentoCano = 29.5f,
                AlturaCanoY = -1.5f,
                DistanciaOmbro = 20.0f,
                PivotNormX = 0.29f,
                PivotNormY = 0.50f,
                OffsetGatilhoX = 0.0f,
                OffsetGatilhoY = 3.0f,
                OffsetSuporteX = 18.0f,
                OffsetSuporteY = 1.5f,
                Escala = 0.85f,
                TirosPorBurst = 1,
                PausaBurstMin = 0.75f,
                PausaBurstMax = 1.15f,
                DistanciaIdealMin = 80.0f,
                DistanciaIdealMax = 200.0f
            };

            // 4. Pistola Laser Falcon
            ArsenalBot[TipoArmaBot.Pistol] = new DefinicaoArmaBot
            {
                Nome = "Falcon Laser",
                Textura = CarregarTexturaSegura(Path.Combine(pastaArmas, "gun_pistol2.png")),
                SomTiro = CarregarSomSeguro(Path.Combine(pastaSons, "102_wea_apistol.mp3")),
                Cadencia = 0.18f,
                Pellets = 1,
                Dispersao = 0.015f,
                VelocidadeProjetil = 1100.0f,
                CorProjetil = new Color(255, 190, 45, 255),
                TipoProjetil = "pistol",
                RecuoLinear = 6.0f,
                RecuoRotacao = 14.0f,
                ComprimentoCano = 26.0f,
                AlturaCanoY = 0.0f,
                DistanciaOmbro = 36.0f,
                PivotNormX = 0.33f,
                PivotNormY = 0.58f,
                OffsetGatilhoX = -19.0f,
                OffsetGatilhoY = 2.5f,
                OffsetSuporteX = -11.5f,
                OffsetSuporteY = 2.5f,
                Escala = 0.90f,
                TemLaser = true,
                CorLaser = new Color(255, 30, 30, 220),
                TirosPorBurst = 2,
                PausaBurstMin = 0.50f,
                PausaBurstMax = 0.90f,
                DistanciaIdealMin = 130.0f,
                DistanciaIdealMax = 280.0f
            };

            RecursosCarregados = true;
        }

        private static Texture2D CarregarTexturaSegura(string caminho)
        {
            if (File.Exists(caminho))
            {
                Texture2D tex = Raylib.LoadTexture(caminho);
                Raylib.SetTextureFilter(tex, TextureFilter.Bilinear);
                return tex;
            }
            return default;
        }

        private static Sound CarregarSomSeguro(string caminho)
        {
            if (File.Exists(caminho))
            {
                return Raylib.LoadSound(caminho);
            }
            return default;
        }

        public static void DescarregarRecursos()
        {
            if (!RecursosCarregados) return;

            SkinCompartilhada?.Dispose();
            SkinCompartilhada = null;

            foreach (var arma in ArsenalBot.Values)
            {
                if (arma.Textura.Id != 0) Raylib.UnloadTexture(arma.Textura);
                if (arma.SomTiro.Stream.Buffer != 0) Raylib.UnloadSound(arma.SomTiro);
            }
            ArsenalBot.Clear();

            RecursosCarregados = false;
        }

        // -------------------------------------------------------------
        // Estado e Cinemática
        // -------------------------------------------------------------
        public Vector2 PosicaoSpawn;
        public Vector2 Posicao;
        public Vector2 Velocidade;
        public float DirecaoOlhar = 1.0f; // 1 = Direita, -1 = Esquerda
        public float AnguloMiraRad = 0.0f;
        public float AnguloMiraGraus = 0.0f;
        public bool NoChao = false;

        // Membros anatômicos
        private const float ComprimentoCoxa = 19.5f;
        private const float ComprimentoCanela = 18.5f;
        private const float ComprimentoBraco = 14.5f;
        private const float ComprimentoAntebraco = 14.5f;

        // Físicas de combate
        public float VelocidadeMovimento = 185.0f;
        public float Aceleracao = 950.0f;
        public float Friccao = 880.0f;
        public float Gravidade = 1250.0f;
        public float ForcaSalto = -460.0f;

        // Animação procedural viva
        private float cicloAndar = 0.0f;
        private float bobbingTroncoY = 0.0f;
        private float bobbingCabecaY = 0.0f;
        private float rotacaoCabeca = 0.0f;
        private float inclinacaoCorpo = 0.0f;

        // Saúde e Dano
        public float VidaMax = 120.0f;
        public float VidaAtual = 120.0f;
        public bool Vivo => VidaAtual > 0.0f;
        private float timerFlashDano = 0.0f;
        private float timerRespawn = 0.0f;

        // -------------------------------------------------------------
        // Sistema de Morte com Ragdoll Direcionado (Plazma Burst 2)
        // -------------------------------------------------------------
        private float direcaoImpactoMorteX = 1.0f;
        private float rotacaoCorpoMorte = 0.0f;
        private float velRotacaoCorpoMorte = 0.0f;

        // Arma arremessada no chão ao morrer
        private bool armaDropada = false;
        private Vector2 posArmaDropada;
        private Vector2 velArmaDropada;
        private float rotArmaDropada;
        private float velRotArmaDropada;

        // Arma equipada do Bot
        public TipoArmaBot TipoArmaEquipada { get; set; } = TipoArmaBot.M4A1;
        public DefinicaoArmaBot ArmaAtual => ArsenalBot.TryGetValue(TipoArmaEquipada, out var a) ? a : ArsenalBot[TipoArmaBot.M4A1];

        // Corpo e Facção do Bot (Visual, Visor, Blindagem e Escala)
        public static readonly Dictionary<CorpoBot, DefinicaoCorpoBot> PerfisCorpoBot = new()
        {
            [CorpoBot.CivilSecurity] = new DefinicaoCorpoBot
            {
                NomeFaccao = "Civil Security",
                CorDianteira = new Color(75, 115, 165, 255),
                CorTraseira = new Color(50, 78, 120, 255),
                CorArticulacao = new Color(26, 38, 60, 255),
                CorVisor = new Color(0, 240, 255, 255),
                CorVisorBrilho = Color.White,
                CorSangue = new Color(140, 20, 20, 190),
                EscalaCorpo = 1.0f,
                VisorDuplo = false
            },
            [CorpoBot.FalkokAlien] = new DefinicaoCorpoBot
            {
                NomeFaccao = "Falkok Alien",
                CorDianteira = new Color(195, 120, 50, 255),
                CorTraseira = new Color(140, 85, 35, 255),
                CorArticulacao = new Color(65, 40, 20, 255),
                CorVisor = new Color(255, 210, 0, 255),
                CorVisorBrilho = new Color(255, 255, 180, 255),
                CorSangue = new Color(185, 140, 20, 200),
                EscalaCorpo = 1.04f,
                VisorDuplo = false
            },
            [CorpoBot.AndroidHeavy] = new DefinicaoCorpoBot
            {
                NomeFaccao = "Android Unit",
                CorDianteira = new Color(215, 225, 235, 255),
                CorTraseira = new Color(145, 155, 170, 255),
                CorArticulacao = new Color(50, 55, 68, 255),
                CorVisor = new Color(60, 140, 255, 255),
                CorVisorBrilho = Color.White,
                CorSangue = new Color(40, 120, 190, 210), // Óleo/fluido sintético azul
                EscalaCorpo = 0.98f,
                VisorDuplo = true
            },
            [CorpoBot.UsurpationElite] = new DefinicaoCorpoBot
            {
                NomeFaccao = "Usurpation Elite",
                CorDianteira = new Color(225, 55, 55, 255),
                CorTraseira = new Color(155, 35, 35, 255),
                CorArticulacao = new Color(75, 18, 18, 255),
                CorVisor = new Color(255, 30, 30, 255),
                CorVisorBrilho = new Color(255, 190, 190, 255),
                CorSangue = new Color(160, 15, 15, 210),
                EscalaCorpo = 1.02f,
                VisorDuplo = false
            },
            [CorpoBot.NoirMercenary] = new DefinicaoCorpoBot
            {
                NomeFaccao = "Noir Mercenary",
                CorDianteira = new Color(72, 65, 90, 255),
                CorTraseira = new Color(46, 42, 60, 255),
                CorArticulacao = new Color(24, 20, 32, 255),
                CorVisor = new Color(205, 80, 255, 255),
                CorVisorBrilho = Color.White,
                CorSangue = new Color(110, 20, 55, 190),
                EscalaCorpo = 1.0f,
                VisorDuplo = false
            }
        };

        public CorpoBot TipoCorpo { get; set; } = CorpoBot.CivilSecurity;
        public DefinicaoCorpoBot CorpoAtual => PerfisCorpoBot.TryGetValue(TipoCorpo, out var c) ? c : PerfisCorpoBot[CorpoBot.CivilSecurity];

        // Recuo e Disparo
        private float cooldownTiro = 0.0f;
        private float recuoLinearAtual = 0.0f;
        private float recuoRotacaoAtual = 0.0f;
        private float timerFlashCano = 0.0f;
        private int tirosNoBurst = 0;
        private float pausaEntreBursts = 0.0f;

        // -------------------------------------------------------------
        // Sistema de Sentidos e IA estilo Plazma Burst 2
        // -------------------------------------------------------------
        public EstadoIABot EstadoAtual { get; private set; } = EstadoIABot.Patrulhando;
        private float timerTrocaPatrulha = 2.0f;
        private float direcaoPatrulha = 1.0f;
        private float tempoVisaoPerdida = 0.0f;
        private float cooldownPulo = 0.0f;

        // Comportamento Orgânico de Patrulha PB2: 0 = ParadoObservando, 1 = Caminhando, 2 = Correndo
        private int botEstadoPatrulha = 1;
        private float timerOlharPatrulha = 1.0f;
        private float offsetOlharYRad = 0.0f;
        private float direcaoOlharParado = 1.0f;
        private float cooldownPuloPatrulha = 3.0f;

        // Inteligência Tática de Combate: Strafe e Evasão
        private float timerStrafeCombate = 0.5f;
        private float direcaoStrafeCombate = 1.0f;

        // Percepção e Tempo de Reação (Surpresa / Alerta com '!')
        private float timerReacaoAlerta = 0.0f;
        private bool jogadorDetectadoAnterior = false;
        private Vector2 ultimaPosicaoConhecidaJogador;

        public string NomeId = "#actor";

        public Rectangle CaixaColisao => new Rectangle(
            Posicao.X - 16.0f,
            Posicao.Y - 62.0f,
            32.0f,
            62.0f
        );

        public Vector2 CentroVisual => Posicao - new Vector2(0, 18.0f) + new Vector2(0, bobbingTroncoY);

        public BotIAPB2(Vector2 posicaoInicial, string diretorioAssets, string nome = "#actor", TipoArmaBot? arma = null, CorpoBot? corpo = null)
        {
            PosicaoSpawn = posicaoInicial;
            Posicao = posicaoInicial;
            NomeId = nome;
            VidaAtual = VidaMax;

            InicializarRecursos(diretorioAssets);

            if (arma.HasValue) TipoArmaEquipada = arma.Value;
            else TipoArmaEquipada = TipoArmaBot.M4A1;

            if (corpo.HasValue) TipoCorpo = corpo.Value;
            else TipoCorpo = CorpoBot.CivilSecurity;
        }

        // Permite definir a arma e corpo do bot
        public void ConfigurarArma(TipoArmaBot arma) => TipoArmaEquipada = arma;
        public void ConfigurarCorpo(CorpoBot corpo) => TipoCorpo = corpo;

        // -------------------------------------------------------------
        // Sistema de Audição: Disparos próximos alertam o bot
        // -------------------------------------------------------------
        public void NotificarDisparoJogador(Vector2 posicaoDisparo, float raioAudicao = 540.0f)
        {
            if (!Vivo) return;

            float dist = Vector2.Distance(Posicao, posicaoDisparo);
            if (dist <= raioAudicao)
            {
                // Vira imediatamente em direção ao barulho do tiro
                DirecaoOlhar = MathF.Sign(posicaoDisparo.X - Posicao.X);
                if (DirecaoOlhar == 0.0f) DirecaoOlhar = 1.0f;

                ultimaPosicaoConhecidaJogador = posicaoDisparo;
                tempoVisaoPerdida = 3.5f;

                if (EstadoAtual == EstadoIABot.Patrulhando)
                {
                    EstadoAtual = EstadoIABot.Alerta;
                    timerReacaoAlerta = 0.35f;
                }
            }
        }

        // -------------------------------------------------------------
        // Ciclo Principal de Atualização
        // -------------------------------------------------------------
        public void Atualizar(float dt, PersonagemPB2 jogador, List<ProjetilPlasma> tiros, List<Rectangle> plataformas, List<GranadaPB2> granadas)
        {
            if (timerFlashDano > 0.0f) timerFlashDano -= dt;
            if (timerFlashCano > 0.0f) timerFlashCano -= dt;
            if (cooldownPulo > 0.0f) cooldownPulo -= dt;

            // ---------------------------------------------------------
            // FÍSICA DE MORTE E RAGDOLL QUANDO DERROTADO
            // ---------------------------------------------------------
            if (!Vivo)
            {
                timerRespawn -= dt;

                // Física do corpo que cai e tomba na direção do impacto
                Velocidade.Y += Gravidade * dt;
                NoChao = FisicaPlataforma.ResolverMovimentoEColisao(ref Posicao, ref Velocidade, plataformas, dt);

                if (!NoChao)
                {
                    rotacaoCorpoMorte += velRotacaoCorpoMorte * dt;
                }
                else
                {
                    // No chão, o corpo desacelera e estabiliza deitado (90° ou -90°)
                    Velocidade.X = MoverEmDirecao(Velocidade.X, 0.0f, Friccao * 2.5f * dt);
                    float anguloAlvoChao = direcaoImpactoMorteX * 90.0f;
                    rotacaoCorpoMorte = Lerp(rotacaoCorpoMorte, anguloAlvoChao, 12.0f * dt);
                }

                // Física da arma arremessada no chão
                if (armaDropada)
                {
                    velArmaDropada.Y += Gravidade * dt;
                    posArmaDropada += velArmaDropada * dt;
                    rotArmaDropada += velRotArmaDropada * dt;

                    foreach (var plat in plataformas)
                    {
                        if (Raylib.CheckCollisionPointRec(posArmaDropada, plat))
                        {
                            posArmaDropada.Y = plat.Y - 2.0f;
                            velArmaDropada.Y = -velArmaDropada.Y * 0.30f;
                            velArmaDropada.X *= 0.60f;
                            velRotArmaDropada *= 0.40f;
                            if (MathF.Abs(velArmaDropada.Y) < 30.0f) velArmaDropada.Y = 0.0f;
                        }
                    }
                }

                // O bot derrotado permanece caído no chão com a arma desarmada.
                // O respawn é coordenado pelo SistemaRespawnPB2 via [ENTER], botão USE ou auto-timer de 2 minutos.
                return;
            }

            // 1. Processar Danos Recebidos
            ProcessarColisoesDano(tiros, granadas);
            if (!Vivo) return;

            // 2. Inteligência Artificial com Sentidos Realistas
            AtualizarIA(dt, jogador, plataformas, tiros);

            // 3. Física de Plataforma
            Velocidade.Y += Gravidade * dt;
            NoChao = FisicaPlataforma.ResolverMovimentoEColisao(ref Posicao, ref Velocidade, plataformas, dt);

            // 4. Animação Procedural dos Membros
            AtualizarAnimacao(dt);

            // 5. Suavização de Recuo
            recuoLinearAtual = MathF.Max(0.0f, recuoLinearAtual - dt * 25.0f);
            recuoRotacaoAtual = MathF.Max(0.0f, recuoRotacaoAtual - dt * 45.0f);
        }

        private void ProcessarColisoesDano(List<ProjetilPlasma> tiros, List<GranadaPB2> granadas)
        {
            Rectangle box = CaixaColisao;

            // Tiros disparados pelo jogador
            for (int i = 0; i < tiros.Count; i++)
            {
                var tiro = tiros[i];
                if (!tiro.Ativo || !tiro.DoJogador) continue;

                if (Raylib.CheckCollisionPointRec(tiro.Posicao, box))
                {
                    tiro.Ativo = false;
                    float dano = tiro.TipoArma switch
                    {
                        "sniper" => 120.0f,
                        "android_sniper" => 95.0f,
                        "railgun" => 135.0f,
                        "shotgun" => 30.0f,
                        "heavy_cannon" => 35.0f,
                        _ => 30.0f
                    };
                    ReceberDano(dano, tiro.Velocidade.X);
                }
            }

            // O dano de área das Granadas PB2 é processado com física e impulso no gestor de colisões central
        }

        public void ReceberDano(float dano, float forcaImpactoX)
        {
            if (!Vivo) return;

            VidaAtual = MathF.Max(0.0f, VidaAtual - dano);
            timerFlashDano = 0.12f;

            // Empurrão de impacto
            float sinalImpacto = MathF.Sign(forcaImpactoX);
            if (sinalImpacto == 0.0f) sinalImpacto = -DirecaoOlhar;
            Velocidade.X += sinalImpacto * 75.0f;

            // Ao levar tiro, o bot se vira instantaneamente para o agressor
            DirecaoOlhar = -sinalImpacto;
            EstadoAtual = EstadoIABot.Atacando;
            tempoVisaoPerdida = 3.5f;

            // Morte: Inicia a animação de queda ragdoll estilo PB2
            if (VidaAtual <= 0.0f)
            {
                IniciarMorte(sinalImpacto);
            }
        }

        private void IniciarMorte(float sinalImpactoX)
        {
            EstadoAtual = EstadoIABot.Morto;
            timerRespawn = 0.0f;
            direcaoImpactoMorteX = sinalImpactoX != 0.0f ? MathF.Sign(sinalImpactoX) : -DirecaoOlhar;

            // Força cinética que arremessa o corpo para trás/lado de onde veio o tiro
            Velocidade = new Vector2(direcaoImpactoMorteX * 240.0f, -210.0f);
            NoChao = false;

            // Rotação acrobática de tombo
            rotacaoCorpoMorte = 0.0f;
            velRotacaoCorpoMorte = direcaoImpactoMorteX * 380.0f;

            // A arma se solta das mãos e voa girando no ar
            armaDropada = true;
            posArmaDropada = ObterPosicaoArma();
            velArmaDropada = new Vector2(
                direcaoImpactoMorteX * 170.0f + ((float)Random.Shared.NextDouble() * 50.0f - 25.0f),
                -190.0f
            );
            rotArmaDropada = AnguloMiraGraus;
            velRotArmaDropada = direcaoImpactoMorteX * 520.0f;
        }

        public void Renascer()
        {
            Posicao = PosicaoSpawn;
            Velocidade = Vector2.Zero;
            VidaAtual = VidaMax;
            timerFlashDano = 0.0f;
            timerFlashCano = 0.0f;
            recuoLinearAtual = 0.0f;
            recuoRotacaoAtual = 0.0f;
            rotacaoCorpoMorte = 0.0f;
            velRotacaoCorpoMorte = 0.0f;
            armaDropada = false;
            EstadoAtual = EstadoIABot.Patrulhando;
            timerReacaoAlerta = 0.0f;
            jogadorDetectadoAnterior = false;
        }

        // -------------------------------------------------------------
        // Inteligência Artificial com Sentidos de Plazma Burst 2
        // -------------------------------------------------------------
        private void AtualizarIA(float dt, PersonagemPB2 jogador, List<Rectangle> plataformas, List<ProjetilPlasma> listaProjeteis)
        {
            Vector2 alvo = jogador.CentroVisual;
            Vector2 ombro = ObterOmbro();
            Vector2 vetorAoJogador = alvo - ombro;
            float distancia = vetorAoJogador.Length();

            // ---------------------------------------------------------
            // 1. CONE DE VISÃO REALISTA (FOV) E DISTÂNCIA LIMITE
            // ---------------------------------------------------------
            // Se o jogador estiver agachado (stealth), a visão do bot é encurtada
            float alcanceMaximoVisao = jogador.Agachado ? 310.0f : 480.0f;

            bool estaDentroDoAlcance = distancia <= alcanceMaximoVisao;
            bool noConeDeVisao = false;

            if (estaDentroDoAlcance && distancia > 0.1f)
            {
                // Produto escalar horizontal: verifica se o jogador está na frente do olhar
                float alinhamentoFrente = (vetorAoJogador.X * DirecaoOlhar) / distancia;

                // Cone de visão frontal de ~78 graus (não enxerga o que está às costas)
                noConeDeVisao = alinhamentoFrente > 0.22f;
            }

            // Sentido de proximidade imediata: se estiver a menos de 55px colado às costas, o bot nota
            if (distancia < 55.0f)
            {
                noConeDeVisao = true;
            }

            // Raycast que verifica se não há paredes sólidas no meio
            bool temLinhaDeVisao = noConeDeVisao && TemLinhaDeVisao(ombro, alvo, plataformas);

            // ---------------------------------------------------------
            // 2. MÁQUINA DE ESTADOS E REAÇÃO DE ALERTA COM '!'
            // ---------------------------------------------------------
            if (temLinhaDeVisao)
            {
                ultimaPosicaoConhecidaJogador = alvo;
                tempoVisaoPerdida = 3.0f;

                // Se acabou de ver o jogador saindo da patrulha, tem tempo de reação
                if (!jogadorDetectadoAnterior && EstadoAtual == EstadoIABot.Patrulhando)
                {
                    EstadoAtual = EstadoIABot.Alerta;
                    timerReacaoAlerta = 0.38f; // 380ms para notar, sacar e disparar
                }

                jogadorDetectadoAnterior = true;

                if (timerReacaoAlerta > 0.0f)
                {
                    timerReacaoAlerta -= dt;
                    EstadoAtual = EstadoIABot.Alerta;
                }
                else
                {
                    EstadoAtual = EstadoIABot.Atacando;
                }
            }
            else
            {
                jogadorDetectadoAnterior = false;

                if (tempoVisaoPerdida > 0.0f)
                {
                    tempoVisaoPerdida -= dt;
                    EstadoAtual = EstadoIABot.Alerta;
                }
                else
                {
                    EstadoAtual = EstadoIABot.Patrulhando;
                }
            }

            // ---------------------------------------------------------
            // 3. MOVIMENTO E NAVEGAÇÃO
            // ---------------------------------------------------------
            float inputMovimentoX = 0.0f;
            var arma = ArmaAtual;

            switch (EstadoAtual)
            {
                case EstadoIABot.Patrulhando:
                    timerTrocaPatrulha -= dt;
                    if (cooldownPuloPatrulha > 0.0f) cooldownPuloPatrulha -= dt;

                    bool obstaculoPatrulha = ModuloPercepcaoTaticaPB2.DetectarParedeAFrente(Posicao, direcaoPatrulha, plataformas, 24.0f, out bool paredeAltaPatrulha);

                    if ((obstaculoPatrulha && paredeAltaPatrulha) || timerTrocaPatrulha <= 0.0f)
                    {
                        if (obstaculoPatrulha && paredeAltaPatrulha)
                        {
                            direcaoPatrulha = -direcaoPatrulha;
                            direcaoOlharParado = direcaoPatrulha;
                            botEstadoPatrulha = (Random.Shared.NextDouble() < 0.35) ? 0 : 1;
                            timerTrocaPatrulha = (float)Random.Shared.NextDouble() * 2.5f + 1.8f;
                        }
                        else
                        {
                            double sorteio = Random.Shared.NextDouble();
                            if (sorteio < 0.35)
                            {
                                // 35% Chance: Ficar Parado Observando (Idle / Vigília PB2)
                                botEstadoPatrulha = 0;
                                timerTrocaPatrulha = (float)Random.Shared.NextDouble() * 2.8f + 1.6f;
                                timerOlharPatrulha = 0.0f;
                                direcaoOlharParado = direcaoPatrulha;
                                if (Random.Shared.NextDouble() < 0.30)
                                    direcaoOlharParado = -direcaoOlharParado;
                            }
                            else if (sorteio < 0.75)
                            {
                                // 40% Chance: Caminhar
                                botEstadoPatrulha = 1;
                                timerTrocaPatrulha = (float)Random.Shared.NextDouble() * 3.5f + 2.5f;
                                if (Random.Shared.NextDouble() < 0.30)
                                    direcaoPatrulha = -direcaoPatrulha;
                                direcaoOlharParado = direcaoPatrulha;
                            }
                            else
                            {
                                // 25% Chance: Correr
                                botEstadoPatrulha = 2;
                                timerTrocaPatrulha = (float)Random.Shared.NextDouble() * 2.5f + 1.5f;
                                if (Random.Shared.NextDouble() < 0.25)
                                    direcaoPatrulha = -direcaoPatrulha;
                                direcaoOlharParado = direcaoPatrulha;
                            }
                        }
                    }

                    if (botEstadoPatrulha == 0)
                    {
                        // 1. Ficar Parado e dar uma olhada ao redor
                        inputMovimentoX = 0.0f;
                        timerOlharPatrulha -= dt;
                        if (timerOlharPatrulha <= 0.0f)
                        {
                            timerOlharPatrulha = (float)Random.Shared.NextDouble() * 1.3f + 0.8f;
                            offsetOlharYRad = ((float)Random.Shared.NextDouble() * 0.45f) - 0.20f; // Inclina para cima ou baixo
                            if (Random.Shared.NextDouble() < 0.20)
                            {
                                direcaoOlharParado = -direcaoOlharParado; // Vira para trás
                            }
                        }

                        DirecaoOlhar = direcaoOlharParado;
                        float anguloBase = (DirecaoOlhar > 0) ? 0.0f : MathF.PI;
                        float anguloObservacao = anguloBase + (DirecaoOlhar > 0 ? offsetOlharYRad : -offsetOlharYRad);
                        AnguloMiraRad = LerpAngulo(AnguloMiraRad, anguloObservacao, 5.0f * dt);
                    }
                    else
                    {
                        // 2. Caminhar ou Correr
                        float vel = (botEstadoPatrulha == 2) ? 0.95f : 0.42f;
                        inputMovimentoX = direcaoPatrulha * vel;
                        DirecaoOlhar = direcaoPatrulha;

                        // Pulo tático sobre degraus ou pulo exploratório ocasional
                        if (NoChao && cooldownPulo <= 0.0f && obstaculoPatrulha && !paredeAltaPatrulha)
                        {
                            Velocidade.Y = ForcaSalto;
                            NoChao = false;
                            cooldownPulo = 1.0f;
                        }
                        else if (NoChao && cooldownPulo <= 0.0f && cooldownPuloPatrulha <= 0.0f && (botEstadoPatrulha == 2 || Random.Shared.NextDouble() < 0.015))
                        {
                            Velocidade.Y = ForcaSalto;
                            NoChao = false;
                            cooldownPulo = 1.0f;
                            cooldownPuloPatrulha = (float)Random.Shared.NextDouble() * 4.0f + 3.0f;
                        }

                        float anguloPatrulha = (DirecaoOlhar > 0) ? 0.0f : MathF.PI;
                        AnguloMiraRad = LerpAngulo(AnguloMiraRad, anguloPatrulha, 7.0f * dt);
                    }
                    break;

                case EstadoIABot.Alerta:
                    // Vira para a última posição conhecida com arma engatilhada
                    Vector2 vetorUltimaPos = ultimaPosicaoConhecidaJogador - ombro;
                    float anguloAlerta = MathF.Atan2(vetorUltimaPos.Y, vetorUltimaPos.X);
                    AnguloMiraRad = LerpAngulo(AnguloMiraRad, anguloAlerta, 8.0f * dt);
                    if (vetorUltimaPos.X != 0.0f) DirecaoOlhar = MathF.Sign(vetorUltimaPos.X);
                    break;

                case EstadoIABot.Atacando:
                    // 1. Mira preditiva com lead aim baseado na velocidade do projétil e movimento do alvo
                    float velProjetilBot = (arma.VelocidadeProjetil > 100.0f) ? arma.VelocidadeProjetil : 1100.0f;
                    float tempoVooBot = Math.Clamp(distancia / velProjetilBot, 0.015f, 0.35f);
                    Vector2 alvoPrevisto = jogador.CentroVisual + (jogador.Velocidade * tempoVooBot);
                    Vector2 vetorAoAlvoPrevisto = alvoPrevisto - ombro;
                    float anguloDesejado = MathF.Atan2(vetorAoAlvoPrevisto.Y, vetorAoAlvoPrevisto.X);
                    AnguloMiraRad = LerpAngulo(AnguloMiraRad, anguloDesejado, 12.0f * dt);

                    if (vetorAoJogador.X > 15.0f) DirecaoOlhar = 1.0f;
                    else if (vetorAoJogador.X < -15.0f) DirecaoOlhar = -1.0f;

                    // 2. Strafe tático em combate (evita ficar parado como alvo fixo)
                    timerStrafeCombate -= dt;
                    if (timerStrafeCombate <= 0.0f)
                    {
                        timerStrafeCombate = (float)Random.Shared.NextDouble() * 0.70f + 0.40f;
                        direcaoStrafeCombate = (Random.Shared.Next(0, 2) == 0) ? 1.0f : -1.0f;
                    }

                    float sinalX = MathF.Sign(vetorAoJogador.X);

                    if (distancia > arma.DistanciaIdealMax)
                    {
                        inputMovimentoX = sinalX * 0.95f; // Avança para fechar distância
                    }
                    else if (distancia < arma.DistanciaIdealMin)
                    {
                        inputMovimentoX = -sinalX * 0.85f; // Recua mantendo espaço
                    }
                    else
                    {
                        inputMovimentoX = direcaoStrafeCombate * 0.65f; // Strafe tático em zig-zag
                    }

                    // 3. Linha de tiro
                    Vector2 pontaCano = ObterPontaDoCano();
                    bool linhaDeTiroLivre = ModuloPercepcaoTaticaPB2.PodeAbrirFogo(pontaCano, jogador.CentroVisual, plataformas);

                    // 4. Pulos inteligentes (subir plataformas, transpor degraus e atirar por cobertura)
                    bool obstaculoAFrente = ModuloPercepcaoTaticaPB2.DetectarParedeAFrente(Posicao, inputMovimentoX, plataformas, 24.0f, out bool paredeAlta);
                    if (NoChao && cooldownPulo <= 0.0f)
                    {
                        // Jogador em plataforma superior: salta ativamente para perseguição
                        if (vetorAoJogador.Y < -35.0f && MathF.Abs(vetorAoJogador.X) < 320.0f)
                        {
                            Velocidade.Y = ForcaSalto;
                            NoChao = false;
                            cooldownPulo = 0.85f;
                        }
                        // Degrau baixo transponível
                        else if (obstaculoAFrente && !paredeAlta)
                        {
                            Velocidade.Y = ForcaSalto;
                            NoChao = false;
                            cooldownPulo = 0.85f;
                        }
                        // Cobertura baixa bloqueando tiro: salta para atirar por cima
                        else if (!linhaDeTiroLivre && MathF.Abs(vetorAoJogador.X) < 340.0f && !paredeAlta)
                        {
                            Velocidade.Y = ForcaSalto;
                            NoChao = false;
                            cooldownPulo = 0.95f;
                        }
                        // Esquiva acrobática de combate ocasional
                        else if (linhaDeTiroLivre && (float)Random.Shared.NextDouble() < 0.020f)
                        {
                            Velocidade.Y = ForcaSalto;
                            NoChao = false;
                            cooldownPulo = 1.15f;
                        }
                    }

                    // 5. Disparo em rajadas quando o cano alinhar com o alvo E a linha de tiro estiver 100% desobstruída
                    if (linhaDeTiroLivre && temLinhaDeVisao)
                    {
                        float diferencaAngular = MathF.Abs(AnguloMiraRad - anguloDesejado);
                        while (diferencaAngular > MathF.PI) diferencaAngular = MathF.Abs(diferencaAngular - MathF.PI * 2.0f);

                        if (diferencaAngular < 0.28f)
                        {
                            TentarDisparar(dt, listaProjeteis);
                        }
                    }
                    break;
            }

            AnguloMiraGraus = AnguloMiraRad * (180.0f / MathF.PI);

            // Aceleração e desaceleração do bot
            if (inputMovimentoX != 0.0f)
            {
                Velocidade.X = MoverEmDirecao(Velocidade.X, inputMovimentoX * VelocidadeMovimento, Aceleracao * dt);
            }
            else
            {
                Velocidade.X = MoverEmDirecao(Velocidade.X, 0.0f, Friccao * dt);
            }
        }

        private void TentarDisparar(float dt, List<ProjetilPlasma> listaProjeteis)
        {
            if (pausaEntreBursts > 0.0f)
            {
                pausaEntreBursts -= dt;
                return;
            }

            if (cooldownTiro > 0.0f)
            {
                cooldownTiro -= dt;
                return;
            }

            var arma = ArmaAtual;
            DispararArma(listaProjeteis, arma);
            cooldownTiro = arma.Cadencia;
            tirosNoBurst++;

            if (tirosNoBurst >= arma.TirosPorBurst)
            {
                tirosNoBurst = 0;
                float pausaAleatoria = (float)Random.Shared.NextDouble() * (arma.PausaBurstMax - arma.PausaBurstMin) + arma.PausaBurstMin;
                pausaEntreBursts = pausaAleatoria;
            }
        }

        private void DispararArma(List<ProjetilPlasma> listaProjeteis, DefinicaoArmaBot arma)
        {
            recuoLinearAtual = arma.RecuoLinear;
            recuoRotacaoAtual = arma.RecuoRotacao;
            timerFlashCano = 0.08f;

            if (arma.SomTiro.Stream.Buffer != 0)
            {
                Raylib.PlaySound(arma.SomTiro);
            }

            Vector2 dirMira = new Vector2(MathF.Cos(AnguloMiraRad), MathF.Sin(AnguloMiraRad));
            Vector2 pontaCano = ObterPontaDoCano();

            for (int p = 0; p < arma.Pellets; p++)
            {
                float dispersao = ((float)Random.Shared.NextDouble() * arma.Dispersao * 2.0f) - arma.Dispersao;
                Vector2 dirDisparo = Vector2.Normalize(dirMira + new Vector2(-dirMira.Y, dirMira.X) * dispersao);

                listaProjeteis.Add(new ProjetilPlasma(
                    pontaCano,
                    dirDisparo,
                    velocidade: arma.VelocidadeProjetil,
                    cor: arma.CorProjetil,
                    tipo: arma.TipoProjetil,
                    doJogador: false
                ));
            }
        }

        private bool TemLinhaDeVisao(Vector2 origem, Vector2 destino, List<Rectangle> paredes)
        {
            return ModuloPercepcaoTaticaPB2.TemLinhaDeTiroLivre(origem, destino, paredes);
        }

        private void AtualizarAnimacao(float dt)
        {
            float velRatio = Math.Clamp(MathF.Abs(Velocidade.X) / VelocidadeMovimento, 0.0f, 1.0f);
            float direcaoTronco = MathF.Sign(Velocidade.X) * DirecaoOlhar;

            float anguloTroncoDeg = 0.0f;
            if (MathF.Abs(Velocidade.X) > 10.0f)
            {
                anguloTroncoDeg = direcaoTronco * Math.Clamp(8.0f * velRatio, 4.0f, 10.0f);
            }
            float alvoInclinacao = anguloTroncoDeg * (MathF.PI / 180.0f);
            inclinacaoCorpo = Lerp(inclinacaoCorpo, alvoInclinacao, 12.0f * dt);

            if (NoChao)
            {
                if (MathF.Abs(Velocidade.X) > 10.0f)
                {
                    float velLocal = (Velocidade.X * DirecaoOlhar) / VelocidadeMovimento;
                    cicloAndar += velLocal * dt * 10.0f;
                    bobbingTroncoY = MathF.Abs(MathF.Cos(cicloAndar)) * 2.0f;
                    bobbingCabecaY = MathF.Abs(MathF.Cos(cicloAndar)) * 1.5f;
                }
                else
                {
                    cicloAndar = Lerp(cicloAndar, 0.0f, 8.0f * dt);
                    bobbingTroncoY = MathF.Sin((float)Raylib.GetTime() * 2.0f) * 0.5f;
                    bobbingCabecaY = MathF.Sin((float)Raylib.GetTime() * 2.0f) * 0.4f;
                }
            }

            float anguloRelativo = AnguloMiraRad;
            if (DirecaoOlhar < 0)
            {
                anguloRelativo = MathF.PI - AnguloMiraRad;
                while (anguloRelativo > MathF.PI) anguloRelativo -= MathF.PI * 2.0f;
                while (anguloRelativo < -MathF.PI) anguloRelativo += MathF.PI * 2.0f;
                anguloRelativo = -anguloRelativo;
            }
            rotacaoCabeca = Lerp(rotacaoCabeca, Math.Clamp(anguloRelativo * 0.28f, -0.4f, 0.4f), 12.0f * dt);
        }

        // -------------------------------------------------------------
        // Renderização Completa: Vivo (IK) ou Morto (Corpo Caído + Arma Dropada)
        // -------------------------------------------------------------
        public void Desenhar()
        {
            if (SkinCompartilhada == null || !SkinCompartilhada.Carregada) return;

            // ---------------------------------------------------------
            // RENDERIZAÇÃO QUANDO MORTO (Ragdoll tombado no chão)
            // ---------------------------------------------------------
            if (!Vivo)
            {
                DesenharCorpoMorto();

                // Desenha a arma caída ao lado do corpo
                if (armaDropada && ArmaAtual.Textura.Id != 0)
                {
                    DesenharArmaCaida();
                }

                return;
            }

            // ---------------------------------------------------------
            // RENDERIZAÇÃO QUANDO VIVO (IK Completo nos Membros)
            // ---------------------------------------------------------
            bool viradoEsquerda = DirecaoOlhar < 0;
            float orientacao = viradoEsquerda ? -1.0f : 1.0f;

            Vector2 quadril = Posicao + new Vector2(0, bobbingTroncoY);
            Vector2 ombro = Posicao - new Vector2(0, 16.0f) + new Vector2(0, bobbingTroncoY);

            // Cores: Visual dinâmico conforme a facção e corpo do bot (Plazma Burst 2)
            Color corTraseira = timerFlashDano > 0.0f ? Color.White : CorpoAtual.CorTraseira;
            Color corDianteira = timerFlashDano > 0.0f ? Color.White : CorpoAtual.CorDianteira;
            Color corArticulacao = timerFlashDano > 0.0f ? Color.White : CorpoAtual.CorArticulacao;

            float rotCorpoDeg = inclinacaoCorpo * (180.0f / MathF.PI);

            // Cálculo dos braços e arma
            var arma = ArmaAtual;
            var (posArma, anguloArmaGraus) = ObterPosicaoEAnguloArma();
            float anguloArmaRad = anguloArmaGraus * (MathF.PI / 180.0f);
            Vector2 dirMiraArma = new Vector2(MathF.Cos(anguloArmaRad), MathF.Sin(anguloArmaRad));
            Vector2 normalMiraArma = new Vector2(-dirMiraArma.Y, dirMiraArma.X);

            float sinalVertical = viradoEsquerda ? -1.0f : 1.0f;
            Vector2 posMaoGatilho = posArma + (dirMiraArma * arma.OffsetGatilhoX) + (normalMiraArma * (arma.OffsetGatilhoY * sinalVertical));
            Vector2 posMaoSuporte = posArma + (dirMiraArma * arma.OffsetSuporteX) + (normalMiraArma * (arma.OffsetSuporteY * sinalVertical));

            Vector2 ombroTras = ombro + new Vector2(viradoEsquerda ? 3.5f : -3.5f, -2.0f);
            Vector2 ombroFrente = ombro + new Vector2(viradoEsquerda ? -2.5f : 2.5f, 1.0f);

            Vector2 cotoveloTras = ResolverIK(ombroTras, posMaoSuporte, ComprimentoBraco, ComprimentoAntebraco, flexaoInvertida: viradoEsquerda);
            Vector2 cotoveloFrente = ResolverIK(ombroFrente, posMaoGatilho, ComprimentoBraco, ComprimentoAntebraco, flexaoInvertida: viradoEsquerda);

            // 1. Perna Traseira (IK)
            DesenharPernaComIK(
                quadril + new Vector2(viradoEsquerda ? 2.5f : -2.5f, 0),
                cicloAndar + MathF.PI,
                orientacao,
                SkinCompartilhada.CoxaTras,
                SkinCompartilhada.CanelaTras,
                corTraseira,
                ehDianteira: false
            );

            // 2. Braço Traseiro
            DesenharSegmentoTexturizado(ombroTras, cotoveloTras, SkinCompartilhada.BracoTras, 0.45f, viradoEsquerda, corTraseira);
            Raylib.DrawCircleV(cotoveloTras, 3.0f, corArticulacao);
            DesenharSegmentoTexturizado(cotoveloTras, posMaoSuporte, SkinCompartilhada.AntebracoTras, 0.45f, viradoEsquerda, corTraseira);
            Raylib.DrawCircleV(posMaoSuporte, 2.5f, corArticulacao);

            // 3. Tronco
            DesenharTronco(quadril, rotCorpoDeg, viradoEsquerda, corDianteira);

            // 4. Cabeça + Visor Hostil Vermelho
            DesenharCabeca(quadril, rotCorpoDeg + (rotacaoCabeca * (180.0f / MathF.PI)), viradoEsquerda, corDianteira);

            // 5. Perna Dianteira (IK)
            DesenharPernaComIK(
                quadril + new Vector2(viradoEsquerda ? -2.5f : 2.5f, 0),
                cicloAndar,
                orientacao,
                SkinCompartilhada.CoxaFrente,
                SkinCompartilhada.CanelaFrente,
                corDianteira,
                ehDianteira: true
            );

            // 6. Arma Empunhada
            DesenharArma(posArma, anguloArmaGraus, viradoEsquerda, arma);

            // Mira laser tática da Falcon Pistol
            if (arma.TemLaser)
            {
                Vector2 ponta = ObterPontaDoCano();
                Raylib.DrawLineEx(ponta, ponta + dirMiraArma * 600.0f, 1.2f, arma.CorLaser);
            }

            // 7. Braço Dianteiro
            DesenharSegmentoTexturizado(ombroFrente, cotoveloFrente, SkinCompartilhada.BracoFrente, 0.48f, viradoEsquerda, corDianteira);
            Raylib.DrawCircleV(ombroFrente, 3.4f, corArticulacao);
            Raylib.DrawCircleV(cotoveloFrente, 3.2f, corArticulacao);
            DesenharSegmentoTexturizado(cotoveloFrente, posMaoGatilho, SkinCompartilhada.AntebracoFrente, 0.48f, viradoEsquerda, corDianteira);
            Raylib.DrawCircleV(posMaoGatilho, 2.8f, corArticulacao);

            // 8. Flash de tiro no cano
            if (timerFlashCano > 0.0f)
            {
                Vector2 ponta = ObterPontaDoCano();
                Raylib.DrawCircleV(ponta, 6.0f, Color.White);
                Raylib.DrawCircleV(ponta, 12.0f, new Color(255, 70, 70, 190));
            }

            // 9. Ícone de Alerta '!' quando o bot descobre o jogador
            if (timerReacaoAlerta > 0.0f)
            {
                Vector2 posAlerta = Posicao - new Vector2(0, 80.0f);
                Raylib.DrawCircleV(posAlerta, 8.0f, new Color(220, 38, 38, 240));
                Raylib.DrawCircleLines((int)posAlerta.X, (int)posAlerta.Y, 8.0f, Color.White);
                FontePB2.DesenharTexto("!", posAlerta.X - 2.5f, posAlerta.Y - 6.0f, 13, Color.White);
            }

            // 10. Barra de Vida no topo da cabeça
            DesenharBarraVida();
        }

        private void DesenharCorpoMorto()
        {
            Vector2 centro = Posicao + new Vector2(0, 10.0f);
            Color corMorta = CorpoAtual.CorDianteira;
            Color corMortaEscura = CorpoAtual.CorTraseira;

            // Desenha as partes do corpo relaxadas no chão com a rotação do impacto
            float escala = 0.5f * CorpoAtual.EscalaCorpo;

            // Tronco caído
            if (SkinCompartilhada?.Tronco.Id != 0)
            {
                float larg = SkinCompartilhada!.Tronco.Width * escala;
                float alt = SkinCompartilhada.Tronco.Height * escala;
                Rectangle src = new Rectangle(0, 0, SkinCompartilhada.Tronco.Width, SkinCompartilhada.Tronco.Height);
                Rectangle dst = new Rectangle(centro.X, centro.Y, larg, alt);
                Vector2 orig = new Vector2(larg * 0.5f, alt * 0.5f);
                Raylib.DrawTexturePro(SkinCompartilhada.Tronco, src, dst, orig, rotacaoCorpoMorte, corMorta);
            }

            // Cabeça caída ao lado do tronco
            if (SkinCompartilhada?.Cabeca.Id != 0)
            {
                float larg = SkinCompartilhada!.Cabeca.Width * escala;
                float alt = SkinCompartilhada.Cabeca.Height * escala;
                Vector2 dirCorpo = new Vector2(MathF.Cos(rotacaoCorpoMorte * (MathF.PI / 180.0f)), MathF.Sin(rotacaoCorpoMorte * (MathF.PI / 180.0f)));
                Vector2 posCab = centro - (dirCorpo * 16.0f);
                Rectangle src = new Rectangle(0, 0, SkinCompartilhada.Cabeca.Width, SkinCompartilhada.Cabeca.Height);
                Rectangle dst = new Rectangle(posCab.X, posCab.Y, larg, alt);
                Vector2 orig = new Vector2(larg * 0.5f, alt * 0.5f);
                Raylib.DrawTexturePro(SkinCompartilhada.Cabeca, src, dst, orig, rotacaoCorpoMorte, corMorta);
            }

            // Pernas caídas relaxadas
            Vector2 dirPernas = new Vector2(MathF.Cos(rotacaoCorpoMorte * (MathF.PI / 180.0f)), MathF.Sin(rotacaoCorpoMorte * (MathF.PI / 180.0f)));
            Vector2 posPernas = centro + (dirPernas * 16.0f);
            Raylib.DrawLineEx(centro, posPernas, 5.0f, corMortaEscura);
            Raylib.DrawLineEx(posPernas, posPernas + (dirPernas * 18.0f), 4.0f, corMortaEscura);

            // Marca de sangue/óleo tático no chão estilo PB2 com a cor de sangue da facção
            Raylib.DrawEllipse((int)centro.X, (int)centro.Y + 12, 19, 5, CorpoAtual.CorSangue);
        }

        private void DesenharArmaCaida()
        {
            var arma = ArmaAtual;
            float larg = arma.Textura.Width * arma.Escala * 0.9f;
            float alt = arma.Textura.Height * arma.Escala * 0.9f;

            Rectangle src = new Rectangle(0, 0, arma.Textura.Width, arma.Textura.Height);
            Rectangle dst = new Rectangle(posArmaDropada.X, posArmaDropada.Y, larg, alt);
            Vector2 orig = new Vector2(larg * 0.5f, alt * 0.5f);

            Raylib.DrawTexturePro(arma.Textura, src, dst, orig, rotArmaDropada, Color.White);
        }

        private void DesenharBarraVida()
        {
            float larguraBarra = 52.0f;
            float barraX = Posicao.X - (larguraBarra * 0.5f);
            float barraY = Posicao.Y - 74.0f;
            float pctVida = Math.Clamp(VidaAtual / VidaMax, 0.0f, 1.0f);

            Raylib.DrawRectangle((int)barraX - 1, (int)barraY - 1, (int)larguraBarra + 2, 7, new Color(15, 20, 28, 220));
            Color corBarra = pctVida > 0.35f ? new Color(239, 68, 68, 255) : new Color(220, 38, 38, 255);
            Raylib.DrawRectangle((int)barraX, (int)barraY, (int)(larguraBarra * pctVida), 5, corBarra);
            Raylib.DrawRectangleLines((int)barraX - 1, (int)barraY - 1, (int)larguraBarra + 2, 7, new Color(40, 50, 65, 255));

            FontePB2.DesenharTexto($"{NomeId} • {CorpoAtual.NomeFaccao} [{ArmaAtual.Nome}]", barraX - 18, barraY - 14.0f, 10, Color.LightGray);
        }

        private (Vector2 Posicao, float AnguloGraus) ObterPosicaoEAnguloArma()
        {
            var arma = ArmaAtual;
            Vector2 ombro = ObterOmbro();
            bool viradoEsquerda = DirecaoOlhar < 0;
            float orientacao = viradoEsquerda ? -1.0f : 1.0f;

            float anguloRecuo = viradoEsquerda ? recuoRotacaoAtual : -recuoRotacaoAtual;
            float anguloArmaGraus = AnguloMiraGraus + anguloRecuo;

            float anguloArmaRad = anguloArmaGraus * (MathF.PI / 180.0f);
            Vector2 dirMiraArma = new Vector2(MathF.Cos(anguloArmaRad), MathF.Sin(anguloArmaRad));

            Vector2 posArma = ombro + new Vector2(orientacao * 4.0f, 2.0f) + (dirMiraArma * (arma.DistanciaOmbro - recuoLinearAtual));
            return (posArma, anguloArmaGraus);
        }

        public Vector2 ObterPosicaoArma() => ObterPosicaoEAnguloArma().Posicao;

        public Vector2 ObterPontaDoCano()
        {
            var arma = ArmaAtual;
            var (posArma, anguloArmaGraus) = ObterPosicaoEAnguloArma();
            float anguloArmaRad = anguloArmaGraus * (MathF.PI / 180.0f);
            Vector2 dirMiraArma = new Vector2(MathF.Cos(anguloArmaRad), MathF.Sin(anguloArmaRad));
            Vector2 normalMiraArma = new Vector2(-dirMiraArma.Y, dirMiraArma.X);
            float sinalVertical = (DirecaoOlhar < 0) ? -1.0f : 1.0f;

            return posArma + (dirMiraArma * arma.ComprimentoCano) + (normalMiraArma * (arma.AlturaCanoY * sinalVertical));
        }

        public Vector2 ObterOmbro()
        {
            return Posicao - new Vector2(0, 16.0f) + new Vector2(0, bobbingTroncoY);
        }

        private void DesenharPernaComIK(Vector2 quadrilPerna, float fasePasso, float orientacao, Texture2D texCoxa, Texture2D texCanela, Color corTint, bool ehDianteira)
        {
            Vector2 alvoPe;

            if (NoChao)
            {
                float fatorCorrida = Math.Clamp(MathF.Abs(Velocidade.X) / 30.0f, 0.0f, 1.0f);
                float oscilacaoLocal = -MathF.Sin(fasePasso) * 12.0f * fatorCorrida;
                float elevacaoY = MathF.Max(0.0f, -MathF.Cos(fasePasso)) * 7.0f * fatorCorrida;

                float baseOffset = ehDianteira ? 1.5f : -1.5f;
                float xLocal = (baseOffset * (1.0f - fatorCorrida)) + oscilacaoLocal;

                alvoPe = new Vector2(
                    quadrilPerna.X + (xLocal * orientacao),
                    Posicao.Y + 33.0f - elevacaoY
                );
            }
            else
            {
                alvoPe = quadrilPerna + new Vector2(orientacao * 2.0f, 32.0f);
            }

            Vector2 joelho = ResolverIKPerna(quadrilPerna, alvoPe, ComprimentoCoxa, ComprimentoCanela, orientacao);

            DesenharSegmentoTexturizado(quadrilPerna, joelho, texCoxa, 0.48f, orientacao < 0, corTint);
            Raylib.DrawCircleV(quadrilPerna, 3.8f, new Color(50, 20, 20, 255));
            Raylib.DrawCircleV(joelho, 3.6f, new Color(50, 20, 20, 255));
            DesenharSegmentoTexturizado(joelho, alvoPe, texCanela, 0.48f, orientacao < 0, corTint);
            Raylib.DrawCircleV(alvoPe, 2.8f, new Color(50, 20, 20, 255));
        }

        private static Vector2 ResolverIKPerna(Vector2 inicio, Vector2 alvo, float l1, float l2, float orientacao)
        {
            Vector2 delta = alvo - inicio;
            float dist = delta.Length();
            dist = Math.Clamp(dist, 1.0f, (l1 + l2) - 0.01f);
            float anguloDireto = MathF.Atan2(delta.Y, delta.X);

            float cosAlfa = (l1 * l1 + dist * dist - l2 * l2) / (2.0f * l1 * dist);
            float alfa = MathF.Acos(Math.Clamp(cosAlfa, -1.0f, 1.0f));

            float anguloSegmento1 = (orientacao >= 0.0f) ? (anguloDireto - alfa) : (anguloDireto + alfa);
            return inicio + new Vector2(MathF.Cos(anguloSegmento1) * l1, MathF.Sin(anguloSegmento1) * l1);
        }

        private static Vector2 ResolverIK(Vector2 inicio, Vector2 alvo, float l1, float l2, bool flexaoInvertida)
        {
            Vector2 delta = alvo - inicio;
            float dist = Math.Clamp(delta.Length(), 1.0f, (l1 + l2) - 0.5f);
            float anguloDireto = MathF.Atan2(delta.Y, delta.X);

            float cosAlfa = (l1 * l1 + dist * dist - l2 * l2) / (2.0f * l1 * dist);
            float alfa = MathF.Acos(Math.Clamp(cosAlfa, -1.0f, 1.0f));

            float anguloSegmento1 = anguloDireto + (flexaoInvertida ? -alfa : alfa);
            return inicio + new Vector2(MathF.Cos(anguloSegmento1) * l1, MathF.Sin(anguloSegmento1) * l1);
        }

        private void DesenharSegmentoTexturizado(Vector2 pInicio, Vector2 pFim, Texture2D textura, float escalaLargura, bool viradoEsquerda, Color tint)
        {
            if (textura.Id == 0) return;

            Vector2 delta = pFim - pInicio;
            float dist = delta.Length();
            if (dist < 0.1f) return;

            float anguloDeg = MathF.Atan2(delta.Y, delta.X) * (180.0f / MathF.PI);
            float larg = textura.Width * escalaLargura;
            float alt = dist + (7.0f * escalaLargura);

            Rectangle src = new Rectangle(0, 0, viradoEsquerda ? -textura.Width : textura.Width, textura.Height);
            Rectangle dst = new Rectangle(pInicio.X, pInicio.Y, larg, alt);
            Vector2 origem = new Vector2(larg * 0.5f, 4.0f * escalaLargura);

            Raylib.DrawTexturePro(textura, src, dst, origem, anguloDeg - 90.0f, tint);
        }

        private void DesenharTronco(Vector2 quadril, float rotacaoDeg, bool viradoEsquerda, Color tint)
        {
            if (SkinCompartilhada == null || SkinCompartilhada.Tronco.Id == 0) return;

            float escala = 0.5f;
            float larg = SkinCompartilhada.Tronco.Width * escala;
            float alt = SkinCompartilhada.Tronco.Height * escala;

            Rectangle src = new Rectangle(0, 0, viradoEsquerda ? -SkinCompartilhada.Tronco.Width : SkinCompartilhada.Tronco.Width, SkinCompartilhada.Tronco.Height);
            Rectangle dst = new Rectangle(quadril.X, quadril.Y - 10.0f, larg, alt);
            Vector2 origem = new Vector2(larg * 0.5f, alt * 0.5f);

            Raylib.DrawTexturePro(SkinCompartilhada.Tronco, src, dst, origem, rotacaoDeg, tint);
        }

        private void DesenharCabeca(Vector2 quadril, float rotacaoDeg, bool viradoEsquerda, Color tint)
        {
            if (SkinCompartilhada == null || SkinCompartilhada.Cabeca.Id == 0) return;

            float escala = 0.5f * CorpoAtual.EscalaCorpo;
            float larg = SkinCompartilhada.Cabeca.Width * escala;
            float alt = SkinCompartilhada.Cabeca.Height * escala;

            Rectangle src = new Rectangle(0, 0, viradoEsquerda ? -SkinCompartilhada.Cabeca.Width : SkinCompartilhada.Cabeca.Width, SkinCompartilhada.Cabeca.Height);
            Vector2 posCabeca = new Vector2(quadril.X, quadril.Y - 26.0f + bobbingCabecaY);
            Rectangle dst = new Rectangle(posCabeca.X, posCabeca.Y, larg, alt);
            Vector2 origem = new Vector2(larg * 0.5f, alt * 0.75f);

            Raylib.DrawTexturePro(SkinCompartilhada.Cabeca, src, dst, origem, rotacaoDeg, tint);

            // Visor neon hostil estilo Plazma Burst 2 personalizado pela facção
            float orientacao = viradoEsquerda ? -1.0f : 1.0f;
            Vector2 posVisor = posCabeca + new Vector2(orientacao * 4.5f, CorpoAtual.OffsetVisorY);

            if (CorpoAtual.VisorDuplo)
            {
                // Dois olhos cibernéticos (Android Unit)
                Vector2 olho1 = posVisor + new Vector2(orientacao * -2.0f, -1.0f);
                Vector2 olho2 = posVisor + new Vector2(orientacao * 2.0f, 1.0f);
                Raylib.DrawCircleV(olho1, 2.0f, CorpoAtual.CorVisor);
                Raylib.DrawCircleV(olho1, 0.9f, CorpoAtual.CorVisorBrilho);
                Raylib.DrawCircleV(olho2, 2.0f, CorpoAtual.CorVisor);
                Raylib.DrawCircleV(olho2, 0.9f, CorpoAtual.CorVisorBrilho);
            }
            else
            {
                // Fenda/visor único clássico
                Raylib.DrawCircleV(posVisor, 2.6f, CorpoAtual.CorVisor);
                Raylib.DrawCircleV(posVisor, 1.2f, CorpoAtual.CorVisorBrilho);
            }
        }

        private void DesenharArma(Vector2 posArma, float anguloGraus, bool viradoEsquerda, DefinicaoArmaBot arma)
        {
            if (arma.Textura.Id == 0) return;

            float larg = arma.Textura.Width * arma.Escala;
            float alt = arma.Textura.Height * arma.Escala;

            Rectangle src = new Rectangle(0, 0, arma.Textura.Width, viradoEsquerda ? -arma.Textura.Height : arma.Textura.Height);
            Rectangle dst = new Rectangle(posArma.X, posArma.Y, larg, alt);
            Vector2 origem = new Vector2(larg * arma.PivotNormX, alt * arma.PivotNormY);

            Raylib.DrawTexturePro(arma.Textura, src, dst, origem, anguloGraus, Color.White);
        }

        private static float MoverEmDirecao(float atual, float alvo, float maxDelta)
        {
            if (MathF.Abs(alvo - atual) <= maxDelta) return alvo;
            return atual + MathF.Sign(alvo - atual) * maxDelta;
        }

        private static float Lerp(float a, float b, float t) => a + (b - a) * Math.Clamp(t, 0.0f, 1.0f);

        private static float LerpAngulo(float a, float b, float t)
        {
            float diff = (b - a) % (MathF.PI * 2.0f);
            if (diff > MathF.PI) diff -= MathF.PI * 2.0f;
            if (diff < -MathF.PI) diff += MathF.PI * 2.0f;
            return a + diff * Math.Clamp(t, 0.0f, 1.0f);
        }

        public void Dispose()
        {
        }
    }
}
