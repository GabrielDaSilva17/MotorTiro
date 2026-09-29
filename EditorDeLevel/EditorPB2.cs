using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Raylib_cs;

namespace JogoDeTiro.EditorDeLevel
{
    public enum FerramentaEditor
    {
        Parede = 1,
        SpawnPlayer = 2,
        SpawnInimigo = 3,
        Borracha = 4
    }

    public class EditorPB2
    {
        public MapaData Mapa { get; private set; }
        public string CaminhoArquivo { get; set; }

        private Camera2D camera;
        public FerramentaEditor FerramentaAtual { get; set; } = FerramentaEditor.Parede;
        public int TamanhoGrid { get; set; } = 32;
        public bool SnapAtivo { get; set; } = true;

        private bool criandoRetangulo = false;
        private Vector2 inicioCliqueMundo;
        private float timerMensagemSalvo = 0.0f;

        private readonly Color corFundoPB2 = new Color(48, 86, 125, 255);
        private readonly Color corGradeLinhas = new Color(68, 108, 150, 140);
        private readonly Color corGradeDestaque = new Color(85, 130, 175, 190);

        public EditorPB2(string caminhoArquivoPadrao)
        {
            CaminhoArquivo = caminhoArquivoPadrao;
            Mapa = GerenciadorMapa.CarregarMapa(CaminhoArquivo);

            camera = new Camera2D
            {
                Target = Mapa.PontoSpawnPlayer,
                Offset = new Vector2(Raylib.GetScreenWidth() * 0.5f, Raylib.GetScreenHeight() * 0.5f),
                Rotation = 0.0f,
                Zoom = 1.0f
            };
        }

        public void Salvar()
        {
            GerenciadorMapa.SalvarMapa(Mapa, CaminhoArquivo);
            timerMensagemSalvo = 2.5f; // Exibe aviso na tela por 2.5 segundos
        }

        public void Atualizar(float dt, out bool solicitarPlaytest)
        {
            solicitarPlaytest = false;
            if (timerMensagemSalvo > 0.0f) timerMensagemSalvo -= dt;

            // Seleção de ferramentas
            if (Raylib.IsKeyPressed(KeyboardKey.One)) FerramentaAtual = FerramentaEditor.Parede;
            if (Raylib.IsKeyPressed(KeyboardKey.Two)) FerramentaAtual = FerramentaEditor.SpawnPlayer;
            if (Raylib.IsKeyPressed(KeyboardKey.Three)) FerramentaAtual = FerramentaEditor.SpawnInimigo;
            if (Raylib.IsKeyPressed(KeyboardKey.Four)) FerramentaAtual = FerramentaEditor.Borracha;

            // Mantém o centro da câmera sincronizado com o tamanho da janela / fullscreen
            camera.Offset = new Vector2(Raylib.GetScreenWidth() * 0.5f, Raylib.GetScreenHeight() * 0.5f);

            // F11 ou Alt+Enter para tela cheia
            if (Raylib.IsKeyPressed(KeyboardKey.F11) || ((Raylib.IsKeyDown(KeyboardKey.LeftAlt) || Raylib.IsKeyDown(KeyboardKey.RightAlt)) && Raylib.IsKeyPressed(KeyboardKey.Enter)))
            {
                Raylib.ToggleFullscreen();
            }

            // Atalho de Salvar
            if ((Raylib.IsKeyDown(KeyboardKey.LeftControl) || Raylib.IsKeyDown(KeyboardKey.RightControl)) && Raylib.IsKeyPressed(KeyboardKey.S))
            {
                Salvar();
            }

            // PLAYTEST: Salva o mapa automaticamente ao testar!
            if (Raylib.IsKeyPressed(KeyboardKey.P) || Raylib.IsKeyPressed(KeyboardKey.F5))
            {
                Salvar();
                solicitarPlaytest = true;
            }

            if (Raylib.IsKeyPressed(KeyboardKey.G))
            {
                TamanhoGrid = (TamanhoGrid == 32) ? 16 : 32;
            }

            // Câmera: Zoom
            float wheel = Raylib.GetMouseWheelMove();
            if (wheel != 0.0f)
            {
                Vector2 ratoTela = Raylib.GetMousePosition();
                Vector2 ratoMundoAntes = Raylib.GetScreenToWorld2D(ratoTela, camera);
                camera.Zoom = Math.Clamp(camera.Zoom + (wheel * 0.15f), 0.3f, 2.5f);
                Vector2 ratoMundoDepois = Raylib.GetScreenToWorld2D(ratoTela, camera);
                camera.Target += (ratoMundoAntes - ratoMundoDepois);
            }

            // Câmera: Arrastar
            if (Raylib.IsMouseButtonDown(MouseButton.Right) || Raylib.IsMouseButtonDown(MouseButton.Middle))
            {
                Vector2 deltaMouse = Raylib.GetMouseDelta();
                camera.Target -= deltaMouse * (1.0f / camera.Zoom);
            }

            Vector2 mouseMundo = Raylib.GetScreenToWorld2D(Raylib.GetMousePosition(), camera);
            Vector2 mouseSnap = AplicarSnap(mouseMundo);

            // Clique na área de trabalho do mapa
            if (Raylib.GetMousePosition().X > 160 && Raylib.GetMousePosition().Y > 45)
            {
                switch (FerramentaAtual)
                {
                    case FerramentaEditor.Parede:
                        if (Raylib.IsMouseButtonPressed(MouseButton.Left))
                        {
                            criandoRetangulo = true;
                            inicioCliqueMundo = mouseSnap;
                        }
                        else if (criandoRetangulo && Raylib.IsMouseButtonReleased(MouseButton.Left))
                        {
                            criandoRetangulo = false;
                            float xMin = MathF.Min(inicioCliqueMundo.X, mouseSnap.X);
                            float yMin = MathF.Min(inicioCliqueMundo.Y, mouseSnap.Y);
                            float larg = MathF.Abs(mouseSnap.X - inicioCliqueMundo.X);
                            float alt = MathF.Abs(mouseSnap.Y - inicioCliqueMundo.Y);

                            bool jaExiste = Mapa.Paredes.Any(p =>
                                MathF.Abs(p.X - xMin) < 1.0f &&
                                MathF.Abs(p.Y - yMin) < 1.0f &&
                                MathF.Abs(p.Largura - larg) < 1.0f &&
                                MathF.Abs(p.Altura - alt) < 1.0f);

                            if (!jaExiste && larg >= 16 && alt >= 16)
                            {
                                Mapa.Paredes.Add(new ParedeData { X = xMin, Y = yMin, Largura = larg, Altura = alt });
                            }
                        }
                        break;

                    case FerramentaEditor.SpawnPlayer:
                        if (Raylib.IsMouseButtonPressed(MouseButton.Left))
                        {
                            Mapa.PontoSpawnPlayer = mouseSnap;
                        }
                        break;

                    case FerramentaEditor.SpawnInimigo:
                        if (Raylib.IsMouseButtonPressed(MouseButton.Left))
                        {
                            // Evita criar no mesmo ponto exato
                            if (!Mapa.SpawnsInimigos.Any(sp => Vector2.Distance(sp.ToVector2(), mouseSnap) < 16.0f))
                            {
                                Mapa.SpawnsInimigos.Add(new InimigoSpawnData(mouseSnap.X, mouseSnap.Y));
                            }
                        }
                        break;

                    case FerramentaEditor.Borracha:
                        if (Raylib.IsMouseButtonDown(MouseButton.Left))
                        {
                            Mapa.Paredes.RemoveAll(p => Raylib.CheckCollisionPointRec(mouseMundo, new Rectangle(p.X, p.Y, p.Largura, p.Altura)));
                            Mapa.SpawnsInimigos.RemoveAll(sp => Vector2.Distance(sp.ToVector2(), mouseMundo) < 24.0f);
                        }
                        break;
                }
            }
        }

        public void Desenhar()
        {
            Raylib.ClearBackground(corFundoPB2);

            Raylib.BeginMode2D(camera);
            DesenharGradePB2();

            // Paredes
            foreach (var p in Mapa.Paredes)
            {
                DesenharParedePB2(new Rectangle(p.X, p.Y, p.Largura, p.Altura));
            }

            // Pré-visualização da parede
            if (criandoRetangulo)
            {
                Vector2 mouseMundo = Raylib.GetScreenToWorld2D(Raylib.GetMousePosition(), camera);
                Vector2 mouseSnap = AplicarSnap(mouseMundo);
                float xMin = MathF.Min(inicioCliqueMundo.X, mouseSnap.X);
                float yMin = MathF.Min(inicioCliqueMundo.Y, mouseSnap.Y);
                float larg = MathF.Abs(mouseSnap.X - inicioCliqueMundo.X);
                float alt = MathF.Abs(mouseSnap.Y - inicioCliqueMundo.Y);

                Rectangle recPreview = new Rectangle(xMin, yMin, larg, alt);
                Raylib.DrawRectangleRec(recPreview, new Color(50, 160, 240, 90));
                Raylib.DrawRectangleLinesEx(recPreview, 2.0f, Color.White);
            }

            // Spawns dos Inimigos
            for (int i = 0; i < Mapa.SpawnsInimigos.Count; i++)
            {
                Vector2 pos = Mapa.SpawnsInimigos[i].ToVector2();
                Raylib.DrawCircleV(pos, 16.0f, new Color(225, 45, 45, 190));
                Raylib.DrawCircleLines((int)pos.X, (int)pos.Y, 16.0f, Color.Red);
                FontePB2.DesenharTexto($"#actor*{i + 1}", pos.X - 22, pos.Y - 30, 12, Color.White);
            }

            // Spawn do Player
            Vector2 spawn = Mapa.PontoSpawnPlayer;
            Raylib.DrawCircleV(spawn, 18.0f, new Color(34, 197, 94, 200));
            Raylib.DrawCircleLines((int)spawn.X, (int)spawn.Y, 18.0f, Color.Green);
            FontePB2.DesenharTexto("#player*1", spawn.X - 24, spawn.Y - 32, 13, new Color(180, 255, 190, 255));

            Raylib.EndMode2D();

            DesenharHUD();
        }

        public static void DesenharParedePB2(Rectangle rec)
        {
            Raylib.DrawRectangleRec(rec, new Color(35, 45, 58, 255));
            Raylib.DrawRectangleLinesEx(rec, 1.5f, new Color(60, 80, 105, 255));

            // Topo luminoso
            Raylib.DrawLineEx(
                new Vector2(rec.X, rec.Y + 1.0f),
                new Vector2(rec.X + rec.Width, rec.Y + 1.0f),
                2.0f,
                new Color(90, 165, 225, 240)
            );

            if (rec.Width >= 32 && rec.Height >= 32)
            {
                Raylib.DrawCircle((int)rec.X + 6, (int)rec.Y + 7, 1.8f, new Color(130, 155, 180, 190));
                Raylib.DrawCircle((int)(rec.X + rec.Width - 6), (int)rec.Y + 7, 1.8f, new Color(130, 155, 180, 190));
            }
        }

        private void DesenharGradePB2()
        {
            Vector2 topoEsquerdo = Raylib.GetScreenToWorld2D(Vector2.Zero, camera);
            Vector2 inferiorDireito = Raylib.GetScreenToWorld2D(new Vector2(Raylib.GetScreenWidth(), Raylib.GetScreenHeight()), camera);

            int startX = ((int)topoEsquerdo.X / TamanhoGrid) * TamanhoGrid - TamanhoGrid;
            int endX = ((int)inferiorDireito.X / TamanhoGrid) * TamanhoGrid + TamanhoGrid;
            int startY = ((int)topoEsquerdo.Y / TamanhoGrid) * TamanhoGrid - TamanhoGrid;
            int endY = ((int)inferiorDireito.Y / TamanhoGrid) * TamanhoGrid + TamanhoGrid;

            for (int x = startX; x <= endX; x += TamanhoGrid)
            {
                bool destaque = (x % (TamanhoGrid * 4) == 0);
                Raylib.DrawLine(x, startY, x, endY, destaque ? corGradeDestaque : corGradeLinhas);
            }

            for (int y = startY; y <= endY; y += TamanhoGrid)
            {
                bool destaque = (y % (TamanhoGrid * 4) == 0);
                Raylib.DrawLine(startX, y, endX, y, destaque ? corGradeDestaque : corGradeLinhas);
            }
        }

        private void DesenharHUD()
        {
            // Barra superior
            Raylib.DrawRectangle(0, 0, Raylib.GetScreenWidth(), 38, new Color(22, 30, 42, 245));
            Raylib.DrawLine(0, 38, Raylib.GetScreenWidth(), 38, new Color(40, 58, 78, 255));

            FontePB2.DesenharTexto("PB2 LEVEL EDITOR", 14, 10, 16, Color.Gold);

            // Botão Clicável: SALVAR MAPA
            Rectangle recSalvar = new Rectangle(180, 6, 110, 26);
            bool hoverSalvar = Raylib.CheckCollisionPointRec(Raylib.GetMousePosition(), recSalvar);
            Raylib.DrawRectangleRec(recSalvar, hoverSalvar ? new Color(16, 185, 129, 230) : new Color(30, 45, 60, 255));
            Raylib.DrawRectangleLinesEx(recSalvar, 1.2f, new Color(16, 185, 129, 255));
            FontePB2.DesenharTexto("[S] Salvar", recSalvar.X + 16, recSalvar.Y + 5, 12, Color.White);

            if (hoverSalvar && Raylib.IsMouseButtonPressed(MouseButton.Left))
            {
                Salvar();
            }

            FontePB2.DesenharTexto($"[P / F5] PLAYTEST  |  [G] Grade: {TamanhoGrid}px  |  Zoom: {camera.Zoom:0.0}x  |  [F11] Tela Cheia", 305, 11, 13, new Color(210, 225, 240, 255));

            // Notificação visual verde ao salvar
            if (timerMensagemSalvo > 0.0f)
            {
                Raylib.DrawRectangle(Raylib.GetScreenWidth() - 250, 6, 235, 26, new Color(16, 185, 129, 240));
                FontePB2.DesenharTexto("✓ MAPA SALVO NO DISCO!", Raylib.GetScreenWidth() - 235, 11, 12, Color.White);
            }

            // Barra lateral
            Raylib.DrawRectangle(0, 38, 150, Raylib.GetScreenHeight() - 38, new Color(25, 36, 50, 235));
            Raylib.DrawLine(150, 38, 150, Raylib.GetScreenHeight(), new Color(40, 58, 78, 255));

            DesenharBotaoFerramenta(FerramentaEditor.Parede, "[1] PAREDE", 52);
            DesenharBotaoFerramenta(FerramentaEditor.SpawnPlayer, "[2] PLAYER", 94);
            DesenharBotaoFerramenta(FerramentaEditor.SpawnInimigo, "[3] INIMIGO", 136);
            DesenharBotaoFerramenta(FerramentaEditor.Borracha, "[4] APAGAR", 178);

            FontePB2.DesenharTexto("Botão Dir.: Mover\nScroll: Zoom", 12, Raylib.GetScreenHeight() - 38, 11, Color.LightGray);
        }

        private void DesenharBotaoFerramenta(FerramentaEditor ferramenta, string texto, int posY)
        {
            bool selecionada = (FerramentaAtual == ferramenta);
            Rectangle rec = new Rectangle(10, posY, 130, 32);

            Color corFundo = selecionada ? new Color(45, 115, 185, 255) : new Color(34, 46, 62, 255);
            Color corBorda = selecionada ? Color.SkyBlue : new Color(55, 75, 100, 255);

            Raylib.DrawRectangleRec(rec, corFundo);
            Raylib.DrawRectangleLinesEx(rec, 1.2f, corBorda);
            FontePB2.DesenharTexto(texto, (int)rec.X + 12, (int)rec.Y + 8, 13, Color.White);

            if (Raylib.CheckCollisionPointRec(Raylib.GetMousePosition(), rec) && Raylib.IsMouseButtonPressed(MouseButton.Left))
            {
                FerramentaAtual = ferramenta;
            }
        }

        private Vector2 AplicarSnap(Vector2 pos)
        {
            if (!SnapAtivo) return pos;
            float x = MathF.Round(pos.X / TamanhoGrid) * TamanhoGrid;
            float y = MathF.Round(pos.Y / TamanhoGrid) * TamanhoGrid;
            return new Vector2(x, y);
        }
    }
}