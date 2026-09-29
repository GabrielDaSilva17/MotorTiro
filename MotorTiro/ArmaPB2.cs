using System;
using System.Collections.Generic;
using System.IO;
using Raylib_cs;

namespace JogoDeTiro
{
    // Define uma arma que pode ser empunhada e disparada pelo personagem
    public class ArmaPB2 : IDisposable
    {
        public string Nome { get; set; } = "Arma";
        public string Tipo { get; set; } = "rifle";
        public Texture2D Textura { get; set; }
        public Sound SomTiro { get; set; }
        public float CadenciaTiro { get; set; } = 0.12f; // Segundos entre disparos
        public bool Automatica { get; set; } = true;
        public int QuantidadePellets { get; set; } = 1;
        public float Dispersao { get; set; } = 0.04f;
        public float ForcaRecuo { get; set; } = 8.0f;
        public float Escala { get; set; } = 0.85f;
        public float DistanciaOmbro { get; set; } = 18.0f; // Distância do ombro ao centro/pivô da arma
        public float PivotNormX { get; set; } = 0.25f;     // Posição horizontal do pivô na textura (0.0 a 1.0)
        public float PivotNormY { get; set; } = 0.50f;     // Posição vertical do pivô na textura (0.0 a 1.0)
        public float OffsetGatilhoX { get; set; } = 0.0f;  // Posição da mão principal ao longo do cano
        public float OffsetGatilhoY { get; set; } = 3.0f;  // Posição vertical da mão principal
        public float OffsetSuporteX { get; set; } = 21.0f; // Posição da mão de apoio ao longo do cano
        public float OffsetSuporteY { get; set; } = 1.0f;  // Posição vertical da mão de apoio
        public float ComprimentoCano { get; set; } = 34.0f; // Distância do pivô da arma até a ponta do cano
        public float AlturaCanoY { get; set; } = 0.0f;      // Deslocamento vertical do cano
        public float DistanciaCano { get; set; } = 42.0f;
        public Color CorProjetil { get; set; } = new Color(0, 220, 255, 255);
        public float VelocidadeProjetil { get; set; } = 950.0f;

        // Animações PB2, Sistema de Munição, Recuo Angular e Físico
        public int CapacidadePente { get; set; } = 30;
        public int MunicaoAtual { get; set; } = 30;
        public float TempoRecarga { get; set; } = 1.3f;
        public float RecuoLinear { get; set; } = 8.0f;       // Coice para trás (pixels)
        public float RecuoRotacaoGraus { get; set; } = 10.0f; // Elevação do cano (muzzle climb em graus)
        public float ImpulsoFisico { get; set; } = 45.0f;    // Empurrão físico corporal PB2 (self-boosting)
        public float TraumaCamera { get; set; } = 0.25f;     // Intensidade do tremor de tela (screen shake)
        public string TipoCapsula { get; set; } = "bala";    // "bala", "shotgun", "plasma", "railgun"

        // Sistema de Carga / Reciclagem Eletrônica (Sniper e Armas Pesadas PB2)
        public bool RequerCarga { get; set; } = false;
        public float TempoCarga { get; set; } = 1.25f;       // Tempo de ciclo de recarga/carga
        public float CargaAtual { get; set; } = 1.25f;       // 0 a TempoCarga
        public bool ProntoParaAtirar => !RequerCarga || CargaAtual >= TempoCarga;
        public Sound SomCarga { get; set; }
        public float PosDiodeNormX { get; set; } = 0.36f;    // Posição normalizada do LED no sprite
        public float PosDiodeNormY { get; set; } = 0.515f;
        public bool TemLaser { get; set; } = false;
        public Color CorLaser { get; set; } = new Color(255, 120, 20, 200);
        public Texture2D TexturaGlow { get; set; }
        public Texture2D TexturaLedOn { get; set; }
        public Texture2D TexturaLedOff { get; set; }
        public List<Texture2D> FramesAnimacao { get; set; } = new List<Texture2D>();

        public static List<ArmaPB2> CarregarArsenalPadrao(string diretorioAssets)
        {
            List<ArmaPB2> arsenal = new List<ArmaPB2>();
            string pastaArmas = Path.Combine(diretorioAssets, "sprites", "weapons");
            string pastaSons = Path.Combine(diretorioAssets, "sounds");

            // 1. Pistola Compacta Glock (DefineSprite 2664 - Arma secundária oficial PB2)
            arsenal.Add(new ArmaPB2
            {
                Nome = "Glock Compact (DS-2664)",
                Tipo = "pistol",
                Textura = CarregarTexturaSegura(Path.Combine(pastaArmas, "gun_pistol3.png")),
                SomTiro = CarregarSomSeguro(Path.Combine(pastaSons, "102_wea_apistol.mp3")),
                CadenciaTiro = 0.16f,
                Automatica = false,
                QuantidadePellets = 1,
                Dispersao = 0.012f,
                ForcaRecuo = 5.0f,
                Escala = 0.95f,
                // DistanciaOmbro: braco(14.5) + antebraco(14.5) = 29px de alcance
                // +6px de folga para a pistola ficar na ponta da mao (empunhadura)
                DistanciaOmbro = 37.0f,     // Empurra a pistola para a frente, esticando o braço até a mão
                OffsetGatilhoX = -9.0f,      // Mão principal segura no cabo/gatilho
                OffsetGatilhoY = 2.0f,
                OffsetSuporteX = -4.5f,      // Mão de apoio fica JUNTO da mão principal (não esticada lá na frente)
                OffsetSuporteY = 5.0f,
                ComprimentoCano = 22.0f,
                AlturaCanoY = 0.0f,
                DistanciaCano = 38.0f,
                CorProjetil = new Color((byte)255, (byte)215, (byte)60, (byte)255),
                VelocidadeProjetil = 1150.0f,
                CapacidadePente = 15,
                MunicaoAtual = 15,
                TempoRecarga = 0.90f,
                RecuoLinear = 5.5f,
                RecuoRotacaoGraus = 14.0f,
                ImpulsoFisico = 28.0f,
                TraumaCamera = 0.15f,
                TipoCapsula = "bala"
            });

            // 2. Pistola Falcon com Mira Laser (DefineSprite 2669 - Pistola com trilho laser vermelho)
            arsenal.Add(new ArmaPB2
            {
                Nome = "Falcon Laser Pistol (DS-2669)",
                Tipo = "pistol",
                Textura = CarregarTexturaSegura(Path.Combine(pastaArmas, "gun_pistol2.png")),
                SomTiro = CarregarSomSeguro(Path.Combine(pastaSons, "102_wea_apistol.mp3")),
                CadenciaTiro = 0.18f,
                Automatica = false,
                QuantidadePellets = 1,
                Dispersao = 0.008f,
                ForcaRecuo = 5.5f,
                Escala = 0.92f,
                // Mesmo alcance do braço + 7px de folga para a empunhadura da Falcon
                DistanciaOmbro = 37.0f,
                PivotNormX = 0.33f,
                PivotNormY = 0.58f,
                OffsetGatilhoX = -19.5f,
                OffsetGatilhoY = 2.5f,
                OffsetSuporteX = -12.0f,
                OffsetSuporteY = 2.5f,
                ComprimentoCano = 26.0f,
                AlturaCanoY = 0.0f,
                DistanciaCano = 42.0f,
                CorProjetil = new Color((byte)255, (byte)205, (byte)55, (byte)255),
                VelocidadeProjetil = 1200.0f,
                CapacidadePente = 12,
                MunicaoAtual = 12,
                TempoRecarga = 0.95f,
                RecuoLinear = 6.0f,
                RecuoRotacaoGraus = 15.0f,
                ImpulsoFisico = 32.0f,
                TraumaCamera = 0.18f,
                TipoCapsula = "bala",
                TemLaser = true,
                CorLaser = new Color((byte)255, (byte)35, (byte)35, (byte)220) // Feixe laser vermelho tático (DefineSprite 2669)
            });

            // 3. Fuzil de Assalto Militar M4A1 (DefineSprite 2739 - Arma Oficial PB2 balística militar)
            arsenal.Add(new ArmaPB2
            {
                Nome = "M4A1 Assault Rifle (DS-2739)",
                Tipo = "m4a1",
                Textura = CarregarTexturaSegura(Path.Combine(pastaArmas, "gun_m4a1.png")),
                SomTiro = CarregarSomSeguro(Path.Combine(pastaSons, "70_wea_rifle.mp3")),
                CadenciaTiro = 0.088f,
                Automatica = true,
                QuantidadePellets = 1,
                Dispersao = 0.020f,
                ForcaRecuo = 6.0f,
                Escala = 0.85f,
                DistanciaOmbro = 22.0f,
                PivotNormX = 0.22f,
                PivotNormY = 0.50f,
                OffsetGatilhoX = 0.0f,
                OffsetGatilhoY = 3.0f,
                OffsetSuporteX = 16.0f,
                OffsetSuporteY = 1.5f,
                ComprimentoCano = 32.0f,
                AlturaCanoY = -1.0f,
                DistanciaCano = 46.0f,
                CorProjetil = new Color((byte)255, (byte)215, (byte)60, (byte)255),
                VelocidadeProjetil = 1250.0f,
                CapacidadePente = 30,
                MunicaoAtual = 30,
                TempoRecarga = 1.35f,
                RecuoLinear = 6.0f,
                RecuoRotacaoGraus = 7.5f,
                ImpulsoFisico = 38.0f,
                TraumaCamera = 0.16f,
                TipoCapsula = "bala"
            });

            // 4. Canhão Pesado / Minigun (DefineSprite 2745 - Arma pesada de disparo rápido PB2)
            arsenal.Add(new ArmaPB2
            {
                Nome = "Heavy Minigun Cannon (DS-2745)",
                Tipo = "heavy_cannon",
                Textura = CarregarTexturaSegura(Path.Combine(pastaArmas, "gun_heavy_cannon.png")),
                SomTiro = CarregarSomSeguro(Path.Combine(pastaSons, "70_wea_rifle.mp3")),
                CadenciaTiro = 0.065f,
                Automatica = true,
                QuantidadePellets = 1,
                Dispersao = 0.035f,
                ForcaRecuo = 7.5f,
                Escala = 0.82f,
                DistanciaOmbro = 21.0f,
                PivotNormX = 0.23f,
                PivotNormY = 0.52f,
                OffsetGatilhoX = 0.0f,
                OffsetGatilhoY = 4.0f,
                OffsetSuporteX = 19.0f,
                OffsetSuporteY = 2.0f,
                ComprimentoCano = 40.0f,
                AlturaCanoY = -1.0f,
                DistanciaCano = 52.0f,
                CorProjetil = new Color((byte)255, (byte)190, (byte)45, (byte)255),
                VelocidadeProjetil = 1350.0f,
                CapacidadePente = 100,
                MunicaoAtual = 100,
                TempoRecarga = 2.2f,
                RecuoLinear = 7.0f,
                RecuoRotacaoGraus = 6.0f,
                ImpulsoFisico = 0.8f,
                TraumaCamera = 0.22f,
                TipoCapsula = "bala"
            });

            // 3. Fuzil de Plasma C-01p (Arma clássica PB2 - disparo automático contínuo de alta cadência)
            arsenal.Add(new ArmaPB2
            {
                Nome = "C-01p Plasma Rifle",
                Tipo = "plasma",
                Textura = CarregarTexturaSegura(Path.Combine(pastaArmas, "gun_plasma.png")),
                SomTiro = CarregarSomSeguro(Path.Combine(pastaSons, "70_wea_rifle.mp3")),
                CadenciaTiro = 0.10f,
                Automatica = true,
                QuantidadePellets = 1,
                Dispersao = 0.035f,
                ForcaRecuo = 7.0f,
                Escala = 0.85f,
                DistanciaOmbro = 20.0f,
                PivotNormX = 0.31f,
                PivotNormY = 0.44f,
                OffsetGatilhoX = 0.0f,
                OffsetGatilhoY = 3.0f,
                OffsetSuporteX = 18.0f,
                OffsetSuporteY = 1.5f,
                ComprimentoCano = 28.0f,
                AlturaCanoY = -2.0f,
                DistanciaCano = 44.0f,
                CorProjetil = new Color(0, 225, 255, 255),
                VelocidadeProjetil = 980.0f,
                CapacidadePente = 35,
                MunicaoAtual = 35,
                TempoRecarga = 1.25f,
                RecuoLinear = 5.5f,
                RecuoRotacaoGraus = 6.5f,
                ImpulsoFisico = 35.0f,
                TraumaCamera = 0.15f,
                TipoCapsula = "plasma"
            });

            // 3. Escopeta CS-DAZ (Disparo devastador com alto coice de recuo)
            arsenal.Add(new ArmaPB2
            {
                Nome = "CS-DAZ Shotgun",
                Tipo = "shotgun",
                Textura = CarregarTexturaSegura(Path.Combine(pastaArmas, "gun_shotgun.png")),
                SomTiro = CarregarSomSeguro(Path.Combine(pastaSons, "66_wea_shotgun.mp3")),
                CadenciaTiro = 0.50f,
                Automatica = false,
                QuantidadePellets = 6,
                Dispersao = 0.20f,
                ForcaRecuo = 14.0f,
                Escala = 0.85f,
                DistanciaOmbro = 20.0f,
                PivotNormX = 0.29f,
                PivotNormY = 0.50f,
                OffsetGatilhoX = 0.0f,
                OffsetGatilhoY = 3.0f,
                OffsetSuporteX = 18.0f,
                OffsetSuporteY = 1.5f,
                ComprimentoCano = 29.5f,
                AlturaCanoY = -1.5f,
                DistanciaCano = 44.0f,
                CorProjetil = new Color(255, 130, 30, 255),
                VelocidadeProjetil = 880.0f,
                CapacidadePente = 6,
                MunicaoAtual = 6,
                TempoRecarga = 1.6f,
                RecuoLinear = 15.0f,
                RecuoRotacaoGraus = 22.0f,
                ImpulsoFisico = 175.0f, // Permite "shotgun jump"
                TraumaCamera = 0.55f,
                TipoCapsula = "shotgun"
            });

            // 4. Canhão de Raios Ray-01 (Railgun de altíssima energia e impulso massivo)
            arsenal.Add(new ArmaPB2
            {
                Nome = "Ray-01 Heavy Railgun",
                Tipo = "railgun",
                Textura = CarregarTexturaSegura(Path.Combine(pastaArmas, "gun_railgun.png")),
                SomTiro = CarregarSomSeguro(Path.Combine(pastaSons, "208_wea_railgun.mp3")),
                CadenciaTiro = 0.85f,
                Automatica = false,
                QuantidadePellets = 1,
                Dispersao = 0.0f,
                ForcaRecuo = 18.0f,
                Escala = 0.88f,
                DistanciaOmbro = 19.0f,
                PivotNormX = 0.31f,
                PivotNormY = 0.50f,
                OffsetGatilhoX = 0.0f,
                OffsetGatilhoY = 3.0f,
                OffsetSuporteX = 16.0f,
                OffsetSuporteY = 1.5f,
                ComprimentoCano = 23.5f,
                AlturaCanoY = 0.0f,
                DistanciaCano = 42.0f,
                CorProjetil = new Color(195, 60, 255, 255),
                VelocidadeProjetil = 1650.0f,
                CapacidadePente = 4,
                MunicaoAtual = 4,
                TempoRecarga = 1.85f,
                RecuoLinear = 18.0f,
                RecuoRotacaoGraus = 26.0f,
                ImpulsoFisico = 300.0f, // Lendário railgun booster do PB2!
                TraumaCamera = 0.80f,
                TipoCapsula = "railgun"
            });

            // 5. Rifle Sniper CS-BNG (DefineSprite 720 - ciclo de carga com LED e laser de alta precisão)
            arsenal.Add(new ArmaPB2
            {
                Nome = "CS-BNG Sniper Rifle",
                Tipo = "sniper",
                Textura = CarregarTexturaSegura(Path.Combine(pastaArmas, "gun_sniper.png")),
                SomTiro = CarregarSomSeguro(Path.Combine(pastaSons, "64_wea_sniper.mp3")),
                SomCarga = CarregarSomSeguro(Path.Combine(pastaSons, "484_beam5_recharge.mp3")),
                CadenciaTiro = 1.20f,
                RequerCarga = true,
                TempoCarga = 1.20f,
                CargaAtual = 1.20f,
                Automatica = false,
                QuantidadePellets = 1,
                Dispersao = 0.0f, // Precisão cirúrgica de longa distância
                ForcaRecuo = 18.0f,
                Escala = 0.85f,
                DistanciaOmbro = 30.5f,
                PivotNormX = 0.2605f,
                PivotNormY = 0.515f,
                OffsetGatilhoX = 0.0f,
                OffsetGatilhoY = 4.5f,
                OffsetSuporteX = 20.0f,
                OffsetSuporteY = 2.0f,
                ComprimentoCano = 43.5f,
                AlturaCanoY = 0.0f,
                DistanciaCano = 58.0f,
                CorProjetil = new Color(0, 235, 255, 255), // Feixe hiperveloz ciano/branco
                VelocidadeProjetil = 2400.0f,
                CapacidadePente = 5,
                MunicaoAtual = 5,
                TempoRecarga = 1.8f,
                RecuoLinear = 17.0f,
                RecuoRotacaoGraus = 19.0f,
                ImpulsoFisico = 150.0f,
                TraumaCamera = 0.65f,
                TipoCapsula = "bala",
                PosDiodeNormX = 0.359f,
                PosDiodeNormY = 0.515f,
                TemLaser = true,
                CorLaser = new Color(255, 120, 20, 200), // Laser alaranjado PB2
                TexturaGlow = CarregarTexturaSegura(Path.Combine(pastaArmas, "sniper_glow.png")),
                TexturaLedOn = CarregarTexturaSegura(Path.Combine(pastaArmas, "sniper_led_on.png")),
                TexturaLedOff = CarregarTexturaSegura(Path.Combine(pastaArmas, "sniper_led_off.png")),
                FramesAnimacao = CarregarFramesAnimacao(Path.Combine(pastaArmas, "sniper_frames"), 60)
            });

            // 6. Fuzil Sniper Android CS-ASR (DefineSprite 2704 - similar à sniper com feixe penetrante carmesim)
            arsenal.Add(new ArmaPB2
            {
                Nome = "CS-ASR Android Sniper",
                Tipo = "android_sniper",
                Textura = CarregarTexturaSegura(Path.Combine(pastaArmas, "gun_android_sniper.png")),
                SomTiro = CarregarSomSeguro(Path.Combine(pastaSons, "103_wea_android_sniper.mp3")),
                SomCarga = CarregarSomSeguro(Path.Combine(pastaSons, "487_beam1_recharge.mp3")),
                CadenciaTiro = 0.95f,
                RequerCarga = true,
                TempoCarga = 0.95f,
                CargaAtual = 0.95f,
                Automatica = false,
                QuantidadePellets = 1,
                Dispersao = 0.0f,
                ForcaRecuo = 15.0f,
                Escala = 0.85f,
                DistanciaOmbro = 22.5f,
                PivotNormX = 0.1793f,
                PivotNormY = 0.424f,
                OffsetGatilhoX = 0.0f,
                OffsetGatilhoY = 4.5f,
                OffsetSuporteX = 21.0f,
                OffsetSuporteY = 2.5f,
                ComprimentoCano = 48.0f,
                AlturaCanoY = 0.0f,
                DistanciaCano = 52.0f,
                CorProjetil = new Color(255, 60, 40, 255), // Feixe de energia carmesim android
                VelocidadeProjetil = 2150.0f,
                CapacidadePente = 6,
                MunicaoAtual = 6,
                TempoRecarga = 1.6f,
                RecuoLinear = 14.0f,
                RecuoRotacaoGraus = 16.0f,
                ImpulsoFisico = 135.0f,
                TraumaCamera = 0.52f,
                TipoCapsula = "plasma",
                PosDiodeNormX = 0.224f,
                PosDiodeNormY = 0.59f,
                TemLaser = true,
                CorLaser = new Color(255, 45, 45, 200), // Laser rubi android
                TexturaGlow = CarregarTexturaSegura(Path.Combine(pastaArmas, "sniper_glow.png")),
                TexturaLedOn = CarregarTexturaSegura(Path.Combine(pastaArmas, "sniper_led_on.png")),
                TexturaLedOff = CarregarTexturaSegura(Path.Combine(pastaArmas, "sniper_led_off.png")),
                FramesAnimacao = CarregarFramesAnimacao(Path.Combine(pastaArmas, "android_sniper_frames"), 30)
            });

            return arsenal;
        }

        private static List<Texture2D> CarregarFramesAnimacao(string pastaFrames, int totalFrames)
        {
            List<Texture2D> lista = new List<Texture2D>();
            if (!Directory.Exists(pastaFrames)) return lista;

            for (int i = 1; i <= totalFrames; i++)
            {
                string arq = Path.Combine(pastaFrames, $"{i}.png");
                if (File.Exists(arq))
                {
                    Texture2D tex = Raylib.LoadTexture(arq);
                    Raylib.SetTextureFilter(tex, TextureFilter.Bilinear);
                    lista.Add(tex);
                }
            }
            return lista;
        }

        private static Texture2D CarregarTexturaSegura(string caminho)
        {
            if (File.Exists(caminho))
            {
                Texture2D tex = Raylib.LoadTexture(caminho);
                Raylib.SetTextureFilter(tex, TextureFilter.Bilinear);
                return tex;
            }
            Console.WriteLine($"[AVISO] Textura de arma não encontrada: {caminho}");
            return default;
        }

        private static Sound CarregarSomSeguro(string caminho)
        {
            if (File.Exists(caminho))
            {
                return Raylib.LoadSound(caminho);
            }
            Console.WriteLine($"[AVISO] Som de arma não encontrado: {caminho}");
            return default;
        }

        public void Dispose()
        {
            if (Textura.Id != 0) Raylib.UnloadTexture(Textura);
            if (SomTiro.Stream.Buffer != IntPtr.Zero) Raylib.UnloadSound(SomTiro);
            if (SomCarga.Stream.Buffer != IntPtr.Zero) Raylib.UnloadSound(SomCarga);
            if (TexturaGlow.Id != 0) Raylib.UnloadTexture(TexturaGlow);
            if (TexturaLedOn.Id != 0) Raylib.UnloadTexture(TexturaLedOn);
            if (TexturaLedOff.Id != 0) Raylib.UnloadTexture(TexturaLedOff);
            foreach (var frame in FramesAnimacao)
            {
                if (frame.Id != 0) Raylib.UnloadTexture(frame);
            }
            FramesAnimacao.Clear();
        }
    }
}
