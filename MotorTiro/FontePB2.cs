using System;
using System.IO;
using System.Numerics;
using Raylib_cs;

namespace JogoDeTiro
{
    public static class FontePB2
    {
        public static Font FontePadrao { get; private set; }
        private static bool carregada = false;

        public static void Inicializar()
        {
            if (carregada) return;

            // Busca fontes limpas e modernas do sistema Windows
            string[] caminhosFontes = new string[]
            {
                @"C:\Windows\Fonts\segoeui.ttf",     // Segoe UI (Moderna e limpa do Windows)
                @"C:\Windows\Fonts\segoeuib.ttf",    // Segoe UI Bold
                @"C:\Windows\Fonts\arial.ttf",       // Arial
                @"C:\Windows\Fonts\tahoma.ttf"      // Tahoma
            };

            string caminhoFinal = "";
            foreach (var caminho in caminhosFontes)
            {
                if (File.Exists(caminho))
                {
                    caminhoFinal = caminho;
                    break;
                }
            }

            if (!string.IsNullOrEmpty(caminhoFinal))
            {
                // Carrega 512 glifos (ASCII + Acentuação completa em português: ã, é, ç, etc.)
                int[] codepoints = new int[512];
                for (int i = 0; i < 512; i++) codepoints[i] = i;

                FontePadrao = Raylib.LoadFontEx(caminhoFinal, 36, codepoints, codepoints.Length);
                Raylib.SetTextureFilter(FontePadrao.Texture, TextureFilter.Bilinear);
                carregada = true;
            }
            else
            {
                FontePadrao = Raylib.GetFontDefault();
            }
        }

        // Desenha textos nítidos com antialiasing
        public static void DesenharTexto(string texto, float x, float y, float tamanho, Color cor)
        {
            if (!carregada) Inicializar();
            Raylib.DrawTextEx(FontePadrao, texto, new Vector2(x, y), tamanho, 1.0f, cor);
        }

        public static Vector2 MedirTexto(string texto, float tamanho)
        {
            if (!carregada) Inicializar();
            return Raylib.MeasureTextEx(FontePadrao, texto, tamanho, 1.0f);
        }

        public static void Descarregar()
        {
            if (carregada && FontePadrao.Texture.Id > 0)
            {
                Raylib.UnloadFont(FontePadrao);
                carregada = false;
            }
        }
    }
}
