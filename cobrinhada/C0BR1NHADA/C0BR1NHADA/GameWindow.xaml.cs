using C0BR1NHADA;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace Cobrinhada
{
    public partial class GameWindow : Window
    {
        private List<Rectangle> snake = new List<Rectangle>();
        private List<Rectangle> comidas = new List<Rectangle>();
        private List<Rectangle> espinhos = new List<Rectangle>();
        private List<Rectangle> orbes = new List<Rectangle>();
        private List<string> powerUps = new List<string>();

        private Vector direction = new Vector(1, 0);
        private Vector nextDirection = new Vector(1, 0);
        private DispatcherTimer timer;

        private Rectangle escudoBolha;
        private bool escudoAtivo = false;
        private DateTime escudoFim;

        private int crescimento = 1;
        private double velocidade = 120;  // velocidade padrão
        private readonly double velocidadePadrao = 120;
        private int score = 0;
        private int bestScore = 0;

        private bool pausado = false;
        private Random rand = new Random();

        // Guardar timers para poder parar ao resetar o jogo
        private List<DispatcherTimer> powerUpTimers = new List<DispatcherTimer>();

        public GameWindow()
        {
            InitializeComponent();
            Loaded += (s, e) => IniciarJogo();
        }

        private void IniciarJogo()
        {
            // Reset geral
            GameCanvas.Children.Clear();
            snake.Clear();
            comidas.Clear();
            espinhos.Clear();
            orbes.Clear();
            powerUps.Clear();

            // Reset direções
            direction = new Vector(1, 0);
            nextDirection = new Vector(1, 0);
            escudoAtivo = false;
            velocidade = velocidadePadrao;
            crescimento = 1;
            score = 0;

            // Para qualquer timer de power-up ativo
            foreach (var t in powerUpTimers)
                t.Stop();
            powerUpTimers.Clear();

            CarregarBestScore();
            AtualizarScore();
            AtualizarListaPowerUps();

            Rectangle cabeca = CriarParteCobra(200, 200);
            snake.Add(cabeca);

            for (int i = 0; i < 20; i++) CriarEspinho();
            CriarComida();

            CriarOrbe("escudo", Brushes.Cyan);
            CriarOrbe("velocidade", Brushes.Yellow);
            CriarOrbe("tamanho", Brushes.Blue);

            escudoBolha = CriarBolha();
            GameCanvas.Children.Add(escudoBolha);
            escudoBolha.Visibility = Visibility.Hidden;

            if (timer != null)
                timer.Stop();

            timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromMilliseconds(velocidade);
            timer.Tick += GameLoop;
            timer.Start();
        }

        private Rectangle CriarParteCobra(double x, double y)
        {
            var fogo = new DropShadowEffect
            {
                Color = Colors.OrangeRed,
                BlurRadius = 15,
                ShadowDepth = 0,
                Opacity = 0.9
            };

            Rectangle r = new Rectangle
            {
                Width = 20,
                Height = 20,
                Fill = Brushes.Blue,
                Effect = fogo
            };
            GameCanvas.Children.Add(r);
            Canvas.SetLeft(r, x);
            Canvas.SetTop(r, y);
            return r;
        }

        private void CriarComida()
        {
            Rectangle comida = new Rectangle
            {
                Width = 20,
                Height = 20,
                Fill = new RadialGradientBrush(Colors.Red, Colors.DarkRed),
                RadiusX = 10,
                RadiusY = 10
            };
            PosicionarAleatorioSeguro(comida);
            GameCanvas.Children.Add(comida);
            comidas.Add(comida);
        }

        private void CriarEspinho()
        {
            Rectangle espinho = new Rectangle
            {
                Width = 20,
                Height = 20,
                Fill = Brushes.SlateGray,
            };
            PosicionarAleatorioSeguro(espinho);
            GameCanvas.Children.Add(espinho);
            espinhos.Add(espinho);
        }

        private void CriarOrbe(string tipo, Brush cor)
        {
            Rectangle orbe = new Rectangle
            {
                Width = 20,
                Height = 20,
                Fill = cor,
                RadiusX = 10,
                RadiusY = 10,
                Tag = tipo
            };
            PosicionarAleatorioSeguro(orbe);
            GameCanvas.Children.Add(orbe);
            orbes.Add(orbe);
        }

        private Rectangle CriarBolha()
        {
            return new Rectangle
            {
                Width = 30,
                Height = 30,
                Stroke = Brushes.Cyan,
                StrokeThickness = 3,
                RadiusX = 15,
                RadiusY = 15
            };
        }

        private void GameLoop(object sender, EventArgs e)
        {
            if (pausado) return;

            MoverCobra();
            VerificarColisoes();
            AtualizarEscudo();
        }

        private void MoverCobra()
        {
            direction = nextDirection;

            for (int i = snake.Count - 1; i > 0; i--)
            {
                Canvas.SetLeft(snake[i], Canvas.GetLeft(snake[i - 1]));
                Canvas.SetTop(snake[i], Canvas.GetTop(snake[i - 1]));
            }

            Rectangle cabeca = snake[0];
            Canvas.SetLeft(cabeca, Canvas.GetLeft(cabeca) + direction.X * 20);
            Canvas.SetTop(cabeca, Canvas.GetTop(cabeca) + direction.Y * 20);

            if (escudoAtivo)
            {
                Canvas.SetLeft(escudoBolha, Canvas.GetLeft(cabeca) - 5);
                Canvas.SetTop(escudoBolha, Canvas.GetTop(cabeca) - 5);
                escudoBolha.Visibility = Visibility.Visible;
            }
            else
            {
                escudoBolha.Visibility = Visibility.Hidden;
            }
        }

        private void VerificarColisoes()
        {
            Rectangle cabeca = snake[0];
            double x = Canvas.GetLeft(cabeca);
            double y = Canvas.GetTop(cabeca);
            double w = GameCanvas.ActualWidth;
            double h = GameCanvas.ActualHeight;

            if (x < 0 || x >= w || y < 0 || y >= h)
            {
                if (escudoAtivo)
                {
                    if (x < 0 || x >= w) nextDirection = new Vector(-direction.X, direction.Y);
                    if (y < 0 || y >= h) nextDirection = new Vector(direction.X, -direction.Y);
                }
                else
                {
                    GameOver("Bateu na parede!");
                    return;
                }
            }

            foreach (var espinho in espinhos)
            {
                if (Colide(cabeca, espinho) && !escudoAtivo)
                {
                    GameOver("Morreu no espinho!");
                    return;
                }
            }

            for (int i = comidas.Count - 1; i >= 0; i--)
            {
                if (Colide(cabeca, comidas[i]))
                {
                    score += crescimento;
                    AtualizarScore();

                    Rectangle novaParte = CriarParteCobra(-100, -100);
                    snake.Add(novaParte);

                    PosicionarAleatorioSeguro(comidas[i]);
                }
            }

            for (int i = orbes.Count - 1; i >= 0; i--)
            {
                if (Colide(cabeca, orbes[i]))
                {
                    string tipo = (string)orbes[i].Tag;
                    GameCanvas.Children.Remove(orbes[i]);
                    orbes.RemoveAt(i);

                    if (tipo == "escudo") AtivarEscudo();
                    if (tipo == "velocidade") AtivarVelocidade();
                    if (tipo == "tamanho") AtivarTamanho();

                    CriarOrbe(tipo,
                        tipo == "escudo" ? Brushes.Cyan :
                        tipo == "velocidade" ? Brushes.Yellow :
                        Brushes.Blue);
                }
            }
        }

        private void AtualizarEscudo()
        {
            if (escudoAtivo && DateTime.Now >= escudoFim)
            {
                escudoAtivo = false;
                escudoBolha.Visibility = Visibility.Hidden;
                RemoverPowerUp("Escudo");
            }
        }

        private void AtivarEscudo()
        {
            escudoAtivo = true;
            escudoFim = DateTime.Now.AddSeconds(10);
            AdicionarPowerUp("Escudo");
        }

        private void AtivarVelocidade()
        {
            // Para qualquer timer antigo de velocidade
            var timersToStop = powerUpTimers.Where(t => t.IsEnabled).ToList();
            foreach (var t in timersToStop)
            {
                t.Stop();
                powerUpTimers.Remove(t);
            }

            velocidade = velocidadePadrao / 2;
            timer.Interval = TimeSpan.FromMilliseconds(velocidade);
            AdicionarPowerUp("Velocidade");

            DispatcherTimer tmr = new DispatcherTimer { Interval = TimeSpan.FromSeconds(10) };
            tmr.Tick += (s, e) =>
            {
                tmr.Stop();
                velocidade = velocidadePadrao;
                timer.Interval = TimeSpan.FromMilliseconds(velocidade);
                RemoverPowerUp("Velocidade");
                powerUpTimers.Remove(tmr);
            };
            powerUpTimers.Add(tmr);
            tmr.Start();
        }

        private void AtivarTamanho()
        {
            // Para qualquer timer antigo de tamanho
            var timersToStop = powerUpTimers.Where(t => t.IsEnabled).ToList();
            foreach (var t in timersToStop)
            {
                t.Stop();
                powerUpTimers.Remove(t);
            }

            crescimento = 2;
            AdicionarPowerUp("Tamanho");

            DispatcherTimer tmr = new DispatcherTimer { Interval = TimeSpan.FromSeconds(60) };
            tmr.Tick += (s, e) =>
            {
                tmr.Stop();
                crescimento = 1;
                RemoverPowerUp("Tamanho");
                powerUpTimers.Remove(tmr);
            };
            powerUpTimers.Add(tmr);
            tmr.Start();
        }

        private void PosicionarAleatorioSeguro(Rectangle r)
        {
            double maxX = (int)(GameCanvas.ActualWidth / 20);
            double maxY = (int)(GameCanvas.ActualHeight / 20);
            double x, y;

            bool colisao;

            do
            {
                x = rand.Next(0, (int)maxX) * 20;
                y = rand.Next(0, (int)maxY) * 20;

                Canvas.SetLeft(r, x);
                Canvas.SetTop(r, y);

                colisao = false;
                // Verifica colisão com espinhos
                foreach (var esp in espinhos)
                {
                    if (Colide(r, esp))
                    {
                        colisao = true;
                        break;
                    }
                }
                // Verifica colisão com a cobra também pra spawn seguro
                if (!colisao)
                {
                    foreach (var parte in snake)
                    {
                        if (Colide(r, parte))
                        {
                            colisao = true;
                            break;
                        }
                    }
                }
            } while (colisao);
        }

        private bool Colide(Rectangle a, Rectangle b)
        {
            double ax = Canvas.GetLeft(a);
            double ay = Canvas.GetTop(a);
            double bx = Canvas.GetLeft(b);
            double by = Canvas.GetTop(b);
            return Math.Abs(ax - bx) < 1 && Math.Abs(ay - by) < 1;
        }

        private void AtualizarScore()
        {
            ScoreText.Text = $"Pontos: {score}";
            if (score > bestScore)
            {
                bestScore = score;
                File.WriteAllText("best.txt", bestScore.ToString());
            }
            BestScoreText.Text = $"Recorde: {bestScore}";
        }

        private void CarregarBestScore()
        {
            if (File.Exists("best.txt"))
                bestScore = int.Parse(File.ReadAllText("best.txt"));
        }

        private void AdicionarPowerUp(string p)
        {
            if (!powerUps.Contains(p))
                powerUps.Add(p);
            AtualizarListaPowerUps();
        }

        private void RemoverPowerUp(string p)
        {
            powerUps.Remove(p);
            AtualizarListaPowerUps();
        }

        private void AtualizarListaPowerUps()
        {
            PowerUpsListBox.Items.Clear();
            foreach (string p in powerUps)
                PowerUpsListBox.Items.Add(p);
        }

        private void GameOver(string msg)
        {
            timer.Stop();

            // Para timers ativos para evitar efeito continuando
            foreach (var t in powerUpTimers)
                t.Stop();
            powerUpTimers.Clear();

            MessageBox.Show(msg, "Game Over", MessageBoxButton.OK, MessageBoxImage.Warning);
            IniciarJogo();
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                AlternarPausa();
                return;
            }

            if (pausado) return;

            Vector nova = direction;
            if (e.Key == Key.W || e.Key == Key.Up) nova = new Vector(0, -1);
            if (e.Key == Key.S || e.Key == Key.Down) nova = new Vector(0, 1);
            if (e.Key == Key.A || e.Key == Key.Left) nova = new Vector(-1, 0);
            if (e.Key == Key.D || e.Key == Key.Right) nova = new Vector(1, 0);

            // Não deixa ir na direção oposta
            if (nova + direction != new Vector(0, 0))
                nextDirection = nova;
        }

        private void AlternarPausa()
        {
            pausado = !pausado;
            PauseOverlay.Visibility = pausado ? Visibility.Visible : Visibility.Collapsed;
            if (pausado) timer.Stop(); else timer.Start();
        }

        private void ResumeButton_Click(object sender, RoutedEventArgs e)
        {
            AlternarPausa();
        }

        private void MenuButton_Click(object sender, RoutedEventArgs e)
        {
            var r = MessageBox.Show("O jogo não será salvo. Deseja Voltar Ao Menu?", "Voltar ao Menu", MessageBoxButton.YesNo);
            if (r == MessageBoxResult.Yes)
            {
                new MainWindow().Show();
                this.Close();
            }
        }
    }
}
