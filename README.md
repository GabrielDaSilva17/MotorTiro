# Plazma Burst 2 - Motor de Tiro & Campanha Tática (C# / Raylib)

Uma recriação fiel e moderna do clássico jogo de tiro em plataforma **Plazma Burst 2 (PB2)**, construída em **C#** utilizando a biblioteca gráfica de alta performance **Raylib-cs**.

![Plazma Burst 2 Remake](assets/sprites/weapons/item_grenade.png)

---

## 🌟 Funcionalidades Principais

### 🎮 Modos de Jogo & Campanha
* **Menu Inicial (Home)**: Interface gráfica com estética cibernética, partículas de plasma e neon animado.
* **Campanha com 45 Níveis**:
  * **5 Capítulos Oficiais**:
    1. *Capítulo 1: O Despertar da IA* (Fases 1 a 9)
    2. *Capítulo 2: Linha de Frente Falkok* (Fases 10 a 18)
    3. *Capítulo 3: Complexo de Pesquisa C-01* (Fases 19 a 27)
    4. *Capítulo 4: Fortaleza Usurpation* (Fases 28 a 36)
    5. *Capítulo 5: O Confronto Final* (Fases 37 a 45)
  * **4 Dificuldades Táticas**: *Fácil*, *Normal*, *Difícil* e *Havoc (Impossível)* com ajuste de dano, tempo de reação e agressividade dos bots.
  * **Salvamento de Progresso**: Registro automático de fases desbloqueadas, concluídas e melhores tempos em formato JSON.
* **Modo Livre / Playtest**: Modo de teste imediato do mapa ativo.
* **Level Editor PB2 Integrado**: Ferramenta completa para desenhar paredes, plataformas, definir spawn do player e posicionar bots inimigos.

---

### 💥 Arsenal & Física de Tiro
* **Arsenal com 9 Armas do Plazma Burst 2**:
  1. *Glock Compact*
  2. *Falcon Laser Pistol*
  3. *M4A1 Assault Rifle*
  4. *Heavy Minigun Cannon*
  5. *Plasma Rifle C-01p*
  6. *Shotgun CS-DAZ*
  7. *Heavy Railgun Ray-01*
  8. *Sniper CS-BNG*
  9. *Android Sniper CS-ASR*
* **Recuo Procedural**: Kickback linear para trás e elevação do cano (*muzzle climb*).
* **Animação de Empunhadura e Recarga**: Animação de sacar arma das costas e recarga com pentes e cooldown realistas.
* **Granadas PB2 (ItemGrenade)**:
  * Trajetória parabólica e física de eixos separados (*swept axes*).
  * Quiques elásticos e rolamento contínuo no chão com atrito.
  * Detonação com onda de choque e oclusão por paredes sólidas (*line-of-sight*).

---

### 🤖 Inteligência Artificial & Cinemática Inversa (IK)
* **Cinemática Inversa (IK)** nos braços, cotovelos e pernas dos personagens.
* **Percepção Sensorial dos Bots**:
  * Visão tática: não atiram através de paredes sólidas.
  * Audição e Alerta Sonoro: tiros e passos próximos colocam os bots em estado de alerta.
  * Manobras táticas de esquiva, saltos e strafe.
* **Física de Morte & Ragdoll**: Corpos caem, tombam na direção do impacto do tiro e arremessam suas armas no chão.
* **Modo Bot Autônomo para o Jogador (`Tecla B`)**: Transforma o jogador em uma IA autônoma capaz de usar livremente todas as armas e granadas.

---

## ⌨️ Controles do Jogo

| Tecla / Comando | Ação |
| :--- | :--- |
| **A / D** | Mover para Esquerda / Direita |
| **W / Espaço** | Pulo (Suporte a Pulo Variável e Duplo Pulo) |
| **S / C / Ctrl** | Agachar / Travar Agachado |
| **Mouse (Botão Esquerdo)** | Mirar e Atirar |
| **1 a 9** | Selecionar Armas do Arsenal PB2 |
| **Scroll do Mouse** | Alternar Próxima / Anterior Arma |
| **R** | Recarregar Arma |
| **G** | Lançar Granada Oficial PB2 |
| **B** | Ativar / Desativar Modo Bot IA para o Jogador |
| **F5** | Playtest (dentro do Level Editor) |
| **F11** | Alternar Tela Cheia (Fullscreen) |
| **ESC** | Pausar / Voltar ao Menu de Fases / Menu Principal |

---

## 🚀 Como Executar o Projeto

### Pré-requisitos
* [.NET 10.0 SDK](https://dotnet.microsoft.com/download) (ou compatível com .NET 8 / 9).

### Passo a Passo
1. Clone o repositório:
```bash
git clone https://github.com/SEU_USUARIO/MotorTiro.git
cd MotorTiro
```

2. Compile e execute:
```bash
dotnet run --project MotorTiro/MotorTiro.csproj
```

---

## 📂 Estrutura do Código

```
MotorTiro/
├── CampanhaPB2.cs              # Gestão dos 45 níveis, capítulos, dificuldades e geração procedural
├── InterfaceMenuPB2.cs         # Telas de Home e Seleção de Level com estética cibernética
├── PersonagemPB2.cs            # Jogador, IK dos membros, inventário, física e modo bot
├── BotIAPB2.cs                 # IA tática dos inimigos, facções (Civil Security, Falkok, etc.)
├── ArmaPB2.cs                  # Definições das 9 armas, recuo e balanceamento
├── GranadaPB2.cs               # Física e explosão das granadas PB2
├── EditorDeLevel/              # Editor de mapas com interface, ferramentas e salvamento JSON
│   ├── EditorPB2.cs
│   ├── DadosMapa.cs
│   └── GerenciadorMapa.cs
├── ModuloPercepcaoTaticaPB2.cs # Linha de visão, acústica e balística
├── SistemaColisao.cs           # Detecção de colisões sólidas e balística
└── Program.cs                  # Loop principal do jogo e alternância de estados
```

---

## 📜 Licença & Créditos
Este projeto é um tributo feito por fãs em homenagem a **Plazma Burst 2**, criado originalmente por **Eric Gurt**. Sprites e efeitos sonoros pertencem aos seus respectivos autores.
