using System;
using System.Collections.Generic;
using System.Numerics;
using Raylib_cs;

namespace JogoDeTiro
{
    public class InimigoTestePB2
    {
        public Vector2 PosicaoSpawn;
        public Vector2 Posicao;
        public float Largura = 32.0f;
        public float Altura = 64.0f;

        public float VidaMax = 100.0f;
        public float VidaAtual = 100.0f;
        public bool Vivo => VidaAtual > 0.0f;

        private float timerFlashDano = 0.0f;
        private float timerRespawn = 0.0f;

        public Rectangle CaixaColisao => new Rectangle(
            Posicao.X - (Largura * 0.5f),
            Posicao.Y - Altura,
            Largura,
            Altura
        );

        public InimigoTestePB2(Vector2 posicaoInicial)
        {
            PosicaoSpawn = posicaoInicial;
            Posicao = posicaoInicial;
            VidaAtual = VidaMax;
        }

        public void Atualizar(float dt, List<ProjetilPlasma> tiros, List<GranadaPB2> granadas)
        {
            if (timerFlashDano > 0.0f) timerFlashDano -= dt;

            // Se estiver destruído, conta o tempo para renascer
            if (!Vivo)
            {
                timerRespawn -= dt;
                if (timerRespawn <= 0.0f)
                {
                    Renascer();
                }
                return;
            }

            Rectangle box = CaixaColisao;

            // 1. Colisão com Tiros
            for (int i = 0; i < tiros.Count; i++)
            {
                var tiro = tiros[i];
                if (!tiro.Ativo) continue;

                if (Raylib.CheckCollisionPointRec(tiro.Posicao, box))
                {
                    tiro.Ativo = false;
                    ReceberDano(30.0f);
                }
            }

            // 2. Colisão com Explosão de Granadas
            for (int i = 0; i < granadas.Count; i++)
            {
                var g = granadas[i];
                if (g.Finalizada)
                {
                    float dist = Vector2.Distance(Posicao - new Vector2(0, Altura * 0.5f), g.Posicao);
                    if (dist < 130.0f)
                    {
                        float fatorDano = 1.0f - (dist / 130.0f);
                        ReceberDano(90.0f * fatorDano);
                    }
                }
            }
        }

        public void ReceberDano(float dano)
        {
            if (!Vivo) return;

            VidaAtual = MathF.Max(0.0f, VidaAtual - dano);
            timerFlashDano = 0.12f;

            if (VidaAtual <= 0.0f)
            {
                timerRespawn = 3.0f; // Renasce em 3 segundos
            }
        }

        public void Renascer()
        {
            Posicao = PosicaoSpawn;
            VidaAtual = VidaMax;
            timerFlashDano = 0.0f;
        }

        public void Desenhar()
        {
            Rectangle box = CaixaColisao;

            if (!Vivo)
            {
                // Silhueta translúcida no chão enquanto aguarda renascer
                Raylib.DrawRectangleLinesEx(box, 1.2f, new Color(200, 50, 50, 100));
                FontePB2.DesenharTexto($"Respawn: {timerRespawn:0.0}s", box.X - 6, box.Y - 18, 11, Color.Orange);
                return;
            }

            // Cor base ou Flash branco ao levar tiro
            Color corCorpo = timerFlashDano > 0.0f ? Color.White : new Color(38, 48, 62, 255);
            Color corBorda = timerFlashDano > 0.0f ? Color.Red : new Color(65, 85, 110, 255);

            // 1. Corpo metálico do Droide
            Raylib.DrawRectangleRec(box, corCorpo);
            Raylib.DrawRectangleLinesEx(box, 1.5f, corBorda);

            // 2. Sensor / Olho Neon Estilo PB2
            float sensorY = box.Y + 14.0f;
            Color corSensor = timerFlashDano > 0.0f ? Color.Yellow : new Color(240, 40, 40, 255);
            Raylib.DrawRectangle((int)box.X + 6, (int)sensorY, (int)box.Width - 12, 6, corSensor);
            Raylib.DrawCircleV(new Vector2(box.X + box.Width * 0.5f, sensorY + 3.0f), 4.5f, Color.Red);

            // 3. Barra de Vida no topo da cabeça
            float larguraBarra = 44.0f;
            float barraX = Posicao.X - (larguraBarra * 0.5f);
            float barraY = box.Y - 14.0f;
            float pctVida = Math.Clamp(VidaAtual / VidaMax, 0.0f, 1.0f);

            // Fundo da barra
            Raylib.DrawRectangle((int)barraX - 1, (int)barraY - 1, (int)larguraBarra + 2, 7, new Color(15, 20, 28, 220));
            // Preenchimento verde
            Color corVida = pctVida > 0.35f ? new Color(34, 197, 94, 255) : new Color(239, 68, 68, 255);
            Raylib.DrawRectangle((int)barraX, (int)barraY, (int)(larguraBarra * pctVida), 5, corVida);
            Raylib.DrawRectangleLines((int)barraX - 1, (int)barraY - 1, (int)larguraBarra + 2, 7, new Color(40, 50, 65, 255));

            // Rótulo de identificação
            FontePB2.DesenharTexto("#dummy", box.X + 2, box.Y + box.Height + 4, 10, Color.LightGray);
        }
    }
}
