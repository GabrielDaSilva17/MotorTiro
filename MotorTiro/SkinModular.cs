using System;
using System.IO;
using Raylib_cs;

namespace JogoDeTiro
{
    // Representa o conjunto modular de sprites que compõem a aparência do personagem.
    // Agora totalmente segmentado para suportar Cinemática Inversa (IK):
    // Cabeça, tronco, coxas, canelas, braços superiores e antebraços.
    public class SkinModular : IDisposable
    {
        public Texture2D Cabeca;
        public Texture2D Tronco;

        // Membros inferiores articulados (Coxa + Canela)
        public Texture2D CoxaFrente;
        public Texture2D CanelaFrente;
        public Texture2D CoxaTras;
        public Texture2D CanelaTras;

        // Membros superiores articulados (Braço + Antebraço)
        public Texture2D BracoFrente;
        public Texture2D AntebracoFrente;
        public Texture2D BracoTras;
        public Texture2D AntebracoTras;

        public bool Carregada { get; private set; } = false;

        public static SkinModular CarregarPadrao(string diretorioAssets)
        {
            SkinModular skin = new SkinModular();
            string pastaStickman = Path.Combine(diretorioAssets, "sprites", "stickman");

            skin.Cabeca = CarregarTexturaSegura(Path.Combine(pastaStickman, "head.png"));
            skin.Tronco = CarregarTexturaSegura(Path.Combine(pastaStickman, "torso.png"));

            // Pernas segmentadas (permitem flexão realista de joelho)
            skin.CoxaFrente = CarregarOuRecortarTextura(
                Path.Combine(pastaStickman, "thigh_front.png"),
                Path.Combine(pastaStickman, "leg_front.png"),
                0, 0, 40, 52
            );
            skin.CanelaFrente = CarregarOuRecortarTextura(
                Path.Combine(pastaStickman, "shin_front.png"),
                Path.Combine(pastaStickman, "leg_front.png"),
                0, 44, 40, 52
            );

            skin.CoxaTras = CarregarOuRecortarTextura(
                Path.Combine(pastaStickman, "thigh_back.png"),
                Path.Combine(pastaStickman, "leg_back.png"),
                0, 0, 40, 52
            );
            skin.CanelaTras = CarregarOuRecortarTextura(
                Path.Combine(pastaStickman, "shin_back.png"),
                Path.Combine(pastaStickman, "leg_back.png"),
                0, 44, 40, 52
            );

            // Braços segmentados (permitem flexão realista de cotovelo)
            skin.BracoFrente = CarregarOuRecortarTextura(
                Path.Combine(pastaStickman, "arm_upper_front.png"),
                Path.Combine(pastaStickman, "arm_front.png"),
                0, 0, 36, 44
            );
            skin.AntebracoFrente = CarregarOuRecortarTextura(
                Path.Combine(pastaStickman, "forearm_front.png"),
                Path.Combine(pastaStickman, "arm_front.png"),
                0, 36, 36, 42
            );

            skin.BracoTras = CarregarOuRecortarTextura(
                Path.Combine(pastaStickman, "arm_upper_back.png"),
                Path.Combine(pastaStickman, "arm_back.png"),
                0, 0, 36, 44
            );
            skin.AntebracoTras = CarregarOuRecortarTextura(
                Path.Combine(pastaStickman, "forearm_back.png"),
                Path.Combine(pastaStickman, "arm_back.png"),
                0, 36, 36, 42
            );

            skin.Carregada = true;
            return skin;
        }

        // Permite trocar o tronco em tempo de execução
        public void TrocarTronco(string caminhoNovaTextura)
        {
            if (File.Exists(caminhoNovaTextura))
            {
                if (Tronco.Id != 0) Raylib.UnloadTexture(Tronco);
                Tronco = Raylib.LoadTexture(caminhoNovaTextura);
                Raylib.SetTextureFilter(Tronco, TextureFilter.Bilinear);
            }
        }

        // Permite trocar a cabeça em tempo de execução
        public void TrocarCabeca(string caminhoNovaTextura)
        {
            if (File.Exists(caminhoNovaTextura))
            {
                if (Cabeca.Id != 0) Raylib.UnloadTexture(Cabeca);
                Cabeca = Raylib.LoadTexture(caminhoNovaTextura);
                Raylib.SetTextureFilter(Cabeca, TextureFilter.Bilinear);
            }
        }

        private static Texture2D CarregarOuRecortarTextura(string caminhoSegmento, string caminhoOriginal, int x, int y, int largura, int altura)
        {
            if (File.Exists(caminhoSegmento))
            {
                return CarregarTexturaSegura(caminhoSegmento);
            }

            if (File.Exists(caminhoOriginal))
            {
                Image imgCompleta = Raylib.LoadImage(caminhoOriginal);
                Rectangle cropRec = new Rectangle(x, y, largura, Math.Min(altura, imgCompleta.Height - y));
                Raylib.ImageCrop(ref imgCompleta, cropRec);
                Texture2D tex = Raylib.LoadTextureFromImage(imgCompleta);
                Raylib.UnloadImage(imgCompleta);
                Raylib.SetTextureFilter(tex, TextureFilter.Bilinear);
                return tex;
            }

            return default;
        }

        private static Texture2D CarregarTexturaSegura(string caminho)
        {
            if (File.Exists(caminho))
            {
                Texture2D tex = Raylib.LoadTexture(caminho);
                Raylib.SetTextureFilter(tex, TextureFilter.Bilinear);
                return tex;
            }
            Console.WriteLine($"[AVISO] Textura não encontrada: {caminho}");
            return default;
        }

        public void Dispose()
        {
            DescarregarTextura(ref Cabeca);
            DescarregarTextura(ref Tronco);
            DescarregarTextura(ref CoxaFrente);
            DescarregarTextura(ref CanelaFrente);
            DescarregarTextura(ref CoxaTras);
            DescarregarTextura(ref CanelaTras);
            DescarregarTextura(ref BracoFrente);
            DescarregarTextura(ref AntebracoFrente);
            DescarregarTextura(ref BracoTras);
            DescarregarTextura(ref AntebracoTras);
            Carregada = false;
        }

        private static void DescarregarTextura(ref Texture2D tex)
        {
            if (tex.Id != 0)
            {
                Raylib.UnloadTexture(tex);
                tex = default;
            }
        }
    }
}
