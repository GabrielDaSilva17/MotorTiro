using System;
using System.Collections.Generic;
using System.Numerics;
using Raylib_cs;

namespace JogoDeTiro
{
    public enum AcaoMenu
    {
        Nenhuma,
        AbrirCampanha,
        AbrirPlaytest,
        AbrirEditor,
        IniciarFase,
        VoltarHome,
        SairJogo
    }

    public class ParticulaFundoMenu
    {
        public Vector2 Posicao;
        public Vector2 Velocidade;
        public float Tamanho;
        public Color Cor;
        public float Alpha;
        public float PulsoOffset;

        public ParticulaFundoMenu(float telaLargura, float telaAltura)
        {
            Reset(telaLargura, telaAltura, true);
        }

        public void Reset(float telaLargura, float telaAltura, bool aleatorioY = false)
        {
            Posicao = new Vector2(
                (float)Random.Shared.NextDouble() * telaLargura,
                aleatorioY ? (float)Random.Shared.NextDouble() * telaAltura : telaAltura + 10.0f
            );
            Velocidade = new Vector2(
                (float)Random.Shared.NextDouble() * 30.0f - 15.0f,
                -((float)Random.Shared.NextDouble() * 45.0f + 25.0f)
            );
            Tamanho = (float)Random.Shared.NextDouble() * 2.8f + 1.2f;
            PulsoOffset = (float)Random.Shared.NextDouble() * MathF.PI * 2.0f;

            Color[] paleta = new Color[]
            {
                new Color(0, 220, 255, 200),   // Ciano Plasma
                new Color(60, 140, 255, 180),  // Azul PB2
                new Color(255, 160, 40, 190),  // Âmbar Centelha
                new Color(180, 240, 255, 220)  // Branco brilhante
            };
            Cor = paleta[Random.Shared.Next(paleta.Length)];
        }

        public void Atualizar(float dt, float telaLargura, float telaAltura)
        {
            Posicao += Velocidade * dt;
            Alpha = 0.5f + (0.5f * MathF.Sin((float)Raylib.GetTime() * 3.0f + PulsoOffset));

            if (Posicao.Y < -20.0f || Posicao.X < -50.0f || Posicao.X > telaLargura + 50.0f)
            {
                Reset(telaLargura, telaAltura, false);
            }
        }

        public void Desenhar()
        {
            Color c = new Color(Cor.R, Cor.G, Cor.B, (byte)(Cor.A * Alpha));
            Raylib.DrawCircleV(Posicao, Tamanho, c);
        }
    }

    public class InterfaceMenuPB2
    {
        private List<ParticulaFundoMenu> particulas = new List<ParticulaFundoMenu>();
        public CampanhaPB2 Campanha { get; private set; }
        private int capituloAbaAtual = 0; // 0 = Cap 1 (1-9), 1 = Cap 2 (10-18), etc.
        private float tempoAnimacao = 0.0f;

        public InterfaceMenuPB2(CampanhaPB2 campanha, string diretorioAssets)
        {
            Campanha = campanha;

            for (int i = 0; i < 40; i++)
            {
                particulas.Add(new ParticulaFundoMenu(960, 640));
            }
        }

        public void Atualizar(float dt, float telaLargura, float telaAltura)
        {
            tempoAnimacao += dt;
            for (int i = 0; i < particulas.Count; i++)
            {
                particulas[i].Atualizar(dt, telaLargura, telaAltura);
            }
        }

        // =========================================================================
        // 1. TELA HOME / MENU PRINCIPAL
        // =========================================================================
        public AcaoMenu DesenharHome(float telaLargura, float telaAltura, Vector2 mousePos)
        {
            DesenharFundoCibernetico(telaLargura, telaAltura);

            AcaoMenu acao = AcaoMenu.Nenhuma;

            // Logotipo e Título Principal Estilo Plazma Burst 2
            float topoY = 70.0f;
            string tituloPrincipal = "PLAZMA BURST 2";
            Vector2 tamTitulo = FontePB2.MedirTexto(tituloPrincipal, 48);
            float titX = (telaLargura - tamTitulo.X) * 0.5f;

            // Efeito de brilho de neon no título
            float pulsoGlow = (MathF.Sin(tempoAnimacao * 3.5f) + 1.0f) * 0.5f;
            int alphaGlow = (int)(80 + (pulsoGlow * 120));
            Raylib.DrawRectangleGradientH(
                (int)(titX - 40), (int)(topoY + tamTitulo.Y + 6),
                (int)(tamTitulo.X + 80), 3,
                new Color(0, 180, 255, 0), new Color(0, 240, 255, alphaGlow)
            );
            Raylib.DrawRectangleGradientH(
                (int)(titX + tamTitulo.X * 0.5f), (int)(topoY + tamTitulo.Y + 6),
                (int)(tamTitulo.X * 0.5f + 40), 3,
                new Color(0, 240, 255, alphaGlow), new Color(0, 180, 255, 0)
            );

            // Sombra e Texto do Título
            FontePB2.DesenharTexto(tituloPrincipal, titX + 2, topoY + 3, 48, new Color(5, 15, 30, 200));
            FontePB2.DesenharTexto(tituloPrincipal, titX, topoY, 48, new Color(240, 250, 255, 255));

            string subtitulo = "REMAKE TATICO OFICIAL • MOTOR PB2 EM C# & RAYLIB";
            Vector2 tamSub = FontePB2.MedirTexto(subtitulo, 13);
            FontePB2.DesenharTexto(subtitulo, (telaLargura - tamSub.X) * 0.5f, topoY + tamTitulo.Y + 14, 13, new Color(0, 220, 255, 220));

            // Painel Central com os Botões
            float botaoLarg = 340.0f;
            float botaoAlt = 46.0f;
            float centroX = (telaLargura - botaoLarg) * 0.5f;
            float inicioY = topoY + 120.0f;
            float espacamentoY = 60.0f;

            // 1. CAMPANHA (45 NÍVEIS)
            if (DesenharBotaoMenu(new Rectangle(centroX, inicioY, botaoLarg, botaoAlt), "► JOGAR CAMPANHA (45 NÍVEIS)", mousePos, ehDestaque: true))
            {
                acao = AcaoMenu.AbrirCampanha;
            }

            // 2. MODO LIVRE / PLAYTEST
            if (DesenharBotaoMenu(new Rectangle(centroX, inicioY + espacamentoY, botaoLarg, botaoAlt), "MODO LIVRE / PLAYTEST", mousePos))
            {
                acao = AcaoMenu.AbrirPlaytest;
            }

            // 3. LEVEL EDITOR
            if (DesenharBotaoMenu(new Rectangle(centroX, inicioY + (espacamentoY * 2), botaoLarg, botaoAlt), "LEVEL EDITOR PB2", mousePos))
            {
                acao = AcaoMenu.AbrirEditor;
            }

            // 4. SAIR DO JOGO
            if (DesenharBotaoMenu(new Rectangle(centroX, inicioY + (espacamentoY * 3), botaoLarg, botaoAlt), "SAIR DO JOGO", mousePos, ehAlerta: true))
            {
                acao = AcaoMenu.SairJogo;
            }

            // Rodapé Informativo
            string infoRodape = "Controles: [W/A/S/D] Mover/Pulo/Agachar • [1-9] Arsenal • [G] Granadas • [B] Modo Bot IA";
            Vector2 tamRodape = FontePB2.MedirTexto(infoRodape, 12);
            FontePB2.DesenharTexto(infoRodape, (telaLargura - tamRodape.X) * 0.5f, telaAltura - 32, 12, new Color(130, 160, 190, 200));

            return acao;
        }

        // =========================================================================
        // 2. TELA PLAY LEVEL / SELEÇÃO DE MISSÃO (45 NÍVEIS)
        // =========================================================================
        public AcaoMenu DesenharSelecaoLevel(float telaLargura, float telaAltura, Vector2 mousePos)
        {
            DesenharFundoCibernetico(telaLargura, telaAltura);

            AcaoMenu acao = AcaoMenu.Nenhuma;

            // Topo da tela
            float cabecalhoY = 22.0f;
            FontePB2.DesenharTexto("CAMPANHA: SELEÇÃO DE MISSÃO", 35, cabecalhoY, 24, new Color(245, 250, 255, 255));
            FontePB2.DesenharTexto("ESCOLHA O NÍVEL E A DIFICULDADE TÁTICA", 35, cabecalhoY + 28, 12, new Color(0, 220, 255, 220));

            // Botão Voltar ao Menu no Topo Direito
            Rectangle btnVoltarRec = new Rectangle(telaLargura - 170, cabecalhoY + 4, 135, 34);
            if (DesenharBotaoPequeno(btnVoltarRec, "◄ VOLTAR", mousePos))
            {
                acao = AcaoMenu.VoltarHome;
            }

            // Barra de Dificuldade
            float barraDifY = cabecalhoY + 54.0f;
            FontePB2.DesenharTexto("DIFICULDADE:", 35, barraDifY + 6, 13, new Color(175, 195, 215, 255));

            float xDif = 150.0f;
            float wDif = 98.0f;
            float hDif = 28.0f;

            DificuldadeJogo[] dificuldades = new DificuldadeJogo[]
            {
                DificuldadeJogo.Facil,
                DificuldadeJogo.Normal,
                DificuldadeJogo.Dificil,
                DificuldadeJogo.Impossivel
            };

            string[] nomesDif = new string[] { "FÁCIL", "NORMAL", "DIFÍCIL", "HAVOC" };
            Color[] coresDif = new Color[]
            {
                new Color(16, 185, 129, 255),  // Verde Esmeralda
                new Color(14, 165, 233, 255),  // Azul Cyan
                new Color(245, 158, 11, 255),  // Âmbar
                new Color(239, 68, 68, 255)    // Carmesim
            };

            for (int d = 0; d < dificuldades.Length; d++)
            {
                Rectangle rDif = new Rectangle(xDif + (d * (wDif + 8.0f)), barraDifY, wDif, hDif);
                bool ativo = (Campanha.DificuldadeAtual == dificuldades[d]);
                bool hover = Raylib.CheckCollisionPointRec(mousePos, rDif);

                if (hover && Raylib.IsMouseButtonPressed(MouseButton.Left))
                {
                    Campanha.DificuldadeAtual = dificuldades[d];
                    Campanha.SalvarProgresso();
                }

                Color bg = ativo ? coresDif[d] : (hover ? new Color(30, 48, 70, 220) : new Color(18, 26, 40, 200));
                Color borda = ativo ? Color.White : (hover ? coresDif[d] : new Color(60, 85, 115, 200));

                Raylib.DrawRectangleRec(rDif, bg);
                Raylib.DrawRectangleLinesEx(rDif, ativo ? 2.0f : 1.0f, borda);

                Vector2 tamTexto = FontePB2.MedirTexto(nomesDif[d], 12);
                FontePB2.DesenharTexto(nomesDif[d], rDif.X + (wDif - tamTexto.X) * 0.5f, rDif.Y + (hDif - tamTexto.Y) * 0.5f, 12, ativo ? Color.White : new Color(200, 220, 240, 255));
            }

            // Abas de Capítulos (5 Capítulos com 9 fases cada)
            float abaY = barraDifY + 38.0f;
            float abaW = 104.0f;
            float abaH = 26.0f;

            for (int c = 0; c < 5; c++)
            {
                Rectangle rAba = new Rectangle(35 + (c * (abaW + 6.0f)), abaY, abaW, abaH);
                bool ativa = (capituloAbaAtual == c);
                bool hover = Raylib.CheckCollisionPointRec(mousePos, rAba);

                if (hover && Raylib.IsMouseButtonPressed(MouseButton.Left))
                {
                    capituloAbaAtual = c;
                    Campanha.LevelSelecionadoIndex = c * 9;
                }

                Color bg = ativa ? new Color(0, 160, 230, 220) : (hover ? new Color(30, 45, 65, 220) : new Color(15, 22, 34, 200));
                Raylib.DrawRectangleRec(rAba, bg);
                Raylib.DrawRectangleLinesEx(rAba, 1.0f, ativa ? new Color(0, 230, 255, 255) : new Color(45, 65, 90, 200));

                string nomeAba = $"CAPÍTULO {c + 1}";
                Vector2 tAba = FontePB2.MedirTexto(nomeAba, 11);
                FontePB2.DesenharTexto(nomeAba, rAba.X + (abaW - tAba.X) * 0.5f, rAba.Y + (abaH - tAba.Y) * 0.5f, 11, ativa ? Color.White : new Color(180, 205, 230, 255));
            }

            // Divisão da Tela: Lado Esquerdo (Grade de Níveis) e Lado Direito (Detalhes e Briefing da Missão)
            float painelTopY = abaY + 34.0f;
            float gradeW = 540.0f;
            float detalhesW = telaLargura - gradeW - 65.0f;
            float painelH = telaAltura - painelTopY - 24.0f;

            // -------------------------------------------------------------
            // GRADE DE NÍVEIS DO CAPÍTULO ATUAL (9 Fases: 3x3)
            // -------------------------------------------------------------
            int levelBase = capituloAbaAtual * 9;
            float cardW = (gradeW - 20.0f) / 3.0f;
            float cardH = (painelH - 20.0f) / 3.0f;

            for (int i = 0; i < 9; i++)
            {
                int lvlIdx = levelBase + i;
                if (lvlIdx >= Campanha.Levels.Count) break;

                var lvl = Campanha.Levels[lvlIdx];
                int col = i % 3;
                int lin = i / 3;

                Rectangle rCard = new Rectangle(35 + (col * (cardW + 10.0f)), painelTopY + (lin * (cardH + 10.0f)), cardW, cardH);
                bool selecionado = (Campanha.LevelSelecionadoIndex == lvlIdx);
                bool hover = Raylib.CheckCollisionPointRec(mousePos, rCard);

                if (hover && Raylib.IsMouseButtonPressed(MouseButton.Left) && lvl.Desbloqueado)
                {
                    Campanha.LevelSelecionadoIndex = lvlIdx;
                }

                Color cardBg;
                Color cardBorda;

                if (!lvl.Desbloqueado)
                {
                    cardBg = new Color(12, 16, 24, 180);
                    cardBorda = new Color(30, 40, 55, 180);
                }
                else if (selecionado)
                {
                    cardBg = new Color(15, 45, 80, 240);
                    cardBorda = new Color(0, 230, 255, 255);
                }
                else if (hover)
                {
                    cardBg = new Color(22, 35, 55, 230);
                    cardBorda = new Color(0, 160, 220, 220);
                }
                else
                {
                    cardBg = new Color(16, 24, 38, 220);
                    cardBorda = lvl.Concluido ? new Color(16, 185, 129, 180) : new Color(40, 60, 85, 200);
                }

                Raylib.DrawRectangleRec(rCard, cardBg);
                Raylib.DrawRectangleLinesEx(rCard, selecionado ? 2.0f : 1.0f, cardBorda);

                // Cabeçalho do Card (Número e Status)
                string strNum = $"LEVEL {lvl.Numero:D2}";
                FontePB2.DesenharTexto(strNum, rCard.X + 10, rCard.Y + 10, 13, lvl.Desbloqueado ? (selecionado ? new Color(0, 240, 255, 255) : Color.White) : new Color(90, 105, 125, 255));

                if (!lvl.Desbloqueado)
                {
                    FontePB2.DesenharTexto("🔒 BLOQUEADO", rCard.X + 10, rCard.Y + cardH - 24, 11, new Color(130, 80, 80, 255));
                }
                else if (lvl.Concluido)
                {
                    FontePB2.DesenharTexto("✓ CONCLUÍDO", rCard.X + 10, rCard.Y + cardH - 24, 11, new Color(16, 185, 129, 255));
                }
                else
                {
                    FontePB2.DesenharTexto("DISPONÍVEL", rCard.X + 10, rCard.Y + cardH - 24, 11, new Color(0, 200, 255, 220));
                }

                // Título da Missão dentro do Card
                FontePB2.DesenharTexto(lvl.Titulo, rCard.X + 10, rCard.Y + 30, 12, lvl.Desbloqueado ? new Color(200, 220, 240, 255) : new Color(75, 90, 110, 255));
            }

            // -------------------------------------------------------------
            // PAINEL LATERAL DIREITO COM DETALHES DA MISSÃO
            // -------------------------------------------------------------
            Rectangle rDetalhes = new Rectangle(gradeW + 45.0f, painelTopY, detalhesW, painelH);
            Raylib.DrawRectangleRec(rDetalhes, new Color(12, 18, 30, 240));
            Raylib.DrawRectangleLinesEx(rDetalhes, 1.2f, new Color(0, 180, 255, 150));

            var lvlAtivo = Campanha.LevelAtual;

            float detX = rDetalhes.X + 18.0f;
            float detY = rDetalhes.Y + 18.0f;

            // Tag do Capítulo
            FontePB2.DesenharTexto(lvlAtivo.Capitulo.ToUpper(), detX, detY, 11, new Color(0, 220, 255, 220));
            detY += 18.0f;

            // Nome da Missão
            string titAtivo = $"LEVEL {lvlAtivo.Numero}: {lvlAtivo.Titulo}";
            FontePB2.DesenharTexto(titAtivo, detX, detY, 17, Color.White);
            detY += 28.0f;

            Raylib.DrawLine((int)detX, (int)detY, (int)(detX + detalhesW - 36.0f), (int)detY, new Color(30, 60, 90, 200));
            detY += 14.0f;

            // Briefing Tático
            FontePB2.DesenharTexto("BRIEFING DA MISSÃO:", detX, detY, 12, new Color(180, 205, 230, 255));
            detY += 20.0f;

            // Caixa de texto com quebra visual do briefing
            DesenharTextoMultiLinha(lvlAtivo.Briefing, detX, detY, detalhesW - 36.0f, 13, new Color(210, 225, 240, 230));
            detY += 60.0f;

            // Especificações Táticas
            FontePB2.DesenharTexto($"• Facção Inimiga: {lvlAtivo.FaccaoInimiga}", detX, detY, 12, new Color(160, 190, 220, 255));
            detY += 20.0f;
            FontePB2.DesenharTexto($"• Inimigos Estimados: {lvlAtivo.QtdInimigos} Alvos", detX, detY, 12, new Color(160, 190, 220, 255));
            detY += 20.0f;
            FontePB2.DesenharTexto($"• Arma Predominante: {lvlAtivo.ArmaDestaque}", detX, detY, 12, new Color(160, 190, 220, 255));
            detY += 20.0f;

            if (lvlAtivo.MelhorTempo > 0.0f)
            {
                FontePB2.DesenharTexto($"★ Melhor Tempo: {lvlAtivo.MelhorTempo:F1}s", detX, detY, 12, new Color(255, 215, 0, 255));
                detY += 20.0f;
            }

            // BOTÃO PRINCIPAL DE INICIAR MISSÃO
            float btnIniciarW = detalhesW - 36.0f;
            float btnIniciarH = 46.0f;
            Rectangle rBtnIniciar = new Rectangle(detX, rDetalhes.Y + rDetalhes.Height - btnIniciarH - 18.0f, btnIniciarW, btnIniciarH);

            if (lvlAtivo.Desbloqueado)
            {
                if (DesenharBotaoGrande(rBtnIniciar, "► INICIAR MISSÃO", mousePos, ehDestaque: true))
                {
                    acao = AcaoMenu.IniciarFase;
                }
            }
            else
            {
                Raylib.DrawRectangleRec(rBtnIniciar, new Color(25, 30, 42, 200));
                Raylib.DrawRectangleLinesEx(rBtnIniciar, 1.0f, new Color(60, 70, 85, 200));
                Vector2 tBloq = FontePB2.MedirTexto("NÍVEL BLOQUEADO", 14);
                FontePB2.DesenharTexto("NÍVEL BLOQUEADO", rBtnIniciar.X + (btnIniciarW - tBloq.X) * 0.5f, rBtnIniciar.Y + (btnIniciarH - tBloq.Y) * 0.5f, 14, new Color(120, 135, 150, 255));
            }

            // Botão secreto de dev/teste para desbloquear todos os 45 levels
            Rectangle rDesbloquear = new Rectangle(gradeW + 45.0f, cabecalhoY + 4, 180, 24);
            bool hoverUnlock = Raylib.CheckCollisionPointRec(mousePos, rDesbloquear);
            if (hoverUnlock && Raylib.IsMouseButtonPressed(MouseButton.Left))
            {
                Campanha.DesbloquearTodosLevels();
            }
            FontePB2.DesenharTexto("[Desbloquear Todos 45]", rDesbloquear.X, rDesbloquear.Y + 4, 10, hoverUnlock ? new Color(0, 240, 255, 255) : new Color(70, 95, 120, 200));

            return acao;
        }

        // =========================================================================
        // COMPONENTES VISUAIS E UTILITÁRIOS DE RENDERIZAÇÃO
        // =========================================================================
        private void DesenharFundoCibernetico(float telaLargura, float telaAltura)
        {
            // Fundo escuro azulado estilo Plazma Burst 2
            Raylib.ClearBackground(new Color(10, 15, 24, 255));

            // Grade geométrica tática
            Color corGrade = new Color(20, 32, 48, 80);
            for (float x = 0; x < telaLargura; x += 40)
            {
                Raylib.DrawLine((int)x, 0, (int)x, (int)telaAltura, corGrade);
            }
            for (float y = 0; y < telaAltura; y += 40)
            {
                Raylib.DrawLine(0, (int)y, (int)telaLargura, (int)y, corGrade);
            }

            // Feixe de plasma dinâmico horizontal
            float scannerY = ((tempoAnimacao * 90.0f) % (telaAltura + 200.0f)) - 100.0f;
            Raylib.DrawRectangleGradientV(0, (int)scannerY, (int)telaLargura, 60, new Color(0, 180, 255, 0), new Color(0, 220, 255, 18));

            // Partículas flutuantes de plasma
            for (int i = 0; i < particulas.Count; i++)
            {
                particulas[i].Desenhar();
            }
        }

        private bool DesenharBotaoMenu(Rectangle rec, string texto, Vector2 mousePos, bool ehDestaque = false, bool ehAlerta = false)
        {
            bool hover = Raylib.CheckCollisionPointRec(mousePos, rec);

            Color bg;
            Color borda;
            Color corTexto;

            if (ehDestaque)
            {
                bg = hover ? new Color(0, 140, 220, 255) : new Color(0, 95, 165, 230);
                borda = hover ? new Color(180, 240, 255, 255) : new Color(0, 220, 255, 240);
                corTexto = Color.White;
            }
            else if (ehAlerta)
            {
                bg = hover ? new Color(180, 35, 35, 230) : new Color(20, 26, 38, 220);
                borda = hover ? new Color(255, 90, 90, 255) : new Color(60, 75, 95, 200);
                corTexto = hover ? Color.White : new Color(220, 160, 160, 255);
            }
            else
            {
                bg = hover ? new Color(22, 38, 60, 245) : new Color(15, 24, 38, 220);
                borda = hover ? new Color(0, 220, 255, 255) : new Color(45, 70, 100, 200);
                corTexto = hover ? Color.White : new Color(210, 230, 250, 255);
            }

            Raylib.DrawRectangleRec(rec, bg);
            Raylib.DrawRectangleLinesEx(rec, hover ? 2.0f : 1.2f, borda);

            if (hover)
            {
                // Indicador luminoso na lateral esquerda do botão
                Raylib.DrawRectangle((int)rec.X, (int)rec.Y, 5, (int)rec.Height, borda);
            }

            Vector2 tamTexto = FontePB2.MedirTexto(texto, 15);
            FontePB2.DesenharTexto(texto, rec.X + (rec.Width - tamTexto.X) * 0.5f, rec.Y + (rec.Height - tamTexto.Y) * 0.5f, 15, corTexto);

            return hover && Raylib.IsMouseButtonPressed(MouseButton.Left);
        }

        private bool DesenharBotaoPequeno(Rectangle rec, string texto, Vector2 mousePos)
        {
            bool hover = Raylib.CheckCollisionPointRec(mousePos, rec);
            Color bg = hover ? new Color(0, 140, 220, 240) : new Color(20, 32, 48, 220);
            Color borda = hover ? Color.White : new Color(0, 200, 255, 200);

            Raylib.DrawRectangleRec(rec, bg);
            Raylib.DrawRectangleLinesEx(rec, 1.2f, borda);

            Vector2 tam = FontePB2.MedirTexto(texto, 12);
            FontePB2.DesenharTexto(texto, rec.X + (rec.Width - tam.X) * 0.5f, rec.Y + (rec.Height - tam.Y) * 0.5f, 12, Color.White);

            return hover && Raylib.IsMouseButtonPressed(MouseButton.Left);
        }

        private bool DesenharBotaoGrande(Rectangle rec, string texto, Vector2 mousePos, bool ehDestaque = false)
        {
            bool hover = Raylib.CheckCollisionPointRec(mousePos, rec);

            float pulso = (MathF.Sin(tempoAnimacao * 6.0f) + 1.0f) * 0.5f;
            Color bg = hover ? new Color(16, 185, 129, 255) : new Color(5, 150, 105, (int)(210 + (pulso * 45)));
            Color borda = hover ? Color.White : new Color(52, 211, 153, 255);

            Raylib.DrawRectangleRec(rec, bg);
            Raylib.DrawRectangleLinesEx(rec, hover ? 2.5f : 1.5f, borda);

            Vector2 tam = FontePB2.MedirTexto(texto, 16);
            FontePB2.DesenharTexto(texto, rec.X + (rec.Width - tam.X) * 0.5f, rec.Y + (rec.Height - tam.Y) * 0.5f, 16, Color.White);

            return hover && Raylib.IsMouseButtonPressed(MouseButton.Left);
        }

        private void DesenharTextoMultiLinha(string texto, float x, float y, float maxLargura, float tamanho, Color cor)
        {
            string[] palavras = texto.Split(' ');
            string linhaAtual = "";
            float yAtual = y;

            for (int i = 0; i < palavras.Length; i++)
            {
                string teste = string.IsNullOrEmpty(linhaAtual) ? palavras[i] : $"{linhaAtual} {palavras[i]}";
                if (FontePB2.MedirTexto(teste, tamanho).X > maxLargura)
                {
                    FontePB2.DesenharTexto(linhaAtual, x, yAtual, tamanho, cor);
                    linhaAtual = palavras[i];
                    yAtual += tamanho + 4.0f;
                }
                else
                {
                    linhaAtual = teste;
                }
            }

            if (!string.IsNullOrEmpty(linhaAtual))
            {
                FontePB2.DesenharTexto(linhaAtual, x, yAtual, tamanho, cor);
            }
        }
    }
}
