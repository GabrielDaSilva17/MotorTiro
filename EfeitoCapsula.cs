using System;
using System.Numerics;
using Raylib_cs;

namespace JogoDeTiro
{
    // Efeito físico de ejeção de cápsulas de munição e partículas de energia/calor
    public class EfeitoCapsula
    {
        public Vector2 Posicao;
        public Vector2 Velocidade;
        public float RotacaoGraus;
        public float VelRotacao;
        public float TempoVida;
        public float TempoMaxVida;
        public string Tipo; // "bala", "shotgun", "plasma", "railgun"
        public Color Cor;
        public bool NoChao;

        public EfeitoCapsula(Vector2 pos, Vector2 vel, string tipo)
        {
            Posicao = pos;
            Velocidade = vel;
            RotacaoGraus = (float)Random.Shared.NextDouble() * 360.0f;
            VelRotacao = ((float)Random.Shared.NextDouble() * 900.0f) - 450.0f;
            Tipo = tipo;
            TempoVida = 0.0f;
            TempoMaxVida = (tipo == "bala" || tipo == "shotgun") ? 4.5f : 1.1f;

            if (tipo == "bala") Cor = new Color(245, 195, 65, 255);
            else if (tipo == "shotgun") Cor = new Color(225, 45, 45, 255);
            else if (tipo == "plasma") Cor = new Color(0, 230, 255, 240);
            else Cor = new Color(195, 65, 255, 240);
        }

        public void Atualizar(float dt, float alturaChao)
        {
            TempoVida += dt;
            if (TempoVida >= TempoMaxVida) return;

            if (!NoChao)
            {
                Velocidade.Y += 850.0f * dt;
                Posicao += Velocidade * dt;
                RotacaoGraus += VelRotacao * dt;

                // Colisão com o chão
                if (Posicao.Y >= alturaChao - 2.0f)
                {
                    Posicao.Y = alturaChao - 2.0f;
                    Velocidade.Y = -Velocidade.Y * 0.42f;
                    Velocidade.X *= 0.65f;
                    VelRotacao *= 0.5f;

                    if (MathF.Abs(Velocidade.Y) < 35.0f)
                    {
                        Velocidade = Vector2.Zero;
                        NoChao = true;
                    }
                }
            }
        }

        public void Desenhar()
        {
            float alpha = Math.Clamp(1.0f - (TempoVida / TempoMaxVida), 0.0f, 1.0f);
            byte a = (byte)(Cor.A * alpha);

            if (Tipo == "bala")
            {
                // Cápsula de pistola/fuzil (dourada)
                Raylib.DrawRectanglePro(
                    new Rectangle(Posicao.X, Posicao.Y, 5.5f, 2.4f),
                    new Vector2(2.75f, 1.2f),
                    RotacaoGraus,
                    new Color(Cor.R, Cor.G, Cor.B, a)
                );
            }
            else if (Tipo == "shotgun")
            {
                // Cartucho de escopeta 12 gauge (corpo vermelho e culatra de latão)
                Raylib.DrawRectanglePro(
                    new Rectangle(Posicao.X, Posicao.Y, 7.0f, 3.5f),
                    new Vector2(3.5f, 1.75f),
                    RotacaoGraus,
                    new Color(Cor.R, Cor.G, Cor.B, a)
                );
            }
            else
            {
                // Resíduo de vapor de plasma / faísca superaquecida
                float raio = 2.5f + (TempoVida * 3.5f);
                Raylib.DrawCircleV(Posicao, raio, new Color(Cor.R, Cor.G, Cor.B, (byte)(a * 0.6f)));
                Raylib.DrawCircleV(Posicao, raio * 0.4f, new Color((byte)255, (byte)255, (byte)255, a));
            }
        }
    }
}
