using System;
using System.Collections.Generic;
using System.Numerics;
using Raylib_cs;

namespace JogoDeTiro
{
    // Partícula da explosão da granada estilo Plazma Burst 2
    public class ParticulaExplosao
    {
        public Vector2 Posicao;
        public Vector2 Velocidade;
        public float TempoVida;
        public float TempoMax;
        public Color Cor;
        public float Tamanho;

        public ParticulaExplosao(Vector2 pos, Vector2 vel, Color cor, float tempoMax, float tamanho)
        {
            Posicao = pos;
            Velocidade = vel;
            Cor = cor;
            TempoMax = tempoMax;
            TempoVida = 0.0f;
            Tamanho = tamanho;
        }

        public void Atualizar(float dt)
        {
            TempoVida += dt;
            Posicao += Velocidade * dt;
            Velocidade *= (1.0f - (1.8f * dt)); // Desaceleração aerodinâmica
        }

        public void Desenhar()
        {
            float progresso = Math.Clamp(TempoVida / TempoMax, 0.0f, 1.0f);
            byte alpha = (byte)(255 * (1.0f - progresso));
            float tamAtual = Tamanho * (1.0f - (progresso * 0.4f));
            Color c = new Color(Cor.R, Cor.G, Cor.B, alpha);
            Raylib.DrawCircleV(Posicao, tamAtual, c);
        }

        public bool Morta => TempoVida >= TempoMax;
    }

    // Granada Oficial de Plazma Burst 2 (ItemGrenade - DefineSprite 2885)
    public class GranadaPB2
    {
        public Vector2 Posicao;
        public Vector2 Velocidade;
        public float RotacaoGraus;
        public float VelRotacao;
        public float TimerDetonacao = 1.75f;
        public bool Explodiu = false;
        public bool DanoAplicado = false;
        public bool Finalizada = false;
        public float RaioExplosao = 125.0f;
        public float Dano = 160.0f;

        private float timerAposExplosao = 0.0f;
        private Texture2D textura;
        private Sound somExplosao;
        private List<ParticulaExplosao> particulas = new List<ParticulaExplosao>();

        public GranadaPB2(Vector2 posicaoInicial, Vector2 direcaoLancamento, float forca, Texture2D texGrenade, Sound sndExplode)
        {
            Posicao = posicaoInicial;
            Velocidade = (direcaoLancamento * forca);
            // Elevação suave em arcos médios/longos, mantendo arremessos verticais precisos
            if (direcaoLancamento.Y > -0.65f)
            {
                Velocidade.Y -= 80.0f;
            }
            RotacaoGraus = (float)Random.Shared.NextDouble() * 360.0f;
            VelRotacao = ((float)Random.Shared.NextDouble() * 720.0f - 360.0f);
            textura = texGrenade;
            somExplosao = sndExplode;
        }

        public void Atualizar(float dt, List<Rectangle> plataformas, Action<float>? onExplosaoTrauma = null)
        {
            if (Finalizada) return;

            if (!Explodiu)
            {
                TimerDetonacao -= dt;

                // 1. Gravidade estilo PB2
                Velocidade.Y += 880.0f * dt;

                const float RaioCorpo = 6.0f;

                // 2. Colisão e Deslocamento Horizontal
                Posicao.X += Velocidade.X * dt;
                Rectangle caixaX = new Rectangle(Posicao.X - RaioCorpo, Posicao.Y - RaioCorpo + 2.0f, RaioCorpo * 2.0f, (RaioCorpo * 2.0f) - 4.0f);

                for (int i = 0; i < plataformas.Count; i++)
                {
                    Rectangle plat = plataformas[i];
                    if (plat.Width <= 0 || plat.Height <= 0) continue;

                    if (Raylib.CheckCollisionRecs(caixaX, plat))
                    {
                        if (Velocidade.X > 0.0f)
                        {
                            Posicao.X = plat.X - RaioCorpo;
                            Velocidade.X = -Velocidade.X * 0.55f; // Quique na parede direita
                        }
                        else if (Velocidade.X < 0.0f)
                        {
                            Posicao.X = plat.X + plat.Width + RaioCorpo;
                            Velocidade.X = -Velocidade.X * 0.55f; // Quique na parede esquerda
                        }

                        if (MathF.Abs(Velocidade.X) < 18.0f)
                        {
                            Velocidade.X = 0.0f;
                            VelRotacao = 0.0f;
                        }
                        else
                        {
                            VelRotacao = -VelRotacao * 0.6f;
                        }
                        caixaX.X = Posicao.X - RaioCorpo;
                    }
                }

                // 3. Colisão e Deslocamento Vertical
                Posicao.Y += Velocidade.Y * dt;
                Rectangle caixaY = new Rectangle(Posicao.X - RaioCorpo + 2.0f, Posicao.Y - RaioCorpo, (RaioCorpo * 2.0f) - 4.0f, RaioCorpo * 2.0f);
                bool noChao = false;

                for (int i = 0; i < plataformas.Count; i++)
                {
                    Rectangle plat = plataformas[i];
                    if (plat.Width <= 0 || plat.Height <= 0) continue;

                    if (Raylib.CheckCollisionRecs(caixaY, plat))
                    {
                        if (Velocidade.Y > 0.0f)
                        {
                            // Aterragem no topo da plataforma ou chão
                            Posicao.Y = plat.Y - RaioCorpo;

                            if (Velocidade.Y > 45.0f)
                            {
                                Velocidade.Y = -Velocidade.Y * 0.42f; // Quique vertical elástico
                                Velocidade.X *= 0.82f;                 // Perda gradual no quique
                            }
                            else
                            {
                                Velocidade.Y = 0.0f;
                                noChao = true;
                            }
                        }
                        else if (Velocidade.Y < 0.0f)
                        {
                            // Impacto no teto / underside da plataforma
                            Posicao.Y = plat.Y + plat.Height + RaioCorpo;
                            Velocidade.Y = -Velocidade.Y * 0.40f;
                        }
                        caixaY.Y = Posicao.Y - RaioCorpo;
                    }
                }

                if (noChao)
                {
                    // Desaceleração suave por rolamento físico no chão
                    float atritoRolamento = 240.0f * dt;
                    if (MathF.Abs(Velocidade.X) <= atritoRolamento)
                    {
                        Velocidade.X = 0.0f;
                        VelRotacao = 0.0f;
                    }
                    else
                    {
                        Velocidade.X -= MathF.Sign(Velocidade.X) * atritoRolamento;
                        VelRotacao = (Velocidade.X / RaioCorpo) * (180.0f / MathF.PI);
                    }
                }

                // Rotação visual da granada acompanha velocidade e rolamento
                RotacaoGraus += VelRotacao * dt;

                // Segurança: se cair num abismo sem fundo, finaliza
                if (Posicao.Y > 2600.0f)
                {
                    Finalizada = true;
                }

                if (TimerDetonacao <= 0.0f)
                {
                    Detonar(onExplosaoTrauma);
                }
            }
            else
            {
                // Atualiza partículas e efeitos da explosão
                timerAposExplosao += dt;
                for (int i = 0; i < particulas.Count; i++)
                {
                    particulas[i].Atualizar(dt);
                }
                particulas.RemoveAll(p => p.Morta);

                if (timerAposExplosao >= 1.2f && particulas.Count == 0)
                {
                    Finalizada = true;
                }
            }
        }

        public void Detonar(Action<float>? onExplosaoTrauma = null)
        {
            if (Explodiu) return;
            Explodiu = true;

            // Áudio de explosão oficial PB2
            if (somExplosao.Stream.Buffer != IntPtr.Zero)
            {
                Raylib.PlaySound(somExplosao);
            }

            // Trauma de câmera (tremor de tela)
            onExplosaoTrauma?.Invoke(0.75f);

            // Gera partículas da detonação
            Random rnd = new Random();
            for (int i = 0; i < 35; i++)
            {
                float ang = (float)rnd.NextDouble() * MathF.PI * 2.0f;
                float spd = (float)rnd.NextDouble() * 380.0f + 60.0f;
                Vector2 vel = new Vector2(MathF.Cos(ang), MathF.Sin(ang)) * spd;
                float vida = (float)rnd.NextDouble() * 0.6f + 0.35f;
                float tam = (float)rnd.NextDouble() * 5.0f + 2.5f;

                Color cor = (i % 3) switch
                {
                    0 => new Color((byte)255, (byte)140, (byte)30, (byte)255),  // Fogo alaranjado
                    1 => new Color((byte)255, (byte)230, (byte)70, (byte)255),  // Brilho quente
                    _ => new Color((byte)70, (byte)180, (byte)255, (byte)240)   // Centelha de plasma PB2
                };

                particulas.Add(new ParticulaExplosao(Posicao, vel, cor, vida, tam));
            }
        }

        public void Desenhar()
        {
            if (!Explodiu)
            {
                if (textura.Id > 0)
                {
                    float escala = 1.35f;
                    float larg = textura.Width * escala;
                    float alt = textura.Height * escala;

                    Rectangle src = new Rectangle(0, 0, textura.Width, textura.Height);
                    Rectangle dst = new Rectangle(Posicao.X, Posicao.Y, larg, alt);
                    Vector2 origem = new Vector2(larg * 0.5f, alt * 0.5f);

                    // LED azul piscante da granada PB2
                    bool piscaLed = MathF.Sin((float)Raylib.GetTime() * 18.0f) > 0.0f;
                    Color corTint = piscaLed ? Color.White : new Color((byte)190, (byte)210, (byte)230, (byte)255);

                    Raylib.DrawTexturePro(textura, src, dst, origem, RotacaoGraus, corTint);

                    if (piscaLed)
                    {
                        Raylib.DrawCircleV(Posicao, 3.0f, new Color((byte)0, (byte)220, (byte)255, (byte)220));
                    }
                }
                else
                {
                    // Fallback procedural
                    Raylib.DrawCircleV(Posicao, 5.0f, new Color((byte)40, (byte)50, (byte)60, (byte)255));
                }
            }
            else
            {
                // Onda de choque em expansão estilo PB2
                float tExp = Math.Clamp(timerAposExplosao / 0.45f, 0.0f, 1.0f);
                if (tExp < 1.0f)
                {
                    float raioAtual = RaioExplosao * MathF.Sin(tExp * MathF.PI * 0.5f);
                    byte alphaOnda = (byte)(230 * (1.0f - tExp));
                    Raylib.DrawCircleLines((int)Posicao.X, (int)Posicao.Y, raioAtual, new Color((byte)255, (byte)170, (byte)40, alphaOnda));
                    Raylib.DrawCircleLines((int)Posicao.X, (int)Posicao.Y, raioAtual * 0.85f, new Color((byte)0, (byte)220, (byte)255, (byte)(alphaOnda * 0.7f)));

                    // Clarão central instantâneo
                    if (tExp < 0.25f)
                    {
                        float alphaCentro = (1.0f - (tExp / 0.25f));
                        Raylib.DrawCircleV(Posicao, 28.0f * (1.0f - tExp), new Color((byte)255, (byte)255, (byte)255, (byte)(240 * alphaCentro)));
                    }
                }

                // Desenha partículas da explosão
                for (int i = 0; i < particulas.Count; i++)
                {
                    particulas[i].Desenhar();
                }
            }
        }
    }
}
