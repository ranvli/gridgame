using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using static System.Net.Mime.MediaTypeNames;
using static System.Windows.Forms.DataFormats;
using Font = System.Drawing.Font;

namespace GridGameRed
{
    public partial class DebugForm : Form
    {
        private GameEngine gameEngine;

        public DebugForm()
        {
            InitializeComponent();
        }

        public DebugForm(GameEngine engine)
        {
            gameEngine = engine;
            // Set the size of the debug form based on the grid size
            ClientSize = new Size(GameEngine.GridSize * 50, GameEngine.GridSize * 50);
            Text = "Debug Grid";
            KeyPreview = true; // Make sure the form listens to key events

            // Enable double buffering
            DoubleBuffered = true;
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e); // This ensures that key events are not suppressed.

            // Handle arrow keys
            if (e.KeyCode == Keys.Up || e.KeyCode == Keys.Down || e.KeyCode == Keys.Left || e.KeyCode == Keys.Right)
            {
                gameEngine.MoveCharacter(e.KeyCode); // Pass the key event to the game engine
                Invalidate(); // Invalidate the form to trigger a repaint with the new game state
                e.Handled = true; // Indicate that the key event has been handled
            }
        }


        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            int cellSize = 50; // The size of the cell in pixels
            Font font = new Font("Consolas", 10); // A monospaced font for numbers
            Brush textBrush = Brushes.Black; // Color of the text
            StringFormat format = new StringFormat()
            {
                Alignment = StringAlignment.Center,
                LineAlignment = StringAlignment.Center
            };

            for (int x = 0; x < gameEngine.LogicalGrid.GetLength(0); x++)
            {
                for (int y = 0; y < gameEngine.LogicalGrid.GetLength(1); y++)
                {
                    int cellValue = gameEngine.LogicalGrid[x, y];
                    // Draw only the numerical value of each cell in the grid
                    e.Graphics.DrawString(cellValue.ToString(), font, textBrush, new RectangleF(x * cellSize, y * cellSize, cellSize, cellSize), format);
                }
            }
        }
    }
}
