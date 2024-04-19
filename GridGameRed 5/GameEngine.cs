using System;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Windows.Forms;

public class GameEngine
{
    public int[,] LogicalGrid { get; private set; }
    private Thread engineThread;
    private volatile bool running = true;
    private readonly object gridLock = new object();
    public Point CharacterPosition { get; private set; }
    public const int GridSize = 10;
    public const int Sky = 300;
    public const int Water = 200;
    public const int Ground = 100;
    public const int CharacterValue = 40; // The character's unique identifier
    public const int CoinValue = 4;      // The coin's unique identifier
    public const int FishValue = 5;
    public const int ObstacleValue = 3;
    public static readonly Color[] ColorMapping = new Color[51];
    public int CurrentCharacterValue { get; private set; } = CharacterValue;

    public static readonly Color[] textureMapping = new Color[]
    {
        Color.Empty, // Assume transparent for index 0
        Color.Gray,  // Stone or some other terrain
        Color.Blue,  // Water
        Color.BurlyWood, // Ground
        // Additional terrain types as needed
    };

    public GameEngine()
    {
        LogicalGrid = InitializeGrid();
        CharacterPosition = new Point(1, 7); // Start character roughly in the middle of the ground area
        PlaceObjects();
        LogicalGrid[CharacterPosition.X, CharacterPosition.Y] = Ground + CharacterValue;

        // Default color
        for (int i = 0; i < ColorMapping.Length; i++)
        {
            ColorMapping[i] = Color.White;
        }

        // Populate the color mapping
        ColorMapping[10] = Color.BurlyWood; // ground
        ColorMapping[20] = Color.Blue;      // water
        ColorMapping[30] = Color.Gray; // sky
        ColorMapping[40] = Color.Red;      // character
        // Continue for other specific objects
        ColorMapping[3] = Color.DarkGray; // obstacle
        ColorMapping[4] = Color.Yellow;   // coin
        ColorMapping[5] = Color.Green;    // fish

        engineThread = new Thread(RunEngine);
        engineThread.IsBackground = true;
        engineThread.Start();
    }

    private void PlaceObjects()
    {
        // Encoding objects with terrain, e.g., a coin on the ground is 104
        LogicalGrid[4, 8] = 103; // obstacle on the ground
        LogicalGrid[4, 7] = 104; // Coin on the ground
        LogicalGrid[1, 4] = Water + FishValue; // Fish in the water
    }

    private int[,] InitializeGrid()
    {
        // Initialize grid based on game logic
        int[,] grid = new int[GridSize, GridSize];

        // Populate the grid with sky (300), water (200), and ground (100)
        for (int x = 0; x < GridSize; x++)
        {
            for (int y = 0; y < GridSize; y++)
            {
                grid[x, y] = y < 3 ? 300 : (y >= 3 && y < 6) ? 200 : 100;
            }
        }

        return grid;
    }


    private void RunEngine()
    {
        while (running)
        { 
            // Game logic updates here
            Thread.Sleep(50); // Sleep to simulate game tick rate (adjust as needed)
        }
    }

    public void Stop()
    {
        running = false;
        engineThread.Join(); // Safely stop the thread
    }

    private bool IsMoveValid(Point newPosition)
    {
        if (newPosition.X < 0 || newPosition.X >= GridSize || newPosition.Y < 0 || newPosition.Y >= GridSize)
        {
            return false; // Position is out of bounds
        }

        int cellValue = LogicalGrid[newPosition.X, newPosition.Y];
        int terrainType = cellValue / 100; // Extracts the hundreds digit
        int objectType = cellValue % 100;  // Extracts the tens and ones digits

        // Check if the terrain is walkable (not sky, which is represented by the value '3')
        bool isTerrainWalkable = terrainType != Sky / 100;

        // Check if there is an object in the cell that is not collectible or walkable (e.g., obstacles)
        bool isObjectWalkableOrCollectible = (objectType == 0 || objectType == CoinValue || objectType == FishValue);

        return isTerrainWalkable && isObjectWalkableOrCollectible;
    }


    // Method for moving character with basic collision detection and game state updates
    public void MoveCharacter(Keys key)
    {
        lock (gridLock)
        {
            Point newPosition = CalculateNewPosition(CharacterPosition, key);

            if (IsMoveValid(newPosition))
            {
                int newCellValue = LogicalGrid[newPosition.X, newPosition.Y];
                int objectType = newCellValue % 100;

                // Handle coin collection at the new position
                if (objectType == CoinValue)
                {
                    CollectCoin(newPosition); // Collect the coin
                }

                // Update character position should be called only if moving onto a valid space
                UpdateCharacterPosition(newPosition);

                // Update the character's stored position
                CharacterPosition = newPosition;
            }
            MoveFish();
        }
    }


    private Point CalculateNewPosition(Point currentPosition, Keys key)
    {
        Point newPosition = currentPosition;
        switch (key)
        {
            case Keys.Up:
                newPosition.Y = Math.Max(0, newPosition.Y - 1);
                break;
            case Keys.Down:
                newPosition.Y = Math.Min(GridSize - 1, newPosition.Y + 1);
                break;
            case Keys.Left:
                newPosition.X = Math.Max(0, newPosition.X - 1);
                break;
            case Keys.Right:
                newPosition.X = Math.Min(GridSize - 1, newPosition.X + 1);
                break;
        }
        return newPosition;
    }

    private void MoveFish()
    {
        Point fishPosition = FindObjectPosition(FishValue); // Find the current fish position.
        if (fishPosition == new Point(-1, -1))
            return; // Fish not found, nothing to move.

        // Possible directions for the fish to move
        Point[] directions = new Point[]
        {
        new Point(-1, 0), // Left
        new Point(1, 0),  // Right
        new Point(0, -1), // Up
        new Point(0, 1)   // Down
        };

        Random rnd = new Random();
        directions = directions.OrderBy(x => rnd.Next()).ToArray(); // Shuffle directions

        foreach (Point dir in directions)
        {
            Point newFishPosition = new Point(fishPosition.X + dir.X, fishPosition.Y + dir.Y);

            // Check if the new position is valid and within the bounds of the grid.
            if (newFishPosition.X >= 0 && newFishPosition.X < GridSize &&
                newFishPosition.Y >= 3 && newFishPosition.Y < 6 && // Assuming fish can only move in the water range (y between 3 and 5 inclusive)
                LogicalGrid[newFishPosition.X, newFishPosition.Y] == Water)
            {
                // Update the grid: remove the fish from the current position.
                LogicalGrid[fishPosition.X, fishPosition.Y] = Water;

                // Place the fish at the new position.
                LogicalGrid[newFishPosition.X, newFishPosition.Y] = Water + FishValue;
                break; // Fish has moved, exit the loop
            }
        }
    }


    private Point FindObjectPosition(int objectValue)
    {
        for (int x = 0; x < GridSize; x++)
        {
            for (int y = 0; y < GridSize; y++)
            {
                int cellValue = LogicalGrid[x, y];
                int objectType = cellValue % 100; // Extract the object code

                if (objectType == objectValue)
                {
                    return new Point(x, y);
                }
            }
        }
        return new Point(-1, -1); // Object not found
    }


    private void CollectCoin(Point position)
    {
        // Increase the character score or value
        CurrentCharacterValue++;

        // Update the logical grid to remove the coin and add the character
        int terrainType = LogicalGrid[position.X, position.Y] / 100 * 100;
        LogicalGrid[position.X, position.Y] = terrainType + CurrentCharacterValue;
    }

    private void UpdateCharacterPosition(Point newPosition)
    {
        // Extract the terrain type of the current character's position
        int terrainTypeAtCurrentPosition = LogicalGrid[CharacterPosition.X, CharacterPosition.Y] / 100 * 100;

        // Reset the old character's position back to its terrain type
        LogicalGrid[CharacterPosition.X, CharacterPosition.Y] = terrainTypeAtCurrentPosition;

        // Set the character's new position to the current character value
        // and maintain the terrain type at the new position
        int terrainTypeAtNewPosition = LogicalGrid[newPosition.X, newPosition.Y] / 100 * 100;
        LogicalGrid[newPosition.X, newPosition.Y] = terrainTypeAtNewPosition + CurrentCharacterValue;

        // Update the character's stored position
        CharacterPosition = newPosition;
    }

}
