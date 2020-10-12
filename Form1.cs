using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace 入侵者
{
    public partial class Invaders : Form
    {
        private Game game;
        private int animationCell = 0;
        private int frame = 0;

        public Invaders()
        {
            InitializeComponent();
            newGame();
        }

        private void game_GameOver(object sender, EventArgs e)
        {
            animationTimer.Stop();
            gameTimer.Stop();
            this.Invalidate();
        }

        private void animationTimer_Tick(object sender, EventArgs e)
        {
            frame++;
            if (frame > 5)
                frame =0;
            switch (frame)
            {
                case 0: animationCell = 0; break;
                case 1: animationCell = 1; break;
                case 2: animationCell = 2; break;
                case 3: animationCell = 3; break;
                case 4: animationCell = 2; break;
                case 5: animationCell = 1; break;
                default: animationCell = 0; break;
            }         
            this.Invalidate();
        }

        private void Form1_Paint(object sender, PaintEventArgs e)
        {
            game.Draw(e.Graphics, animationCell);
            game.Twinkle();
        }

        private void newGame()
        {
            game = new Game(this);
            game.NewLevel();
            animationTimer.Start();
            gameTimer.Start();
            game.GameOver += new EventHandler(game_GameOver);         
        }

        private void gameTimer_Tick(object sender, EventArgs e)
        {
            game.Go();
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            switch (keyData)
            {              
                case Keys.Left:
                    game.MovePlayer(Direction.Left);
                    break;
                case Keys.Right:
                    game.MovePlayer(Direction.Right);
                    break;       
                case Keys.Space:
                    game.FileShot();
                    break;
                case Keys.S:
                    newGame();
                    break;
                case Keys.Q:
                    this.Close();
                    break;
                default:
                    break;
            }
            return true;
        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }
    }

    public enum Direction
    {
        Left,
        Right,
        Up,
        Down
    }

    public class Game
    {
        private Invaders form;
        private int score = 0;//游戏分数
        private int livesLeft = 2;//玩家姓名
        private int wave = 0;//怪物波数
        private int framesSkipped = 20;//怪物移动等待帧数
        private int frame;
        private int invaderWidth = 70;//怪物左右间隔

        private int invaderShotsCount = 1;//怪物允许最大发射子弹量
        public int InvaderShotsCount { get { return invaderShotsCount; } }

        private int invaderHeight = 50;//怪物上下间隔
        private Point startPoint;//怪物群起始位置

        private Rectangle boundaries;//游戏边界
        private Random random = new Random();
        private bool deadFlag;
        private bool gameOver = false;

        private Direction invaderDirection = Direction.Right;//怪物方向
        private List<Invader> invaders;//怪物

        private PlayerShip playerShip = null;//玩家
        private List<Shot> playerShots = new List<Shot> ();//玩家发射的子弹
        private List<Shot> invaderShots = new List<Shot> ();//怪物的子弹

        private Stars stars;

        public event EventHandler GameOver;

        public Game(Invaders form)
        {
            this.form = form;
            Size size = new Size(form.ClientSize.Width - 100,form.ClientSize.Height);
            boundaries = new Rectangle(new Point(50, 0), size);       
            startPoint = new Point(boundaries.X + 10, boundaries.Y + 40);
            stars = new Stars(form, random);               
        }

        public bool NewLevel()
        {
            //随着关卡的提升 怪物移动的更快，子弹更多
            if (wave < 10)
            {
                framesSkipped--;
                frame = framesSkipped;
                invaderShotsCount++;
                invaders = new List<Invader>();
                playerShip = new PlayerShip(new Point(form.ClientSize.Width / 2, boundaries.Height - Properties.Resources.player.Height), true, boundaries);
                Point startPoint = this.startPoint;
                for (int i = 4; i >= 0; i--)
                {
                    for (int j = 0; j < 6; j++)
                    {
                        invaders.Add(new Invader((Invader.Type)i, startPoint, (i + 1) * 10, boundaries));
                        startPoint.X += invaderWidth;
                    }
                    startPoint.Y += invaderHeight;
                    startPoint.X = this.startPoint.X;
                }
                wave++;
                return true;
            }
            else
            { return false; }
                                     
        }

        public void Draw(Graphics g,int animationCell)
        {
            //右上角画出战舰剩余
            for (int i = livesLeft; i > 0; i--)
            {
                Point point = new Point(form.ClientSize.Width - i * playerShip.Area.Width - i * 2, 5);
                playerShip.Draw(g, point);
            }

            //左上角显示分数,中间显示关书
            using (Font arial20 = new Font("Arial", 20, FontStyle.Bold))
            {
                g.DrawString(score.ToString(), arial20, Brushes.Yellow, 0, 5);
                g.DrawString("Game Level " + (wave).ToString(), arial20, Brushes.Green, form.ClientSize.Width / 2 - 15, 5);
            }

            if (playerShip != null)
                playerShip.Draw(g);
            foreach (Invader invader in invaders)
                invader.Draw(g,animationCell);
            foreach (Shot shot in playerShots)
                shot.Draw(g);
            foreach (Shot shot in invaderShots)
                shot.Draw(g);

            //游戏结束
            if (gameOver)
            {
                using (Font aria64 = new Font("Arial", 64, FontStyle.Bold))
                {
                    Size fontSize = Size.Ceiling(g.MeasureString("Game Over!", aria64));
                    g.DrawString("Game Over!", aria64, Brushes.Red, form.ClientSize.Width / 2 - fontSize.Width / 2, form.ClientSize.Height / 2 - fontSize.Height);
                }

                using (Font arial20 = new Font("Arial", 20, FontStyle.Bold))
                {
                    Size fontSize = Size.Ceiling(g.MeasureString("S to restart，Q to quit", arial20));
                    g.DrawString("S to restart，Q to quit", arial20, Brushes.Yellow, form.ClientSize.Width-fontSize.Width-15, form.ClientSize.Height-fontSize.Height-5);
                }
            }
            
            stars.Draw(g);
            
        }

        public void Go()
        {
            //先判断游戏是否结束
            if (playerShip.Alive == false)
            {
                invaderShots.Clear();
                playerShots.Clear();
                if (livesLeft >= 0)
                {
                    //由于人物中弹将会改变飞船形状，这里生命将只减少一条
                    if (deadFlag != playerShip.Alive)
                    { livesLeft--; deadFlag = playerShip.Alive; }
                }
                else
                {
                    gameOver = true;
                }
            }

            else
            {
                if (invaders.Count == 0 && !gameOver)
                {
                    NewLevel();
                    return;
                }

                if (gameOver)
                {
                    if (GameOver != null)
                    {
                        playerShots.Clear();
                        invaders.Clear();
                        playerShip = null;
                        GameOver(null, null);
                        return;
                    }
                }

                deadFlag = playerShip.Alive;

                ReturnFire(random);

                //怪物移动  
                if (framesSkipped > 0)
                    framesSkipped--;
                else
                    invaders.ToList().ForEach(invader => { invader.HorizontalMove(invaderDirection); framesSkipped = frame; });

                //碰到左边界右移动
                if (invaders.FindIndex(invader => invader.Location.X <= boundaries.X) >= 0)
                {
                    if (framesSkipped > 0)
                        framesSkipped--;
                    else
                    {
                        invaders.ToList().ForEach(invader => { invader.VerticalMove(); framesSkipped = frame; });
                        invaderDirection = Direction.Right;
                    }
                }

                //碰到右边界左移动
                if (invaders.FindIndex(invader => invader.Location.X + invader.Area.Width >= boundaries.X + boundaries.Width) >= 0)
                {
                    if (framesSkipped > 0)
                        framesSkipped--;
                    else
                    {
                        invaders.ToList().ForEach(invader => { invader.VerticalMove(); framesSkipped = frame; });
                        invaderDirection = Direction.Left;
                    }
                }

                //碰到底线游戏结束
                if (invaders.FindIndex(invader => invader.Location.Y >= (boundaries.Height - playerShip.Area.Height - invader.Area.Height)) >= 0)
                {
                    gameOver = true;
                    return;
                }
                
                //玩家发射子弹中有击中目标的，删除怪物和子弹
                playerShots.ToList().ForEach(shot =>
                {
                    invaders.Where(invader => invader.Area.Contains(new Point(shot.Location.X, shot.Location.Y))).ToList().ForEach(invader =>
                    {
                        score += invader.Score;
                        invaders.Remove(invader);
                        playerShots.Remove(shot);
                    });
                });
                //玩家发射的子弹，飞出边界删除子弹
                playerShots.Where(shot => shot.Move() == false).ToList().ForEach(shot => playerShots.Remove(shot));

                //怪物发射子弹击中玩家时，减少玩家性命和删除子弹
                invaderShots.ToList().ForEach(shot =>
                {
                    if (playerShip.Area.Contains(new Point(shot.Location.X, shot.Location.Y + 15)))
                    {
                        playerShip.Alive = false;
                        invaderShots.Remove(shot);
                    }
                });
                //怪物发射子弹，飞出边界删除子弹
                invaderShots.Where(shot => shot.Move() == false).ToList().ForEach(shot => invaderShots.Remove(shot));
            }      
        }

        /// <summary>
        /// 向敌方开火开火
        /// </summary>
        public void FileShot()
        {
            if (playerShots.Count <2)
                playerShots.Add(playerShip.playerShot());
        }

        /// <summary>
        /// 怪物开火
        /// </summary>
        /// <param name="random"></param>
        private void ReturnFire(Random random)
        {
            if (random.Next(15) == 1 &&invaderShots.Count<invaderShotsCount)
            {
                var invaderG = from invader in invaders
                           group invader by invader.Location.X
                               into invaderGroup
                               select invaderGroup;
                var invaderRandom=invaderG.ElementAt(random.Next(invaderG.Count()));
                invaderShots.Add(invaderRandom.ElementAt(invaderRandom.Count()-1).InvaderShot());
            }        
        }

        /// <summary>
        /// 星星闪耀
        /// </summary>
        public void Twinkle()
        {
            stars.Twinkle(random);
        }

        /// <summary>
        /// 玩家飞船移动
        /// </summary>
        /// <param name="direction"></param>
        public void MovePlayer(Direction direction)
        {
            if (playerShip != null)
            {
                playerShip.Move(direction);
                form.Invalidate();
            }
        }
    }

    public class Invader
    {
        private const int HorizontalInterval = 10;
        private const int VertivalInterval = 40;
        private const int ImageWidth = 30;
        private const int ImageHeight = 30;
        Rectangle boundaries;

        public enum Type
        {
            Star,
            Spaceship,          
            Watchit,
            Bug,
            Satellite,                     
        }

        private Bitmap image;
        private Bitmap [] images;

        private Point location;
        public Point Location { get{return location;}  }

        public Type InvaderType { get; private set; }

        public Rectangle Area { get { return new Rectangle(location, image.Size); } }

        public int Score { get; private set; }

        public Invader(Type invaderType, Point location, int score, Rectangle boundaries)
        {
            this.InvaderType = invaderType;
            this.location = location;
            this.Score = score;
            InitialBitmap(invaderType);
            image = InvaderImage(0);
            this.boundaries = boundaries;
        }

        public void Draw(Graphics g, int aimationCell)
        { 
            g.DrawImage(InvaderImage(aimationCell),location.X, location.Y);
        }

        private void InitialBitmap(Type invaderType)
        { 
           images=new Bitmap[4];
           switch (invaderType)
           {
               case Type.Bug:
                   images[0] = ResizeImage(Properties.Resources.bug1, ImageWidth, ImageHeight);
                   images[1] = ResizeImage(Properties.Resources.bug2, ImageWidth, ImageHeight);
                   images[2] = ResizeImage(Properties.Resources.bug3, ImageWidth, ImageHeight);
                   images[3] = ResizeImage(Properties.Resources.bug4, ImageWidth, ImageHeight);
                   break;
               case Type.Satellite:
                   images[0] = ResizeImage(Properties.Resources.satellite1, ImageWidth, ImageHeight);
                   images[1] = ResizeImage(Properties.Resources.satellite2, ImageWidth, ImageHeight);
                   images[2] = ResizeImage(Properties.Resources.satellite3, ImageWidth, ImageHeight);
                   images[3] = ResizeImage(Properties.Resources.satellite4, ImageWidth, ImageHeight);
                   break;
               case Type.Watchit:
                   images[0] = ResizeImage(Properties.Resources.watchit1, ImageWidth, ImageHeight);
                   images[1] = ResizeImage(Properties.Resources.watchit2, ImageWidth, ImageHeight);
                   images[2] = ResizeImage(Properties.Resources.watchit3, ImageWidth, ImageHeight);
                   images[3] = ResizeImage(Properties.Resources.watchit4, ImageWidth, ImageHeight);
                   break;
               case Type.Spaceship:
                   images[0] = ResizeImage(Properties.Resources.spaceship1, ImageWidth, ImageHeight);
                   images[1] = ResizeImage(Properties.Resources.spaceship2, ImageWidth, ImageHeight);
                   images[2] = ResizeImage(Properties.Resources.spaceship3, ImageWidth, ImageHeight);
                   images[3] = ResizeImage(Properties.Resources.spaceship4, ImageWidth, ImageHeight);
                   break;
               case Type.Star:
                   images[0] = ResizeImage(Properties.Resources.star1, ImageWidth, ImageHeight);
                   images[1] = ResizeImage(Properties.Resources.star2, ImageWidth, ImageHeight);
                   images[2] = ResizeImage(Properties.Resources.star3, ImageWidth, ImageHeight);
                   images[3] = ResizeImage(Properties.Resources.star4, ImageWidth, ImageHeight);
                   break;
               default:
                   break;
           }
        }

        private Bitmap InvaderImage(int animationCell)
        {
            return images[animationCell];
        }

        public static Bitmap ResizeImage(Bitmap picture, int width, int height)
        {
            Bitmap resizedPicture = new Bitmap(width, height);
            using (Graphics graphics = Graphics.FromImage(resizedPicture))
            {
                graphics.DrawImage(picture, 0, 0, width, height);
            }
            return resizedPicture;
        }

        public void HorizontalMove(Direction direction)
        {
            switch (direction)
            { 
                case Direction.Left:
                    location.X -= HorizontalInterval;
                    break;
                case Direction.Right:
                    location.X += HorizontalInterval;
                    break;
            }
        }

        /// <summary>
        /// 竖直方向移动，判断怪物是否到达底界
        /// </summary>
        /// <returns></returns>
        public void VerticalMove()
        { 
           location.Y+=VertivalInterval;
        }

        public Shot InvaderShot()
        {
            return new Shot(new Point(location.X + Area.Width / 2, Area.Y+Area.Height), Direction.Down, boundaries);
        }
    }

    public class PlayerShip
    {
        private int deadShipHeight = 0;
        private int moveSpeed = 10;
        private int width = 45;
        private int height = 30;

        private Point location;
        public Point Location { get {return location ;} set{} }
        Rectangle boundaries;

        public Rectangle Area { get { return new Rectangle(location, Properties.Resources.player.Size); } }
        public bool Alive { get; set; }

        public PlayerShip(Point location, bool alive, Rectangle boundarie)
        {
            this.location = location;
            this.Alive = alive;          
            this.boundaries = boundarie;
        }

        public void Draw(Graphics g)
        {
            if (Alive)
            {
                g.DrawImage(Invader.ResizeImage(Properties.Resources.player,width,height), Location);
                deadShipHeight = Properties.Resources.player.Height;
            }
            else
            {            
                if (deadShipHeight > 0)
                {
                    g.DrawImage(Invader.ResizeImage(Properties.Resources.player, width, height), Location.X, boundaries.Height- deadShipHeight, width, deadShipHeight);
                    deadShipHeight = deadShipHeight - 2;
                }
                else
                {
                    this.Alive = true;
                }
            }
        }

        public void Draw(Graphics g, Point location)
        {
            g.DrawImage(Invader.ResizeImage(Properties.Resources.player, width, height), location);
        }

        public void Move(Direction direction)
        {
            switch (direction)
            { 
                case Direction.Left:
                    if(location.X>boundaries.X-Area.Width / 2)
                    location.X -= moveSpeed;
                    break;
                case Direction.Right:
                    if(location.X<boundaries.X + boundaries.Width - Area.Width / 2)
                    location.X += moveSpeed;
                    break;
                default:
                    break;
            }
        }

        public Shot playerShot()
        {
            return new Shot(new Point(location.X + Area.Width / 2, Area.Y), Direction.Up, boundaries);
        }
    }

    public class Shot
    {
        private const int moveInterval = 20;//子弹运行速度
        private const int width = 5;
        private const int height = 15;

        private Point location;
        public Point Location { get { return location; } }

        private Direction direction;
        private Rectangle boundaries;

        public Shot(Point location, Direction direction, Rectangle boundaries)
        {
            this.location = location;
            this.direction = direction;
            this.boundaries = boundaries;
        }

        public void Draw(Graphics g)
        {
            g.FillRectangle(Brushes.Yellow, Location.X, Location.Y, width, height);
        }

        public bool Move()
        {
            switch (direction)
            { 
                case Direction.Up:
                    location.Y -= moveInterval;
                    break;
                case Direction.Down:
                    location.Y += moveInterval;
                    break;
                default:
                    break;
            }
            if (location.Y <= 0 || location.Y > boundaries.Height)
                return false;
            return true;          
        }
    }

    public class Stars
    {
        List<Star> stars=new List<Star> ();
        Pen pen = null;
        Point point;
        Random random;
        Invaders form;

        private struct Star
        {
            public Point point;
            public Pen pen;
         
            public Star(Point point, Pen pen)
            {
                this.point = point;
                this.pen = pen;
            }           
        }

        public Stars(Invaders form,Random random)
        {
            this.form = form;
            this.random = random;
            AddStars(form, random, 300);
        }

        public void AddStars(Invaders form, Random random, int number)
        {
            for (int i = 0; i < number; i++)
            {
                point = new Point(random.Next(0, form.ClientSize.Width), random.Next(0, form.ClientSize.Height));
                switch (random.Next(0, 5))
                {
                    case 0:
                        pen = new Pen(Color.Red);
                        break;
                    case 1:
                        pen = new Pen(Color.Yellow);
                        break;
                    case 2:
                        pen = new Pen(Color.Blue);
                        break;
                    case 3:
                        pen = new Pen(Color.Green);
                        break;
                    case 4:
                        pen = new Pen(Color.White);
                        break;
                    default:
                        break;
                }
                stars.Add(new Star(point, pen));
            }          
        }
        
        public void Draw(Graphics g)
        {
            foreach (Star star in stars)
            {
                g.DrawEllipse(star.pen, star.point.X, star.point.Y, 1, 1);
            }
        }

        public void Twinkle(Random random)
        {
            for (int i = 10; i > 0; i--)
            {
                stars.RemoveAt(i);
            }
            AddStars(form, random, 10);
        }
    }
}
