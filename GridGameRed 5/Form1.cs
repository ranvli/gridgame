using System;
using System.Drawing;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Windows.Forms;

namespace GridGameRed
{
    public partial class Form1 : Form
    {
        private GameEngine gameEngine;
        private Renderer renderer;
        private bool running = true; 
        private Thread gameLogicThread; // Declare the thread
        private DebugForm debugForm;

        // Define textureMapping as a field to be accessible by all methods in Form1
        private readonly Color[] textureMapping = new Color[]
        {
            Color.Empty, // Index 0 might be unused or transparent
            Color.Gray,  // Index 1 for a different terrain like stone
            Color.Blue,  // Index 2 for water
            Color.BurlyWood, // Index 3 for ground
                             // Add more colors as needed for other terrains
        };

        public Form1()
        {
            InitializeComponent();
            gameEngine = new GameEngine();
            renderer = new Renderer(gameEngine, this);
            this.DoubleBuffered = true;

            // Assume a cell size of 50 for this example; adjust as needed.
            this.Width = 50 * GameEngine.GridSize + 16; // GameEngine.GridSize is public and static
            this.Height = 50 * GameEngine.GridSize + 39; // Same as above
             
            // Initialize the game logic thread
            gameLogicThread = new Thread(new ThreadStart(GameLogicLoop));
            gameLogicThread.Start(); // Start the game logic thread

            this.Load += Form1_Load;
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            // Initialize the debug form
            debugForm = new DebugForm(gameEngine);
            debugForm.Show(this); // Show the debug form next to the main form
        }

        private void GameLogicLoop()
        {
            while (running) // Make sure 'running' is a boolean controlling the thread's life
            {
                // This loop is currently empty since game logic is handled via user input (OnKeyDown)
                Thread.Sleep(16); // For a 60Hz update rate; adjust as needed
            }
        }

        private void DrawCellValue(Graphics graphics, int value, int x, int y, int cellSize)
        {
            Font font = new Font("Arial", cellSize / 2); // Choose an appropriate size for your cell size
            StringFormat format = new StringFormat
            {
                Alignment = StringAlignment.Center,
                LineAlignment = StringAlignment.Center
            };
            RectangleF textRect = new RectangleF(x * cellSize, y * cellSize, cellSize, cellSize);
            using (Brush textBrush = new SolidBrush(Color.Black)) // Ensure the text color contrasts with the background
            {
                graphics.DrawString(value.ToString(), font, textBrush, textRect, format);
            }
        }

        private Color GetColorForTerrain(int terrainType)
        {
            // Reference to the textureMapping array in GameEngine
            return GameEngine.textureMapping[terrainType];
        }

        private Color GetColorForValue(int value)
        {
            int terrainType = value / 100; // Decode the hundreds place for terrain
            if (terrainType >= 0 && terrainType < textureMapping.Length)
            {
                return textureMapping[terrainType];
            }
            else
            {
                // Handle unexpected values
                Console.WriteLine("Warning: Unmapped value " + value);
                return Color.Magenta; // Return a noticeable color for unmapped values
            }
        }

        private void DrawCellValue(Graphics graphics, int value, int x, int y)
        {
            // This method centers the cell's value in the cell as text.
            int cellSize = 50;
            Font font = new Font("Arial", 10);
            StringFormat format = new StringFormat() { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
            graphics.DrawString(value.ToString(), font, Brushes.Black, new RectangleF(x * cellSize, y * cellSize, cellSize, cellSize), format);
        }

        private void DrawGridLines(Graphics graphics, int width, int height)
        {
            // This method draws grid lines.
            int cellSize = 50;
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    graphics.DrawRectangle(Pens.Black, x * cellSize, y * cellSize, cellSize, cellSize);
                }
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            // Start the renderer thread
            renderer.Start();

            // Start the game logic thread
            gameLogicThread = new Thread(GameLogicUpdate)
            {
                IsBackground = true
            };
            gameLogicThread.Start();
        }

        private void GameLogicUpdate()
        {
            while (running)
            {
                // Redraw the form and the debug form
                this.Invoke(new Action(() => this.Invalidate()));
                this.Invoke(new Action(() => debugForm.Invalidate()));

                Thread.Sleep(50); // Increase this value if the update rate is too high
            }
        }

        private void DrawGameGrid(Graphics graphics, int cellSize)
        {
            for (int x = 0; x < GameEngine.GridSize; x++)
            {
                for (int y = 0; y < GameEngine.GridSize; y++)
                {
                    int encodedValue = gameEngine.LogicalGrid[x, y];
                    int terrainType = encodedValue / 100;
                    int objectCode = encodedValue % 100;

                    Color terrainColor = GetColorForTerrain(terrainType);
                    DrawCell(graphics, x, y, cellSize, terrainColor);

                    if (objectCode != 0)
                    {
                        DrawObjectOrCharacter(graphics, objectCode, x, y, cellSize);
                    }
                }
            }
        }

        private void DrawObjectOrCharacter(Graphics graphics, int objectCode, int x, int y, int cellSize)
        {
            const int fishCode = GameEngine.FishValue; // Assuming FishValue is defined in GameEngine
            const int obstacleCode = GameEngine.ObstacleValue; // Assuming ObstacleValue is defined in GameEngine
            const int characterBaseValue = GameEngine.CharacterValue;

            // Based on the object code, draw the appropriate object
            switch (objectCode)
            {
                // Check if the object code is within the range of valid character values
                case var _ when (objectCode >= characterBaseValue && objectCode < characterBaseValue + 10):
                    graphics.FillRectangle(Brushes.Red, x * cellSize, y * cellSize, cellSize, cellSize);
                    break;
                case 4: // Coin
                    graphics.FillEllipse(Brushes.Yellow, x * cellSize, y * cellSize, cellSize, cellSize);
                    break;
                case GameEngine.CharacterValue:
                    // Draw the character at its current position with updated code
                    graphics.FillRectangle(Brushes.Red, x * cellSize, y * cellSize, cellSize, cellSize);
                    break;
                case fishCode:
                    graphics.FillEllipse(Brushes.Green, x * cellSize, y * cellSize, cellSize, cellSize);
                    break;
                case obstacleCode:
                    graphics.FillRectangle(Brushes.DarkGray, x * cellSize, y * cellSize, cellSize, cellSize);
                    break;
            }
        }


        private void DrawCell(Graphics graphics, int x, int y, int cellSize, Color color)
        {
            Rectangle cellRect = new Rectangle(x * cellSize, y * cellSize, cellSize, cellSize);
            using (Brush brush = new SolidBrush(color))
            {
                graphics.FillRectangle(brush, cellRect); // Fill the cell with the color
            }
            graphics.DrawRectangle(Pens.Black, cellRect); // Draw the cell border
        }

        private Brush GetBrushForValue(int value)
        {
            if (value >= 40)
            {
                return Brushes.Red; // Character
            }

            switch (value)
            {
                case 0: // Sky
                    return Brushes.Gray;
                case 1: // Ground
                    return Brushes.BurlyWood;
                case 2: // Water
                    return Brushes.Blue;
                case 4: // Coin, assuming a coin value of 4
                    return Brushes.Yellow;
                case 5: // Fish, assuming a fish value of 5
                    return Brushes.Green;
                // Add additional cases if there are other objects with unique values.
                default:
                    return Brushes.White; // Default for unknown elements
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            DrawGameGrid(e.Graphics, 50); // Assuming the cell size is 50 pixels
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            running = false;
            renderer.Stop();
            if (gameLogicThread != null && gameLogicThread.IsAlive)
            {
                gameLogicThread.Join();
            }
            base.OnFormClosing(e);
        }
    }
}
