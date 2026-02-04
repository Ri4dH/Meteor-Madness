
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Media;
using System.Windows.Forms;
using static Meteor_Madness.Bullet;



namespace Meteor_Madness
{
    public partial class Form1 : Form
    {
        private Timer gameTimer;
        //to store bullets
        private List<Bullet> bullets = new List<Bullet>();
        //to add bullet count
        private int bulletCount = 10;
        private const int maxBullets = 10;
        private Timer bulletRegenTimer;
        //the timer that counts when the bullet regens which starts at 10
        private int bulletRegenTimeLeft = 10;
        //to spawn meteors
        private List<Meteor> meteors = new List<Meteor>();
        private Random random = new Random();
        private Timer meteorSpawnTimer;
        //tracking player lives
        private int playerLives = 3;
        private bool gameOver = false;
        //prevent multiple pop ups
        private bool gameOverDisplayed = false;
        //to make sure this path is correct
        Image heartImage = Image.FromFile("heart.png");
        //to load the space image
        Image spaceBackground = Image.FromFile("space.jpeg");
        //declare the score variable
        private int score = 0;
        //declare timer variables 
        private int timeLeft = 60; // 60 second per level

        //implementing levels
        private int currentLevel = 1;
        private const int maxLevel = 3;
        private int levelDuration = 60;


        private bool isBossLevel = false;
        private bool gameWon = false;


        private AlienShip alienBoss = null;

        public static float PlayerX;
        public static float PlayerY;



        public Form1()
        {
            InitializeComponent();
            this.DoubleBuffered = true;  // Prevents flickering
            bulletRegenTimer = new Timer();
            bulletRegenTimer.Interval = 10000;
            bulletRegenTimer.Tick += RegenerateBullet;
            bulletRegenTimer.Start();
            this.KeyDown += new KeyEventHandler(OnKeyDown);
            this.KeyUp += new KeyEventHandler(OnKeyUp);
            this.MouseDown += new MouseEventHandler(OnMouseDown);
            //another timer to decrease the countdown
            Timer countdownTimer = new Timer();
            countdownTimer.Interval = 1000;//1 second
            countdownTimer.Tick += CountdownTick;
            countdownTimer.Start();
            // Timer for spawning the meteors
            meteorSpawnTimer = new Timer();
            meteorSpawnTimer.Interval = 300; // every 1 second
            meteorSpawnTimer.Tick += SpawnMeteor;
            meteorSpawnTimer.Start();

            gameTimer = new Timer();
            gameTimer.Interval = 16; // around 60 FPS
            gameTimer.Tick += new EventHandler(GameLoop);
            gameTimer.Start();

            gameTimer = new Timer();
            gameTimer.Interval = 1000; // 1 second
            gameTimer.Tick += GameTimer_Tick;
            gameTimer.Start();

            //to make the game run in full screen
            this.FormBorderStyle = FormBorderStyle.None;
            this.WindowState = FormWindowState.Maximized;
            this.Bounds = Screen.PrimaryScreen.Bounds;

            //to make the character spawn in the middle of the screen
            this.Shown += Form1_Shown;

        }

        //to make the character spawn in the middle of the screen
        private void Form1_Shown(object sender, EventArgs e)
        {
            //center the player
            playerX = ClientSize.Width / 2;
            playerY = ClientSize.Height / 2;
        }


        //Path to store file of the high score
        // ---------- High‑Score helpers ----------
        private readonly string highScorePath =
            Path.Combine(Environment.GetFolderPath(
                Environment.SpecialFolder.LocalApplicationData),
                "MeteorMadness", "highscore.txt");

        // high score gets loaded from the file
        //if the file does not exist or somehow is corrupted it will return 0
        private int LoadHighScore()
        {
            try
            {
                if (File.Exists(highScorePath)) { 
                string content = File.ReadAllText(highScorePath);
                if (int.TryParse(content, out int score))
                    return score;
            }
            }
            catch { /* ignore corrupt file */ }
            return 0;
        }

        //saves the new high score in a file
        private void SaveHighScore(int newScore)
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(highScorePath)); //checks if directory exists befor the file is written
                File.WriteAllText(highScorePath, newScore.ToString()); //new high score gets written
            }
            catch { /* ignore write errors */ }
        }

        //game state gets updated base on the user input
        private void GameLoop(object sender, EventArgs e)
        {

            if (leftPressed) angle -= 5;
            if (rightPressed) angle += 5;

            if (upPressed) speed = Math.Min(speed + 0.2f, 5);
            else speed = Math.Max(speed - 0.1f, 0);  // Gradual slowdown

            // Move the player based on angle
            playerX += (float)(Math.Cos(angle * Math.PI / 180) * speed);
            playerY += (float)(Math.Sin(angle * Math.PI / 180) * speed);

            PlayerX = playerX;
            PlayerY = playerY;

            // Screen wrapping logic
            if (playerX < 0) playerX = this.ClientSize.Width;
            if (playerX > this.ClientSize.Width) playerX = 0;
            if (playerY < 0) playerY = this.ClientSize.Height;
            if (playerY > this.ClientSize.Height) playerY = 0;

            //Move the bullet
            foreach (var bullet in bullets)
                bullet.Move();
            // Remove the bullets that go off the screen
            bullets.RemoveAll(b => b.x < 0 || b.x > this.ClientSize.Width || b.y < 0 || b.y > this.ClientSize.Height);

            //move the meteors
            for (int i = 0; i < meteors.Count; i++)
            {
                meteors[i].Move();
            }
            CheckCollisions(); // Detect bullet meteor collisons 
            CheckPlayerCollision(); //detect player and meteor collision

            //game over condition if player loses all lives 
            if (playerLives <= 0)
            {
                GameOver();

            }

            // logic boss level
            if (isBossLevel && alienBoss != null)
            {
                foreach (var laser in alienBoss.Lasers)
                    laser.Move();

                //checks collisions for the boss's lasers
                CheckLaserPlayerCollision();
                CheckLaserBulletCollisions();
            }


            this.Invalidate(); // Refresh screen
        }

        //overrides onPaint method
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            Graphics g = e.Graphics;

            //adding the space background
            g.DrawImage(spaceBackground, 0, 0, ClientSize.Width, ClientSize.Height);

            // Convert angle to radians
            float rad = (float)(angle * Math.PI / 180);

            // Calculate triangle points relative to playerX, playerY
            PointF p1 = new PointF(
                playerX + (float)(Math.Cos(rad) * 20),
                playerY + (float)(Math.Sin(rad) * 20)
            );

            PointF p2 = new PointF(
                playerX + (float)(Math.Cos(rad + 2.5) * 15),
                playerY + (float)(Math.Sin(rad + 2.5) * 15)
            );

            PointF p3 = new PointF(
                playerX + (float)(Math.Cos(rad - 2.5) * 15),
                playerY + (float)(Math.Sin(rad - 2.5) * 15)
            );

            // Draw the triangle
            g.FillPolygon(Brushes.White, new PointF[] { p1, p2, p3 });

            // Drawing bullets
            foreach (var bullet in bullets)
                bullet.Draw(g);


            // Drawing the bullet count at the top right
            string bulletText = $"Bullets: {bulletCount}/{maxBullets}";
            string timerText = bulletCount < maxBullets ? $"Next Bullet: {bulletRegenTimeLeft}s" : "Full Ammo";

            Font font = new Font("Impact", 14);
            Brush brush = Brushes.White;

            SizeF bulletSize = g.MeasureString(bulletText, font);
            SizeF timerSize = g.MeasureString(timerText, font);

            g.DrawString(bulletText, font, brush, this.ClientSize.Width - bulletSize.Width - 10, 10);
            g.DrawString(timerText, font, brush, this.ClientSize.Width - timerSize.Width - 10, 30);

            //drawing hearts

            for (int i = 0; i < playerLives; i++)
            {
                g.DrawImage(heartImage, 10 + (i * 40), 10, 32, 32);
            }
            base.OnPaint(e);

            //drawing meteorite
            foreach (var meteor in meteors)
            {
                meteor.Draw(g);
            }

            //Drawing a score board
            e.Graphics.DrawString($"Score: {score}", new Font("Impact", 18, FontStyle.Bold), Brushes.White, ClientSize.Width / 2 - 60, 10);

            //Drawing a timer 
            e.Graphics.DrawString($"Time: {timeLeft}", new Font("Impact", 14, FontStyle.Bold), Brushes.White, ClientSize.Width / 2 - 50, 40);

            //Showing high score during pla
            e.Graphics.DrawString($"High Score: {LoadHighScore()}", new Font("Impact", 14, FontStyle.Bold), Brushes.White, ClientSize.Width / 2 - 60, 60);

            //drawing an Alien ship and lasers
            if (isBossLevel && alienBoss != null)
            {
                alienBoss.Draw(g);
                foreach (var laser in alienBoss.Lasers)
                    laser.Draw(g);
            }

        }


        //player movement
        private bool upPressed, leftPressed, rightPressed;
        private float playerX = 100, playerY = 100, angle = 0;
        private float speed = 0;


        //key press events to control game
        private void OnKeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Up || e.KeyCode == Keys.W) upPressed = true;
            if (e.KeyCode == Keys.Left || e.KeyCode == Keys.A) leftPressed = true;
            if (e.KeyCode == Keys.Right || e.KeyCode == Keys.D) rightPressed = true;
            //make it shoot bullets
            if (e.KeyCode == Keys.Space) ShootBullet();
        }

        private void OnKeyUp(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Up || e.KeyCode == Keys.W) upPressed = false;
            if (e.KeyCode == Keys.Left || e.KeyCode == Keys.A) leftPressed = false;
            if (e.KeyCode == Keys.Right || e.KeyCode == Keys.D) rightPressed = false;
        }

        private void OnMouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                ShootBullet();
            }
        }

        //new bullet at the player angle and position, also plays sound
        private void ShootBullet()
        {
            if (bulletCount > 0)
            {
                bullets.Add(new Bullet(playerX, playerY, angle));
                bulletCount--;


                //plays sound when player fires a bullet
                SoundPlayer playerBulletSound = new SoundPlayer(Properties.Resources.pewpewpew);
                playerBulletSound.Play();
            }

        }
        //this will regenerate bullets
        private void RegenerateBullet(object sender, EventArgs e)
        {
            if (bulletCount < maxBullets)
            {
                bulletCount++; //to increase the bullet count
                bulletRegenTimeLeft = 10; // To reset the countdown of the timer
            }
        }

        //the method that handles the countdown of the bullet regen
        private void CountdownTick(object sender, EventArgs e)
        {
            if (bulletCount < maxBullets && bulletRegenTimeLeft > 0)
            {
                bulletRegenTimeLeft--; //to decrease the countdown
                this.Invalidate(); //to refresh the screen
            }
        }

        // To spawn meteors at random position
        private void SpawnMeteor(object sender, EventArgs e)
        {

            int size = random.Next(20, 60); // meteors will be between 20 to 60 pixels
            float x, y;
            // spawn at random edges
            int edge = random.Next(4);
            switch (edge)
            {
                case 0: // Top
                    x = random.Next(ClientSize.Width);
                    y = -size; // start slightly offscreen
                    break;
                case 1: // Bottom
                    x = random.Next(ClientSize.Width);
                    y = ClientSize.Height + size;
                    break;
                case 2: // Left
                    x = -size;
                    y = random.Next(ClientSize.Height);
                    break;
                case 3: // Right
                    x = ClientSize.Width + size;
                    y = random.Next(ClientSize.Height);
                    break;
                default:
                    x = 0;
                    y = 0;
                    break;
            }

            // Actually spawn the meteor
            meteors.Add(new Meteor(x, y, size, playerX, playerY));
        }
        // this function checks bullet-meteor collision and break the meteor into smaller ones when hit
        private void CheckCollisions()
        {
            List<Bullet> bulletsToRemove = new List<Bullet>();
            List<Meteor> meteorsToRemove = new List<Meteor>();
            List<Meteor> newMeteors = new List<Meteor>();

            foreach (var meteor in meteors)
            {
                foreach (var bullet in bullets)
                {
                    if (meteor.IsHit(bullet))
                    {
                        // Accurate distance-based collision
                        float dx = meteor.X - bullet.x;
                        float dy = meteor.Y - bullet.y;
                        float distance = (float)Math.Sqrt(dx * dx + dy * dy);

                        if (distance < meteor.Size / 2)
                        {
                            bulletsToRemove.Add(bullet);
                            meteorsToRemove.Add(meteor);
                            score += 100;

                            //break into 2 smaller meteroids if size is big enough
                            if (meteor.Size > 30)
                            {
                                newMeteors.Add(new Meteor(meteor.X, meteor.Y, meteor.Size / 2, playerX, playerY));
                                newMeteors.Add(new Meteor(meteor.X, meteor.Y, meteor.Size / 2, playerX, playerY));
                            }

                            //plays sound when bullet collides with meteor
                            SoundPlayer rockBreakSound = new SoundPlayer(Properties.Resources.rockExplode);
                            rockBreakSound.Play();


                            break;
                        }
                    }

                }
            }
            // Removing destroyed meteors and bullets
            foreach (var bullet in bulletsToRemove)
                bullets.Remove(bullet);
            foreach (var meteor in meteorsToRemove)
                meteors.Remove(meteor);



            // Add new smaller meteors
            meteors.AddRange(newMeteors);
        }


        // the funtion checks if the player collides with meteors
        private void CheckPlayerCollision()
        {
            for (int i = meteors.Count - 1; i >= 0; i--)
            {
                var meteor = meteors[i];
                float dx = playerX - meteor.X;
                float dy = playerY - meteor.Y;
                float distance = (float)Math.Sqrt(dx * dx + dy * dy);

                if (distance < meteor.Size / 2 + 10) // collision range
                {
                    playerLives--; // u lose a life
                    meteors.RemoveAt(i); //remove meteoroids without error

                    //plays sound when player loses a life
                    SoundPlayer lifeLostSound = new SoundPlayer(Properties.Resources.lostLife);
                    lifeLostSound.Play();
                }
            }
        }

        // to show the pop up window and reset the game
        private void GameOver()
        {
            if (gameOverDisplayed) return; //makes sure no dublicate popups

            gameOverDisplayed = true;
            gameOver = true;

            int previousHigh = LoadHighScore();
            if (score > previousHigh)
            {
                SaveHighScore(score);
                previousHigh = score;
            }

            if (isBossLevel)
            {
                //stop alien boss
                alienBoss?.Stop();
                alienBoss = null;
                isBossLevel = false;
            }
            //plays sound when player loses game
            SoundPlayer gameOverSound = new SoundPlayer(Properties.Resources.gameLost);
            gameOverSound.Play();

            // makes a new form for the game over screen
            Form gameOverForm = new Form
            {
                Text = "Game Over",
                Size = new Size(300, 200),
                StartPosition = FormStartPosition.CenterScreen
            };

            Label message = new Label
            {
                Text = $"You have lost!\nYour score is: {score}\nHigh Score: {previousHigh}",
                Font = new Font("Roboto", 10),  //make sure this is not too big
                AutoSize = true,
                TextAlign = ContentAlignment.MiddleCenter,
                Location = new Point(85, 30)
            };

            Button tryAgainButton = new Button
            {
                Text = "Try Again",
                Size = new Size(100, 30),
                Location = new Point(100, 80)
            };

            Button quitButton = new Button
            {
                Text = "Quit",
                Size = new Size(100, 30),
                Location = new Point(100, 120)
            };

            tryAgainButton.Click += (sender, e) =>
            {
                gameOverForm.Close();
                RestartGame();
            };

            quitButton.Click += (sender, e) =>
            {
                Application.Exit(); //clean exit
            };

            gameOverForm.Controls.Add(message);
            gameOverForm.Controls.Add(tryAgainButton);
            gameOverForm.Controls.Add(quitButton);

            gameOverForm.ShowDialog();


        }

        //
        private void QuitButton_Click(object sender, EventArgs e)
        {
            throw new NotImplementedException();
        }

        //restart game button
        private void RestartGame()
        {
            playerLives = 3;
            bulletCount = 10;
            score = 0;
            timeLeft = 60;
            speed = 0;

            currentLevel = 1;
            meteorSpawnTimer.Interval = 300;

            alienBoss = null;
            isBossLevel = false;
            gameWon = false;
            isBossLevelMessageShown = false;


            bullets.Clear();
            meteors.Clear();
            // Reset player position to center
            playerX = ClientSize.Width / 2;
            playerY = ClientSize.Height / 2;

            //Reset game states
            gameOver = false;
            gameOverDisplayed = false;

            //Restart timers
            gameTimer.Start();
            meteorSpawnTimer.Start();

            //Refresh Screen
            Invalidate();

        }

        private void GameTimer_Tick(object sender, EventArgs e)
        {
            timeLeft--; // decrement time

            // Boss level already active
            if (isBossLevel)
            {
                if (timeLeft <= 0)
                {
                    WinGame(); // Survived boss level!
                }
            }
            else
            {
                //if time is out win the game

                if (timeLeft <= 0)
                {
                    currentLevel++;

                    //if next level is boss level
                    if (!isBossLevel && currentLevel >= maxLevel)
                    {
                        StartBossLevel(); // Launch boss level
                    }
                    else
                    {
                        timeLeft = levelDuration; //reset time left
                        IncreaseDifficulty();
                    }
                }
            }

            Invalidate(); // Redraw UI with updated time/level
        }

        private void IncreaseDifficulty()
        {
            // stopping it here if we want level 3 to be Boss level
            if (currentLevel == maxLevel && !isBossLevel)
            {
                StartBossLevel(); // triggers boss level instead
                return;
            }

            meteors.Clear(); //removes all the meteors when leveling up



            //increase spawn rate
            if (currentLevel == 2)
            {
                meteorSpawnTimer.Interval = 150; //faster spawn rate for level 2

            }
            else
            {
                meteorSpawnTimer.Interval = Math.Max(200, meteorSpawnTimer.Interval - 200);
            }

            MessageBox.Show($"Level {currentLevel} begins!\nMore meteors are coming faster!");
        }

        private bool isBossLevelMessageShown = false;

        private void StartBossLevel()
        {

            isBossLevel = true;
            timeLeft = 120;
            meteors.Clear();

            // Slow meteor rate to once every 3-4 second
            meteorSpawnTimer.Interval = 1000; //1 secs

            alienBoss = new AlienShip(ClientSize.Width / 2, 100);

            if (!isBossLevelMessageShown)
            {
                DisplayBossLevelMessage();
            }

        }

        private void DisplayBossLevelMessage()
        {
            MessageBox.Show("!!! BOSS FIGHT !!!\nSurvive for 2 Minutes");


            isBossLevelMessageShown = true;
        }

        private void WinGame()
        {
            if (gameWon) return;

            gameWon = true;
            gameTimer.Stop();
            meteorSpawnTimer.Stop();



            int previousHigh = LoadHighScore();

            if (score > previousHigh) 
            {
                SaveHighScore(score);
                previousHigh = score;
            }

            Form winForm = new Form
            {
                Text = "You Won!",
                Size = new Size(300, 200),
                StartPosition = FormStartPosition.CenterScreen
            };

            Label message = new Label
            {
                Text = $"You beat the game!\nYour final score: {score}\nHigh Score: {previousHigh}",
                Font = new Font("Roboto", 10),
                AutoSize = true,
                TextAlign = ContentAlignment.MiddleCenter,
                Location = new Point(70, 30)
            };

            Button playAgain = new Button
            {
                Text = "Play Again",
                Size = new Size(100, 40),
                Location = new Point(100, 80)
            };

            Button quitButton = new Button
            {
                Text = "Quit",
                Size = new Size(100, 40),
                Location = new Point(100, 120)
            };

            playAgain.Click += (s, e) =>
            {
                winForm.Close();
                RestartGame();
            };

            quitButton.Click += (sender, e) =>
            {
                Application.Exit();
            };

            winForm.Controls.Add(message);
            winForm.Controls.Add(playAgain);
            winForm.Controls.Add(quitButton);

            winForm.ShowDialog();

            alienBoss?.Stop();
        }

        private void CheckLaserPlayerCollision()
        {
            if (alienBoss == null || alienBoss.Lasers == null) return;

            for (int i = alienBoss.Lasers.Count - 1; i >= 0; i--)
            {
                if (alienBoss.Lasers[i].HitsPlayer(playerX, playerY))
                {
                    playerLives--;
                    alienBoss.Lasers.RemoveAt(i);

                    SoundPlayer hitSound = new SoundPlayer(Properties.Resources.lostLife);
                    hitSound.Play();

                    if (playerLives <= 0)
                    {
                        if (alienBoss != null)
                        {

                            alienBoss.Stop();

                        }
                        alienBoss = null;
                        GameOver();
                        return;
                    }

                }
            }
        }

        //check collision between laser and player
        private void CheckLaserBulletCollisions()
        {
            if (alienBoss == null || alienBoss.Lasers == null) return; //prevents crash if boss hansn't spawned yet

            List<Laser> lasersToRemove = new List<Laser>();
            List<Bullet> bulletsToRemove = new List<Bullet>();

            foreach (var laser in alienBoss.Lasers)
            {
                foreach (var bullet in bullets)
                {
                    if (laser.IsHit(bullet))
                    {
                        lasersToRemove.Add(laser);
                        bulletsToRemove.Add(bullet);
                    }
                }
            }

            foreach (var laser in lasersToRemove)
                alienBoss.Lasers.Remove(laser);
            foreach (var bullet in bulletsToRemove)
                bullets.Remove(bullet);
        }

    }



    public class Bullet
    {
        public float x, y, angle;
        private float speed = 7;
        public bool isActive = true;

        public Bullet(float startX, float startY, float startAngle)
        {
            x = startX;
            y = startY;
            angle = startAngle;
        }

        public void Move()
        {
            x += (float)(Math.Cos(angle * Math.PI / 180) * speed);
            y += (float)(Math.Sin(angle * Math.PI / 180) * speed);
        }


        public class Meteor
        {
            public float X, Y;
            public float SpeedX, SpeedY;
            public int Size; //to make sure how big the meteor is
            private static Random rand = new Random();

            public float RotationAngle = 0f;
            public float RotationSpeed = 0f;
            private PointF[] shapePoints;
            public Meteor(float x, float y, int size, float targetX, float targetY)
            {
                X = x;
                Y = y;
                Size = size;

                // Direction towards the player
                float dx = targetX - x;
                float dy = targetY - y;
                float length = (float)Math.Sqrt(dx * dy * dx + dy * dy);

                // fixing the direction and applying random speed
                SpeedX = (dx / length) * (5 + (float)rand.NextDouble() * 5); // speed 5 - 10
                SpeedY = (dy / length) * (5 + (float)rand.NextDouble() * 5);

                RotationSpeed = (float)(rand.NextDouble() * 4 - 2); // -2 to 2 degrees

                GenerateShape();
            }

            private void GenerateShape()
            {
                int pointsCount = 10;
                shapePoints = new PointF[pointsCount];
                double angleStep = Math.PI * 2 / pointsCount;
                double radius = Size / 2.0;

                for (int i = 0; i < pointsCount; i++)
                {
                    double angle = i * angleStep;
                    double jaggedness = rand.NextDouble() * 0.4 + 0.8;
                    float px = (float)(radius * jaggedness * Math.Cos(angle));
                    float py = (float)(radius * jaggedness * Math.Sin(angle));
                    shapePoints[i] = new PointF(px, py);
                }
            }

            public void Move()
            {
                X += SpeedX;
                Y += SpeedY;
                RotationAngle += RotationSpeed;

                if (RotationAngle >= 360f) RotationAngle -= 360f;
                if (RotationAngle < 0f) RotationAngle += 360f;
            }

            public void Draw(Graphics g)
            {
                if (shapePoints == null || shapePoints.Length < 3) return;

                // Apply rotation to shape
                PointF[] rotatedPoints = new PointF[shapePoints.Length];
                double rad = RotationAngle * Math.PI / 180.0;

                for (int i = 0; i < shapePoints.Length; i++)
                {
                    float px = shapePoints[i].X;
                    float py = shapePoints[i].Y;

                    float rotatedX = (float)(px * Math.Cos(rad) - py * Math.Sin(rad));
                    float rotatedY = (float)(px * Math.Sin(rad) + py * Math.Cos(rad));

                    rotatedPoints[i] = new PointF(X + rotatedX, Y + rotatedY);
                }

                using (Pen pen = new Pen(Color.Gray, 2))
                {
                    g.DrawPolygon(pen, rotatedPoints);
                }
            }

            // checks if the bullet hit the meteriod
            public bool IsHit(Bullet bullet)
            {
                float dx = X - bullet.x;
                float dy = Y - bullet.y;
                float distance = (float)Math.Sqrt(dx * dx + dy * dx);
                return distance < Size / 2; // collision happens if the bullet is inside the meteroid

            }
        }

        public void Draw(Graphics g)
        {
            g.FillEllipse(Brushes.Red, x - 3, y - 3, 6, 6);
        }
    }

    public class AlienShip
    {
        public float X, Y;
        private Timer laserTimer;
        private Random rand = new Random();
        public List<Laser> Lasers = new List<Laser>();


        private static readonly Image sprite = Properties.Resources.alienBoss;
        public AlienShip(float x, float y)
        {
            X = x;
            Y = y;
            laserTimer = new Timer();
            laserTimer.Interval = 1000;
            laserTimer.Tick += FireLaser;
            laserTimer.Start();
        }

        private void FireLaser(object sender, EventArgs e)
        {
            int shapeType = rand.Next(3); // different laser visuals
            float speed = 4 + (float)rand.NextDouble() * 4; // speed 4–8

            // Shoot toward player location
            float dx = Form1.PlayerX - X;
            float dy = Form1.PlayerY - Y;
            float length = (float)Math.Sqrt(dx * dx + dy * dy);
            float vx = (dx / length) * speed;
            float vy = (dy / length) * speed;

            Lasers.Add(new Laser(X, Y + 30, shapeType, vx, vy));
        }
        public void Draw(Graphics g)
        {
            //can make the boss as large as we want
            int width = 120;
            int height = 60;

            //Draw the bitmap centered on (X, Y)
            g.DrawImage(sprite, X - width / 2, Y - height / 2, width, height);
        }

        public void Stop()
        {
            laserTimer.Stop();
        }
    }

    public class Laser
    {
        public float X, Y;
        private int shapeType;
        private float vx, vy;

        public Laser(float x, float y, int type, float vx, float vy)
        {
            X = x;
            Y = y;
            shapeType = type;
            this.vx = vx;
            this.vy = vy;
        }

        public void Move()
        {
            X += vx;
            Y += vy;
        }

        public void Draw(Graphics g)
        {
            switch (shapeType)
            {
                case 0:
                    g.FillRectangle(Brushes.Red, X - 3, Y, 6, 20);
                    break;
                case 1:
                    g.FillEllipse(Brushes.Orange, X - 5, Y, 10, 15);
                    break;
                case 2:
                    PointF[] triangle = {
                    new PointF(X, Y),
                    new PointF(X - 5, Y + 15),
                    new PointF(X + 5, Y + 15)
                };
                    g.FillPolygon(Brushes.Yellow, triangle);
                    break;
            }
        }

        public bool IsHit(Bullet b)
        {
            float dx = X - b.x;
            float dy = Y - b.y;
            return Math.Sqrt(dx * dx + dy * dy) < 10; // collision threshold
        }

        public bool HitsPlayer(float px, float py)
        {
            float dx = X - px;
            float dy = Y - py;
            return Math.Sqrt(dx * dx + dy * dy) < 20;
        }


    }
}
