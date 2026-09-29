using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using Raylib_cs;

namespace JogoDeTiro
{
    /// <summary>
    /// Sistema de Respawn e Botão 'USE' tático clássico do Plazma Burst 2.
    /// Fica desligado/bloqueado (vermelho) enquanto houver bots vivos.
    /// Liga de volta (verde pulsante) apenas quando TODOS os bots estão mortos,
    /// permitindo respawn imediato via tecla [ENTER] ou clique no botão,
    /// com contagem regressiva automática de 2 minutos.
    /// </summary>
    public class SistemaRespawnPB2
    {
        private Texture2D texVerde;
        private Texture2D texVerdeOff;
        private Texture2D texVermelho;
        private Texture2D texAzul;
        private Sound somAtivacao;
        private bool sonsDisponiveis = false;

        public const float TEMPO_MAXIMO_RESPAWN = 120.0f; // 2 minutos (120 segundos)
        public float TempoRestanteAutoRespawn { get; private set; } = TEMPO_MAXIMO_RESPAWN;
        public bool TodosBotsMortos { get; private set; } = false;
        public int BotsVivos { get; private set; } = 0;
        public int TotalBots { get; private set; } = 0;

        private float timerPiscar = 0.0f;
        private float efeitoClique = 0.0f;

        // Painel HUD no canto superior direito
        public Rectangle RetanguloHUD = new Rectangle(700, 12, 248, 84);

        // Posição no mundo (fixo no mapa próximo ao spawn do jogador)
        public Vector2 PosicaoMundo { get; set; } = Vector2.Zero;

        public SistemaRespawnPB2(string diretorioAssets)
        {
            string pastaSwitch = Path.Combine(diretorioAssets, "sprites", "switch");

            string arqVerde = Path.Combine(pastaSwitch, "switch_green.png");
            string arqVerdeOff = Path.Combine(pastaSwitch, "switch_green_off.png");
            string arqVermelho = Path.Combine(pastaSwitch, "switch_red.png");
            string arqAzul = Path.Combine(pastaSwitch, "switch_blue.png");

            texVerde = File.Exists(arqVerde) ? Raylib.LoadTexture(arqVerde) : default;
            texVerdeOff = File.Exists(arqVerdeOff) ? Raylib.LoadTexture(arqVerdeOff) : default;
            texVermelho = File.Exists(arqVermelho) ? Raylib.LoadTexture(arqVermelho) : default;
            texAzul = File.Exists(arqAzul) ? Raylib.LoadTexture(arqAzul) : default;

            string somPath = Path.Combine(diretorioAssets, "sounds", "61_wea_pickup.mp3");
            if (!File.Exists(somPath))
            {
                somPath = Path.Combine(diretorioAssets, "sounds", "487_beam1_recharge.mp3");
            }

            if (File.Exists(somPath))
            {
                somAtivacao = Raylib.LoadSound(somPath);
                sonsDisponiveis = true;
            }
        }

        private float cooldownRespawn = 0.0f;

        public void Atualizar(
            float dt,
            List<BotIAPB2> bots,
            Action aoRespawnar,
            Vector2 ratoTela,
            bool mouseSobreUIBloqueada)
        {
            if (cooldownRespawn > 0.0f)
            {
                cooldownRespawn -= dt;
                TodosBotsMortos = false;
                TempoRestanteAutoRespawn = TEMPO_MAXIMO_RESPAWN;
                return;
            }

            TotalBots = bots.Count;
            BotsVivos = 0;
            for (int i = 0; i < bots.Count; i++)
            {
                if (bots[i].Vivo) BotsVivos++;
            }

            bool todosMortos = (TotalBots > 0 && BotsVivos == 0);

            if (efeitoClique > 0.0f) efeitoClique -= dt;

            if (!todosMortos)
            {
                // Enquanto houver inimigos vivos, o botão fica bloqueado/desligado
                TodosBotsMortos = false;
                TempoRestanteAutoRespawn = TEMPO_MAXIMO_RESPAWN;
                timerPiscar = 0.0f;
            }
            else
            {
                // Todos os bots estão mortos: O BOTÃO LIGA DE VOLTA!
                TodosBotsMortos = true;
                timerPiscar += dt;
                TempoRestanteAutoRespawn -= dt;

                bool mouseSobreBotao = Raylib.CheckCollisionPointRec(ratoTela, RetanguloHUD);
                bool clique = mouseSobreBotao && Raylib.IsMouseButtonPressed(MouseButton.Left);
                bool apertouEnter = Raylib.IsKeyPressed(KeyboardKey.Enter) || Raylib.IsKeyPressed(KeyboardKey.KpEnter);

                // 1. Acionamento manual por tecla ENTER ou clique no botão USE
                if (apertouEnter || clique)
                {
                    DispararRespawn(aoRespawnar);
                    return;
                }

                // 2. Acionamento automático após passar o tempo de 2 minutos
                if (TempoRestanteAutoRespawn <= 0.0f)
                {
                    DispararRespawn(aoRespawnar);
                    return;
                }
            }
        }

        private void DispararRespawn(Action aoRespawnar)
        {
            if (sonsDisponiveis && somAtivacao.Stream.Buffer != 0)
            {
                Raylib.PlaySound(somAtivacao);
            }

            efeitoClique = 0.35f;
            cooldownRespawn = 2.0f;
            TodosBotsMortos = false;
            TempoRestanteAutoRespawn = TEMPO_MAXIMO_RESPAWN;
            aoRespawnar?.Invoke();
        }

        /// <summary>
        /// Desenha o terminal / switch USE físico dentro do cenário do mapa.
        /// </summary>
        public void DesenharMundo()
        {
            if (PosicaoMundo == Vector2.Zero) return;

            Texture2D texAtual = texVermelho;
            if (TodosBotsMortos)
            {
                bool piscar = ((int)(timerPiscar * 3.5f) % 2) == 0;
                texAtual = piscar ? texVerde : texVerdeOff;
            }

            // Suporte metálico na parede
            Raylib.DrawRectangle((int)PosicaoMundo.X - 22, (int)PosicaoMundo.Y - 17, 44, 34, new Color(30, 35, 45, 230));
            Raylib.DrawRectangleLines((int)PosicaoMundo.X - 22, (int)PosicaoMundo.Y - 17, 44, 34, new Color(70, 85, 100, 255));

            if (texAtual.Id != 0)
            {
                Raylib.DrawTexture(texAtual, (int)PosicaoMundo.X - (texAtual.Width / 2), (int)PosicaoMundo.Y - (texAtual.Height / 2), Color.White);
            }

            if (TodosBotsMortos)
            {
                // Halo de luz verde fluorescente
                Raylib.DrawCircleV(PosicaoMundo, 32.0f, new Color(0, 255, 100, 45));
                Raylib.DrawCircleLines((int)PosicaoMundo.X, (int)PosicaoMundo.Y, 32.0f, new Color(0, 255, 120, 160));
                FontePB2.DesenharTexto("[ENTER] USE", PosicaoMundo.X - 28, PosicaoMundo.Y - 32, 10, new Color(0, 255, 120, 240));
            }
            else
            {
                Raylib.DrawCircleV(PosicaoMundo, 18.0f, new Color(255, 40, 40, 30));
            }
        }

        /// <summary>
        /// Desenha o painel HUD tático com o botão original 'USE' do Plazma Burst 2.
        /// </summary>
        public void DesenharHUD(Vector2 ratoTela)
        {
            bool mouseSobre = Raylib.CheckCollisionPointRec(ratoTela, RetanguloHUD);

            // Moldura Sci-Fi do painel
            Color corFundo = TodosBotsMortos
                ? (mouseSobre ? new Color(18, 38, 30, 245) : new Color(12, 28, 22, 235))
                : new Color(28, 16, 18, 230);

            Color corBorda = TodosBotsMortos
                ? (mouseSobre ? new Color(0, 255, 160, 255) : new Color(0, 200, 100, 200))
                : new Color(180, 50, 60, 180);

            Raylib.DrawRectangleRec(RetanguloHUD, corFundo);
            Raylib.DrawRectangleLinesEx(RetanguloHUD, 1.5f, corBorda);

            // Borda interna chanfrada estilo terminal
            Raylib.DrawRectangleLines(
                (int)RetanguloHUD.X + 3,
                (int)RetanguloHUD.Y + 3,
                (int)RetanguloHUD.Width - 6,
                (int)RetanguloHUD.Height - 6,
                new Color(255, 255, 255, 25)
            );

            // Escolha da textura do switch USE do PB2
            Texture2D texDesenho = texVermelho;
            if (TodosBotsMortos)
            {
                if (efeitoClique > 0.0f || (mouseSobre && Raylib.IsMouseButtonDown(MouseButton.Left)))
                {
                    texDesenho = (texAzul.Id != 0) ? texAzul : texVerde;
                }
                else
                {
                    bool piscar = ((int)(timerPiscar * 3.5f) % 2) == 0;
                    texDesenho = piscar ? texVerde : ((texVerdeOff.Id != 0) ? texVerdeOff : texVerde);
                }
            }

            // Posição do botão USE na esquerda do painel HUD (em escala 1.6x para destaque nítido)
            float escalaBotao = 1.6f;
            float larguraBotao = 40.0f * escalaBotao;
            float alturaBotao = 30.0f * escalaBotao;
            float posXBotao = RetanguloHUD.X + 12.0f;
            float posYBotao = RetanguloHUD.Y + (RetanguloHUD.Height - alturaBotao) / 2.0f;

            if (texDesenho.Id != 0)
            {
                Rectangle fonte = new Rectangle(0, 0, texDesenho.Width, texDesenho.Height);
                Rectangle destino = new Rectangle(posXBotao, posYBotao, larguraBotao, alturaBotao);
                Raylib.DrawTexturePro(texDesenho, fonte, destino, Vector2.Zero, 0.0f, Color.White);
            }

            // Textos informativos e contagem regressiva
            float textoX = posXBotao + larguraBotao + 10.0f;

            if (TodosBotsMortos)
            {
                // Botão LIGADO DE VOLTA
                FontePB2.DesenharTexto("BOTÃO USE: LIGADO", textoX, RetanguloHUD.Y + 14, 11, new Color(0, 255, 140, 255));

                // Ação imediata via Enter ou Clique
                Color corDestaque = ((int)(timerPiscar * 4.0f) % 2 == 0)
                    ? new Color(255, 255, 255, 255)
                    : new Color(0, 240, 255, 240);
                FontePB2.DesenharTexto("APERTE [ENTER] OU CLIQUE", textoX, RetanguloHUD.Y + 32, 10, corDestaque);

                // Contador de 2 minutos (120 segundos)
                int minutos = (int)(TempoRestanteAutoRespawn / 60.0f);
                int segundos = (int)(TempoRestanteAutoRespawn % 60.0f);
                string textoTimer = $"Auto em: {minutos:D2}:{segundos:D2}";
                FontePB2.DesenharTexto(textoTimer, textoX, RetanguloHUD.Y + 52, 11, new Color(255, 215, 0, 240));

                // Barra de tempo de 2 minutos
                float progresso = Math.Clamp(TempoRestanteAutoRespawn / TEMPO_MAXIMO_RESPAWN, 0.0f, 1.0f);
                float barraW = RetanguloHUD.Width - (textoX - RetanguloHUD.X) - 12.0f;
                Raylib.DrawRectangle((int)textoX, (int)RetanguloHUD.Y + 68, (int)barraW, 4, new Color(30, 45, 40, 200));
                Raylib.DrawRectangle((int)textoX, (int)RetanguloHUD.Y + 68, (int)(barraW * progresso), 4, new Color(0, 255, 140, 255));
            }
            else
            {
                // Botão DESLIGADO / BLOQUEADO
                FontePB2.DesenharTexto("BOTÃO USE: DESLIGADO", textoX, RetanguloHUD.Y + 16, 11, new Color(255, 90, 100, 240));
                FontePB2.DesenharTexto($"INIMIGOS: {BotsVivos} VIVOS", textoX, RetanguloHUD.Y + 36, 11, new Color(220, 225, 230, 220));
                FontePB2.DesenharTexto("Liga ao eliminar todos", textoX, RetanguloHUD.Y + 54, 9, new Color(160, 170, 180, 180));
            }
        }
    }
}
