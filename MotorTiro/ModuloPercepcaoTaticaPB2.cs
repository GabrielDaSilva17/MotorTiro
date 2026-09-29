using System;
using System.Collections.Generic;
using System.Numerics;
using Raylib_cs;

namespace JogoDeTiro
{
    // =========================================================================
    // Módulo de Percepção Tática e Consciência Espacial (Estilo Plazma Burst 2)
    // =========================================================================
    // Responsável por:
    // 1. Raycast de Linha de Tiro (Cessar fogo se houver parede/chão no caminho)
    // 2. Raio de Percepção Acústica Dinâmica (Nem todos os bots ouvem ao mesmo tempo)
    // 3. Detecção de Obstáculos e Degraus para Pulo Tático e Reposicionamento
    // =========================================================================
    public static class ModuloPercepcaoTaticaPB2
    {
        /// <summary>
        /// Verifica se há linha direta desobstruída entre a origem (cano da arma/olhos)
        /// e o destino (centro do alvo). Se houver qualquer plataforma sólida no caminho,
        /// retorna falso para que a IA pare de atirar inutilmente na parede.
        /// </summary>
        public static bool TemLinhaDeTiroLivre(Vector2 origem, Vector2 destino, List<Rectangle> plataformas)
        {
            if (origem == destino) return true;

            for (int i = 0; i < plataformas.Count; i++)
            {
                Rectangle plat = plataformas[i];
                if (plat.Width <= 0 || plat.Height <= 0) continue;

                // Se a ponta da arma ou o alvo estiver dentro de uma parede sólida, o tiro está bloqueado!
                if (Raylib.CheckCollisionPointRec(origem, plat))
                    return false;

                if (Raylib.CheckCollisionPointRec(destino, plat))
                    return false;

                // Otimização por Bounding Box (AABB rápido)
                float minX = MathF.Min(origem.X, destino.X);
                float maxX = MathF.Max(origem.X, destino.X);
                float minY = MathF.Min(origem.Y, destino.Y);
                float maxY = MathF.Max(origem.Y, destino.Y);

                if (maxX < plat.X || minX > plat.X + plat.Width ||
                    maxY < plat.Y || minY > plat.Y + plat.Height)
                {
                    continue;
                }

                // 4 arestas da plataforma sólida
                Vector2 tEsq = new Vector2(plat.X, plat.Y);
                Vector2 tDir = new Vector2(plat.X + plat.Width, plat.Y);
                Vector2 bEsq = new Vector2(plat.X, plat.Y + plat.Height);
                Vector2 bDir = new Vector2(plat.X + plat.Width, plat.Y + plat.Height);

                Vector2 col = Vector2.Zero;
                if (Raylib.CheckCollisionLines(origem, destino, tEsq, tDir, ref col) ||
                    Raylib.CheckCollisionLines(origem, destino, tDir, bDir, ref col) ||
                    Raylib.CheckCollisionLines(origem, destino, bDir, bEsq, ref col) ||
                    Raylib.CheckCollisionLines(origem, destino, bEsq, tEsq, ref col))
                {
                    return false; // Trajetória interceptada por parede ou chão!
                }
            }

            return true;
        }

        /// <summary>
        /// Controle de fogo tático: garante que o cano da arma tenha linha de tiro limpa
        /// até o alvo antes de puxar o gatilho.
        /// </summary>
        public static bool PodeAbrirFogo(Vector2 pontaCano, Vector2 centroAlvo, List<Rectangle> plataformas)
        {
            // Se a ponta do cano colide diretamente com qualquer parede, cessa fogo!
            for (int i = 0; i < plataformas.Count; i++)
            {
                if (Raylib.CheckCollisionPointRec(pontaCano, plataformas[i]))
                    return false;
            }

            return TemLinhaDeTiroLivre(pontaCano, centroAlvo, plataformas);
        }

        /// <summary>
        /// Propaga o som de disparos em um raio de percepção acústica realista.
        /// Bots distantes ou abafados por múltiplas paredes grossas não percebem o tiro,
        /// evitando o efeito de todos os bots do mapa acordarem juntos.
        /// </summary>
        public static void PropagarSomDisparo(
            Vector2 posicaoDisparo,
            bool disparoDoJogador,
            List<BotIAPB2> bots,
            PersonagemPB2 jogador,
            List<Rectangle> plataformas,
            float raioMaximoBase = 380.0f)
        {
            if (disparoDoJogador)
            {
                for (int i = 0; i < bots.Count; i++)
                {
                    var bot = bots[i];
                    if (!bot.Vivo) continue;

                    float dist = Vector2.Distance(bot.CentroVisual, posicaoDisparo);
                    if (dist > raioMaximoBase) continue; // Fora do raio de alcance auditivo!

                    // A queima-roupa (< 150px): som imediato e inconfundível
                    if (dist < 150.0f)
                    {
                        bot.NotificarDisparoJogador(posicaoDisparo);
                        continue;
                    }

                    // Média distância: calcula abafamento sonoro por obstáculos sólidos
                    int barreiras = ContarObstaculos(posicaoDisparo, bot.CentroVisual, plataformas);
                    float raioEfetivo = raioMaximoBase - (barreiras * 110.0f);

                    if (dist <= raioEfetivo)
                    {
                        bot.NotificarDisparoJogador(posicaoDisparo);
                    }
                }
            }
            else
            {
                // Disparo de um bot inimigo: alerta o bot do jogador se estiver no modo bot!
                if (jogador.ModoBotAtivo)
                {
                    float dist = Vector2.Distance(jogador.CentroVisual, posicaoDisparo);
                    if (dist <= raioMaximoBase)
                    {
                        if (dist < 150.0f)
                        {
                            jogador.NotificarDisparoInimigo(posicaoDisparo);
                        }
                        else
                        {
                            int barreiras = ContarObstaculos(posicaoDisparo, jogador.CentroVisual, plataformas);
                            float raioEfetivo = raioMaximoBase - (barreiras * 110.0f);
                            if (dist <= raioEfetivo)
                            {
                                jogador.NotificarDisparoInimigo(posicaoDisparo);
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Conta quantas barreiras sólidas estão entre a origem e o destino para abafamento acústico.
        /// </summary>
        public static int ContarObstaculos(Vector2 a, Vector2 b, List<Rectangle> plataformas)
        {
            int total = 0;
            for (int i = 0; i < plataformas.Count; i++)
            {
                var plat = plataformas[i];
                float minX = MathF.Min(a.X, b.X);
                float maxX = MathF.Max(a.X, b.X);
                float minY = MathF.Min(a.Y, b.Y);
                float maxY = MathF.Max(a.Y, b.Y);

                if (maxX < plat.X || minX > plat.X + plat.Width ||
                    maxY < plat.Y || minY > plat.Y + plat.Height)
                {
                    continue;
                }

                Vector2 tEsq = new Vector2(plat.X, plat.Y);
                Vector2 tDir = new Vector2(plat.X + plat.Width, plat.Y);
                Vector2 bEsq = new Vector2(plat.X, plat.Y + plat.Height);
                Vector2 bDir = new Vector2(plat.X + plat.Width, plat.Y + plat.Height);
                Vector2 col = Vector2.Zero;

                if (Raylib.CheckCollisionLines(a, b, tEsq, tDir, ref col) ||
                    Raylib.CheckCollisionLines(a, b, tDir, bDir, ref col) ||
                    Raylib.CheckCollisionLines(a, b, bDir, bEsq, ref col) ||
                    Raylib.CheckCollisionLines(a, b, bEsq, tEsq, ref col))
                {
                    total++;
                }
            }
            return total;
        }

        /// <summary>
        /// Detecta se há uma parede ou degrau sólido logo à frente na direção de movimento.
        /// Identifica se é um degrau transponível por pulo ou uma parede alta vertical.
        /// </summary>
        public static bool DetectarParedeAFrente(Vector2 posicaoCorpo, float direcaoX, List<Rectangle> plataformas, float distanciaSensor, out bool paredeAlta)
        {
            paredeAlta = false;
            if (direcaoX == 0.0f) return false;

            float sinal = MathF.Sign(direcaoX);
            // Sensor no joelho (-18px) e na altura do peito/cabeça (-52px)
            Vector2 pJoelho = posicaoCorpo + new Vector2(sinal * 12.0f, -18.0f);
            Vector2 pJoelhoFim = posicaoCorpo + new Vector2(sinal * (12.0f + distanciaSensor), -18.0f);

            Vector2 pCabeca = posicaoCorpo + new Vector2(sinal * 12.0f, -52.0f);
            Vector2 pCabecaFim = posicaoCorpo + new Vector2(sinal * (12.0f + distanciaSensor), -52.0f);

            bool colideJoelho = false;
            bool colideCabeca = false;

            for (int i = 0; i < plataformas.Count; i++)
            {
                var plat = plataformas[i];
                if (Raylib.CheckCollisionPointRec(pJoelhoFim, plat)) colideJoelho = true;
                if (Raylib.CheckCollisionPointRec(pCabecaFim, plat)) colideCabeca = true;

                Vector2 tEsq = new Vector2(plat.X, plat.Y);
                Vector2 bEsq = new Vector2(plat.X, plat.Y + plat.Height);
                Vector2 tDir = new Vector2(plat.X + plat.Width, plat.Y);
                Vector2 bDir = new Vector2(plat.X + plat.Width, plat.Y + plat.Height);
                Vector2 col = Vector2.Zero;

                if (Raylib.CheckCollisionLines(pJoelho, pJoelhoFim, tEsq, bEsq, ref col) ||
                    Raylib.CheckCollisionLines(pJoelho, pJoelhoFim, tDir, bDir, ref col))
                {
                    colideJoelho = true;
                }

                if (Raylib.CheckCollisionLines(pCabeca, pCabecaFim, tEsq, bEsq, ref col) ||
                    Raylib.CheckCollisionLines(pCabeca, pCabecaFim, tDir, bDir, ref col))
                {
                    colideCabeca = true;
                }
            }

            paredeAlta = colideJoelho && colideCabeca;
            return colideJoelho;
        }

        public static bool DetectarParedeAFrente(Vector2 posicaoCorpo, float direcaoX, List<Rectangle> plataformas, float distanciaSensor = 24.0f)
        {
            return DetectarParedeAFrente(posicaoCorpo, direcaoX, plataformas, distanciaSensor, out _);
        }
    }
}
