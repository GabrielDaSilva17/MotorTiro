using System;
using System.Collections.Generic;
using System.Numerics;
using Raylib_cs;

namespace JogoDeTiro.BotIA
{
    public class BotPB2 : IDisposable
    {
        public Vector2 PosicaoSpawn;
        public Vector2 Posicao;
        public Vector2 Velocidade;
        public float DirecaoOlhar = 1.0f;
        public float AnguloMiraRad = 0.0f;
        public float AnguloMiraGraus = 0.0f;

        // Físicas
        public float VelocidadeMovimento = 280.0f;
        public float Aceleracao = 1800.0f;
        public float Friccao = 1500.0f;
        public float ForcaSalto = -480.0f;
        public float Gravidade = 1250.0f;
        public bool NoChao = true;

        // Saúde e Dano
        public float VidaMax = 100.0f;
        public float Vida = 100.0f;
        public bool Vivo => Vida > 0.0f;
        private float timerFlashDano = 0.0f;
        private float timerRespawn = 0.0f;

        // Agachamento e Animação
        public bool Agachado = false;
        private float transicaoAgachar = 0.0f;
        private float cicloAndar = 0.0f;
        private float bobbingTroncoY = 0.0f;

        // Comprimentos Anatômicos IK
        private const float ComprimentoCoxa = 19.5f;
        private const float ComprimentoCanela = 18.5f;
        private const float ComprimentoBraco = 14.5f;
        private const float ComprimentoAntebraco = 14.5f;

        // Armas e Inteligência
        public CerebroBot Cerebro { get; private set; }
        public List<ArmaPB2> Arsenal = new List<ArmaPB2>();
        public int ArmaAtualIndex = 0;
        public ArmaPB2? ArmaAtual => (Arsenal.Count > 0 && ArmaAtualIndex >= 0 && ArmaAtualIndex < Arsenal.Count) ? Arsenal[ArmaAtualIndex] : null;
        private float cooldownDisparo = 0.0f;

        // Sprites e Aparência (Sombreado Vermelho/Inimigo)
        public SkinModular Skin { get; set; }
        private readonly Color corInimigo = new Color(255, 175, 175, 255);
        private readonly Color corTraseira = new Color(180, 130, 130, 255);
        private readonly Color corArticulacao = new Color(40, 25, 25, 255);

        public Rectangle CaixaColisao => new Rectangle(
            Posicao.X - 14.0f,
            Posicao.Y - (Agachado ? 34.0f : 48.0f),
            28.0f,
            Agachado ? 34.0f : 48.0f
        );

        public BotPB2(Vector2 posicaoInicial, string diretorioAssets)
        {
            PosicaoSpawn = posicaoInicial;
            Posicao = posicaoInicial;
            Vida = VidaMax;

            Cerebro = new CerebroBot(posicaoInicial);
            Skin = SkinModular.CarregarPadrao(diretorioAssets);
            Arsenal = ArmaPB2.CarregarArsenalPadrao(diretorioAssets);
            ArmaAtualIndex = Arsenal.Count > 2 ? 2 : 0; // M4A1 inicial
        }

        public void Atualizar(
            float dt,
            Vector2 posJogador,
            Vector2 velJogador,
            List<ProjetilPlasma> tirosCenario,
            List<Rectangle> plataformas)
        {
            if (timerFlashDano > 0.0f) timerFlashDano -= dt;

            // Se morreu, conta tempo para renascer
            if (!Vivo)
            {
                timerRespawn -= dt;
                if (timerRespawn <= 0.0f)
                {
                    Renascer();
                }
                return;
            }

            // 1. Processa decisões da IA
            ComandoBot cmd = Cerebro.ProcessarDecisoes(
                Posicao,
                Velocidade,
                Vida,
                posJogador,
                velJogador,
                plataformas,
                dt
            );

            // 2. Movimento horizontal e física
            Agachado = cmd.Agachar && NoChao;
            float velMax = Agachado ? (VelocidadeMovimento * 0.45f) : VelocidadeMovimento;

            if (cmd.MoverX != 0.0f)
            {
                Velocidade.X = MoverEmDirecao(Velocidade.X, cmd.MoverX * velMax, Aceleracao * dt);
            }
            else
            {
                Velocidade.X = MoverEmDirecao(Velocidade.X, 0.0f, Friccao * dt);
            }

            // Pulo
            if (cmd.Pular && NoChao)
            {
                Velocidade.Y = ForcaSalto;
                NoChao = false;
            }

            // Gravidade e Resolução de Colisão
            Velocidade.Y += Gravidade * dt;
            NoChao = FisicaPlataforma.ResolverMovimentoEColisao(ref Posicao, ref Velocidade, plataformas, dt);

            // 3. Mira do Bot
            Vector2 ombro = ObterOmbro();
            Vector2 vetorMira = cmd.PontoMira - ombro;
            if (vetorMira.Length() > 10.0f)
            {
                DirecaoOlhar = vetorMira.X >= 0.0f ? 1.0f : -1.0f;
                AnguloMiraRad = MathF.Atan2(vetorMira.Y, vetorMira.X);
                AnguloMiraGraus = AnguloMiraRad * (180.0f / MathF.PI);
            }

            // 4. Disparo
            cooldownDisparo -= dt;
            if (cmd.Atirar && cooldownDisparo <= 0.0f && ArmaAtual != null)
            {
                Disparar(tirosCenario);
            }

            // 5. Animações de Pernas / Tronco
            float alvoAgachar = Agachado ? 1.0f : 0.0f;
            transicaoAgachar = Lerp(transicaoAgachar, alvoAgachar, 15.0f * dt);

            if (NoChao && MathF.Abs(Velocidade.X) > 10.0f)
            {
                cicloAndar += (Velocidade.X * DirecaoOlhar / VelocidadeMovimento) * dt * 11.0f;
                bobbingTroncoY = MathF.Abs(MathF.Cos(cicloAndar)) * 2.2f;
            }
            else
            {
                cicloAndar = Lerp(cicloAndar, 0.0f, 10.0f * dt);
                bobbingTroncoY = 0.0f;
            }

            // 6. Dano por Projéteis do Jogador
            Rectangle box = CaixaColisao;
            for (int i = 0; i < tirosCenario.Count; i++)
            {
                var tiro = tirosCenario[i];
                if (!tiro.Ativo) continue;

                if (Raylib.CheckCollisionPointRec(tiro.Posicao, box))
                {
                    tiro.Ativo = false;
                    ReceberDano(34.0f);
                }
            }
        }

        private void Disparar(List<ProjetilPlasma> tirosCenario)
        {
            if (ArmaAtual == null) return;

            cooldownDisparo = ArmaAtual.CadenciaTiro;
            Vector2 dirMira = new Vector2(MathF.Cos(AnguloMiraRad), MathF.Sin(AnguloMiraRad));
            Vector2 pontaCano = ObterPontaDoCano();

            tirosCenario.Add(new ProjetilPlasma(
                pontaCano,
                dirMira,
                ArmaAtual.VelocidadeProjetil,
                ArmaAtual.CorProjetil,
                ArmaAtual.Tipo
            ));
        }

        public void ReceberDano(float dano)
        {
            if (!Vivo) return;
            Vida = MathF.Max(0.0f, Vida - dano);
            timerFlashDano = 0.12f;

            if (Vida <= 0.0f)
            {
                timerRespawn = 4.0f;
            }
        }

        public void Renascer()
        {
            Posicao = PosicaoSpawn;
            Velocidade = Vector2.Zero;
            Vida = VidaMax;
            timerFlashDano = 0.0f;
        }

        public Vector2 ObterOmbro() => Posicao - new Vector2(0, 16.0f) + new Vector2(0, bobbingTroncoY + (transicaoAgachar * 15.0f));

        public Vector2 ObterPontaDoCano()
        {
            Vector2 ombro = ObterOmbro();
            Vector2 dir = new Vector2(MathF.Cos(AnguloMiraRad), MathF.Sin(AnguloMiraRad));
            return ombro + (dir * 28.0f);
        }

        public void Desenhar()
        {
            if (!Vivo)
            {
                FontePB2.DesenharTexto($"[BOT RENASCENDO EM {timerRespawn:0.0}s]", Posicao.X - 70, Posicao.Y - 50, 11, Color.Orange);
                return;
            }

            bool viradoEsquerda = DirecaoOlhar < 0;
            float orientacao = viradoEsquerda ? -1.0f : 1.0f;
            float offsetAgacharY = transicaoAgachar * 15.0f;

            Vector2 quadril = Posicao + new Vector2(0, bobbingTroncoY + offsetAgacharY);
            Vector2 ombro = ObterOmbro();

            Color corPintura = timerFlashDano > 0.0f ? Color.White : corInimigo;

            // 1. Perna Traseira
            DesenharPerna(quadril + new Vector2(orientacao * -2.5f, 0), cicloAndar + MathF.PI, orientacao, Skin.CoxaTras, Skin.CanelaTras, corTraseira);

            // 2. Tronco
            if (Skin.Tronco.Id > 0)
            {
                Rectangle src = new Rectangle(0, 0, viradoEsquerda ? -Skin.Tronco.Width : Skin.Tronco.Width, Skin.Tronco.Height);
                Rectangle dst = new Rectangle(quadril.X, quadril.Y - 10.0f, Skin.Tronco.Width * 0.5f, Skin.Tronco.Height * 0.5f);
                Raylib.DrawTexturePro(Skin.Tronco, src, dst, new Vector2(dst.Width * 0.5f, dst.Height * 0.5f), 0.0f, corPintura);
            }

            // 3. Cabeça
            if (Skin.Cabeca.Id > 0)
            {
                Rectangle src = new Rectangle(0, 0, viradoEsquerda ? -Skin.Cabeca.Width : Skin.Cabeca.Width, Skin.Cabeca.Height);
                Rectangle dst = new Rectangle(quadril.X, quadril.Y - 26.0f, Skin.Cabeca.Width * 0.5f, Skin.Cabeca.Height * 0.5f);
                Raylib.DrawTexturePro(Skin.Cabeca, src, dst, new Vector2(dst.Width * 0.5f, dst.Height * 0.75f), 0.0f, corPintura);
            }

            // 4. Perna Dianteira
            DesenharPerna(quadril + new Vector2(orientacao * 2.5f, 0), cicloAndar, orientacao, Skin.CoxaFrente, Skin.CanelaFrente, corPintura);

            // 5. Arma
            if (ArmaAtual != null && ArmaAtual.Textura.Id > 0)
            {
                Vector2 dir = new Vector2(MathF.Cos(AnguloMiraRad), MathF.Sin(AnguloMiraRad));
                Vector2 posArma = ombro + (dir * 14.0f);
                Rectangle src = new Rectangle(0, 0, ArmaAtual.Textura.Width, viradoEsquerda ? -ArmaAtual.Textura.Height : ArmaAtual.Textura.Height);
                Rectangle dst = new Rectangle(posArma.X, posArma.Y, ArmaAtual.Textura.Width * ArmaAtual.Escala, ArmaAtual.Textura.Height * ArmaAtual.Escala);
                Vector2 orig = new Vector2(dst.Width * ArmaAtual.PivotNormX, dst.Height * ArmaAtual.PivotNormY);
                Raylib.DrawTexturePro(ArmaAtual.Textura, src, dst, orig, AnguloMiraGraus, Color.White);
            }

            // 6. Barra de Vida e Estado Mental no topo
            DesenharBarraVidaEEstado();
        }

        private void DesenharPerna(Vector2 quadrilPerna, float fase, float orientacao, Texture2D texCoxa, Texture2D texCanela, Color tint)
        {
            float elevacaoY = MathF.Max(0.0f, -MathF.Cos(fase)) * 7.0f * (MathF.Abs(Velocidade.X) / VelocidadeMovimento);
            Vector2 alvoPe = new Vector2(quadrilPerna.X, Posicao.Y + 33.0f - elevacaoY);

            Vector2 joelho = ResolverIKPerna(quadrilPerna, alvoPe, ComprimentoCoxa, ComprimentoCanela, orientacao);

            DesenharSegmento(quadrilPerna, joelho, texCoxa, orientacao < 0, tint);
            Raylib.DrawCircleV(joelho, 3.2f, corArticulacao);
            DesenharSegmento(joelho, alvoPe, texCanela, orientacao < 0, tint);
            Raylib.DrawCircleV(alvoPe, 2.5f, corArticulacao);
        }

        private static Vector2 ResolverIKPerna(Vector2 inicio, Vector2 alvo, float l1, float l2, float orientacao)
        {
            Vector2 delta = alvo - inicio;
            float dist = Math.Clamp(delta.Length(), 1.0f, (l1 + l2) - 0.01f);
            float anguloDireto = MathF.Atan2(delta.Y, delta.X);
            float cosAlfa = (l1 * l1 + dist * dist - l2 * l2) / (2.0f * l1 * dist);
            float alfa = MathF.Acos(Math.Clamp(cosAlfa, -1.0f, 1.0f));

            float anguloSegmento1 = (orientacao >= 0.0f) ? anguloDireto - alfa : anguloDireto + alfa;
            return inicio + new Vector2(MathF.Cos(anguloSegmento1) * l1, MathF.Sin(anguloSegmento1) * l1);
        }

        private void DesenharSegmento(Vector2 pInicio, Vector2 pFim, Texture2D tex, bool viradoEsquerda, Color tint)
        {
            if (tex.Id == 0) return;
            Vector2 delta = pFim - pInicio;
            float dist = delta.Length();
            if (dist < 0.1f) return;

            float anguloDeg = MathF.Atan2(delta.Y, delta.X) * (180.0f / MathF.PI);
            float larg = tex.Width * 0.48f;
            float alt = dist + (7.0f * 0.48f);

            Rectangle src = new Rectangle(0, 0, viradoEsquerda ? -tex.Width : tex.Width, tex.Height);
            Rectangle dst = new Rectangle(pInicio.X, pInicio.Y, larg, alt);
            Raylib.DrawTexturePro(tex, src, dst, new Vector2(larg * 0.5f, 4.0f * 0.48f), anguloDeg - 90.0f, tint);
        }

        private void DesenharBarraVidaEEstado()
        {
            float largBarra = 40.0f;
            float barraX = Posicao.X - (largBarra * 0.5f);
            float barraY = Posicao.Y - (Agachado ? 42.0f : 56.0f);
            float pct = Math.Clamp(Vida / VidaMax, 0.0f, 1.0f);

            Raylib.DrawRectangle((int)barraX - 1, (int)barraY - 1, (int)largBarra + 2, 6, new Color(20, 20, 25, 230));
            Color corVida = pct > 0.35f ? new Color(239, 68, 68, 255) : new Color(245, 158, 11, 255);
            Raylib.DrawRectangle((int)barraX, (int)barraY, (int)(largBarra * pct), 4, corVida);

            string textoEstado = $"[{Cerebro.EstadoAtual.ToString().ToUpper()}]";
            Color corTexto = Cerebro.EstadoAtual switch
            {
                EstadoCombate.Engajar => Color.Red,
                EstadoCombate.Alerta => Color.Yellow,
                EstadoCombate.Recuar => Color.Orange,
                _ => Color.LightGray
            };
            FontePB2.DesenharTexto(textoEstado, barraX - 8, barraY - 14, 10, corTexto);
        }

        private static float Lerp(float a, float b, float t) => a + (b - a) * Math.Clamp(t, 0.0f, 1.0f);
        private static float MoverEmDirecao(float atual, float alvo, float maxDelta)
        {
            if (MathF.Abs(alvo - atual) <= maxDelta) return alvo;
            return atual + MathF.Sign(alvo - atual) * maxDelta;
        }

        public void Dispose()
        {
            Skin?.Dispose();
            foreach (var arma in Arsenal) arma.Dispose();
        }
    }
}