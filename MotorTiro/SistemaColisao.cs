using System;
using System.Collections.Generic;
using System.Numerics;
using Raylib_cs;

namespace JogoDeTiro
{
    // Partícula de faísca gerada quando um tiro atinge uma parede ou metal
    public class FaiscaImpacto
    {
        public Vector2 Posicao;
        public Vector2 Velocidade;
        public Color Cor;
        public float TempoVida;
        public float MaxVida;

        public FaiscaImpacto(Vector2 pos, Vector2 vel, Color cor)
        {
            Posicao = pos;
            Velocidade = vel;
            Cor = cor;
            TempoVida = 0.0f;
            MaxVida = (float)Random.Shared.NextDouble() * 0.25f + 0.15f;
        }

        public void Atualizar(float dt)
        {
            TempoVida += dt;
            Posicao += Velocidade * dt;
            Velocidade *= (1.0f - 4.0f * dt);
        }

        public void Desenhar()
        {
            float alpha = Math.Clamp(1.0f - (TempoVida / MaxVida), 0.0f, 1.0f);
            Raylib.DrawCircleV(Posicao, 2.0f * alpha, new Color(Cor.R, Cor.G, Cor.B, (byte)(255 * alpha)));
        }

        public bool Morta => TempoVida >= MaxVida;
    }

    // Inimigo de teste com barra de vida e caixa de colisão
    public class InimigoAlvo
    {
        public Vector2 Posicao;
        public float Largura = 36.0f;
        public float Altura = 68.0f;
        public float Vida = 120.0f;
        public float VidaMaxima = 120.0f;
        public bool Vivo => Vida > 0.0f;
        public float TimerDano = 0.0f;

        public InimigoAlvo(Vector2 pos)
        {
            Posicao = pos;
        }

        public Rectangle ObterCaixaColisao()
        {
            return new Rectangle(Posicao.X - (Largura * 0.5f), Posicao.Y - Altura, Largura, Altura);
        }

        public void ReceberDano(float dano)
        {
            Vida = Math.Max(0.0f, Vida - dano);
            TimerDano = 0.12f;
        }

        public void Atualizar(float dt)
        {
            if (TimerDano > 0.0f) TimerDano -= dt;
        }

        public void Desenhar()
        {
            if (!Vivo) return;

            Rectangle caixa = ObterCaixaColisao();
            Color corCorpo = TimerDano > 0.0f ? Color.Red : new Color(50, 55, 65, 255);

            Raylib.DrawRectangleRec(caixa, corCorpo);
            Raylib.DrawRectangleLinesEx(caixa, 2.0f, new Color(25, 30, 38, 255));
            Raylib.DrawRectangle((int)caixa.X + 8, (int)caixa.Y + 12, (int)caixa.Width - 16, 6, Color.Red);

            float larguraBarra = 44.0f;
            float barraX = Posicao.X - (larguraBarra * 0.5f);
            float barraY = caixa.Y - 14.0f;
            float percentualVida = Vida / VidaMaxima;

            Raylib.DrawRectangle((int)barraX, (int)barraY, (int)larguraBarra, 5, new Color(20, 20, 20, 200));
            Raylib.DrawRectangle((int)barraX, (int)barraY, (int)(larguraBarra * percentualVida), 5, Color.Green);
            Raylib.DrawRectangleLines((int)barraX, (int)barraY, (int)larguraBarra, 5, Color.Black);
        }
    }

    // Gestor central de colisões do cenário, tiros e granadas
    public class SistemaColisao
    {
        public List<Rectangle> Paredes = new List<Rectangle>();
        public List<InimigoAlvo> Inimigos = new List<InimigoAlvo>();
        public List<FaiscaImpacto> Faiscas = new List<FaiscaImpacto>();

        // Memória para não aplicar dano da mesma granada mais de uma vez
        private HashSet<GranadaPB2> granadasDetonadas = new HashSet<GranadaPB2>();

        public SistemaColisao(float alturaChao)
        {
            Paredes.Add(new Rectangle(-2000, alturaChao, 5000, 400));
            Paredes.Add(new Rectangle(220, alturaChao - 90, 45, 90));
            Paredes.Add(new Rectangle(760, alturaChao - 160, 50, 160));
            Paredes.Add(new Rectangle(400, alturaChao - 120, 180, 24));

            Inimigos.Add(new InimigoAlvo(new Vector2(650, alturaChao)));
            Inimigos.Add(new InimigoAlvo(new Vector2(490, alturaChao - 120)));
            Inimigos.Add(new InimigoAlvo(new Vector2(300, alturaChao)));
        }

        // AGORA RECEBE TAMBÉM A LISTA DE GRANADAS
        public void Atualizar(float dt, List<ProjetilPlasma> tiros, List<GranadaPB2> granadas)
        {
            // 1. Atualizar Inimigos e Faíscas
            for (int i = 0; i < Inimigos.Count; i++) Inimigos[i].Atualizar(dt);
            for (int i = 0; i < Faiscas.Count; i++) Faiscas[i].Atualizar(dt);
            Faiscas.RemoveAll(f => f.Morta);

            // 2. Colisão de Tiros (Projéteis de Plasma/Balas)
            for (int t = 0; t < tiros.Count; t++)
            {
                ProjetilPlasma tiro = tiros[t];
                if (!tiro.Ativo) continue;

                Vector2 posNova = tiro.Posicao;
                Vector2 posAnterior = posNova - (tiro.Velocidade * dt);
                bool colidiu = false;
                Vector2 pontoImpacto = posNova;

                // Colisão Tiros vs Inimigos
                for (int e = 0; e < Inimigos.Count; e++)
                {
                    InimigoAlvo inimigo = Inimigos[e];
                    if (!inimigo.Vivo) continue;

                    if (LinhaColideComRetangulo(posAnterior, posNova, inimigo.ObterCaixaColisao(), out pontoImpacto))
                    {
                        float dano = tiro.TipoArma switch
                        {
                            "sniper" => 120.0f,
                            "android_sniper" => 95.0f,
                            "railgun" => 140.0f,
                            "shotgun" => 25.0f,
                            "m4a1" => 28.0f,
                            "heavy_cannon" => 35.0f,
                            _ => 22.0f
                        };
                        inimigo.ReceberDano(dano);
                        GerarFaiscas(pontoImpacto, -tiro.Velocidade, Color.Red);
                        tiro.Ativo = false;
                        colidiu = true;
                        break;
                    }
                }

                if (colidiu) continue;

                // Colisão Tiros vs Paredes
                for (int p = 0; p < Paredes.Count; p++)
                {
                    if (LinhaColideComRetangulo(posAnterior, posNova, Paredes[p], out pontoImpacto))
                    {
                        GerarFaiscas(pontoImpacto, -tiro.Velocidade, tiro.Cor);
                        tiro.Ativo = false;
                        break;
                    }
                }
            }

            // 3. FÍSICA E EXPLOSÃO DAS GRANADAS
            for (int g = 0; g < granadas.Count; g++)
            {
                GranadaPB2 granada = granadas[g];

                // Limpar da memória se já desapareceu
                if (granada.Finalizada)
                {
                    granadasDetonadas.Remove(granada);
                    continue;
                }

                if (granada.Explodiu && !granadasDetonadas.Contains(granada))
                {
                    // BOOM! A granada explodiu neste exato frame: Aplicar dano em Área (AoE)
                    foreach (var inimigo in Inimigos)
                    {
                        if (!inimigo.Vivo) continue;

                        // Calcula a distância do centro da explosão até ao meio do corpo do inimigo
                        Vector2 centroInimigo = new Vector2(inimigo.Posicao.X, inimigo.Posicao.Y - (inimigo.Altura / 2));
                        float distancia = Vector2.Distance(granada.Posicao, centroInimigo);

                        if (distancia <= granada.RaioExplosao)
                        {
                            // Quanto mais próximo o inimigo estiver, mais dano leva
                            float fatorDano = 1.0f - (distancia / granada.RaioExplosao);
                            inimigo.ReceberDano(granada.Dano * fatorDano);
                        }
                    }

                    // Regista a granada para não continuar a dar dano infinito em todos os frames seguintes
                    granadasDetonadas.Add(granada);
                }
            }
        }

        private void GerarFaiscas(Vector2 ponto, Vector2 direcaoRebote, Color corBase)
        {
            Vector2 normal = Vector2.Normalize(direcaoRebote);
            for (int i = 0; i < 7; i++)
            {
                float angulo = ((float)Random.Shared.NextDouble() * 1.6f - 0.8f);
                float velocidade = (float)Random.Shared.NextDouble() * 180.0f + 60.0f;
                float cos = MathF.Cos(angulo);
                float sin = MathF.Sin(angulo);

                Vector2 velFaisca = new Vector2(
                    normal.X * cos - normal.Y * sin,
                    normal.X * sin + normal.Y * cos
                ) * velocidade;

                Faiscas.Add(new FaiscaImpacto(ponto, velFaisca, corBase));
            }
        }

        private static bool LinhaColideComRetangulo(Vector2 p1, Vector2 p2, Rectangle rec, out Vector2 pontoColisao)
        {
            pontoColisao = p2;
            if (Raylib.CheckCollisionPointRec(p2, rec)) return true;

            Vector2 topoEsq = new Vector2(rec.X, rec.Y);
            Vector2 topoDir = new Vector2(rec.X + rec.Width, rec.Y);
            Vector2 baixoEsq = new Vector2(rec.X, rec.Y + rec.Height);
            Vector2 baixoDir = new Vector2(rec.X + rec.Width, rec.Y + rec.Height);

            Vector2 ponto = Vector2.Zero;
            if (Raylib.CheckCollisionLines(p1, p2, topoEsq, topoDir, ref ponto) ||
                Raylib.CheckCollisionLines(p1, p2, topoDir, baixoDir, ref ponto) ||
                Raylib.CheckCollisionLines(p1, p2, baixoDir, baixoEsq, ref ponto) ||
                Raylib.CheckCollisionLines(p1, p2, baixoEsq, topoEsq, ref ponto))
            {
                pontoColisao = ponto;
                return true;
            }

            return false;
        }

        public void Desenhar()
        {
            for (int i = 0; i < Paredes.Count; i++)
            {
                if (Paredes[i].Height > 300) continue;
                Raylib.DrawRectangleRec(Paredes[i], new Color(75, 88, 102, 255));
                Raylib.DrawRectangleLinesEx(Paredes[i], 2.0f, new Color(130, 145, 165, 255));
            }
            for (int i = 0; i < Inimigos.Count; i++) Inimigos[i].Desenhar();
            for (int i = 0; i < Faiscas.Count; i++) Faiscas[i].Desenhar();
        }
    }
}