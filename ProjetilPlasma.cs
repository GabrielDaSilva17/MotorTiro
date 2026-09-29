using System.Numerics;
using Raylib_cs;

namespace JogoDeTiro
{
    // Projétil disparado pelas armas do personagem
    public class ProjetilPlasma
    {
        public Vector2 Posicao;
        public Vector2 Velocidade;
        public bool Ativo = true;
        public Color Cor = new Color(0, 220, 255, 255);
        public string TipoArma = "plasma";
        public bool DoJogador = true;
        private float tempoVida = 2.5f;
        private Vector2 posicaoOrigem;

        public ProjetilPlasma(Vector2 posicao, Vector2 direcao, float velocidade = 950.0f, Color? cor = null, string tipo = "plasma", bool doJogador = true)
        {
            Posicao = posicao;
            posicaoOrigem = posicao;
            Velocidade = direcao * velocidade;
            TipoArma = tipo;
            DoJogador = doJogador;
            if (cor.HasValue) Cor = cor.Value;
        }

        public void Atualizar(float dt)
        {
            Posicao += Velocidade * dt;
            tempoVida -= dt;

            // Desativa se o tempo de vida expirar (2.5s) ou se viajar mais de 3500 pixels da origem
            if (tempoVida <= 0.0f || Vector2.Distance(Posicao, posicaoOrigem) > 3500.0f)
            {
                Ativo = false;
            }
        }

        public void Desenhar()
        {
            if (Velocidade.LengthSquared() < 0.1f) return;

            Vector2 dir = Vector2.Normalize(Velocidade);
            float comprimentoRastro = TipoArma switch
            {
                "sniper" => 85.0f,
                "android_sniper" => 75.0f,
                "railgun" => 36.0f,
                "shotgun" => 12.0f,
                "pistol" => 14.0f,
                _ => 20.0f
            };

            Vector2 cauda = Posicao - (dir * comprimentoRastro);

            if (TipoArma == "sniper" || TipoArma == "android_sniper")
            {
                // Traçante de sniper hipersônico PB2: feixe fino de altíssima energia penetrante
                float espessuraHalo = TipoArma == "sniper" ? 5.5f : 5.0f;
                // Feixe externo com halo de choque
                Raylib.DrawLineEx(cauda, Posicao, espessuraHalo, new Color(Cor.R, Cor.G, Cor.B, (byte)160));
                // Núcleo branco puro de plasma concentrado
                Raylib.DrawLineEx(cauda, Posicao, 2.2f, Color.White);

                // Ponta do projétil: clarão pontual em agulha estelar
                Raylib.DrawCircleV(Posicao, 3.5f, Color.White);
                Raylib.DrawCircleV(Posicao, 7.5f, new Color(Cor.R, Cor.G, Cor.B, (byte)180));
                Raylib.DrawCircleV(Posicao, 12.0f, new Color(Cor.R, Cor.G, Cor.B, (byte)60));
            }
            else
            {
                // Rastro brilhante padrão de tiros de energia
                float espessura = TipoArma == "railgun" ? 6.0f : 4.0f;
                Raylib.DrawLineEx(cauda, Posicao, espessura, Cor);
                Raylib.DrawLineEx(cauda, Posicao, 1.8f, Color.White);

                // Núcleo brilhante na ponta do feixe
                Raylib.DrawCircleV(Posicao, TipoArma == "railgun" ? 4.0f : 3.0f, Color.White);
                Raylib.DrawCircleV(Posicao, TipoArma == "railgun" ? 7.0f : 5.5f, new Color(Cor.R, Cor.G, Cor.B, (byte)140));
            }
        }
    }
}