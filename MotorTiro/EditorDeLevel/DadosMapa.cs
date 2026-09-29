using System.Collections.Generic;
using System.Numerics;

namespace JogoDeTiro.EditorDeLevel
{
    public class ParedeData
    {
        public float X { get; set; }
        public float Y { get; set; }
        public float Largura { get; set; }
        public float Altura { get; set; }
    }

    // Estrutura dedicada para os pontos de spawn de inimigos
    public class InimigoSpawnData
    {
        public float X { get; set; }
        public float Y { get; set; }
        public string Tipo { get; set; } = "droide_teste";

        public InimigoSpawnData() { }
        public InimigoSpawnData(float x, float y)
        {
            X = x;
            Y = y;
        }

        public Vector2 ToVector2() => new Vector2(X, Y);
    }

    public class MapaData
    {
        public string NomeMapa { get; set; } = "Fase 1 - PB2";
        
        // Coordenadas diretas do Player
        public float SpawnPlayerX { get; set; } = 250.0f;
        public float SpawnPlayerY { get; set; } = 380.0f;

        public List<ParedeData> Paredes { get; set; } = new List<ParedeData>();
        public List<InimigoSpawnData> SpawnsInimigos { get; set; } = new List<InimigoSpawnData>();

        // Propriedade auxiliar para facilitar o uso no código
        public Vector2 PontoSpawnPlayer
        {
            get => new Vector2(SpawnPlayerX, SpawnPlayerY);
            set
            {
                SpawnPlayerX = value.X;
                SpawnPlayerY = value.Y;
            }
        }
    }
}