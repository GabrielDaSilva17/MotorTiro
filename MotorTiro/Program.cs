using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
#if BROWSER
using System.Runtime.InteropServices.JavaScript;
#endif
using Raylib_cs;
using JogoDeTiro.EditorDeLevel;

namespace JogoDeTiro
{
    public enum EstadoApp
    {
        Home,
        SelecaoLevel,
        Playtest,
        Editor
    }

    public static partial class Program
    {
        private const int LarguraPadrao = 960;
        private const int AlturaPadrao = 640;

        private static bool jogoInicializado = false;
        private static bool jogoDeveFechar = false;

        private static string diretorioAssets = string.Empty;
        private static string caminhoMapa = string.Empty;

        private static EditorPB2 editor = null!;
        private static PersonagemPB2 jogador = null!;
        private static List<ProjetilPlasma> tiros = new();
        private static SistemaColisao sistemaColisao = null!;
        private static List<BotIAPB2> bots = new();
        private static SistemaRespawnPB2 sistemaRespawn = null!;

        private static CampanhaPB2 campanha = null!;
        private static InterfaceMenuPB2 menu = null!;
        private static bool modoCampanhaAtivo = false;
        private static float tempoLevelAtual = 0.0f;

        private static EstadoApp estadoAtual = EstadoApp.Home;
        private static Camera2D camera;
        private static float traumaCamera = 0.0f;
        private static float flashDanoJogador = 0.0f;

        public static void Main(string[] args)
        {
            Inicializar();

            #if !BROWSER
            // ==========================================
            // MODO 1: DESKTOP (Windows / Linux)
            // ==========================================
            while (!Raylib.WindowShouldClose() && !jogoDeveFechar)
            {
                ExecutarCicloDoJogo();
            }

            Finalizar();
            #endif
        }

        public static void Inicializar()
        {
            if (jogoInicializado) return;

            Raylib.SetConfigFlags(ConfigFlags.Msaa4xHint | ConfigFlags.ResizableWindow);
            Raylib.InitWindow(LarguraPadrao, AlturaPadrao, "Plazma Burst 2 - Jogo & Level Editor");
            Raylib.InitAudioDevice();
            Raylib.SetTargetFPS(60);

            FontePB2.Inicializar();

            diretorioAssets = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "assets");
            if (!Directory.Exists(diretorioAssets)) diretorioAssets = Path.Combine(Directory.GetCurrentDirectory(), "assets");
            if (!Directory.Exists(diretorioAssets)) diretorioAssets = Path.Combine(Directory.GetCurrentDirectory(), "MotorTiro", "assets");

            caminhoMapa = GerenciadorMapa.ObterCaminhoPersistente();

            editor = new EditorPB2(caminhoMapa);
            jogador = new PersonagemPB2(editor.Mapa.PontoSpawnPlayer, diretorioAssets);
            tiros = new List<ProjetilPlasma>();
            sistemaColisao = new SistemaColisao(jogador.AlturaChao);
            bots = new List<BotIAPB2>();
            sistemaRespawn = new SistemaRespawnPB2(diretorioAssets);

            campanha = new CampanhaPB2();
            menu = new InterfaceMenuPB2(campanha, diretorioAssets);
            modoCampanhaAtivo = false;
            tempoLevelAtual = 0.0f;
            estadoAtual = EstadoApp.Home;

            SincronizarMapaParaFisica();

            camera = new Camera2D
            {
                Target = jogador.Posicao - new Vector2(0, 20),
                Offset = new Vector2(LarguraPadrao / 2.0f, AlturaPadrao / 2.0f),
                Rotation = 0.0f,
                Zoom = 1.85f
            };

            traumaCamera = 0.0f;
            flashDanoJogador = 0.0f;

            jogoInicializado = true;
        }

        // ==========================================
        // MODO 2: BROWSER / WEB (WebAssembly) & CICLO CENTRAL
        // ==========================================
        #if BROWSER
        [System.Runtime.Versioning.SupportedOSPlatform("browser")]
        [JSExport]
        #endif
        public static void ExecutarCicloDoJogo()
        {
            if (jogoDeveFechar) return;
            if (!jogoInicializado) Inicializar();

            float dt = Raylib.GetFrameTime();

            // Suporte a alternância de Tela Cheia com F11 ou Alt+Enter
            if (Raylib.IsKeyPressed(KeyboardKey.F11) || ((Raylib.IsKeyDown(KeyboardKey.LeftAlt) || Raylib.IsKeyDown(KeyboardKey.RightAlt)) && Raylib.IsKeyPressed(KeyboardKey.Enter)))
            {
                Raylib.ToggleFullscreen();
            }

            int telaLargura = Raylib.GetScreenWidth();
            int telaAltura = Raylib.GetScreenHeight();

            Vector2 ratoTela = Raylib.GetMousePosition();

            // Posicionamento responsivo dos painéis HUD (adapta a qualquer tamanho de tela / fullscreen)
            float hudRespawnW = 248.0f;
            float hudRespawnH = 84.0f;
            float hudRespawnX = telaLargura - hudRespawnW - 12.0f;
            sistemaRespawn.RetanguloHUD = new Rectangle(hudRespawnX, 12, hudRespawnW, hudRespawnH);

            float botaoAgacharW = 155.0f;
            float botaoAgacharH = 84.0f;
            float botaoAgacharX = hudRespawnX - botaoAgacharW - 10.0f;
            Rectangle recBotaoAgachar = new Rectangle(botaoAgacharX, 12, botaoAgacharW, botaoAgacharH);

            float hudArmaW = MathF.Max(320.0f, botaoAgacharX - 22.0f);
            Rectangle recHUDArma = new Rectangle(12, 12, hudArmaW, 84);

            bool mouseSobreBotaoAgachar = Raylib.CheckCollisionPointRec(ratoTela, recBotaoAgachar);
            bool mouseSobreHUD = Raylib.CheckCollisionPointRec(ratoTela, recHUDArma);
            bool mouseSobreRespawn = Raylib.CheckCollisionPointRec(ratoTela, sistemaRespawn.RetanguloHUD);
            bool mouseSobreUI = mouseSobreBotaoAgachar || mouseSobreHUD || mouseSobreRespawn;

            // =============================================================
            // LÓGICA
            // =============================================================
            if (estadoAtual == EstadoApp.Home || estadoAtual == EstadoApp.SelecaoLevel)
            {
                menu.Atualizar(dt, telaLargura, telaAltura);
                if (Raylib.IsKeyPressed(KeyboardKey.Escape))
                {
                    if (estadoAtual == EstadoApp.SelecaoLevel) estadoAtual = EstadoApp.Home;
                }
            }
            else if (estadoAtual == EstadoApp.Editor)
            {
                if (Raylib.IsKeyPressed(KeyboardKey.Escape))
                {
                    estadoAtual = EstadoApp.Home;
                }
                else
                {
                    editor.Atualizar(dt, out bool solicitarPlaytest);
                    if (solicitarPlaytest)
                    {
                        modoCampanhaAtivo = false;
                        SincronizarMapaParaFisica();
                        estadoAtual = EstadoApp.Playtest;
                    }
                }
            }
            else if (estadoAtual == EstadoApp.Playtest)
            {
                tempoLevelAtual += dt;

                if (Raylib.IsKeyPressed(KeyboardKey.Escape))
                {
                    estadoAtual = modoCampanhaAtivo ? EstadoApp.SelecaoLevel : EstadoApp.Home;
                }
                else if (Raylib.IsKeyPressed(KeyboardKey.F1))
                {
                    estadoAtual = EstadoApp.Editor;
                }

                // Sincroniza o centro da câmera com a resolução atual (janela ou fullscreen)
                camera.Offset = new Vector2(telaLargura * 0.5f, telaAltura * 0.5f);

                if (Raylib.IsKeyDown(KeyboardKey.Equal) || Raylib.IsKeyDown(KeyboardKey.KpAdd) || Raylib.IsKeyDown(KeyboardKey.E))
                {
                    camera.Zoom = Math.Min(camera.Zoom + dt * 1.5f, 3.2f);
                }
                if (Raylib.IsKeyDown(KeyboardKey.Minus) || Raylib.IsKeyDown(KeyboardKey.KpSubtract) || Raylib.IsKeyDown(KeyboardKey.Q))
                {
                    camera.Zoom = Math.Max(camera.Zoom - dt * 1.5f, 1.0f);
                }

                if (jogador.TraumaCameraAcumulado > 0.0f)
                {
                    traumaCamera = Math.Clamp(traumaCamera + jogador.TraumaCameraAcumulado, 0.0f, 1.0f);
                    jogador.TraumaCameraAcumulado = 0.0f;
                }

                traumaCamera = Math.Max(0.0f, traumaCamera - dt * 2.8f);
                float forcaShake = traumaCamera * traumaCamera;
                Vector2 offsetShake = new Vector2(
                    ((float)Random.Shared.NextDouble() * 2.0f - 1.0f) * forcaShake * 8.5f,
                    ((float)Random.Shared.NextDouble() * 2.0f - 1.0f) * forcaShake * 8.5f
                );

                camera.Target = Vector2.Lerp(camera.Target, jogador.CentroVisual + offsetShake, 14.0f * dt);

                Vector2 ratoMundo = Raylib.GetScreenToWorld2D(ratoTela, camera);

                if (mouseSobreBotaoAgachar && Raylib.IsMouseButtonPressed(MouseButton.Left))
                {
                    jogador.AlternarAgachamento();
                }

                // Atualiza o Sistema de Respawn e Botão USE (Enter ou clique, timer de 2 minutos)
                sistemaRespawn.Atualizar(dt, bots, RespawnarInimigos, ratoTela, mouseSobreUI);

                // Tecla B: Transforma o Jogador em BOT (IA Autônoma com uso livre do arsenal)
                if (Raylib.IsKeyPressed(KeyboardKey.B))
                {
                    jogador.ModoBotAtivo = !jogador.ModoBotAtivo;
                }

                if (jogador.ModoBotAtivo)
                {
                    jogador.AtualizarIABot(dt, bots, sistemaColisao.Paredes);
                }

                int tirosAntes = tiros.Count;
                jogador.Atualizar(dt, ratoMundo, tiros, sistemaColisao.Paredes, mouseSobreUI);

                // Alerta sonoro da IA estilo PB2: apenas bots dentro do raio de percepção acústica percebem
                if (tiros.Count > tirosAntes)
                {
                    ModuloPercepcaoTaticaPB2.PropagarSomDisparo(jogador.Posicao, true, bots, jogador, sistemaColisao.Paredes, 380.0f);
                }

                sistemaColisao.Atualizar(dt, tiros, jogador.Granadas);

                // Processar dano de impacto e física de explosão das Granadas PB2
                for (int g = 0; g < jogador.Granadas.Count; g++)
                {
                    var granada = jogador.Granadas[g];

                    // Colisão física da granada em voo com bots vivos (quica no corpo)
                    if (!granada.Explodiu)
                    {
                        for (int b = 0; b < bots.Count; b++)
                        {
                            var bot = bots[b];
                            if (!bot.Vivo) continue;

                            if (Raylib.CheckCollisionCircleRec(granada.Posicao, 6.0f, bot.CaixaColisao))
                            {
                                granada.Velocidade.X = -granada.Velocidade.X * 0.5f;
                                granada.Velocidade.Y = -granada.Velocidade.Y * 0.4f;
                                granada.Posicao.X += (granada.Posicao.X < bot.CaixaColisao.X + (bot.CaixaColisao.Width * 0.5f) ? -6.0f : 6.0f);
                                break;
                            }
                        }
                    }

                    if (granada.Explodiu && !granada.DanoAplicado)
                    {
                        granada.DanoAplicado = true;

                        // Dano e arremesso físico nos Bots (bloqueado por paredes)
                        for (int b = 0; b < bots.Count; b++)
                        {
                            var bot = bots[b];
                            if (!bot.Vivo) continue;

                            float distBot = Vector2.Distance(bot.CentroVisual, granada.Posicao);
                            if (distBot <= granada.RaioExplosao)
                            {
                                // A onda de choque só atinge se houver linha direta desimpedida
                                if (ModuloPercepcaoTaticaPB2.TemLinhaDeTiroLivre(granada.Posicao, bot.CentroVisual, sistemaColisao.Paredes))
                                {
                                    float fatorDano = 1.0f - (distBot / granada.RaioExplosao);
                                    float dano = granada.Dano * fatorDano;
                                    float empurraoX = bot.Posicao.X - granada.Posicao.X;
                                    if (MathF.Abs(empurraoX) < 1.0f) empurraoX = (float)Random.Shared.NextDouble() * 2.0f - 1.0f;

                                    bot.ReceberDano(dano, empurraoX * 3.5f);
                                    bot.Velocidade.Y = -340.0f * fatorDano; // Lança o corpo no ar!
                                }
                            }
                        }

                        // Dano e arremesso físico no próprio Jogador (bloqueado por paredes)
                        float distJogador = Vector2.Distance(jogador.CentroVisual, granada.Posicao);
                        if (distJogador <= granada.RaioExplosao)
                        {
                            if (ModuloPercepcaoTaticaPB2.TemLinhaDeTiroLivre(granada.Posicao, jogador.CentroVisual, sistemaColisao.Paredes))
                            {
                                float fatorDano = 1.0f - (distJogador / granada.RaioExplosao);
                                jogador.TraumaCameraAcumulado = Math.Max(jogador.TraumaCameraAcumulado, 0.95f * fatorDano);
                                flashDanoJogador = 0.40f * fatorDano;
                                float empurraoX = jogador.Posicao.X - granada.Posicao.X;
                                if (MathF.Abs(empurraoX) < 1.0f) empurraoX = 1.0f;
                                jogador.Velocidade.X += MathF.Sign(empurraoX) * 360.0f * fatorDano;
                                jogador.Velocidade.Y = -360.0f * fatorDano;
                            }
                        }
                    }
                }

                // Atualiza cada um dos Bots IA (movimento, mira, tiros e dano)
                for (int i = 0; i < bots.Count; i++)
                {
                    int tirosAntesBot = tiros.Count;
                    bots[i].Atualizar(dt, jogador, tiros, sistemaColisao.Paredes, jogador.Granadas);
                    if (tiros.Count > tirosAntesBot)
                    {
                        ModuloPercepcaoTaticaPB2.PropagarSomDisparo(bots[i].Posicao, false, bots, jogador, sistemaColisao.Paredes, 380.0f);
                    }
                }

                // Colisão de tiros dos bots contra o jogador
                if (flashDanoJogador > 0.0f) flashDanoJogador -= dt;
                Rectangle caixaJogador = new Rectangle(jogador.Posicao.X - 16.0f, jogador.Posicao.Y - 62.0f, 32.0f, 62.0f);
                for (int i = 0; i < tiros.Count; i++)
                {
                    var tiro = tiros[i];
                    if (tiro.Ativo && !tiro.DoJogador)
                    {
                        if (Raylib.CheckCollisionPointRec(tiro.Posicao, caixaJogador))
                        {
                            tiro.Ativo = false;
                            jogador.TraumaCameraAcumulado += 0.35f;
                            flashDanoJogador = 0.15f;
                            if (jogador.ModoBotAtivo)
                            {
                                Vector2 origemTiro = tiro.Posicao - Vector2.Normalize(tiro.Velocidade) * 70.0f;
                                jogador.NotificarDisparoInimigo(origemTiro);
                            }
                        }
                    }
                }

                for (int i = 0; i < tiros.Count; i++) tiros[i].Atualizar(dt);
                tiros.RemoveAll(t => !t.Ativo);
            }

            // =============================================================
            // DESENHO
            // =============================================================
            Raylib.BeginDrawing();

            if (estadoAtual == EstadoApp.Home)
            {
                AcaoMenu acao = menu.DesenharHome(telaLargura, telaAltura, ratoTela);
                if (acao == AcaoMenu.AbrirCampanha)
                {
                    estadoAtual = EstadoApp.SelecaoLevel;
                }
                else if (acao == AcaoMenu.AbrirPlaytest)
                {
                    modoCampanhaAtivo = false;
                    SincronizarMapaParaFisica();
                    estadoAtual = EstadoApp.Playtest;
                }
                else if (acao == AcaoMenu.AbrirEditor)
                {
                    estadoAtual = EstadoApp.Editor;
                }
                else if (acao == AcaoMenu.SairJogo)
                {
                    jogoDeveFechar = true;
                }
            }
            else if (estadoAtual == EstadoApp.SelecaoLevel)
            {
                AcaoMenu acao = menu.DesenharSelecaoLevel(telaLargura, telaAltura, ratoTela);
                if (acao == AcaoMenu.VoltarHome)
                {
                    estadoAtual = EstadoApp.Home;
                }
                else if (acao == AcaoMenu.IniciarFase)
                {
                    modoCampanhaAtivo = true;
                    tempoLevelAtual = 0.0f;
                    CarregarFaseCampanha(campanha.LevelAtual.Numero);
                    estadoAtual = EstadoApp.Playtest;
                }
            }
            else if (estadoAtual == EstadoApp.Editor)
            {
                editor.Desenhar();
            }
            else if (estadoAtual == EstadoApp.Playtest)
            {
                Raylib.ClearBackground(new Color(236, 240, 244, 255));

                Raylib.BeginMode2D(camera);

                // Grade do mundo responsiva
                int minX = (int)camera.Target.X - 3200;
                int maxX = (int)camera.Target.X + 3200;
                Color corGradeMundo = new Color(215, 224, 232, 255);
                for (int x = minX - (minX % 40); x < maxX; x += 40)
                {
                    Raylib.DrawLine(x, -1600, x, 2200, corGradeMundo);
                }
                for (int y = -1600; y < 2200; y += 40)
                {
                    Raylib.DrawLine(minX, y, maxX, y, corGradeMundo);
                }

                // Paredes
                foreach (var plat in sistemaColisao.Paredes)
                {
                    EditorPB2.DesenharParedePB2(plat);
                }

                // Bots IA com corpos montados a partir dos sprites oficiais e IK
                foreach (var bot in bots)
                {
                    bot.Desenhar();
                }

                foreach (var tiro in tiros) tiro.Desenhar();

                jogador.Desenhar();

                // Granadas PB2 e Efeitos Visuais de Explosão no Mundo
                for (int i = 0; i < jogador.Granadas.Count; i++)
                {
                    jogador.Granadas[i].Desenhar();
                }

                // Switch USE físico no cenário próximo ao spawn
                sistemaRespawn.DesenharMundo();

                // Efeito holográfico de travamento no Modo Bot
                if (jogador.ModoBotAtivo && jogador.BotAlvoAtual != null && jogador.BotAlvoAtual.Vivo)
                {
                    Vector2 posAlvo = jogador.BotAlvoAtual.CentroVisual;
                    Raylib.DrawCircleLines((int)posAlvo.X, (int)posAlvo.Y, 22.0f, new Color(0, 240, 255, 180));
                    Raylib.DrawCircleV(posAlvo, 2.5f, new Color(0, 240, 255, 240));
                    FontePB2.DesenharTexto("[ALVO TRAVADO]", posAlvo.X - 35, posAlvo.Y - 34, 10, new Color(0, 240, 255, 240));
                }

                if (jogador.ArmaAtual == null || !jogador.ArmaAtual.TemLaser)
                {
                    Vector2 pontaCano = jogador.ObterPontaDoCano();
                    Vector2 dirMiraLaser = new Vector2(MathF.Cos(jogador.AnguloMiraRad), MathF.Sin(jogador.AnguloMiraRad));
                    Vector2 pontoAlvoLaser = jogador.ModoBotAtivo 
                        ? jogador.BotMiraSuavizada 
                        : Raylib.GetScreenToWorld2D(Raylib.GetMousePosition(), camera);
                    Vector2 vetorAoPonto = pontoAlvoLaser - pontaCano;
                    float alinhamento = Vector2.Dot(vetorAoPonto, dirMiraLaser);
                    Vector2 fimLaser = (alinhamento > 20.0f) ? pontoAlvoLaser : (pontaCano + dirMiraLaser * 800.0f);
                    Raylib.DrawLineEx(pontaCano, fimLaser, 1.2f, new Color(0, 160, 220, 100));
                }

                Raylib.EndMode2D();

                // Flash sutil vermelho na tela quando o jogador é atingido por tiros dos bots (cobre toda a tela cheia)
                if (flashDanoJogador > 0.0f)
                {
                    Raylib.DrawRectangle(0, 0, telaLargura, telaAltura, new Color(255, 0, 0, (int)(flashDanoJogador * 220)));
                }

                // Cursor mira dinâmica
                if (!mouseSobreUI)
                {
                    if (!jogador.ArmaEmpunhada)
                    {
                        Raylib.DrawCircleLines((int)ratoTela.X, (int)ratoTela.Y, 12, new Color(59, 130, 246, 190));
                        Raylib.DrawCircleV(ratoTela, 2.2f, new Color(100, 115, 130, 255));
                    }
                    else
                    {
                        Raylib.DrawCircleLines((int)ratoTela.X, (int)ratoTela.Y, 10, new Color(0, 150, 220, 220));
                        Raylib.DrawCircleV(ratoTela, 2.5f, new Color(220, 40, 40, 255));
                    }
                }

                // Painel de Armas e Munição (responsivo e adaptativo)
                Raylib.DrawRectangleRec(recHUDArma, new Color(255, 255, 255, 240));
                Raylib.DrawRectangleLinesEx(recHUDArma, 1.2f, new Color(180, 195, 210, 255));

                string nomeArma = jogador.ArmaAtual != null ? jogador.ArmaAtual.Nome : "Nenhuma";
                string nomeCostas = jogador.ObterArmaNasCostas()?.Nome ?? "Nenhuma";
                FontePB2.DesenharTexto($"Mão: {nomeArma} (Slot {jogador.ArmaAtualIndex + 1})", 22, 17, 15, new Color(20, 40, 60, 255));

                if (jogador.ArmaAtual != null)
                {
                    float barraW = MathF.Min(205.0f, recHUDArma.Width - 310.0f);
                    if (barraW < 120.0f) barraW = 120.0f;
                    float barraX = MathF.Min(310.0f, recHUDArma.X + recHUDArma.Width - barraW - 12.0f);

                    if (!jogador.ArmaEmpunhada)
                    {
                        float progressoSaque = jogador.ProgressoEquip;
                        Raylib.DrawRectangle((int)barraX, 16, (int)barraW, 18, new Color(239, 246, 255, 255));
                        Raylib.DrawRectangle((int)barraX, 16, (int)(barraW * progressoSaque), 18, new Color(59, 130, 246, 180));
                        Raylib.DrawRectangleLines((int)barraX, 16, (int)barraW, 18, new Color(37, 99, 235, 255));
                        FontePB2.DesenharTexto($"SACANDO: {(int)(progressoSaque * 100)}%", barraX + 15, 18, 12, new Color(30, 64, 175, 255));
                    }
                    else if (jogador.EstaRecarregando)
                    {
                        float progressoRecarga = 1.0f - (jogador.TimerRecarga / jogador.DuracaoRecargaTotal);
                        Raylib.DrawRectangle((int)barraX, 16, (int)barraW, 18, new Color(255, 240, 210, 255));
                        Raylib.DrawRectangle((int)barraX, 16, (int)(barraW * progressoRecarga), 18, new Color(245, 158, 11, 200));
                        Raylib.DrawRectangleLines((int)barraX, 16, (int)barraW, 18, new Color(217, 119, 6, 255));
                        FontePB2.DesenharTexto("RECARREGANDO...", barraX + 18, 18, 12, new Color(146, 64, 14, 255));
                    }
                    else if (jogador.ArmaAtual.RequerCarga && !jogador.ArmaAtual.ProntoParaAtirar)
                    {
                        float progressoCarga = Math.Clamp(jogador.ArmaAtual.CargaAtual / jogador.ArmaAtual.TempoCarga, 0.0f, 1.0f);
                        FontePB2.DesenharTexto($"CARREGANDO {(int)(progressoCarga * 100)}%", barraX + 15, 18, 12, new Color(194, 65, 12, 255));
                    }
                    else if (jogador.ArmaAtual.MunicaoAtual == 0)
                    {
                        FontePB2.DesenharTexto("! SEM BALA [R] !", barraX + 25, 18, 12, new Color(185, 28, 28, 255));
                    }
                    else
                    {
                        FontePB2.DesenharTexto($"Mun: {jogador.ArmaAtual.MunicaoAtual}/{jogador.ArmaAtual.CapacidadePente} [R]", barraX + 15, 18, 14, new Color(15, 118, 110, 255));
                    }
                }

                string statusColdre = jogador.ArmaAtual != null && jogador.ArmaAtual.Tipo == "pistol" ? $"Costas: {nomeCostas}" : "Cintura: Pistola";
                FontePB2.DesenharTexto(statusColdre, 22, 40, 12, new Color(100, 115, 130, 255));
                FontePB2.DesenharTexto("A/D: Andar | S/Ctrl/C: Agachar | W: Pulo", 240, 40, 12, new Color(70, 85, 100, 255));
                FontePB2.DesenharTexto("[1-9] Arsenal PB2 | [G] Granada | [B] Virar Bot | [R] Recarga", 22, 62, 12, new Color(0, 120, 180, 255));

                // Indicador no topo da tela quando o Modo Bot estiver ativo
                if (jogador.ModoBotAtivo)
                {
                    int bannerLarg = 440;
                    int bannerX = (telaLargura - bannerLarg) / 2;
                    Raylib.DrawRectangle(bannerX, 10, bannerLarg, 38, new Color(15, 23, 42, 240));
                    Raylib.DrawRectangleLines(bannerX, 10, bannerLarg, 38, new Color(0, 230, 255, 245));
                    FontePB2.DesenharTexto("🤖 MODO BOT ATIVO [Pressione B para Desativar]", bannerX + 16, 15, 13, new Color(0, 240, 255, 255));
                    FontePB2.DesenharTexto("IA AUTÔNOMA • 9 ARMAS LIVRES • ESQUIVA • GRANADAS", bannerX + 16, 31, 10, new Color(180, 230, 255, 220));
                }
                else if (modoCampanhaAtivo)
                {
                    var lvlAtual = campanha.LevelAtual;
                    int bannerLarg = 510;
                    int bannerX = (telaLargura - bannerLarg) / 2;
                    Raylib.DrawRectangle(bannerX, 10, bannerLarg, 38, new Color(12, 18, 30, 240));
                    Raylib.DrawRectangleLines(bannerX, 10, bannerLarg, 38, new Color(0, 180, 255, 220));

                    string txtLvl = $"CAMPANHA • LEVEL {lvlAtual.Numero:D2}: {lvlAtual.Titulo.ToUpper()}";
                    FontePB2.DesenharTexto(txtLvl, bannerX + 16, 13, 13, Color.White);

                    int vivos = bots.FindAll(b => b.Vivo).Count;
                    string txtStatus = $"DIF: {campanha.DificuldadeAtual.ToString().ToUpper()} | INIMIGOS: {vivos}/{bots.Count} | TEMPO: {tempoLevelAtual:F1}s";
                    FontePB2.DesenharTexto(txtStatus, bannerX + 16, 29, 10, new Color(0, 220, 255, 230));

                    // Checagem de Vitória do Level
                    if (bots.Count > 0 && vivos == 0)
                    {
                        campanha.ConcluirLevel(lvlAtual.Numero, tempoLevelAtual);

                        int vitW = 520;
                        int vitH = 68;
                        int vitX = (telaLargura - vitW) / 2;
                        int vitY = (telaAltura / 2) - 80;

                        Raylib.DrawRectangle(vitX, vitY, vitW, vitH, new Color(10, 20, 32, 250));
                        Raylib.DrawRectangleLinesEx(new Rectangle(vitX, vitY, vitW, vitH), 2.0f, new Color(16, 185, 129, 255));

                        FontePB2.DesenharTexto("★ MISSÃO CUMPRIDA! SETOR LIBERADO ★", vitX + 75, vitY + 14, 18, new Color(52, 211, 153, 255));
                        FontePB2.DesenharTexto("[ENTER] Próxima Missão    •    [ESC] Menu de Fases", vitX + 90, vitY + 40, 13, Color.White);

                        if (Raylib.IsKeyPressed(KeyboardKey.Enter))
                        {
                            if (lvlAtual.Numero < CampanhaPB2.TOTAL_LEVELS)
                            {
                                campanha.LevelSelecionadoIndex++;
                                tempoLevelAtual = 0.0f;
                                CarregarFaseCampanha(campanha.LevelAtual.Numero);
                            }
                            else
                            {
                                estadoAtual = EstadoApp.SelecaoLevel;
                            }
                        }
                    }
                }

                // Botão Agachar
                Color corFundoBotao = jogador.Agachado ? new Color(230, 252, 242, 245) : new Color(255, 255, 255, 230);
                Color corBordaBotao = jogador.Agachado ? new Color(16, 185, 129, 255) : (mouseSobreBotaoAgachar ? new Color(0, 140, 220, 255) : new Color(180, 195, 210, 255));

                Raylib.DrawRectangleRec(recBotaoAgachar, corFundoBotao);
                Raylib.DrawRectangleLinesEx(recBotaoAgachar, mouseSobreBotaoAgachar ? 2.0f : 1.2f, corBordaBotao);

                FontePB2.DesenharTexto("AGACHAR", recBotaoAgachar.X + 35, recBotaoAgachar.Y + 16, 16, new Color(30, 45, 60, 255));
                FontePB2.DesenharTexto(jogador.Agachado ? "[X] AGACHADO" : "[ ] EM PÉ", recBotaoAgachar.X + 35, recBotaoAgachar.Y + 39, 13, jogador.Agachado ? new Color(5, 150, 105, 255) : new Color(110, 125, 140, 255));
                FontePB2.DesenharTexto("[S / Ctrl / C]", recBotaoAgachar.X + 35, recBotaoAgachar.Y + 60, 11, new Color(120, 135, 150, 255));

                // Terminal Sci-Fi / Botão USE de Respawn PB2
                sistemaRespawn.DesenharHUD(ratoTela);

                // Botões de Rodapé no canto inferior direito
                Rectangle recBtnSairPlaytest = new Rectangle(telaLargura - 210, telaAltura - 34, 198, 26);
                bool hoverBtnSair = Raylib.CheckCollisionPointRec(ratoTela, recBtnSairPlaytest);
                if (hoverBtnSair && Raylib.IsMouseButtonPressed(MouseButton.Left))
                {
                    estadoAtual = modoCampanhaAtivo ? EstadoApp.SelecaoLevel : EstadoApp.Home;
                }

                Raylib.DrawRectangleRec(recBtnSairPlaytest, hoverBtnSair ? new Color(40, 60, 85, 240) : new Color(25, 35, 48, 220));
                Raylib.DrawRectangleLinesEx(recBtnSairPlaytest, 1.0f, hoverBtnSair ? new Color(0, 220, 255, 255) : new Color(55, 75, 100, 255));

                string txtBtnSair = modoCampanhaAtivo ? "[ESC] Menu de Fases" : "[ESC] Menu Principal";
                Vector2 tamBtnSair = FontePB2.MedirTexto(txtBtnSair, 12);
                FontePB2.DesenharTexto(txtBtnSair, recBtnSairPlaytest.X + (recBtnSairPlaytest.Width - tamBtnSair.X) * 0.5f, recBtnSairPlaytest.Y + 6, 12, hoverBtnSair ? Color.White : Color.Gold);

                Raylib.DrawRectangle(telaLargura - 330, telaAltura - 34, 112, 26, new Color(25, 35, 48, 190));
                Raylib.DrawRectangleLines(telaLargura - 330, telaAltura - 34, 112, 26, new Color(55, 75, 100, 200));
                FontePB2.DesenharTexto("[F11] Tela Cheia", telaLargura - 322, telaAltura - 28, 11, new Color(175, 205, 235, 230));
            }

            Raylib.EndDrawing();
        }

        private static void CarregarFaseCampanha(int numeroLevel)
        {
            var mapaFase = campanha.GerarOuCarregarMapaLevel(numeroLevel);

            sistemaColisao.Paredes.Clear();
            foreach (var p in mapaFase.Paredes)
            {
                sistemaColisao.Paredes.Add(new Rectangle(p.X, p.Y, p.Largura, p.Altura));
            }

            bots.Clear();
            var def = campanha.Levels[Math.Clamp(numeroLevel - 1, 0, campanha.Levels.Count - 1)];
            for (int i = 0; i < mapaFase.SpawnsInimigos.Count; i++)
            {
                var spawn = mapaFase.SpawnsInimigos[i];
                TipoArmaBot armaBot = def.ArmaDestaque;
                if (!string.IsNullOrEmpty(spawn.Tipo) && Enum.TryParse<TipoArmaBot>(spawn.Tipo, true, out var armaConfig))
                {
                    armaBot = armaConfig;
                }
                else if (i % 2 == 1)
                {
                    armaBot = (TipoArmaBot)(i % 4);
                }

                CorpoBot corpoBot = def.FaccaoInimiga;
                bots.Add(new BotIAPB2(spawn.ToVector2(), diretorioAssets, $"#actor*{i + 1}", armaBot, corpoBot));
            }

            jogador.Posicao = mapaFase.PontoSpawnPlayer;
            jogador.Velocidade = Vector2.Zero;
            tiros.Clear();
            jogador.Granadas.Clear();
            jogador.Capsulas.Clear();

            sistemaRespawn.PosicaoMundo = mapaFase.PontoSpawnPlayer + new Vector2(60.0f, -24.0f);
        }

        private static void RespawnarInimigos()
        {
            if (modoCampanhaAtivo)
            {
                CarregarFaseCampanha(campanha.LevelAtual.Numero);
            }
            else
            {
                bots.Clear();
                for (int i = 0; i < editor.Mapa.SpawnsInimigos.Count; i++)
                {
                    var spawn = editor.Mapa.SpawnsInimigos[i];
                    TipoArmaBot armaBot = (TipoArmaBot)(i % 4);
                    CorpoBot corpoBot = (CorpoBot)(i % 5);
                    if (!string.IsNullOrEmpty(spawn.Tipo) && Enum.TryParse<TipoArmaBot>(spawn.Tipo, true, out var armaConfig))
                    {
                        armaBot = armaConfig;
                    }
                    bots.Add(new BotIAPB2(spawn.ToVector2(), diretorioAssets, $"#actor*{i + 1}", armaBot, corpoBot));
                }
            }
        }

        private static void SincronizarMapaParaFisica()
        {
            sistemaColisao.Paredes.Clear();
            foreach (var p in editor.Mapa.Paredes)
            {
                sistemaColisao.Paredes.Add(new Rectangle(p.X, p.Y, p.Largura, p.Altura));
            }

            RespawnarInimigos();

            jogador.Posicao = editor.Mapa.PontoSpawnPlayer;
            jogador.Velocidade = Vector2.Zero;
            tiros.Clear();
            jogador.Granadas.Clear();
            jogador.Capsulas.Clear();

            sistemaRespawn.PosicaoMundo = editor.Mapa.PontoSpawnPlayer + new Vector2(60.0f, -24.0f);
        }

        public static void Finalizar()
        {
            BotIAPB2.DescarregarRecursos();
            FontePB2.Descarregar();
            jogador?.Dispose();
            Raylib.CloseAudioDevice();
            Raylib.CloseWindow();
        }
    }
}