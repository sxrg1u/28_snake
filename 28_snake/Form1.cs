using Windows.Gaming.Input;
using Timer = System.Windows.Forms.Timer;

namespace _28_snake;

public partial class Form1 : Form
{
    const int Rows = 29;
    const int Cols = 29;
    const int ElementSize = 20;
    const int SegmentSize = 18;
    const int AppleSize = 18;
    const int StartMovingInterval = 100;
    const int StepsToFirstApple = Cols / 4;

    readonly static Random random = new Random();

    Panel head = new Panel() { BackColor = Color.LimeGreen, Size = new Size(SegmentSize, SegmentSize) };
    Queue<Panel> tail = new Queue<Panel>();
    MovingDirection snakeMovingDirection = MovingDirection.None;

    Panel apple = new Panel() { BackColor = Color.Red, Size = new Size(AppleSize, AppleSize) };

    Timer tmrMoveSnake = new Timer() { Interval = StartMovingInterval, Enabled = true };
    RawGameController? controller = null;
    Timer tmrPollingControllerStatus = new Timer() { Interval = 16, Enabled = true };

    // --- POINTS ---
    int points = 0;
    Label lblPoints = new Label() { ForeColor = Color.White, Location = new Point(5, 5), AutoSize = true };

    private Point CellToScreen(Point cell, int size)
    {
        int offset = (ElementSize - size) / 2;
        return new Point(cell.X + offset, cell.Y + offset);
    }

    private Point ScreenToCell(Point screenPos, int size)
    {
        int offset = (ElementSize - size) / 2;
        return new Point(screenPos.X - offset, screenPos.Y - offset);
    }

    public Form1()
    {
        Text = "Snake";
        BackColor = Color.Black;
        Width = Cols * ElementSize + (Width - ClientSize.Width);
        Height = Rows * ElementSize + (Height - ClientSize.Height);

        // --- POINTS LABEL HINZUFÜGEN ---
        Controls.Add(lblPoints);
        lblPoints.Text = "Points: 0";

        GameInit();

        KeyDown += Form1_KeyDown;
        tmrMoveSnake.Tick += TmrMoveSnake_Tick;

        RawGameController.RawGameControllerAdded += RawGameController_RawGameControllerAdded;
        RawGameController.RawGameControllerRemoved += RawGameController_RawGameControllerRemoved;

        tmrPollingControllerStatus.Tick += TmrPollingControllerStatus_Tick;
    }

    private void TmrPollingControllerStatus_Tick(object? sender, EventArgs e)
    {
        if (controller != null)
        {
            int numOfAxes = Math.Max(0, controller.AxisCount);
            double[] axesValues = new double[numOfAxes];

            controller.GetCurrentReading(null, null, axesValues);

            if (numOfAxes > 1)
            {
                axesValues[0] = NormalizeAxisValue(axesValues[0]);
                axesValues[1] = NormalizeAxisValue(axesValues[1]);

                double deadZone = 0.3;

                if (axesValues[0] < -deadZone)
                    snakeMovingDirection = MovingDirection.Left;
                if (axesValues[0] > deadZone)
                    snakeMovingDirection = MovingDirection.Right;
                if (axesValues[1] < -deadZone)
                    snakeMovingDirection = MovingDirection.Up;
                if (axesValues[1] > deadZone)
                    snakeMovingDirection = MovingDirection.Down;
            }
        }
    }

    private double NormalizeAxisValue(double value)
    {
        if (double.IsNaN(value))
            return 0;
        if (value >= 0 && value <= 1)
            return value * 2 - 1;
        if (value < 0)
            return -1;
        if (value > 1)
            return 1;
        return value;
    }

    private void RawGameController_RawGameControllerRemoved(object? sender, RawGameController e)
    {
        controller = null;
    }

    private void RawGameController_RawGameControllerAdded(object? sender, RawGameController e)
    {
        controller = e;
    }

    private void GameInit()
    {
        Point headCell = new Point((Cols / 2) * ElementSize, (Rows / 2) * ElementSize);
        head.Location = CellToScreen(headCell, SegmentSize);
        snakeMovingDirection = MovingDirection.None;

        Point appleCell = new Point((Cols / 2 + StepsToFirstApple) * ElementSize, (Rows / 2) * ElementSize);
        apple.Location = CellToScreen(appleCell, AppleSize);

        Controls.Add(head);
        Controls.Add(apple);
    }

    private void TmrMoveSnake_Tick(object? sender, EventArgs e)
    {
        Point headCell = ScreenToCell(head.Location, SegmentSize);

        // Wandkollision
        if (headCell.Y == 0 && snakeMovingDirection == MovingDirection.Up ||
            headCell.Y + ElementSize == ClientSize.Height && snakeMovingDirection == MovingDirection.Down ||
            headCell.X == 0 && snakeMovingDirection == MovingDirection.Left ||
            headCell.X + ElementSize == ClientSize.Width && snakeMovingDirection == MovingDirection.Right)
        {
            GameOver();
        }

        // Apfel-Kollision
        Point appleCell = ScreenToCell(apple.Location, AppleSize);
        if (headCell == appleCell)
        {
            Panel tailElem = new Panel()
            {
                BackColor = Color.LimeGreen,
                Size = new Size(SegmentSize, SegmentSize),
                Location = head.Location
            };
            tail.Enqueue(tailElem);
            Controls.Add(tailElem);

            // --- POINTS ERHÖHEN ---
            points++;
            lblPoints.Text = $"Points: {points}";

            Point newAppleCell;
            do
            {
                newAppleCell = new Point(random.Next(0, Cols) * ElementSize, random.Next(0, Rows) * ElementSize);
            } while (newAppleCell == headCell);
            apple.Location = CellToScreen(newAppleCell, AppleSize);
        }

        // Schwanz verschieben
        if (tail.Count > 0)
        {
            Panel lastTailElement = tail.Dequeue();
            lastTailElement.Location = head.Location;
            tail.Enqueue(lastTailElement);
        }

        MoveSnakeHead();

        // Selbstkollision
        headCell = ScreenToCell(head.Location, SegmentSize);
        foreach (Panel tailElement in new Queue<Panel>(tail))
        {
            if (ScreenToCell(tailElement.Location, SegmentSize) == headCell)
            {
                GameOver();
            }
        }
    }

    private void GameOver()
    {
        tmrMoveSnake.Stop();
        DialogResult dialogResult = MessageBox.Show("Nochmals spielen?", "Game Over", MessageBoxButtons.YesNo);
        if (dialogResult == DialogResult.Yes)
        {
            Controls.Clear();
            tail.Clear();
            points = 0;                 // Punkte zurücksetzen
            Controls.Add(lblPoints);    // Label neu hinzufügen
            lblPoints.Text = "Points: 0";

            GameInit();
            tmrMoveSnake.Start();
        }
        else
            Close();
    }

    private void MoveSnakeHead()
    {
        Point headCell = ScreenToCell(head.Location, SegmentSize);
        switch (snakeMovingDirection)
        {
            case MovingDirection.Up: headCell.Y -= ElementSize; break;
            case MovingDirection.Down: headCell.Y += ElementSize; break;
            case MovingDirection.Left: headCell.X -= ElementSize; break;
            case MovingDirection.Right: headCell.X += ElementSize; break;
        }
        head.Location = CellToScreen(headCell, SegmentSize);
    }

    private void Form1_KeyDown(object? sender, KeyEventArgs e)
    {
        switch (e.KeyCode)
        {
            case Keys.Up:
                snakeMovingDirection = MovingDirection.Up;
                break;
            case Keys.Down:
                snakeMovingDirection = MovingDirection.Down;
                break;
            case Keys.Left:
                snakeMovingDirection = MovingDirection.Left;
                break;
            case Keys.Right:
                snakeMovingDirection = MovingDirection.Right;
                break;
        }
    }
}
