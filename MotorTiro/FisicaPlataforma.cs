using System;
using System.Collections.Generic;
using System.Numerics;
using Raylib_cs;

namespace JogoDeTiro
{
    public static class FisicaPlataforma
    {
        public const float LarguraCorpo = 18.0f;
        public const float AlturaCorpo = 62.0f;
        public const float OffsetPeY = 34.0f; // Distância do quadril aos pés

        // Caixa para colisão HORIZONTAL (Paredes)
        // Reduz 6px nos pés e na cabeça para o chão/teto NUNCA bloquearem o movimento lateral
        public static Rectangle ObterCaixaColisaoX(Vector2 posicaoQuadril)
        {
            const float margemVertical = 6.0f;
            return new Rectangle(
                posicaoQuadril.X - (LarguraCorpo * 0.5f),
                posicaoQuadril.Y + OffsetPeY - AlturaCorpo + margemVertical,
                LarguraCorpo,
                AlturaCorpo - (margemVertical * 2.0f)
            );
        }

        // Caixa para colisão VERTICAL (Chão e Plataformas)
        // Reduz 2px nas laterais para não prender nas quinas
        public static Rectangle ObterCaixaColisaoY(Vector2 posicaoQuadril)
        {
            const float margemHorizontal = 6.0f;
            return new Rectangle(
                posicaoQuadril.X - (LarguraCorpo * 0.5f) + margemHorizontal,
                posicaoQuadril.Y + OffsetPeY - AlturaCorpo,
                LarguraCorpo - (margemHorizontal * 2.0f),
                AlturaCorpo
            );
        }

        public static bool ResolverMovimentoEColisao(ref Vector2 posicao, ref Vector2 velocidade, List<Rectangle> plataformas, float dt)
        {
            bool tocouChao = false;

            // 1. Movimento e Bloqueio Horizontal (Paredes)
            posicao.X += velocidade.X * dt;
            Rectangle caixaX = ObterCaixaColisaoX(posicao);

            foreach (var plat in plataformas)
            {
                if (Raylib.CheckCollisionRecs(caixaX, plat))
                {
                    if (velocidade.X > 0.0f)
                    {
                        posicao.X = plat.X - (LarguraCorpo * 0.5f);
                        velocidade.X = 0.0f;
                    }
                    else if (velocidade.X < 0.0f)
                    {
                        posicao.X = plat.X + plat.Width + (LarguraCorpo * 0.5f);
                        velocidade.X = 0.0f;
                    }
                    caixaX = ObterCaixaColisaoX(posicao);
                }
            }

            // 2. Movimento e Bloqueio Vertical (Chão e Topo de Plataformas)
            posicao.Y += velocidade.Y * dt;
            Rectangle caixaY = ObterCaixaColisaoY(posicao);

            foreach (var plat in plataformas)
            {
                if (Raylib.CheckCollisionRecs(caixaY, plat))
                {
                    // Aterragem no topo
                    if (velocidade.Y > 0.0f)
                    {
                        posicao.Y = plat.Y - OffsetPeY;
                        velocidade.Y = 0.0f;
                        tocouChao = true;
                    }
                    // Bateu a cabeça por baixo
                    else if (velocidade.Y < 0.0f)
                    {
                        posicao.Y = (plat.Y + plat.Height) - OffsetPeY + AlturaCorpo;
                        velocidade.Y = 0.0f;
                    }
                    caixaY = ObterCaixaColisaoY(posicao);
                }
            }

            // 3. Segurança anti-abismo: se cair no vazio ou bugar a posição, ressurge no centro da tela
            if (posicao.Y > 900.0f || float.IsNaN(posicao.X) || float.IsNaN(posicao.Y))
            {
                posicao = new Vector2(480.0f, 300.0f);
                velocidade = Vector2.Zero;
            }

            return tocouChao;
        }
    }
}