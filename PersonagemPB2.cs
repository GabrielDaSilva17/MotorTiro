using System;
using System.Collections.Generic;
using System.Numerics;
using Raylib_cs;

namespace JogoDeTiro
{
    // Personagem no estilo Plazma Burst 2 com montagem modular de sprites
    // e Cinemática Inversa (IK de 2 segmentos) completa para pernas e braços.
    // Agora os joelhos dobram realisticamente na corrida e os cotovelos
    // seguram a arma firmemente em qualquer ângulo de mira 360°.
    public class PersonagemPB2 : IDisposable
    {
        // Posição e cinemática
        public Vector2 Posicao;
        public Vector2 Velocidade;
        public float AnguloMiraGraus = 0.0f;
        public float AnguloMiraRad = 0.0f;
        public float DirecaoOlhar = 1.0f; // 1.0 = Direita, -1.0 = Esquerda
        public float AlturaChao { get; set; } = 480.0f;

        // Comprimentos anatômicos dos membros (proporcionais aos sprites)
        private const float ComprimentoCoxa = 19.5f;
        private const float ComprimentoCanela = 18.5f;
        private const float ComprimentoBraco = 14.5f;
        private const float ComprimentoAntebraco = 14.5f;

        // Físicas
        public float VelocidadeMovimento = 320.0f;
        public float Aceleracao = 2000.0f;
        public float Friccao = 1600.0f;
        public float ForcaSalto = -500.0f;
        public float ForcaSaltoDuplo = -440.0f;
        public float Gravidade = 1250.0f;

        public bool NoChao { get; private set; } = true;
        private bool podeSaltoDuplo = true;
        private float tempoCoyote = 0.0f;
        private float tempoBufferPulo = 0.0f;

        // Animação procedural
        private float cicloAndar = 0.0f;
        private float timerPassos = 0.0f;
        private float inclinacaoCorpo = 0.0f; // Lean dinâmico
        private float bobbingTroncoY = 0.0f;
        private float bobbingCabecaY = 0.0f;
        private float rotacaoCabeca = 0.0f;

        // Sistema de Agachamento (Crouch - Estilo Plazma Burst 2)
        public bool Agachado { get; private set; } = false;
        public bool TravarAgachado { get; set; } = false; // Permite alternar (toggle) via botão ou tecla C
        private bool agachadoAnterior = false;
        private float transicaoAgachar = 0.0f; // 0.0f = Em pé, 1.0f = Agachado

        public void AlternarAgachamento()
        {
            TravarAgachado = !TravarAgachado;
        }

        public Vector2 CentroVisual => Posicao - new Vector2(0, 16.0f) + new Vector2(0, bobbingTroncoY + (transicaoAgachar * 7.5f));

        // Sistema de Armas e Recuo Procedural (Estilo Plazma Burst 2)
        public List<ArmaPB2> Arsenal = new List<ArmaPB2>();
        public int ArmaAtualIndex = 0;
        public int ArmaCostasIndex = 4; // Slot da arma guardada nas costas (ex: Sniper Rifle CS-BNG)
        public ArmaPB2? ArmaAtual => (Arsenal.Count > 0 && ArmaAtualIndex >= 0 && ArmaAtualIndex < Arsenal.Count) ? Arsenal[ArmaAtualIndex] : null;

        private float cooldownDisparo = 0.0f;
        private float recuoLinearAtual = 0.0f;     // Deslocamento para trás (kickback em px)
        private float recuoRotacaoAtual = 0.0f;    // Elevação do cano (muzzle climb em graus)

        // Sistema de Saque das Costas e Empunhadura (PB2 Draw/Equip Animation)
        public float DuracaoAnimacaoEquip { get; set; } = 0.40f; // Tempo para puxar das costas e empunhar
        public float TimerAnimacaoEquip { get; private set; } = 0.0f;
        public bool ArmaEmpunhada => TimerAnimacaoEquip <= 0.0f; // Só pode atirar se a arma estiver na mão!
        public float ProgressoEquip => 1.0f - Math.Clamp(TimerAnimacaoEquip / DuracaoAnimacaoEquip, 0.0f, 1.0f);

        private float timerFlashCano = 0.0f;       // Clarão na ponta do cano
        private float balancoCostas = 0.0f;        // Balanço dinâmico/inércia da arma nas costas

        // Sistema de Recarga Tática
        public float TimerRecarga { get; private set; } = 0.0f;
        public float DuracaoRecargaTotal { get; private set; } = 1.0f;
        public bool EstaRecarregando => TimerRecarga > 0.0f;

        // Ejeção de Cápsulas Físicas e Tremor de Tela
        public List<EfeitoCapsula> Capsulas = new List<EfeitoCapsula>();
        public float TraumaCameraAcumulado = 0.0f;

        // Sistema Oficial de Granadas PB2 (ItemGrenade - DefineSprite 2885)
        public List<GranadaPB2> Granadas = new List<GranadaPB2>();
        private Texture2D texGranada;
        private Sound somLancamentoGranada;
        private Sound somExplosaoGranada;
        public int QtdGranadas { get; set; } = 5;

        // Rig Modular
        public SkinModular Skin { get; set; }
        private Sound somPulo;
        private Sound somPasso;
        private Sound somTrocaArma;
        private Sound somEquipArma;

        // Cores de sombreamento para dar profundidade de luz entre membros dianteiros e traseiros
        private readonly Color corDianteira = Color.White;
        private readonly Color corTraseira = new Color(175, 185, 200, 255);
        private readonly Color corArticulacao = new Color(24, 28, 36, 255);

        // =============================================================
        // Modo Bot IA Autônomo para o Jogador (Tecla B)
        // =============================================================
        public bool ModoBotAtivo { get; set; } = false;
        public BotIAPB2? BotAlvoAtual { get; private set; }
        public Vector2 BotMiraSuavizada => botMiraSuavizada;
        private float botTimerTrocaArma = 0.0f;
        private float botTimerDisparo = 0.0f;
        private float botTimerGranada = 3.5f;
        private Vector2 botMiraSuavizada;
        private float botInputX = 0.0f;
        private bool botInputPulo = false;
        private bool botInputAgachar = false;
        private bool botInputAtirar = false;
        private float botCooldownPulo = 0.0f;
        private float botTimerBloqueadoParede = 0.0f;
        private float botDirecaoManobraX = 0.0f;
        private float botTimerManobra = 0.0f;

        // Sistema de Sentidos e Percepção Tática Realista (Estilo Plazma Burst 2)
        private Vector2 botPontoAlertaSom = Vector2.Zero;
        private float botTimerAlertaSom = 0.0f;
        private float botTimerPerdaVisao = 0.0f;
        private float botDirecaoPatrulha = 1.0f;

        // Comportamento Orgânico de Exploração PB2: 0 = ParadoObservando, 1 = Caminhando, 2 = Correndo
        private int botEstadoExploracao = 1;
        private float botTimerAcaoExploracao = 3.0f;
        private float botTimerOlhada = 1.0f;
        private float botOffsetOlharY = 0.0f;
        private float botDirecaoOlharParado = 1.0f;
        private float botCooldownPuloExploratorio = 3.0f;

        // Inteligência Tática de Combate: Strafe e Manobras Evasivas
        private float botTimerStrafeCombate = 0.5f;
        private float botDirecaoStrafe = 1.0f;
        private float cooldownLancamentoGranada = 0.0f;

        /// <summary>
        /// Alerta o bot do jogador quando um disparo inimigo ou impacto de bala ocorre nas proximidades.
        /// </summary>
        public void NotificarDisparoInimigo(Vector2 posTiro)
        {
            if (!ModoBotAtivo) return;
            botPontoAlertaSom = posTiro;
            botTimerAlertaSom = 3.5f;
            // Se não estiver em combate visual direto, vira o olhar e patrulha em direção à ameaça
            if (BotAlvoAtual == null)
            {
                float dirSom = MathF.Sign(posTiro.X - Posicao.X);
                if (dirSom != 0.0f)
                {
                    botDirecaoPatrulha = dirSom;
                }
            }
        }

        public PersonagemPB2(Vector2 posicaoInicial, string diretorioAssets)
        {
            Posicao = posicaoInicial;
            AlturaChao = 480.0f;

            // 1. Carrega sprites modulares segmentados
            Skin = SkinModular.CarregarPadrao(diretorioAssets);

            // 2. Carrega sons do personagem e armas
            string pastaSons = System.IO.Path.Combine(diretorioAssets, "sounds");
            string pastaArmas = System.IO.Path.Combine(diretorioAssets, "sprites", "weapons");

            somPulo = CarregarSomSeguro(System.IO.Path.Combine(pastaSons, "147_step1.mp3"));
            somPasso = CarregarSomSeguro(System.IO.Path.Combine(pastaSons, "146_step2.mp3"));
            somTrocaArma = CarregarSomSeguro(System.IO.Path.Combine(pastaSons, "192_reload.mp3"));
            somEquipArma = CarregarSomSeguro(System.IO.Path.Combine(pastaSons, "61_wea_pickup.mp3"));

            // 3. Granada Oficial PB2
            texGranada = CarregarTexturaSegura(System.IO.Path.Combine(pastaArmas, "item_grenade.png"));
            somLancamentoGranada = CarregarSomSeguro(System.IO.Path.Combine(pastaSons, "grenade_throw.mp3"));
            somExplosaoGranada = CarregarSomSeguro(System.IO.Path.Combine(pastaSons, "explode.mp3"));

            // 4. Carrega arsenal de armas
            Arsenal = ArmaPB2.CarregarArsenalPadrao(diretorioAssets);
            ArmaAtualIndex = Arsenal.Count > 2 ? 2 : 0; // M4A1 inicial (DefineSprite 2739)
            ArmaCostasIndex = Arsenal.Count > 7 ? 7 : (Arsenal.Count > 2 ? 2 : 0); // Sniper CS-BNG nas costas!
        }

        public void Atualizar(float dt, Vector2 posicaoRato, List<ProjetilPlasma> listaProjeteis, List<Rectangle> plataformas, bool mouseSobreUI = false)
        {
            Vector2 miraEfetiva = ModoBotAtivo ? botMiraSuavizada : posicaoRato;

            AtualizarEntradaEFisica(dt, plataformas);
            AtualizarAnimacao(dt, plataformas);
            AtualizarMira(dt, miraEfetiva);
            AtualizarArmaETiros(dt, miraEfetiva, listaProjeteis, mouseSobreUI);
        }

        public void AtualizarIABot(float dt, List<BotIAPB2> inimigos, List<Rectangle> plataformas)
        {
            if (!ModoBotAtivo) return;

            botTimerGranada -= dt;
            botTimerTrocaArma -= dt;
            botTimerDisparo -= dt;
            if (botTimerAlertaSom > 0.0f) botTimerAlertaSom -= dt;

            // Raio de percepção visual e tátil realista (evita bot apelão/onisciente estilo aimbot)
            const float RAIO_VISAO_FRENTE = 440.0f;
            const float RAIO_VISAO_PERIFERICA = 160.0f;
            const float RAIO_TOQUE_PROXIMO = 75.0f; // Sente passos e presença colada ou embaixo da plataforma

            Vector2 ombro = ObterOmbro();
            float dirFrente = DirecaoOlhar != 0.0f ? DirecaoOlhar : 1.0f;

            // 1. Busca inteligente de alvos dentro dos limites sensoriais
            BotIAPB2? novoAlvo = null;
            float menorDistancia = float.MaxValue;

            for (int i = 0; i < inimigos.Count; i++)
            {
                var bot = inimigos[i];
                if (!bot.Vivo) continue;

                Vector2 posInimigo = bot.CentroVisual;
                float d = Vector2.Distance(ombro, posInimigo);

                bool percebido = false;

                // Regra A: Sentido de proximidade imediata / tátil (colado ou logo embaixo da plataforma <= 75px)
                if (d <= RAIO_TOQUE_PROXIMO)
                {
                    percebido = true;
                }
                else
                {
                    // Regra B: Linha de visão direta livre de obstáculos sólidos (chão, paredes, plataformas)
                    bool linhaLivre = ModuloPercepcaoTaticaPB2.TemLinhaDeTiroLivre(ombro, posInimigo, plataformas);

                    if (linhaLivre)
                    {
                        float dxInimigo = posInimigo.X - ombro.X;
                        bool naFrente = (dxInimigo * dirFrente) > -25.0f;

                        // Campo de visão frontal limpo
                        if (naFrente && d <= RAIO_VISAO_FRENTE)
                        {
                            percebido = true;
                        }
                        // Visão periférica/costas desobstruída (alcance curto)
                        else if (!naFrente && d <= RAIO_VISAO_PERIFERICA)
                        {
                            percebido = true;
                        }
                    }

                    // Regra C: Alerta por som de tiro ou impacto recente
                    if (!percebido && botTimerAlertaSom > 0.0f)
                    {
                        float distInimigoAoSom = Vector2.Distance(posInimigo, botPontoAlertaSom);
                        if (distInimigoAoSom <= 260.0f && d <= RAIO_VISAO_FRENTE)
                        {
                            if (linhaLivre || d <= 170.0f)
                            {
                                percebido = true;
                            }
                        }
                    }
                }

                if (percebido && d < menorDistancia)
                {
                    menorDistancia = d;
                    novoAlvo = bot;
                }
            }

            // Persistência de memória tática (se o alvo se escondeu atrás de uma parede, mantém por até 1.8s)
            if (novoAlvo != null)
            {
                BotAlvoAtual = novoAlvo;
                botTimerPerdaVisao = 1.8f;
            }
            else if (BotAlvoAtual != null)
            {
                if (!BotAlvoAtual.Vivo)
                {
                    BotAlvoAtual = null;
                    botTimerPerdaVisao = 0.0f;
                }
                else
                {
                    botTimerPerdaVisao -= dt;
                    if (botTimerPerdaVisao <= 0.0f)
                    {
                        BotAlvoAtual = null;
                    }
                }
            }

            // -------------------------------------------------------------
            // ESTADO: SEM INIMIGO À VISTA (Patrulha Natural e Exploração)
            // -------------------------------------------------------------
            if (BotAlvoAtual == null)
            {
                botInputAtirar = false;
                botInputAgachar = false;

                // Se houver alerta sonoro ativo recente, investiga a fonte do disparo
                if (botTimerAlertaSom > 0.0f)
                {
                    Vector2 dirAoSom = botPontoAlertaSom - ombro;
                    if (dirAoSom.Length() > 5.0f)
                    {
                        botMiraSuavizada = Vector2.Lerp(botMiraSuavizada, botPontoAlertaSom, 14.0f * dt);
                    }
                    float dxSom = botPontoAlertaSom.X - Posicao.X;
                    if (MathF.Abs(dxSom) > 80.0f)
                    {
                        botInputX = MathF.Sign(dxSom) * 0.55f;
                    }
                    else
                    {
                        botInputX = 0.0f;
                    }
                }
                else
                {
                    // Patrulha orgânica estilo Plazma Burst 2:
                    // Alterna aleatoriamente entre Ficar Parado Observando, Caminhar e Correr
                    botTimerAcaoExploracao -= dt;
                    if (botCooldownPuloExploratorio > 0.0f) botCooldownPuloExploratorio -= dt;

                    bool obstaculoFrente = ModuloPercepcaoTaticaPB2.DetectarParedeAFrente(Posicao, botDirecaoPatrulha, plataformas, 24.0f, out bool paredeAltaPatrulha);

                    // Se colidir com uma parede alta ou terminar o tempo da ação atual, sorteia novo comportamento
                    if ((obstaculoFrente && paredeAltaPatrulha) || botTimerAcaoExploracao <= 0.0f)
                    {
                        if (obstaculoFrente && paredeAltaPatrulha)
                        {
                            // Ao encontrar parede alta, inverte de lado imediatamente
                            botDirecaoPatrulha = -botDirecaoPatrulha;
                            botDirecaoOlharParado = botDirecaoPatrulha;
                            // 35% de chance de parar para inspecionar, 65% de caminhar na outra direção
                            botEstadoExploracao = (Random.Shared.NextDouble() < 0.35) ? 0 : 1;
                            botTimerAcaoExploracao = (float)Random.Shared.NextDouble() * 2.5f + 1.8f;
                        }
                        else
                        {
                            // Sorteia nova ação orgânica estilo Plazma Burst 2
                            double sorteio = Random.Shared.NextDouble();
                            if (sorteio < 0.35)
                            {
                                // 35% Chance: Ficar Parado Observando (Idle / Vigília)
                                botEstadoExploracao = 0;
                                botTimerAcaoExploracao = (float)Random.Shared.NextDouble() * 2.8f + 1.6f;
                                botTimerOlhada = 0.0f;
                                botDirecaoOlharParado = botDirecaoPatrulha;
                                if (Random.Shared.NextDouble() < 0.30)
                                    botDirecaoOlharParado = -botDirecaoOlharParado; // Pode virar para vigiar as costas
                            }
                            else if (sorteio < 0.75)
                            {
                                // 40% Chance: Caminhada suave / exploração cautelosa
                                botEstadoExploracao = 1;
                                botTimerAcaoExploracao = (float)Random.Shared.NextDouble() * 3.5f + 2.5f;
                                if (Random.Shared.NextDouble() < 0.30)
                                    botDirecaoPatrulha = -botDirecaoPatrulha;
                                botDirecaoOlharParado = botDirecaoPatrulha;
                            }
                            else
                            {
                                // 25% Chance: Corrida rápida / sprint entre plataformas
                                botEstadoExploracao = 2;
                                botTimerAcaoExploracao = (float)Random.Shared.NextDouble() * 2.5f + 1.5f;
                                if (Random.Shared.NextDouble() < 0.25)
                                    botDirecaoPatrulha = -botDirecaoPatrulha;
                                botDirecaoOlharParado = botDirecaoPatrulha;
                            }
                        }
                    }

                    botInputPulo = false;

                    // Execução dos comportamentos:
                    if (botEstadoExploracao == 0)
                    {
                        // -----------------------------------------------------
                        // 1. FICAR PARADO E "DAR UMA OLHADA" (Vigília PB2)
                        // -----------------------------------------------------
                        botInputX = 0.0f;

                        botTimerOlhada -= dt;
                        if (botTimerOlhada <= 0.0f)
                        {
                            botTimerOlhada = (float)Random.Shared.NextDouble() * 1.3f + 0.8f;
                            // Olha ligeiramente para cima (andar superior) ou para baixo (andar inferior)
                            botOffsetOlharY = ((float)Random.Shared.NextDouble() * 140.0f) - 60.0f;

                            // 20% de chance de dar uma olhada para trás enquanto vigia
                            if (Random.Shared.NextDouble() < 0.20)
                            {
                                botDirecaoOlharParado = -botDirecaoOlharParado;
                            }
                        }

                        Vector2 miraParado = ombro + new Vector2(botDirecaoOlharParado * 220.0f, botOffsetOlharY);
                        if (botMiraSuavizada == Vector2.Zero) botMiraSuavizada = miraParado;
                        botMiraSuavizada = Vector2.Lerp(botMiraSuavizada, miraParado, 6.0f * dt);
                    }
                    else
                    {
                        // -----------------------------------------------------
                        // 2. CAMINHAR OU CORRER
                        // -----------------------------------------------------
                        float velInput = (botEstadoExploracao == 2) ? 0.95f : 0.42f;
                        botInputX = botDirecaoPatrulha * velInput;

                        // Pulo tático sobre degraus baixos
                        if (obstaculoFrente && !paredeAltaPatrulha && NoChao && botCooldownPulo <= 0.0f)
                        {
                            botInputPulo = true;
                            botCooldownPulo = 1.0f;
                        }
                        // Pulo exploratório ocasional para subir em plataformas elevadas ou pular vãos
                        else if (NoChao && botCooldownPulo <= 0.0f && botCooldownPuloExploratorio <= 0.0f && (botEstadoExploracao == 2 || Random.Shared.NextDouble() < 0.015))
                        {
                            botInputPulo = true;
                            botCooldownPulo = 1.0f;
                            botCooldownPuloExploratorio = (float)Random.Shared.NextDouble() * 4.0f + 3.0f;
                        }

                        // Mira voltada para a frente na direção do deslocamento
                        Vector2 miraMovimento = ombro + new Vector2(botDirecaoPatrulha * 240.0f, 0.0f);
                        if (botMiraSuavizada == Vector2.Zero) botMiraSuavizada = miraMovimento;
                        botMiraSuavizada = Vector2.Lerp(botMiraSuavizada, miraMovimento, 9.0f * dt);
                    }
                }

                if (botCooldownPulo > 0.0f) botCooldownPulo -= dt;
                return;
            }

            // -------------------------------------------------------------
            // ESTADO: INIMIGO ENCONTRADO (Engajamento Tático Inteligente)
            // -------------------------------------------------------------
            // 2. Mira preditiva com Lead Aim real baseado na velocidade do projétil
            float velTiro = (ArmaAtual != null && ArmaAtual.VelocidadeProjetil > 100.0f) ? ArmaAtual.VelocidadeProjetil : 1150.0f;
            float tempoVoo = Math.Clamp(menorDistancia / velTiro, 0.015f, 0.35f);
            Vector2 pontoMiraAlvo = BotAlvoAtual.CentroVisual + (BotAlvoAtual.Velocidade * tempoVoo);
            if (botMiraSuavizada == Vector2.Zero) botMiraSuavizada = pontoMiraAlvo;
            botMiraSuavizada = Vector2.Lerp(botMiraSuavizada, pontoMiraAlvo, 24.0f * dt);

            // 3. Escolha tática inteligente de armas e recarga preventiva
            if (botTimerTrocaArma <= 0.0f && Arsenal.Count > 0)
            {
                int melhorSlot = ArmaAtualIndex;
                if (menorDistancia < 160.0f)
                {
                    // Curta distância: Prefere Shotgun CS-DAZ (slot 5) ou Minigun (slot 3)
                    melhorSlot = Arsenal.Count > 5 ? 5 : (Arsenal.Count > 3 ? 3 : 0);
                }
                else if (menorDistancia > 340.0f)
                {
                    // Longa distância: Prefere Sniper CS-BNG (slot 7), Railgun Ray-01 (slot 6) ou Android Sniper (slot 8)
                    melhorSlot = Arsenal.Count > 7 ? 7 : (Arsenal.Count > 6 ? 6 : 2);
                }
                else
                {
                    // Média distância: M4A1 (slot 2) ou Plasma Rifle (slot 4)
                    melhorSlot = Arsenal.Count > 2 ? 2 : (Arsenal.Count > 4 ? 4 : 1);
                }

                if (melhorSlot != ArmaAtualIndex && Arsenal[melhorSlot].MunicaoAtual > 0)
                {
                    TrocarArma(melhorSlot);
                    botTimerTrocaArma = (float)Random.Shared.NextDouble() * 1.5f + 1.8f;
                }
                else
                {
                    botTimerTrocaArma = 1.0f;
                }
            }

            // Recarga tática preventiva e troca emergencial se a munição zerar
            if (ArmaAtual != null && ArmaAtual.MunicaoAtual <= 0 && !EstaRecarregando)
            {
                int slotPronto = -1;
                for (int s = 0; s < Arsenal.Count; s++)
                {
                    if (s != ArmaAtualIndex && Arsenal[s].MunicaoAtual > 0)
                    {
                        slotPronto = s;
                        break;
                    }
                }
                if (slotPronto != -1 && menorDistancia < 260.0f)
                {
                    TrocarArma(slotPronto);
                }
                else
                {
                    Recarregar();
                }
            }

            // 4. Movimentação tática com STRAFE ativo (sem ficar parado como alvo fácil)
            float dx = BotAlvoAtual.Posicao.X - Posicao.X;
            float dy = BotAlvoAtual.Posicao.Y - Posicao.Y;
            float absDx = MathF.Abs(dx);

            if (botCooldownPulo > 0.0f) botCooldownPulo -= dt;

            // Timer de alternância do strafe em combate (zig-zag evasivo)
            botTimerStrafeCombate -= dt;
            if (botTimerStrafeCombate <= 0.0f)
            {
                botTimerStrafeCombate = (float)Random.Shared.NextDouble() * 0.70f + 0.40f;
                botDirecaoStrafe = (Random.Shared.Next(0, 2) == 0) ? 1.0f : -1.0f;
            }

            float dirDesejadaX = MathF.Sign(dx);
            bool paredeAFrente = ModuloPercepcaoTaticaPB2.DetectarParedeAFrente(Posicao, dirDesejadaX, plataformas, 26.0f, out bool paredeAlta);

            // Se estiver colidindo com uma parede alta intransponível:
            if (paredeAFrente && paredeAlta)
            {
                botTimerBloqueadoParede += dt;
                if (botTimerBloqueadoParede > 0.6f)
                {
                    botTimerManobra = 1.1f;
                    botDirecaoManobraX = -dirDesejadaX;
                    botTimerBloqueadoParede = 0.0f;
                }
            }
            else
            {
                botTimerBloqueadoParede = MathF.Max(0.0f, botTimerBloqueadoParede - dt * 2.0f);
            }

            if (botTimerManobra > 0.0f)
            {
                botTimerManobra -= dt;
                botInputX = botDirecaoManobraX;
            }
            else
            {
                if (absDx > 230.0f)
                {
                    botInputX = dirDesejadaX * 0.95f; // Avança com velocidade para encurtar
                }
                else if (absDx < 105.0f)
                {
                    botInputX = -dirDesejadaX * 0.85f; // Recua para manter espaço seguro de fogo
                }
                else
                {
                    // Distância ideal de engajamento: STRAFE tático (movimento em zig-zag contínuo)
                    botInputX = botDirecaoStrafe * 0.70f;
                }
            }

            // 5. Linha de tiro limpa (Cessa fogo se houver parede, chão ou plataforma)
            Vector2 pontaCano = ObterPontaDoCano();
            bool temLinhaDeTiroLivre = ModuloPercepcaoTaticaPB2.PodeAbrirFogo(pontaCano, BotAlvoAtual.CentroVisual, plataformas);

            // Pulo inteligente com cooldown (degraus, andares superiores e jump-shot sobre parapeitos)
            botInputPulo = false;
            if (NoChao && botCooldownPulo <= 0.0f)
            {
                // Inimigo em andar superior: sobe plataformas ativamente
                if (dy < -35.0f && absDx < 350.0f)
                {
                    botInputPulo = true;
                    botCooldownPulo = 0.85f;
                }
                // Obstáculo ou degrau transponível à frente
                else if (paredeAFrente && !paredeAlta)
                {
                    botInputPulo = true;
                    botCooldownPulo = 0.85f;
                }
                // Linha de tiro bloqueada por cobertura baixa: salta para atirar por cima
                else if (!temLinhaDeTiroLivre && absDx < 340.0f && dy < 25.0f && !paredeAlta)
                {
                    botInputPulo = true;
                    botCooldownPulo = 0.95f;
                }
                // Salto evasivo ocasional em combate aberto
                else if (temLinhaDeTiroLivre && (float)Random.Shared.NextDouble() < 0.020f)
                {
                    botInputPulo = true;
                    botCooldownPulo = 1.15f;
                }
            }

            // Agachamento tático: ao recarregar ou para estabilizar armas de precisão à distância
            bool atirandoDeLonge = menorDistancia > 300.0f && ArmaAtual != null && (ArmaAtual.Tipo == "sniper" || ArmaAtual.Tipo == "railgun");
            botInputAgachar = EstaRecarregando || (atirandoDeLonge && NoChao);

            Vector2 vetorMira = botMiraSuavizada - ObterOmbro();
            float anguloParaAlvo = MathF.Atan2(vetorMira.Y, vetorMira.X) * (180.0f / MathF.PI);
            float diffAngulo = MathF.Abs(AnguloMiraGraus - anguloParaAlvo);
            while (diffAngulo > 180.0f) diffAngulo = MathF.Abs(diffAngulo - 360.0f);

            bool alinhado = diffAngulo < 16.0f;
            // Só atira se o cano estiver alinhado E a trajetória estiver 100% desobstruída!
            botInputAtirar = alinhado && ArmaEmpunhada && !EstaRecarregando && temLinhaDeTiroLivre;

            // 6. Lançamento tático de Granada PB2 (apenas se a trajetória estiver viável e sem parede na cara)
            if (QtdGranadas > 0 && botTimerGranada <= 0.0f && menorDistancia > 140.0f && menorDistancia < 390.0f && temLinhaDeTiroLivre)
            {
                LancarGranada(BotAlvoAtual.Posicao);
                botTimerGranada = (float)Random.Shared.NextDouble() * 3.5f + 4.0f;
            }
        }

        private void AtualizarEntradaEFisica(float dt, List<Rectangle> plataformas)
        {
            // 1. Entrada horizontal e Agachamento
            if (Raylib.IsKeyPressed(KeyboardKey.C))
            {
                TravarAgachado = !TravarAgachado;
            }
            // Teclas de segurar (Hold): S, Seta para Baixo, LeftControl
            bool segurandoAgachar = (ModoBotAtivo && botInputAgachar) ||
                                    Raylib.IsKeyDown(KeyboardKey.S) || 
                                    Raylib.IsKeyDown(KeyboardKey.Down) || 
                                    Raylib.IsKeyDown(KeyboardKey.LeftControl);

            // Ao pular, solta o agachamento travado
            if (Raylib.IsKeyPressed(KeyboardKey.W) || Raylib.IsKeyPressed(KeyboardKey.Space) || Raylib.IsKeyPressed(KeyboardKey.Up) || (ModoBotAtivo && botInputPulo))
            {
                TravarAgachado = false;
            }

            // O agachamento é ativado se estiver no chão e segurando a tecla ou se o botão/toggle estiver ativo
            Agachado = NoChao && (segurandoAgachar || TravarAgachado);

            // Feedback sonoro tático ao agachar
            if (Agachado && !agachadoAnterior)
            {
                TocarSomSeguro(somPasso);
            }
            agachadoAnterior = Agachado;

            float inputX = 0.0f;
            if (ModoBotAtivo)
            {
                inputX = botInputX;
            }
            if (Raylib.IsKeyDown(KeyboardKey.A) || Raylib.IsKeyDown(KeyboardKey.Left)) inputX = -1.0f;
            if (Raylib.IsKeyDown(KeyboardKey.D) || Raylib.IsKeyDown(KeyboardKey.Right)) inputX = 1.0f;

            // Velocidade reduzida durante o agachamento (caminhada agachada/rastejo PB2)
            float velMax = Agachado ? (VelocidadeMovimento * 0.45f) : VelocidadeMovimento;

            if (inputX != 0.0f)
            {
                Velocidade.X = MoverEmDirecao(Velocidade.X, inputX * velMax, Aceleracao * dt);
            }
            else
            {
                Velocidade.X = MoverEmDirecao(Velocidade.X, 0.0f, Friccao * dt);
            }

            // 2. Gravidade
            Velocidade.Y += Gravidade * dt;

            // 3. Resolução sólida com plataformas e chão através da nova classe
            bool tocouChao = FisicaPlataforma.ResolverMovimentoEColisao(ref Posicao, ref Velocidade, plataformas, dt);

            if (tocouChao)
            {
                NoChao = true;
                podeSaltoDuplo = true;
                tempoCoyote = 0.12f;
            }
            else
            {
                NoChao = false;
                tempoCoyote -= dt;
            }

            // 4. Salto e Salto Duplo
            if (Raylib.IsKeyPressed(KeyboardKey.W) || Raylib.IsKeyPressed(KeyboardKey.Space) || Raylib.IsKeyPressed(KeyboardKey.Up) || (ModoBotAtivo && botInputPulo))
            {
                tempoBufferPulo = 0.14f;
            }
            else
            {
                tempoBufferPulo -= dt;
            }

            if (tempoBufferPulo > 0.0f)
            {
                if (NoChao || tempoCoyote > 0.0f)
                {
                    Velocidade.Y = ForcaSalto;
                    NoChao = false;
                    tempoCoyote = 0.0f;
                    tempoBufferPulo = 0.0f;
                    TocarSomSeguro(somPulo);
                }
                else if (podeSaltoDuplo)
                {
                    Velocidade.Y = ForcaSaltoDuplo;
                    podeSaltoDuplo = false;
                    tempoBufferPulo = 0.0f;
                    TocarSomSeguro(somPulo);
                }
            }

            // Pulo variável ao soltar tecla
            if ((Raylib.IsKeyReleased(KeyboardKey.W) || Raylib.IsKeyReleased(KeyboardKey.Space)) && Velocidade.Y < -100.0f)
            {
                Velocidade.Y *= 0.5f;
            }
        }

        public Vector2 ObterOmbro()
        {
            float offsetAgacharY = transicaoAgachar * 15.0f;
            return Posicao - new Vector2(0, 16) + new Vector2(0, bobbingTroncoY + offsetAgacharY);
        }

        private void AtualizarMira(float dt, Vector2 posicaoRato)
        {
            Vector2 ombro = ObterOmbro();
            Vector2 vetorMira = posicaoRato - ombro;
            float distMira = vetorMira.Length();

            // Se o cursor estiver praticamente em cima do centro do corpo,
            // evita jitter/inversão de lado mantendo o lado atual
            if (distMira > 16.0f)
            {
                if (MathF.Abs(vetorMira.X) > 10.0f)
                {
                    DirecaoOlhar = vetorMira.X >= 0.0f ? 1.0f : -1.0f;
                }

                AnguloMiraRad = MathF.Atan2(vetorMira.Y, vetorMira.X);
                AnguloMiraGraus = AnguloMiraRad * (180.0f / MathF.PI);
            }

            // A cabeça acompanha o ângulo de mira suavemente
            float anguloRelativo = AnguloMiraRad;
            if (DirecaoOlhar < 0)
            {
                anguloRelativo = MathF.PI - AnguloMiraRad;
                while (anguloRelativo > MathF.PI) anguloRelativo -= MathF.PI * 2.0f;
                while (anguloRelativo < -MathF.PI) anguloRelativo += MathF.PI * 2.0f;
                anguloRelativo = -anguloRelativo;
            }
            float anguloAlvoCabeca = Math.Clamp(anguloRelativo * 0.28f, -0.4f, 0.4f);
            rotacaoCabeca = Lerp(rotacaoCabeca, anguloAlvoCabeca, 14.0f * dt);
        }

        private void AtualizarAnimacao(float dt, List<Rectangle> plataformas)
        {
            // Interpolação suave de agachamento
            float alvoAgachar = Agachado ? 1.0f : 0.0f;
            transicaoAgachar = Lerp(transicaoAgachar, alvoAgachar, 15.0f * dt);

            // Inclinação dinâmica do tronco ao caminhar/correr (proporcional à velocidade)
            const float MinInclinacaoTroncoDeg = 8.0f;
            const float MaxInclinacaoTroncoDeg = 13.0f;
            float velRatioTronco = Math.Clamp(MathF.Abs(Velocidade.X) / VelocidadeMovimento, 0.0f, 1.0f);
            float direcaoTronco = MathF.Sign(Velocidade.X) * DirecaoOlhar;

            float anguloTroncoDeg = 0.0f;
            if (MathF.Abs(Velocidade.X) > 10.0f)
            {
                float anguloEscalado = Lerp(MinInclinacaoTroncoDeg, MaxInclinacaoTroncoDeg, velRatioTronco);
                anguloTroncoDeg = direcaoTronco * Math.Clamp(anguloEscalado, MinInclinacaoTroncoDeg, MaxInclinacaoTroncoDeg);
            }
            float alvoInclinacao = anguloTroncoDeg * (MathF.PI / 180.0f);
            float velInterpTronco = 13.0f + (velRatioTronco * 5.0f);
            inclinacaoCorpo = Lerp(inclinacaoCorpo, alvoInclinacao, velInterpTronco * dt);

            if (NoChao)
            {
                if (MathF.Abs(Velocidade.X) > 12.0f)
                {
                    // Progresso do ciclo de marcha na direção local para frente
                    float fatorVel = Math.Clamp(MathF.Abs(Velocidade.X) / VelocidadeMovimento, 0.35f, 1.25f);
                    float velLocal = (Velocidade.X * DirecaoOlhar) / VelocidadeMovimento;
                    cicloAndar += velLocal * dt * 11.0f;

                    // Balanço vertical orgânico do quadril
                    bobbingTroncoY = MathF.Abs(MathF.Cos(cicloAndar)) * 2.2f;
                    bobbingCabecaY = MathF.Abs(MathF.Cos(cicloAndar)) * 1.8f;

                    // Áudio de passos
                    timerPassos += dt;
                    if (timerPassos >= 0.28f / MathF.Max(fatorVel, 0.6f))
                    {
                        timerPassos = 0.0f;
                        TocarSomSeguro(somPasso);
                    }
                }
                else
                {
                    // Postura neutra parada: pés firmes no chão, respiração suave
                    cicloAndar = Lerp(cicloAndar, 0.0f, 10.0f * dt);
                    bobbingTroncoY = MathF.Sin((float)Raylib.GetTime() * 2.2f) * 0.6f;
                    bobbingCabecaY = MathF.Sin((float)Raylib.GetTime() * 2.2f) * 0.5f;
                    timerPassos = 0.0f;
                }
            }
            else
            {
                bobbingTroncoY = MoverEmDirecao(bobbingTroncoY, 0.0f, 20.0f * dt);
                bobbingCabecaY = MoverEmDirecao(bobbingCabecaY, 0.0f, 20.0f * dt);
            }

            // ---------------------------------------------------------------------------------
            // Balanço dinâmico da arma nas costas (faixa de 10° a 15° proporcional à velocidade com min/max)
            // ---------------------------------------------------------------------------------
            const float MinInclinacaoCostasDeg = 10.0f; // Mínimo de 10° ao iniciar movimento
            const float MaxInclinacaoCostasDeg = 15.0f; // Máximo de 15° na velocidade máxima de corrida

            float fatorVelCostas = Velocidade.X / VelocidadeMovimento; // -1.0 a +1.0
            float velRelativaCostas = fatorVelCostas * DirecaoOlhar;    // +1.0 avançando, -1.0 recuando
            float velRatioAbs = Math.Clamp(MathF.Abs(fatorVelCostas), 0.0f, 1.0f);

            float alvoBalanco = 0.0f;
            if (MathF.Abs(Velocidade.X) > 10.0f)
            {
                // Escala linearmente entre 10° (min) e 13.5° conforme a velocidade horizontal
                float baseInclinacao = Lerp(MinInclinacaoCostasDeg, 13.5f, velRatioAbs);
                float sinalDirecao = MathF.Sign(velRelativaCostas);

                // Oscilação rítmica orgânica dos passos (atinge o ápice de 15°)
                float oscilacao = 0.0f;
                if (NoChao)
                {
                    oscilacao = MathF.Sin(cicloAndar) * 1.5f * velRatioAbs;
                }
                else
                {
                    float fatorAr = Math.Clamp(-Velocidade.Y / 320.0f, -1.0f, 1.0f);
                    oscilacao = fatorAr * 2.0f;
                }

                // Limites rígidos de Mínimo e Máximo: -15° a +15°
                alvoBalanco = Math.Clamp((baseInclinacao * sinalDirecao) + (oscilacao * DirecaoOlhar), -MaxInclinacaoCostasDeg, MaxInclinacaoCostasDeg);
            }

            // Velocidade de interpolação viva e ágil escalada com a velocidade
            float velocidadeInterpCostas = 16.0f + (velRatioAbs * 6.0f);
            balancoCostas = Lerp(balancoCostas, alvoBalanco, velocidadeInterpCostas * dt);

            // Decaimento elástico suave do recuo da arma (linear e angular - estilo PB2)
            recuoLinearAtual = Lerp(recuoLinearAtual, 0.0f, 22.0f * dt);
            recuoRotacaoAtual = Lerp(recuoRotacaoAtual, 0.0f, 19.0f * dt);
            if (timerFlashCano > 0.0f) timerFlashCano -= dt;

            // Atualização física das cápsulas ejetadas
            for (int i = 0; i < Capsulas.Count; i++)
            {
                Capsulas[i].Atualizar(dt, AlturaChao);
            }
            Capsulas.RemoveAll(c => c.TempoVida >= c.TempoMaxVida);

            // Atualização física e detonação das Granadas PB2
            for (int i = 0; i < Granadas.Count; i++)
            {
                Granadas[i].Atualizar(dt, plataformas, (trauma) => TraumaCameraAcumulado = Math.Max(TraumaCameraAcumulado, trauma));
            }
            Granadas.RemoveAll(g => g.Finalizada);
        }

        public void LancarGranada(Vector2 posicaoAlvo)
        {
            Vector2 ombro = ObterOmbro();
            Vector2 delta = posicaoAlvo - ombro;
            float dist = delta.Length();
            Vector2 dir;
            if (dist > 3.0f)
            {
                dir = delta / dist;
            }
            else
            {
                dir = new Vector2(DirecaoOlhar, -0.35f);
                dist = 200.0f;
            }

            float forca = Math.Clamp(dist * 1.60f, 260.0f, 680.0f);
            Vector2 spawn = ombro + (dir * 22.0f);
            Granadas.Add(new GranadaPB2(spawn, dir, forca, texGranada, somExplosaoGranada));

            TocarSomSeguro(somLancamentoGranada);
        }

        private void AtualizarArmaETiros(float dt, Vector2 posicaoRato, List<ProjetilPlasma> listaProjeteis, bool mouseSobreUI = false)
        {
            // Seleção de armas 1 a 9 estilo Plazma Burst 2
            if (Raylib.IsKeyPressed(KeyboardKey.One)) TrocarArma(0); // 1: Glock Compact (DefineSprite 2664)
            if (Raylib.IsKeyPressed(KeyboardKey.Two)) TrocarArma(1); // 2: Falcon Laser Pistol (DefineSprite 2669)
            if (Raylib.IsKeyPressed(KeyboardKey.Three)) TrocarArma(2); // 3: M4A1 Assault Rifle (DefineSprite 2739)
            if (Raylib.IsKeyPressed(KeyboardKey.Four)) TrocarArma(3); // 4: Heavy Minigun Cannon (DefineSprite 2745)
            if (Raylib.IsKeyPressed(KeyboardKey.Five)) TrocarArma(4); // 5: Plasma Rifle C-01p
            if (Raylib.IsKeyPressed(KeyboardKey.Six)) TrocarArma(5); // 6: Shotgun CS-DAZ
            if (Raylib.IsKeyPressed(KeyboardKey.Seven)) TrocarArma(6); // 7: Heavy Railgun Ray-01
            if (Raylib.IsKeyPressed(KeyboardKey.Eight)) TrocarArma(7); // 8: Sniper CS-BNG
            if (Raylib.IsKeyPressed(KeyboardKey.Nine)) TrocarArma(8); // 9: Android Sniper CS-ASR

            if (cooldownLancamentoGranada > 0.0f) cooldownLancamentoGranada -= dt;

            // Tecla "G" - Lançar Granada Oficial de Plazma Burst 2 (ItemGrenade - DefineSprite 2885)
            if (Raylib.IsKeyPressed(KeyboardKey.G) && cooldownLancamentoGranada <= 0.0f)
            {
                LancarGranada(posicaoRato);
                cooldownLancamentoGranada = 0.65f;
            }

            float scroll = Raylib.GetMouseWheelMove();
            if (scroll > 0.1f)
            {
                TrocarArma((ArmaAtualIndex - 1 + Arsenal.Count) % Arsenal.Count);
            }
            else if (scroll < -0.1f)
            {
                TrocarArma((ArmaAtualIndex + 1) % Arsenal.Count);
            }

            // Tecla R: Recarga manual
            if (Raylib.IsKeyPressed(KeyboardKey.R))
            {
                Recarregar();
            }

            // Atualização do ciclo de carga / reciclagem da arma
            if (ArmaAtual != null && ArmaAtual.RequerCarga)
            {
                if (ArmaAtual.CargaAtual < ArmaAtual.TempoCarga)
                {
                    ArmaAtual.CargaAtual += dt;
                    if (ArmaAtual.CargaAtual >= ArmaAtual.TempoCarga)
                    {
                        ArmaAtual.CargaAtual = ArmaAtual.TempoCarga;
                    }
                }
            }

            // Atualização do temporizador de recarga
            if (TimerRecarga > 0.0f)
            {
                TimerRecarga -= dt;
                if (TimerRecarga <= 0.0f && ArmaAtual != null)
                {
                    ArmaAtual.MunicaoAtual = ArmaAtual.CapacidadePente;
                    TimerRecarga = 0.0f;
                }
            }

            // Atualização da animação de saque das costas (equip)
            if (TimerAnimacaoEquip > 0.0f)
            {
                TimerAnimacaoEquip -= dt;
                if (TimerAnimacaoEquip <= 0.0f)
                {
                    TimerAnimacaoEquip = 0.0f;
                    // A arma agora está 100% na mão! Toca som de engatilhamento / empunhadura
                    TocarSomSeguro(somEquipArma);
                }
            }

            // Disparo: cadência, munição, ciclo de carga, SAQUE e auto-recarga
            cooldownDisparo -= dt;
            if (ArmaAtual != null && !EstaRecarregando)
            {
                bool atirarManual = !mouseSobreUI && (ArmaAtual.Automatica ? Raylib.IsMouseButtonDown(MouseButton.Left) : Raylib.IsMouseButtonPressed(MouseButton.Left));
                bool querAtirar = (ModoBotAtivo && botInputAtirar) || atirarManual;

                if (querAtirar && cooldownDisparo <= 0.0f)
                {
                    // 1. REQUISITO FUNDAMENTAL: Só pode atirar se a arma estiver na mão corretamente (saque finalizado!)
                    if (!ArmaEmpunhada)
                    {
                        // Bloqueado: o personagem ainda está sacando a arma das costas!
                    }
                    // 2. Armas como a Sniper só podem disparar se completarem a carga e o LED estiver verde!
                    else if (ArmaAtual.RequerCarga && !ArmaAtual.ProntoParaAtirar)
                    {
                        // Bloqueado pelo ciclo de carga
                    }
                    else if (ArmaAtual.MunicaoAtual > 0)
                    {
                        Disparar(posicaoRato, listaProjeteis);
                    }
                    else
                    {
                        // Sem munição: aciona recarga automática
                        Recarregar();
                    }
                }
            }
        }

        public void TrocarArma(int index)
        {
            if (Arsenal.Count == 0 || index < 0 || index >= Arsenal.Count) return;
            if (ArmaAtualIndex != index)
            {
                if (ArmaAtual != null && ArmaAtual.Tipo != "pistol")
                {
                    ArmaCostasIndex = ArmaAtualIndex; // Guarda a grande arma anterior nas costas
                }
                ArmaAtualIndex = index;
                TimerAnimacaoEquip = DuracaoAnimacaoEquip; // Inicia animação de saque (da cintura ou das costas)
                cooldownDisparo = DuracaoAnimacaoEquip;
                TimerRecarga = 0.0f; // Cancela recarga da arma anterior
                TocarSomSeguro(somTrocaArma);
            }
        }

        public ArmaPB2? ObterArmaNasCostas()
        {
            if (Arsenal.Count <= 1) return null;

            // Se o slot das costas for válido e diferente da arma da mão, usa ele
            if (ArmaCostasIndex >= 0 && ArmaCostasIndex < Arsenal.Count && ArmaCostasIndex != ArmaAtualIndex)
            {
                return Arsenal[ArmaCostasIndex];
            }

            // Caso contrário, pega a primeira arma do arsenal diferente da que está na mão
            for (int i = 0; i < Arsenal.Count; i++)
            {
                if (i != ArmaAtualIndex)
                {
                    return Arsenal[i];
                }
            }

            return null;
        }

        public void Recarregar()
        {
            if (ArmaAtual == null || EstaRecarregando) return;
            if (ArmaAtual.MunicaoAtual >= ArmaAtual.CapacidadePente) return;

            TimerRecarga = ArmaAtual.TempoRecarga;
            DuracaoRecargaTotal = ArmaAtual.TempoRecarga;
            TocarSomSeguro(somTrocaArma);
        }

        private void Disparar(Vector2 posicaoRato, List<ProjetilPlasma> listaProjeteis)
        {
            if (ArmaAtual == null || ArmaAtual.MunicaoAtual <= 0) return;

            ArmaAtual.MunicaoAtual--;
            cooldownDisparo = ArmaAtual.CadenciaTiro;
            recuoLinearAtual = ArmaAtual.RecuoLinear;
            recuoRotacaoAtual = ArmaAtual.RecuoRotacaoGraus;
            timerFlashCano = 0.08f;
            TraumaCameraAcumulado += ArmaAtual.TraumaCamera;

            // Se a arma requer carga (Sniper / Android Sniper), inicia o ciclo de reciclagem
            if (ArmaAtual.RequerCarga)
            {
                ArmaAtual.CargaAtual = 0.0f;
                TocarSomSeguro(ArmaAtual.SomCarga);
            }

            TocarSomSeguro(ArmaAtual.SomTiro);

            Vector2 dirMira = new Vector2(MathF.Cos(AnguloMiraRad), MathF.Sin(AnguloMiraRad));
            Vector2 pontaCano = ObterPontaDoCano();
            Vector2 posArma = ObterPosicaoArma();

            // 1. Impulso físico corporal do recuo (PB2 self-boosting / rocket jumping)
            Velocidade -= dirMira * ArmaAtual.ImpulsoFisico;

            // 2. Ejeção de cápsula / cartucho físico ou energia
            bool viradoEsquerda = DirecaoOlhar < 0;
            Vector2 pontoEjecao = posArma + (dirMira * 2.0f);
            float sinalEjetar = viradoEsquerda ? 1.0f : -1.0f;
            float velEjetarX = sinalEjetar * ((float)Random.Shared.NextDouble() * 90.0f + 60.0f);
            float velEjetarY = -((float)Random.Shared.NextDouble() * 130.0f + 70.0f);
            Capsulas.Add(new EfeitoCapsula(pontoEjecao, new Vector2(velEjetarX, velEjetarY) + (Velocidade * 0.25f), ArmaAtual.TipoCapsula));

            // 3. Disparo dos projéteis
            Vector2 direcaoBase = dirMira;
            Random rnd = new Random();
            for (int i = 0; i < ArmaAtual.QuantidadePellets; i++)
            {
                Vector2 dirFinal = direcaoBase;
                if (ArmaAtual.Dispersao > 0.0f)
                {
                    float offsetAngulo = ((float)rnd.NextDouble() * 2.0f - 1.0f) * ArmaAtual.Dispersao;
                    float cos = MathF.Cos(offsetAngulo);
                    float sin = MathF.Sin(offsetAngulo);
                    dirFinal = new Vector2(
                        direcaoBase.X * cos - direcaoBase.Y * sin,
                        direcaoBase.X * sin + direcaoBase.Y * cos
                    );
                }

                listaProjeteis.Add(new ProjetilPlasma(
                    pontaCano,
                    dirFinal,
                    ArmaAtual.VelocidadeProjetil,
                    ArmaAtual.CorProjetil,
                    ArmaAtual.Tipo
                ));
            }
        }

        public (Vector2 Posicao, float AnguloGraus) ObterPosicaoEAnguloArma()
        {
            bool viradoEsquerda = DirecaoOlhar < 0;
            float orientacao = viradoEsquerda ? -1.0f : 1.0f;
            float offsetAgacharY = transicaoAgachar * 15.0f;

            Vector2 quadril = Posicao + new Vector2(0, bobbingTroncoY + offsetAgacharY);
            Vector2 ombro = ObterOmbro();
            Vector2 dirMira = new Vector2(MathF.Cos(AnguloMiraRad), MathF.Sin(AnguloMiraRad));
            float distOmbro = ArmaAtual != null ? ArmaAtual.DistanciaOmbro : 18.0f;

            float inclinacaoAgachar = transicaoAgachar * (orientacao * 8.0f);
            float rotCorpoDeg = (inclinacaoCorpo * (180.0f / MathF.PI)) + inclinacaoAgachar;

            float sinalVertical = viradoEsquerda ? -1.0f : 1.0f;
            float anguloPronto = AnguloMiraGraus - (recuoRotacaoAtual * sinalVertical);
            Vector2 posPronta = ombro + (dirMira * (distOmbro - recuoLinearAtual));

            // Animação de saque estilo Plazma Burst 2 (cintura para pistolas, costas para armas grandes)
            if (TimerAnimacaoEquip > 0.0f)
            {
                float t = ProgressoEquip; // 0.0f (no coldre) -> 1.0f (empunhada na mão)
                bool ehPistola = ArmaAtual != null && ArmaAtual.Tipo == "pistol";

                // Ponto de origem do saque: da cintura para pistolas, do ombro/costas para grandes armas
                Vector2 posOrigem = ehPistola
                    ? quadril + new Vector2(orientacao * -3.0f, -5.0f)
                    : quadril + new Vector2(orientacao * -7.0f, -18.0f);

                // Ângulo inicial com cano apontado para baixo
                float baseAng = ehPistola ? 90.0f : (viradoEsquerda ? 85.0f : 95.0f);
                float anguloOrigem = baseAng + inclinacaoAgachar + (orientacao * (ehPistola ? balancoCostas * 0.4f : balancoCostas));

                Vector2 pontoTransicao = ehPistola
                    ? quadril + new Vector2(orientacao * 8.0f, -10.0f)
                    : ombro + new Vector2(orientacao * 2.0f, -13.0f);

                if (t < 0.40f)
                {
                    // Fase 1: Mão vai ao coldre/costas e puxa a arma
                    float subT = MathF.Sin((t / 0.40f) * MathF.PI * 0.5f);
                    Vector2 pos = Vector2.Lerp(posOrigem, pontoTransicao, subT);
                    float ang = Lerp(anguloOrigem, rotCorpoDeg + (orientacao * (ehPistola ? 10.0f : -15.0f)), subT);
                    return (pos, ang);
                }
                else
                {
                    // Fase 2: Arma vai para a postura de disparo na linha de mira
                    float subT = MathF.Sin(((t - 0.40f) / 0.60f) * MathF.PI * 0.5f);
                    Vector2 pos = Vector2.Lerp(pontoTransicao, posPronta, subT);
                    float ang = Lerp(rotCorpoDeg + (orientacao * (ehPistola ? 10.0f : -15.0f)), anguloPronto, subT);
                    return (pos, ang);
                }
            }

            return (posPronta, anguloPronto);
        }

        public Vector2 ObterPosicaoArma() => ObterPosicaoEAnguloArma().Posicao;
        public float ObterAnguloArmaGraus() => ObterPosicaoEAnguloArma().AnguloGraus;

        public Vector2 ObterPontaDoCano()
        {
            bool viradoEsquerda = DirecaoOlhar < 0;
            float sinalVertical = viradoEsquerda ? -1.0f : 1.0f;
            var (posArma, anguloArmaGraus) = ObterPosicaoEAnguloArma();

            float anguloArmaRad = anguloArmaGraus * (MathF.PI / 180.0f);
            Vector2 dirMiraArma = new Vector2(MathF.Cos(anguloArmaRad), MathF.Sin(anguloArmaRad));
            Vector2 normalMiraArma = new Vector2(-dirMiraArma.Y, dirMiraArma.X);

            float compCano = ArmaAtual != null ? ArmaAtual.ComprimentoCano : 34.0f;
            float altCano = ArmaAtual != null ? ArmaAtual.AlturaCanoY : 0.0f;

            return posArma + (dirMiraArma * compCano) + (normalMiraArma * (altCano * sinalVertical));
        }

        public void Desenhar()
        {
            bool viradoEsquerda = DirecaoOlhar < 0;
            float orientacao = viradoEsquerda ? -1.0f : 1.0f;

            float offsetAgacharY = transicaoAgachar * 15.0f;
            Vector2 quadril = Posicao + new Vector2(0, bobbingTroncoY + offsetAgacharY);
            Vector2 ombro = Posicao - new Vector2(0, 16) + new Vector2(0, bobbingTroncoY + offsetAgacharY);

            // Tronco inclina levemente para a frente ao agachar (postura tática PB2)
            float inclinacaoAgachar = transicaoAgachar * (orientacao * 8.0f);
            float rotCorpoDeg = (inclinacaoCorpo * (180.0f / MathF.PI)) + inclinacaoAgachar;

            // Posição central e ângulo dinâmico da arma (com saque das costas e recuo)
            var (posArma, anguloArmaGraus) = ObterPosicaoEAnguloArma();
            float anguloArmaRad = anguloArmaGraus * (MathF.PI / 180.0f);
            Vector2 dirMiraArma = new Vector2(MathF.Cos(anguloArmaRad), MathF.Sin(anguloArmaRad));
            Vector2 normalMiraArma = new Vector2(-dirMiraArma.Y, dirMiraArma.X);

            float sinalVertical = viradoEsquerda ? -1.0f : 1.0f;
            float offGatX = ArmaAtual != null ? ArmaAtual.OffsetGatilhoX : 6.0f;
            float offGatY = ArmaAtual != null ? ArmaAtual.OffsetGatilhoY : 3.0f;
            float offSupX = ArmaAtual != null ? ArmaAtual.OffsetSuporteX : 21.0f;
            float offSupY = ArmaAtual != null ? ArmaAtual.OffsetSuporteY : 1.0f;

            // Pontos exatos onde as mãos seguram a arma
            // 1. Mão do gatilho (frente): empunhadura da arma durante todo o saque e tiro
            Vector2 posMaoGatilho = posArma + (dirMiraArma * offGatX) + (normalMiraArma * (offGatY * sinalVertical));

            // 2. Mão de apoio (trás):
            Vector2 posMaoSuporte;
            if (EstaRecarregando)
            {
                float progresso = 1.0f - (TimerRecarga / DuracaoRecargaTotal);
                Vector2 pontoCintura = quadril + new Vector2(orientacao * 3.0f, 6.0f);
                Vector2 pontoReceptor = posArma + (dirMiraArma * offGatX) + (normalMiraArma * (5.0f * sinalVertical));

                if (progresso < 0.45f)
                {
                    float t = progresso / 0.45f;
                    posMaoSuporte = Vector2.Lerp(posArma + (dirMiraArma * offSupX), pontoCintura, t);
                }
                else
                {
                    float t = (progresso - 0.45f) / 0.55f;
                    posMaoSuporte = Vector2.Lerp(pontoCintura, pontoReceptor, t);
                }
            }
            else if (!ArmaEmpunhada)
            {
                // Durante o saque das costas: mão de apoio aguarda no peito e depois sobe para travar na arma
                float tEquip = ProgressoEquip;
                Vector2 posDescanso = quadril + new Vector2(orientacao * 4.0f, -8.0f);
                Vector2 posSuportePronta = posArma + (dirMiraArma * offSupX) + (normalMiraArma * (offSupY * sinalVertical));

                if (tEquip < 0.55f)
                {
                    posMaoSuporte = posDescanso;
                }
                else
                {
                    float subT = MathF.Sin(((tEquip - 0.55f) / 0.45f) * MathF.PI * 0.5f);
                    posMaoSuporte = Vector2.Lerp(posDescanso, posSuportePronta, subT);
                }
            }
            else
            {
                posMaoSuporte = posArma + (dirMiraArma * offSupX) + (normalMiraArma * (offSupY * sinalVertical));
            }

            // Ombros dianteiro e traseiro
            Vector2 ombroTras = ombro + new Vector2(viradoEsquerda ? 3.5f : -3.5f, -2.0f);
            Vector2 ombroFrente = ombro + new Vector2(viradoEsquerda ? -2.5f : 2.5f, 1.0f);

            // Resolução IK dos Braços (Ambos os cotovelos flexionam naturalmente para baixo em direção ao corpo)
            Vector2 cotoveloTras = ResolverIK(ombroTras, posMaoSuporte, ComprimentoBraco, ComprimentoAntebraco, flexaoInvertida: viradoEsquerda);
            Vector2 cotoveloFrente = ResolverIK(ombroFrente, posMaoGatilho, ComprimentoBraco, ComprimentoAntebraco, flexaoInvertida: viradoEsquerda);

            // -------------------------------------------------------------
            // CAMADA 1: Perna Traseira (IK de Joelho - desenhada atrás)
            // -------------------------------------------------------------
            DesenharPernaComIK(
                quadril + new Vector2(viradoEsquerda ? 2.5f : -2.5f, 0),
                cicloAndar + MathF.PI,
                orientacao,
                Skin.CoxaTras,
                Skin.CanelaTras,
                corTraseira,
                ehDianteira: false
            );

            // -------------------------------------------------------------
            // CAMADA 2: Braço de Apoio Traseiro (Ombro -> Cotovelo -> Mão)
            // -------------------------------------------------------------
            DesenharSegmentoTexturizado(ombroTras, cotoveloTras, Skin.BracoTras, 0.45f, viradoEsquerda, corTraseira);
            Raylib.DrawCircleV(cotoveloTras, 3.2f, corArticulacao);
            DesenharSegmentoTexturizado(cotoveloTras, posMaoSuporte, Skin.AntebracoTras, 0.45f, viradoEsquerda, corTraseira);
            Raylib.DrawCircleV(posMaoSuporte, 2.5f, corArticulacao);

            // -------------------------------------------------------------
            // CAMADA 2.5: Grande Arma nas Costas (apontada para baixo, do ombro à cintura)
            // -------------------------------------------------------------
            int camada = 0;
            for (int i = 0; i < Arsenal.Count; i++)
            {
                if (i == ArmaAtualIndex) continue; // Pula a arma que está na mão

                ArmaPB2 arma = Arsenal[i];
                DesenharArmaNasCostas(arma, quadril, rotCorpoDeg, viradoEsquerda, camada);
                camada++;
            }

            // -------------------------------------------------------------
            // CAMADA 3: Tronco (peça central modular)
            // -------------------------------------------------------------
            DesenharTronco(quadril, rotCorpoDeg, viradoEsquerda);

            // Obtém a arma atualmente guardada nas costas (se houver)
            ArmaPB2? armaCostas = ObterArmaNasCostas();

            // Alça da bandoleira cruzando o peito sobre o tronco se houver arma nas costas
            if (camada > 0)
            {
                Vector2 alcaPeito1 = ombro + new Vector2(orientacao * -4.0f, -4.0f);
                Vector2 alcaPeito2 = quadril + new Vector2(orientacao * 5.0f, 4.0f);
                Raylib.DrawLineEx(alcaPeito1, alcaPeito2, 2.4f, new Color(30, 35, 45, 230));
            }

            // -------------------------------------------------------------
            // CAMADA 3.5: Pistola na Cintura (armas pequenas no coldre lateral)
            // -------------------------------------------------------------
            DesenharArmaNaCintura(quadril, viradoEsquerda);

            // -------------------------------------------------------------
            // CAMADA 4: Cabeça (peça modular independente)
            // -------------------------------------------------------------
            DesenharCabeca(quadril, rotCorpoDeg + (rotacaoCabeca * (180.0f / MathF.PI)), viradoEsquerda);

            // -------------------------------------------------------------
            // CAMADA 5: Perna Dianteira (IK de Joelho - desenhada na frente)
            // -------------------------------------------------------------
            DesenharPernaComIK(
                quadril + new Vector2(viradoEsquerda ? -2.5f : 2.5f, 0),
                cicloAndar,
                orientacao,
                Skin.CoxaFrente,
                Skin.CanelaFrente,
                corDianteira,
                ehDianteira: true
            );

            // -------------------------------------------------------------
            // CAMADA 6: Arma (rotaciona 360° em direção ao mouse com recuo angular)
            // -------------------------------------------------------------
            DesenharArma(posArma, anguloArmaGraus, viradoEsquerda);

            // -------------------------------------------------------------
            // CAMADA 7: Braço Principal Dianteiro (Ombro -> Cotovelo -> Gatilho)
            // -------------------------------------------------------------
            DesenharSegmentoTexturizado(ombroFrente, cotoveloFrente, Skin.BracoFrente, 0.48f, viradoEsquerda, corDianteira);
            Raylib.DrawCircleV(ombroFrente, 3.6f, corArticulacao);
            Raylib.DrawCircleV(cotoveloFrente, 3.4f, corArticulacao);
            DesenharSegmentoTexturizado(cotoveloFrente, posMaoGatilho, Skin.AntebracoFrente, 0.48f, viradoEsquerda, corDianteira);
            Raylib.DrawCircleV(posMaoGatilho, 2.8f, corArticulacao);

            // -------------------------------------------------------------
            // CAMADA 8: Flash de disparo estilizado no cano (Muzzle Flare PB2)
            // -------------------------------------------------------------
            if (timerFlashCano > 0.0f && ArmaAtual != null)
            {
                Vector2 ponta = ObterPontaDoCano();
                DesenharFlashCano(ponta, dirMiraArma, ArmaAtual.CorProjetil);
            }

            // Desenhar Cápsulas Físicas Ejetadas (acima do personagem)
            for (int i = 0; i < Capsulas.Count; i++)
            {
                Capsulas[i].Desenhar();
            }
        }

        private void DesenharPernaComIK(Vector2 quadrilPerna, float fasePasso, float orientacao, Texture2D texCoxa, Texture2D texCanela, Color corTint, bool ehDianteira)
        {
            Vector2 alvoPe;

            if (NoChao)
            {
                // Transição suave entre parado e a caminhar
                float fatorCorrida = Math.Clamp(MathF.Abs(Velocidade.X) / 30.0f, 0.0f, 1.0f);
                float escalaPassoAgachado = 1.0f - (transicaoAgachar * 0.35f);

                // Movimento do passo (avanço e elevação)
                float oscilacaoLocal = -MathF.Sin(fasePasso) * 14.0f * fatorCorrida * escalaPassoAgachado;
                float elevacaoY = MathF.Max(0.0f, -MathF.Cos(fasePasso)) * 8.0f * fatorCorrida * escalaPassoAgachado;

                // Base dos pés mais junta (elimina a postura aberta/quebrada estilo tesoura)
                float baseDianteira = 1.5f + (transicaoAgachar * 2.0f);
                float baseTraseira = -1.5f - (transicaoAgachar * 1.5f);
                float offsetParado = ehDianteira ? baseDianteira : baseTraseira;
                float xLocal = (offsetParado * (1.0f - fatorCorrida)) + oscilacaoLocal;

                // CORREÇÃO: O alvo do pé aponta para o chão real (Posicao.Y).
                // O quadril já desce offsetAgacharY px ao agachar, portanto usar
                // quadrilPerna.Y + fixo fazia o pé passar o chão. Agora é independente.
                alvoPe = new Vector2(
                    quadrilPerna.X + (xLocal * orientacao),
                    Posicao.Y + 33.0f - elevacaoY
                   );
            }
            else
            {
                // Poses no ar durante saltos ou quedas
                if (Velocidade.Y < 0.0f)
                {
                    alvoPe = quadrilPerna + new Vector2(orientacao * -3.5f, 24.0f);
                }
                else
                {
                    alvoPe = quadrilPerna + new Vector2(orientacao * 2.0f, 33.0f);
                }
            }

            // Cinemática Inversa: garante matematicamente que o joelho dobra SEMPRE para a frente
            Vector2 joelho = ResolverIKPerna(quadrilPerna, alvoPe, ComprimentoCoxa, ComprimentoCanela, orientacao);

            // 1. Coxa (Quadril -> Joelho)
            DesenharSegmentoTexturizado(quadrilPerna, joelho, texCoxa, 0.48f, orientacao < 0, corTint);
            Raylib.DrawCircleV(quadrilPerna, 4.0f, corArticulacao);

            // 2. Articulação do Joelho
            Raylib.DrawCircleV(joelho, 3.8f, corArticulacao);

            // 3. Canela e Pé (Joelho -> Alvo Pé)
            DesenharSegmentoTexturizado(joelho, alvoPe, texCanela, 0.48f, orientacao < 0, corTint);
            Raylib.DrawCircleV(alvoPe, 3.0f, corArticulacao);
        }

        // 1. Cinemática para as PERNAS (nunca quebra o joelho para trás)
        // Cinemática Inversa analítica que garante flexão anatômica sempre para a frente (estilo PB2)
        private static Vector2 ResolverIKPerna(Vector2 inicio, Vector2 alvo, float l1, float l2, float orientacao)
        {
            Vector2 delta = alvo - inicio;
            float dist = delta.Length();

            // Evita divisão por zero e limita ao alcance anatômico máximo
            dist = Math.Clamp(dist, 1.0f, (l1 + l2) - 0.01f);

            float anguloDireto = MathF.Atan2(delta.Y, delta.X);

            // Lei dos Cossenos
            float cosAlfa = (l1 * l1 + dist * dist - l2 * l2) / (2.0f * l1 * dist);
            float alfa = MathF.Acos(Math.Clamp(cosAlfa, -1.0f, 1.0f));

            // Força o joelho a dobrar sempre para a frente do olhar
            float anguloSegmento1 = (orientacao >= 0.0f)
                ? anguloDireto - alfa
                : anguloDireto + alfa;

            return inicio + new Vector2(MathF.Cos(anguloSegmento1) * l1, MathF.Sin(anguloSegmento1) * l1);
        }

        // 2. Cinemática para os BRAÇOS (cotovelos - mantida separada)
        private static Vector2 ResolverIK(Vector2 inicio, Vector2 alvo, float l1, float l2, bool flexaoInvertida)
        {
            Vector2 delta = alvo - inicio;
            float dist = Math.Clamp(delta.Length(), 1.0f, (l1 + l2) - 0.5f);
            float anguloDireto = MathF.Atan2(delta.Y, delta.X);

            float cosAlfa = (l1 * l1 + dist * dist - l2 * l2) / (2.0f * l1 * dist);
            float alfa = MathF.Acos(Math.Clamp(cosAlfa, -1.0f, 1.0f));

            float anguloSegmento1 = anguloDireto + (flexaoInvertida ? -alfa : alfa);

            return inicio + new Vector2(
                MathF.Cos(anguloSegmento1) * l1,
                MathF.Sin(anguloSegmento1) * l1
            );
        }

        private void DesenharSegmentoTexturizado(Vector2 pInicio, Vector2 pFim, Texture2D textura, float escalaLargura, bool viradoEsquerda, Color tint)
        {
            if (textura.Id == 0) return;

            Vector2 delta = pFim - pInicio;
            float dist = delta.Length();
            if (dist < 0.1f) return;

            float anguloRad = MathF.Atan2(delta.Y, delta.X);
            float anguloDeg = anguloRad * (180.0f / MathF.PI);

            float larg = textura.Width * escalaLargura;
            float alt = dist + (7.0f * escalaLargura);

            Rectangle src = new Rectangle(0, 0, viradoEsquerda ? -textura.Width : textura.Width, textura.Height);
            Rectangle dst = new Rectangle(pInicio.X, pInicio.Y, larg, alt);
            Vector2 origem = new Vector2(larg * 0.5f, 4.0f * escalaLargura);

            Raylib.DrawTexturePro(textura, src, dst, origem, anguloDeg - 90.0f, tint);
        }

        private void DesenharTronco(Vector2 quadril, float rotacaoDeg, bool viradoEsquerda)
        {
            if (Skin.Tronco.Id == 0) return;

            float escala = 0.5f;
            float larg = Skin.Tronco.Width * escala;
            float alt = Skin.Tronco.Height * escala;

            Rectangle src = new Rectangle(0, 0, viradoEsquerda ? -Skin.Tronco.Width : Skin.Tronco.Width, Skin.Tronco.Height);
            Rectangle dst = new Rectangle(quadril.X, quadril.Y - 10.0f, larg, alt);
            Vector2 origem = new Vector2(larg * 0.5f, alt * 0.5f);

            Raylib.DrawTexturePro(Skin.Tronco, src, dst, origem, rotacaoDeg, Color.White);
        }

        private void DesenharCabeca(Vector2 quadril, float rotacaoDeg, bool viradoEsquerda)
        {
            if (Skin.Cabeca.Id == 0) return;

            float escala = 0.5f;
            float larg = Skin.Cabeca.Width * escala;
            float alt = Skin.Cabeca.Height * escala;

            Rectangle src = new Rectangle(0, 0, viradoEsquerda ? -Skin.Cabeca.Width : Skin.Cabeca.Width, Skin.Cabeca.Height);
            Rectangle dst = new Rectangle(quadril.X, quadril.Y - 26.0f + bobbingCabecaY, larg, alt);
            Vector2 origem = new Vector2(larg * 0.5f, alt * 0.75f);

            Raylib.DrawTexturePro(Skin.Cabeca, src, dst, origem, rotacaoDeg, Color.White);
        }

        private void DesenharArmaNasCostas(ArmaPB2 armaCostas, Vector2 quadril, float rotCorpoDeg, bool viradoEsquerda, int camada = 0)
        {
            if (armaCostas == null || armaCostas.Textura.Id == 0) return;

            float orientacao = viradoEsquerda ? -1.0f : 1.0f;

            // Espaçamento e leque angular de cada arma nas costas
            Vector2 posCostas = quadril + new Vector2(orientacao * (-3.5f - (camada * 1.6f)), -16.0f + (camada * 0.4f));
            float baseAngulo = viradoEsquerda ? (80.0f - (camada * 2.2f)) : (100.0f + (camada * 2.2f));
            float inclinacaoAgachar = transicaoAgachar * (orientacao * 6.0f);
            float anguloCostas = baseAngulo + inclinacaoAgachar + (orientacao * balancoCostas);

            // Usa frame limpo (sem feixe de laser aceso)
            Texture2D tex = armaCostas.FramesAnimacao.Count > 0 ? armaCostas.FramesAnimacao[0] : armaCostas.Textura;

            // Tamanho adequado nas costas
            float fatorTamanho = (armaCostas.Tipo == "sniper" || armaCostas.Tipo == "android_sniper") ? 0.55f : 0.78f;
            float escala = armaCostas.Escala * fatorTamanho;
            float larg = tex.Width * escala;
            float alt = tex.Height * escala;

            Rectangle src = new Rectangle(
                0, 0,
                tex.Width,
                viradoEsquerda ? -tex.Height : tex.Height
            );

            Rectangle dst = new Rectangle(posCostas.X, posCostas.Y, larg, alt);
            Vector2 origem = new Vector2(larg * 0.12f, alt * 0.18f);

            // Tom ligeiramente mais sombreado nas armas mais ao fundo para profundidade
            byte tom = (byte)Math.Clamp(180 - (camada * 8), 110, 255);
            Color tintCostas = new Color(tom, (byte)(tom + 5), (byte)(tom + 15), (byte)255);

            Raylib.DrawTexturePro(tex, src, dst, origem, anguloCostas, tintCostas);
        }

        private void DesenharArmaNaCintura(Vector2 quadril, bool viradoEsquerda)
        {
            // Localiza a pistola no arsenal que não esteja na mão
            ArmaPB2? pistola = Arsenal.Find(a => a.Tipo == "pistol" && a != ArmaAtual);
            if (pistola == null || pistola.Textura.Id == 0) return;

            float orientacao = viradoEsquerda ? -1.0f : 1.0f;

            // Posição lateral no cinto: deslocada para FORA do corpo (longe dos braços)
            Vector2 posCintura = quadril + new Vector2(orientacao * 9.0f, -4.0f);

            // Apontando para BAIXO (+90°), com suave balanço ao caminhar
            float inclinacaoAgachar = transicaoAgachar * (orientacao * 3.0f);
            float anguloPistola = 90.0f + inclinacaoAgachar + (orientacao * (balancoCostas * 0.35f));

            Texture2D tex = pistola.Textura;
            float escala = pistola.Escala * 0.48f;
            float larg = tex.Width * escala;
            float alt = tex.Height * escala;

            Rectangle src = new Rectangle(
                0, 0,
                tex.Width,
                viradoEsquerda ? -tex.Height : tex.Height
            );

            Rectangle dst = new Rectangle(posCintura.X, posCintura.Y, larg, alt);
            Vector2 origem = new Vector2(larg * 0.50f, alt * 0.20f);

            Color tintCintura = new Color((byte)180, (byte)190, (byte)205, (byte)255);
            Raylib.DrawTexturePro(tex, src, dst, origem, anguloPistola, tintCintura);

            // Coldre tático de polímero na cintura (pequena linha visual)
            Raylib.DrawLineEx(
                posCintura + new Vector2(-3.5f, 3.0f),
                posCintura + new Vector2(3.5f, 3.0f),
                2.5f, new Color((byte)25, (byte)30, (byte)40, (byte)220));
        }

        private void DesenharArma(Vector2 posArma, float anguloGraus, bool viradoEsquerda)
        {
            var arma = ArmaAtual;
            if (arma == null || arma.Textura.Id == 0) return;

            Texture2D tex = arma.Textura;

            // Se possui frames de animação originais de PB2 (DefineSprite 720 / 2704)
            if (arma.FramesAnimacao.Count > 0)
            {
                if (arma.RequerCarga && !arma.ProntoParaAtirar)
                {
                    float progresso = Math.Clamp(arma.CargaAtual / arma.TempoCarga, 0.0f, 1.0f);
                    int totalCiclo = arma.FramesAnimacao.Count - 2;
                    int fIndex = Math.Clamp(2 + (int)(progresso * (totalCiclo - 1)), 2, arma.FramesAnimacao.Count - 1);
                    tex = arma.FramesAnimacao[fIndex];
                }
                else
                {
                    int fIndex = Math.Min(1, arma.FramesAnimacao.Count - 1);
                    tex = arma.FramesAnimacao[fIndex];
                }
            }

            float escala = arma.Escala;
            float larg = tex.Width * escala;
            float alt = tex.Height * escala;

            Rectangle src = new Rectangle(
                0, 0,
                tex.Width,
                viradoEsquerda ? -tex.Height : tex.Height
            );

            Rectangle dst = new Rectangle(posArma.X, posArma.Y, larg, alt);
            float pNormX = arma.PivotNormX;
            float pNormY = arma.PivotNormY;
            Vector2 origem = new Vector2(larg * pNormX, alt * pNormY);

            // 1. Renderiza o corpo da arma
            Raylib.DrawTexturePro(tex, src, dst, origem, anguloGraus, Color.White);

            // 2. Sistema de Indicador de Carga / Diodo LED e Aura (DefineSprite 718 e 717)
            if (arma.RequerCarga)
            {
                float sinalY = viradoEsquerda ? -1.0f : 1.0f;
                float localX = (arma.PosDiodeNormX * larg) - origem.X;
                float localY = ((arma.PosDiodeNormY * alt) - origem.Y) * sinalY;

                float rad = anguloGraus * (MathF.PI / 180.0f);
                float cos = MathF.Cos(rad);
                float sin = MathF.Sin(rad);

                Vector2 posDiodeMundo = new Vector2(
                    posArma.X + (localX * cos - localY * sin),
                    posArma.Y + (localX * sin + localY * cos)
                );

                if (!ArmaEmpunhada)
                {
                    Raylib.DrawCircleV(posDiodeMundo, 2.0f, new Color(40, 20, 20, 140));
                }
                else if (arma.ProntoParaAtirar)
                {
                    float pulso = 0.90f + 0.10f * MathF.Sin((float)Raylib.GetTime() * 6.0f);

                    if (arma.Tipo == "android_sniper")
                    {
                        Raylib.DrawCircleV(posDiodeMundo, 13.0f * pulso, new Color(255, 30, 20, (int)(35 * pulso)));
                        Raylib.DrawCircleV(posDiodeMundo, 9.0f * pulso, new Color(255, 50, 30, (int)(80 * pulso)));
                        Raylib.DrawCircleV(posDiodeMundo, 5.5f * pulso, new Color(255, 80, 40, (int)(160 * pulso)));
                        Raylib.DrawCircleV(posDiodeMundo, 3.2f, new Color(255, 120, 60, 255));
                        Raylib.DrawCircleV(posDiodeMundo, 1.5f, Color.White);
                    }
                    else
                    {
                        Raylib.DrawCircleV(posDiodeMundo, 13.0f * pulso, new Color(0, 255, 180, (int)(38 * pulso)));
                        Raylib.DrawCircleV(posDiodeMundo, 9.0f * pulso, new Color(30, 255, 170, (int)(85 * pulso)));
                        Raylib.DrawCircleV(posDiodeMundo, 5.5f * pulso, new Color(70, 255, 190, (int)(165 * pulso)));
                        Raylib.DrawCircleV(posDiodeMundo, 3.2f, new Color(130, 255, 210, 255));
                        Raylib.DrawCircleV(posDiodeMundo, 1.5f, Color.White);
                    }
                }
                else
                {
                    float freqPisca = (float)Raylib.GetTime() * 18.0f;
                    bool ledAceso = MathF.Sin(freqPisca) > 0.0f;

                    if (ledAceso)
                    {
                        Color corPisca = (arma.Tipo == "android_sniper")
                            ? new Color(255, 45, 20, 240)
                            : new Color(255, 125, 0, 240);

                        Raylib.DrawCircleV(posDiodeMundo, 10.0f, new Color((int)corPisca.R, (int)corPisca.G, (int)corPisca.B, 45));
                        Raylib.DrawCircleV(posDiodeMundo, 6.5f, new Color((int)corPisca.R, (int)corPisca.G, (int)corPisca.B, 95));
                        Raylib.DrawCircleV(posDiodeMundo, 3.2f, corPisca);
                        Raylib.DrawCircleV(posDiodeMundo, 1.5f, new Color(255, 240, 180, 255));
                    }
                    else
                    {
                        Raylib.DrawCircleV(posDiodeMundo, 2.0f, new Color(60, 20, 10, 160));
                    }
                }
            }

            // 3. Laser de Mira Tático da Sniper
            if (arma.TemLaser && ArmaEmpunhada)
            {
                Vector2 ponta = ObterPontaDoCano();
                float rad = anguloGraus * (MathF.PI / 180.0f);
                Vector2 dirLaser = new Vector2(MathF.Cos(rad), MathF.Sin(rad));
                float alcanceLaser = 1200.0f;
                Vector2 fimLaser = ponta + (dirLaser * alcanceLaser);

                if (arma.ProntoParaAtirar)
                {
                    Raylib.DrawLineEx(ponta, fimLaser, 1.4f, arma.CorLaser);
                    Raylib.DrawLineEx(ponta, fimLaser, 0.6f, Color.White);
                    Raylib.DrawCircleV(fimLaser, 3.2f, arma.CorLaser);
                    Raylib.DrawCircleV(fimLaser, 1.6f, Color.White);
                }
                else
                {
                    float freqPiscaLaser = (float)Raylib.GetTime() * 18.0f;
                    if (MathF.Sin(freqPiscaLaser) > 0.0f)
                    {
                        Raylib.DrawLineEx(ponta, fimLaser, 0.9f, new Color((int)arma.CorLaser.R, (int)arma.CorLaser.G, (int)arma.CorLaser.B, 75));
                    }
                }
            }
        }

        private void DesenharFlashCano(Vector2 ponta, Vector2 dirMira, Color corArma)
        {
            float escalaFlash = ArmaAtual?.Tipo == "shotgun" ? 1.45f : (ArmaAtual?.Tipo == "railgun" ? 1.6f : 1.0f);
            float raio = 8.5f * escalaFlash;

            Raylib.DrawCircleV(ponta, raio * 1.5f, new Color(corArma.R, corArma.G, corArma.B, (byte)90));
            Raylib.DrawCircleV(ponta, raio * 0.85f, new Color(corArma.R, corArma.G, corArma.B, (byte)220));
            Raylib.DrawCircleV(ponta, raio * 0.45f, Color.White);

            Vector2 normal = new Vector2(-dirMira.Y, dirMira.X);
            Raylib.DrawLineEx(ponta - normal * (raio * 1.1f), ponta + normal * (raio * 1.1f), 2.2f, Color.White);
            Raylib.DrawLineEx(ponta, ponta + dirMira * (raio * 2.2f), 3.0f, Color.White);
            Raylib.DrawLineEx(ponta, ponta + (dirMira + normal * 0.7f) * (raio * 1.5f), 1.8f, new Color(corArma.R, corArma.G, corArma.B, (byte)220));
            Raylib.DrawLineEx(ponta, ponta + (dirMira - normal * 0.7f) * (raio * 1.5f), 1.8f, new Color(corArma.R, corArma.G, corArma.B, (byte)220));
        }

        private static float Lerp(float a, float b, float t) => a + (b - a) * Math.Clamp(t, 0.0f, 1.0f);
        private static float MoverEmDirecao(float atual, float alvo, float maxDelta)
        {
            if (MathF.Abs(alvo - atual) <= maxDelta) return alvo;
            return atual + MathF.Sign(alvo - atual) * maxDelta;
        }

        private static Texture2D CarregarTexturaSegura(string caminho)
        {
            if (System.IO.File.Exists(caminho))
            {
                Texture2D tex = Raylib.LoadTexture(caminho);
                Raylib.SetTextureFilter(tex, TextureFilter.Bilinear);
                return tex;
            }
            Console.WriteLine($"[AVISO] Textura não encontrada: {caminho}");
            return default;
        }

        private static Sound CarregarSomSeguro(string caminho)
        {
            if (System.IO.File.Exists(caminho)) return Raylib.LoadSound(caminho);
            Console.WriteLine($"[AVISO] Som não encontrado: {caminho}");
            return default;
        }

        private static void TocarSomSeguro(Sound som)
        {
            if (som.Stream.Buffer != IntPtr.Zero)
            {
                Raylib.SetSoundPitch(som, 0.95f + (float)Random.Shared.NextDouble() * 0.1f);
                Raylib.PlaySound(som);
            }
        }

        public void Dispose()
        {
            Skin?.Dispose();
            foreach (var arma in Arsenal) arma.Dispose();
            if (texGranada.Id > 0) Raylib.UnloadTexture(texGranada);

            if (somPulo.Stream.Buffer != IntPtr.Zero) Raylib.UnloadSound(somPulo);
            if (somPasso.Stream.Buffer != IntPtr.Zero) Raylib.UnloadSound(somPasso);
            if (somTrocaArma.Stream.Buffer != IntPtr.Zero) Raylib.UnloadSound(somTrocaArma);
            if (somEquipArma.Stream.Buffer != IntPtr.Zero) Raylib.UnloadSound(somEquipArma);
            if (somLancamentoGranada.Stream.Buffer != IntPtr.Zero) Raylib.UnloadSound(somLancamentoGranada);
            if (somExplosaoGranada.Stream.Buffer != IntPtr.Zero) Raylib.UnloadSound(somExplosaoGranada);
        }
    }
}
