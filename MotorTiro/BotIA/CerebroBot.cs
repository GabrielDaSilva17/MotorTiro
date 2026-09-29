using System;
using System.Collections.Generic;
using System.Numerics;
using Raylib_cs;

namespace JogoDeTiro.BotIA
{
    // Comandos gerados pela inteligência artificial para o corpo executar
    public struct ComandoBot
    {
        public float MoverX;          // -1.0f (esquerda), 0.0f (parado), 1.0f (direita)
        public bool Pular;
        public bool Agachar;          // Permite agachar
        public bool Agachado { get => Agachar; set => Agachar = value; } // Suporte aos dois nomes
        public Vector2 PontoMira;      // Ponto de mira 360° no espaço do mundo
        public bool Atirar;
        public bool LancarGranada;
        public int TrocarParaSlot;     // -1 (mantém arma), 0 a 8 (seleciona slot)
    }

    public enum EstadoCombate
    {
        Patrulhar,
        Alerta,
        Engajar,
        BuscarAbrigo,
        Recuar
    }

    public class CerebroBot
    {
        public EstadoCombate EstadoAtual { get; private set; } = EstadoCombate.Patrulhar;

        // Sensores
        public float RaioVisao = 750.0f;
        public float RaioAudicao = 500.0f;
        public bool VendoJogador { get; private set; } = false;
        public Vector2 UltimaPosicaoConhecida;

        // Temporizadores e pesos de reação
        private float timerReacaoTiro = 0.0f;
        private float direcaoPatrulha = 1.0f;
        private float timerTrocaPatrulha = 3.0f;
        private float cooldownGranadaBot = 6.0f;

        // Suavização orgânica da mira
        private Vector2 miraSuavizada;
        private readonly Random rnd = new Random();

        public CerebroBot(Vector2 posicaoInicial)
        {
            miraSuavizada = posicaoInicial + new Vector2(100, 0);
            UltimaPosicaoConhecida = posicaoInicial;
        }

        public ComandoBot ProcessarDecisoes(
            Vector2 posBot,
            Vector2 velBot,
            float vidaBot,
            Vector2 posJogador,
            Vector2 velJogador,
            List<Rectangle> paredes,
            float dt)
        {
            ComandoBot cmd = new ComandoBot();
            cmd.TrocarParaSlot = -1;

            if (cooldownGranadaBot > 0.0f) cooldownGranadaBot -= dt;

            // 1. SENSOR DE LINHA DE VISÃO (Raycast direto aos olhos do jogador)
            Vector2 olhosBot = posBot - new Vector2(0, 32.0f);
            Vector2 centroJogador = posJogador - new Vector2(0, 24.0f);
            float distJogador = Vector2.Distance(olhosBot, centroJogador);

            VendoJogador = false;
            if (distJogador <= RaioVisao)
            {
                bool linhaBloqueada = false;
                foreach (var parede in paredes)
                {
                    if (LinhaColideComRetangulo(olhosBot, centroJogador, parede))
                    {
                        linhaBloqueada = true;
                        break;
                    }
                }

                if (!linhaBloqueada)
                {
                    VendoJogador = true;
                    UltimaPosicaoConhecida = centroJogador;
                }
            }

            // 2. MÁQUINA DE ESTADOS / PESOS TÁTICOS
            if (VendoJogador)
            {
                if (vidaBot < 35.0f && distJogador < 300.0f)
                {
                    EstadoAtual = EstadoCombate.Recuar;
                }
                else
                {
                    EstadoAtual = EstadoCombate.Engajar;
                }
            }
            else
            {
                float distanciaMemoria = Vector2.Distance(posBot, UltimaPosicaoConhecida);
                if (distanciaMemoria < 50.0f)
                {
                    EstadoAtual = EstadoCombate.Patrulhar;
                }
                else
                {
                    EstadoAtual = EstadoCombate.Alerta;
                }
            }

            // 3. EXECUÇÃO DE COMPORTAMENTO
            switch (EstadoAtual)
            {
                case EstadoCombate.Patrulhar:
                    timerTrocaPatrulha -= dt;
                    if (timerTrocaPatrulha <= 0.0f)
                    {
                        direcaoPatrulha = -direcaoPatrulha;
                        timerTrocaPatrulha = (float)rnd.NextDouble() * 3.0f + 2.0f;
                    }
                    cmd.MoverX = direcaoPatrulha * 0.5f;
                    cmd.PontoMira = posBot + new Vector2(direcaoPatrulha * 200.0f, -10.0f);
                    cmd.Atirar = false;
                    cmd.Agachar = false;
                    break;

                case EstadoCombate.Alerta:
                    float deltaXAlerta = UltimaPosicaoConhecida.X - posBot.X;
                    cmd.MoverX = MathF.Sign(deltaXAlerta);
                    cmd.PontoMira = UltimaPosicaoConhecida;
                    cmd.Atirar = false;
                    cmd.Agachar = false;
                    break;

                case EstadoCombate.Engajar:
                    // Predição balística simples do movimento do alvo
                    Vector2 pontoAlvoMira = centroJogador + (velJogador * 0.08f);
                    pontoAlvoMira += new Vector2(
                        (float)(rnd.NextDouble() * 8.0 - 4.0),
                        (float)(rnd.NextDouble() * 8.0 - 4.0)
                    );

                    miraSuavizada = Vector2.Lerp(miraSuavizada, pontoAlvoMira, 14.0f * dt);
                    cmd.PontoMira = miraSuavizada;

                    // Controle de distância
                    float deltaX = centroJogador.X - posBot.X;
                    if (MathF.Abs(deltaX) > 280.0f)
                    {
                        cmd.MoverX = MathF.Sign(deltaX);
                    }
                    else if (MathF.Abs(deltaX) < 160.0f)
                    {
                        cmd.MoverX = -MathF.Sign(deltaX);
                    }
                    else
                    {
                        cmd.MoverX = 0.0f;
                        cmd.Agachar = MathF.Abs(velBot.X) < 15.0f && rnd.Next(0, 100) > 40;
                    }

                    // Verifica alinhamento de mira antes de disparar
                    Vector2 vetorMira = Vector2.Normalize(cmd.PontoMira - olhosBot);
                    Vector2 vetorRealPlayer = Vector2.Normalize(centroJogador - olhosBot);
                    float alinhamento = Vector2.Dot(vetorMira, vetorRealPlayer);

                    if (alinhamento > 0.94f)
                    {
                        timerReacaoTiro += dt;
                        if (timerReacaoTiro > 0.15f)
                        {
                            cmd.Atirar = true;
                        }
                    }
                    else
                    {
                        timerReacaoTiro = 0.0f;
                        cmd.Atirar = false;
                    }

                    // Decisão de arremesso de granada
                    if (cooldownGranadaBot <= 0.0f && distJogador > 220.0f && distJogador < 520.0f)
                    {
                        if (rnd.Next(0, 1000) < 12)
                        {
                            cmd.LancarGranada = true;
                            cooldownGranadaBot = (float)rnd.NextDouble() * 4.0f + 6.0f;
                        }
                    }
                    break;

                case EstadoCombate.Recuar:
                    float fugirX = posBot.X - centroJogador.X;
                    cmd.MoverX = MathF.Sign(fugirX);
                    cmd.PontoMira = centroJogador;
                    cmd.Atirar = VendoJogador;
                    cmd.Agachar = false;
                    break;
            }

            // 4. SALTO AUTOMÁTICO SOBRE OBSTÁCULOS
            if (cmd.MoverX != 0.0f)
            {
                Rectangle detectorObstaculo = new Rectangle(
                    posBot.X + (cmd.MoverX * 24.0f) - 6.0f,
                    posBot.Y - 20.0f,
                    12.0f,
                    24.0f
                );

                foreach (var parede in paredes)
                {
                    if (Raylib.CheckCollisionRecs(detectorObstaculo, parede))
                    {
                        cmd.Pular = true;
                        break;
                    }
                }
            }

            return cmd;
        }

        private static bool LinhaColideComRetangulo(Vector2 p1, Vector2 p2, Rectangle rec)
        {
            Vector2 rTopLeft = new Vector2(rec.X, rec.Y);
            Vector2 rTopRight = new Vector2(rec.X + rec.Width, rec.Y);
            Vector2 rBottomLeft = new Vector2(rec.X, rec.Y + rec.Height);
            Vector2 rBottomRight = new Vector2(rec.X + rec.Width, rec.Y + rec.Height);

            Vector2 pontoDummy = Vector2.Zero;
            return Raylib.CheckCollisionLines(p1, p2, rTopLeft, rTopRight, ref pontoDummy) ||
                   Raylib.CheckCollisionLines(p1, p2, rTopRight, rBottomRight, ref pontoDummy) ||
                   Raylib.CheckCollisionLines(p1, p2, rBottomRight, rBottomLeft, ref pontoDummy) ||
                   Raylib.CheckCollisionLines(p1, p2, rBottomLeft, rTopLeft, ref pontoDummy);
        }
    }
}