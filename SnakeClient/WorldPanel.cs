using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using IImage = Microsoft.Maui.Graphics.IImage;
#if MACCATALYST
using Microsoft.Maui.Graphics.Platform;
#else
using Microsoft.Maui.Graphics.Win2D;
#endif
using Color = Microsoft.Maui.Graphics.Color;
using System.Reflection;
using Microsoft.Maui;
using System.Net;
using Font = Microsoft.Maui.Graphics.Font;
using SizeF = Microsoft.Maui.Graphics.SizeF;
using Controller;
using Microsoft.Maui.Controls;
using Model;
using Microsoft.UI.Xaml.Controls;
using System.ComponentModel;
using Microsoft.Maui.Graphics;

namespace SnakeGame;
public class WorldPanel : IDrawable
{

    private IImage wall;
    private IImage background;
    private int viewWidth = 900;
    private int viewHeight = 900;
    private World theWorld;
    private GraphicsView graphicsView = new();
    public delegate void ObjectDrawer(object o, ICanvas canvas);

    private bool initializedForDrawing = false;

    public WorldPanel()
    {
        graphicsView.Drawable = this;
    }

    /// <summary>
    /// Sets the world.
    /// </summary>
    /// <param name="w">The w.</param>
    public void SetWorld(World w)
    {
        theWorld = w;
    }


#if MACCATALYST
    private IImage loadImage(string name)
    {
        Assembly assembly = GetType().GetTypeInfo().Assembly;
        string path = "SnakeGame.Resources.Images";
        return PlatformImage.FromStream(assembly.GetManifestResourceStream($"{path}.{name}"));
    }
#else
    private IImage loadImage(string name)
    {
        Assembly assembly = GetType().GetTypeInfo().Assembly;
        string path = "SnakeGame.Resources.Images";
        var service = new W2DImageLoadingService();
        return service.FromStream(assembly.GetManifestResourceStream($"{path}.{name}"));
    }
#endif


    private void InitializeDrawing()
    {
        wall = loadImage("WallSprite.png");
        background = loadImage("Background.png");
        initializedForDrawing = true;
    }

    /// <summary>
    /// Drawing template for wall, can be used as an ObjectDrawer delegate
    /// </summary>
    /// <param name="o">The o.</param>
    /// <param name="canvas">The canvas.</param>
    private void WallDrawer(object o, ICanvas canvas)
    {
        Wall w = o as Wall;

        float wallWidth = 50;
        float wallHeight = 50;

        Vector2D point1 = w.FirstPosition;
        Vector2D point2 = w.SecondPosition;


        // horizonal walls
        if (point1.Y == point2.Y)
        {
            double smallerX = 0;
            double largerX = 0;

            if (point1.X <= point2.X)
            {
                smallerX = point1.X;
                largerX = point2.X;
            }
            else if (point2.X < point1.X)
            {
                smallerX = point2.X;
                largerX = point1.X;
            }

            // draw walls from left to right
            for (double i = smallerX; i <= largerX; i += wallWidth)
            {
                canvas.DrawImage(wall, (float)i, (float)point1.Y, wallWidth, wallHeight);
            }

        }
        // vertical walls
        else if (point1.X == point2.X)
        {
            double smallerY = 0;
            double largerY = 0;

            if (point1.Y <= point2.Y)
            {
                smallerY = point1.Y;
                largerY = point2.Y;
            }
            else if (point2.Y < point1.Y)
            {
                smallerY = point2.Y;
                largerY = point1.Y;
            }

            // draw walls from down to up
            for (double i = smallerY; i <= largerY; i += wallHeight)
            {
                canvas.DrawImage(wall, (float)point1.X, (float)i, wallWidth, wallHeight);
            }
        }
    }

    /// <summary>
    /// Drawing template for snake segment
    /// </summary>
    /// <param name="o">The o.</param>
    /// <param name="canvas">The canvas.</param>
    private void SnakeSegmentDrawer(object o, ICanvas canvas)
    {
        Tuple<Vector2D, Vector2D, long> snakeSegment = o as Tuple<Vector2D, Vector2D, long>;

        Vector2D pointCloserToTail = snakeSegment.Item1;
        Vector2D pointCloserToHead = snakeSegment.Item2;
        long snakeID = snakeSegment.Item3;

        if (snakeID % 10 == 0)
            canvas.StrokeColor = Colors.Black;
        else if (snakeID % 10 == 1)
            canvas.StrokeColor = Colors.White;
        else if (snakeID % 10 == 2)
            canvas.StrokeColor = Colors.Blue;
        else if (snakeID % 10 == 3)
            canvas.StrokeColor = Colors.Yellow;
        else if (snakeID % 10 == 4)
            canvas.StrokeColor = Colors.Orange;
        else if (snakeID % 10 == 5)
            canvas.StrokeColor = Colors.Red;
        else if (snakeID % 10 == 6)
            canvas.StrokeColor = Colors.Aqua;
        else if (snakeID % 10 == 7)
            canvas.StrokeColor = Colors.Purple;
        else if (snakeID % 10 == 8)
            canvas.StrokeColor = Colors.LightBlue;
        else if (snakeID % 10 == 9)
            canvas.StrokeColor = Colors.Magenta;

        canvas.StrokeSize = 10;
        canvas.StrokeLineCap = LineCap.Round;

        canvas.DrawLine((float)pointCloserToTail.X, (float)pointCloserToTail.Y, (float)pointCloserToHead.X, (float)pointCloserToHead.Y);
    }

    /// <summary>
    /// Drawing template for powerup, a method that can be used as an ObjectDrawer delegate
    /// </summary>
    /// <param name="o">The powerup to draw</param>
    /// <param name="canvas"></param>
    private void PowerupDrawer(object o, ICanvas canvas)
    {
        Powerup p = o as Powerup;
        int width = 10;
        int height = 10;

        // outer layer of powerup
        canvas.StrokeSize = 6;
        canvas.StrokeColor = Colors.Yellow;
        canvas.DrawEllipse(-(width / 2), -(height / 2), width, height);

        // inner layer of powerup
        canvas.FillColor = Colors.OrangeRed;
        canvas.FillEllipse(-(width / 2), -(height / 2), width, height);
    }

    /// <summary>
    /// This method performs a translation and rotation to draw an object.
    /// </summary>
    /// <param name="canvas">The canvas object for drawing onto</param>
    /// <param name="o">The object to draw</param>
    /// <param name="worldX">The X component of the object's position in world space</param>
    /// <param name="worldY">The Y component of the object's position in world space</param>
    /// <param name="angle">The orientation of the object, measured in degrees clockwise from "up"</param>
    /// <param name="drawer">The drawer delegate. After the transformation is applied, the delegate is invoked to draw whatever it wants</param>
    private void DrawObjectWithTransform(ICanvas canvas, object o, double worldX, double worldY, double angle, ObjectDrawer drawer)
    {
        // "push" the current transform
        canvas.SaveState();

        canvas.Translate((float)worldX, (float)worldY);
        canvas.Rotate((float)angle);
        drawer(o, canvas);

        // "pop" the transform
        canvas.RestoreState();
    }

    /// <summary>
    /// Draws the specified canvas.
    /// </summary>
    /// <param name="canvas">The canvas.</param>
    /// <param name="dirtyRect">The dirty rect.</param>
    public void Draw(ICanvas canvas, RectF dirtyRect)
    {
        if (!initializedForDrawing)
            InitializeDrawing();
        canvas.ResetState();

        lock (theWorld)
        {
            if (theWorld.snakes.ContainsKey(theWorld.playerID))
            {
                Snake snake = theWorld.snakes[theWorld.playerID];

                Vector2D snakeHead = snake.Position[snake.Position.Count - 1];

                float playerX = (float)snakeHead.X;
                float playerY = (float)snakeHead.Y;

                // center player's view to the snake
                canvas.Translate((viewWidth / 2) - playerX, (viewHeight / 2) - playerY);

                // draw the background
                canvas.DrawImage(background, -(theWorld.Size / 2), -(theWorld.Size / 2), theWorld.Size, theWorld.Size);
            }
        }


        lock (theWorld.walls)
        {
            float wallOffset = 25;   // offset distance for wall
            foreach (Wall w in theWorld.walls.Values)
            {
                DrawObjectWithTransform(canvas, w, -wallOffset, -wallOffset, 0, WallDrawer);
            }
        }


        lock (theWorld.powerups)
        {
            foreach (Powerup powerup in theWorld.powerups.Values)
            {
                DrawObjectWithTransform(canvas, powerup, powerup.position.X, powerup.position.Y, 0, PowerupDrawer);
            }
        }


        lock (theWorld.snakes)
        {
            foreach (Snake snake in theWorld.snakes.Values)
            {
                // draw exlopsion if snake is not alive
                if (!snake.IsAlive)
                {
                    // where the explosion ocurrs
                    Vector2D headPosition = snake.Position[snake.Position.Count - 1];
                    float headPositionX = (float)headPosition.X;
                    float headPositionY = (float)headPosition.Y;

                    canvas.StrokeColor = Colors.White;
                    canvas.StrokeSize = 3;
                    canvas.StrokeDashPattern = new float[] { 2, 1 };

                    for (int radius = 10; radius < 100; radius += 10)
                        canvas.DrawEllipse(headPositionX - radius, headPositionY - radius, 2 * radius, 2 * radius);

                    // reset the dash pattern to no dash for canvas
                    canvas.StrokeDashPattern = new float[] { 0, 0 };
                }
                // display snake if snake is avlive
                else if (snake.IsAlive)
                {
                    // draw snake body
                    for (int i = 0; i < snake.Position.Count - 1; i++)
                    {
                        Vector2D pointCloserToTail = snake.Position[i];
                        Vector2D pointCloserToHead = snake.Position[i + 1];

                        float direction = pointCloserToTail.ToAngle();

                        Tuple<Vector2D, Vector2D, long> snakeSegment = new Tuple<Vector2D, Vector2D, long>(pointCloserToTail, pointCloserToHead, snake.ID);

                        SnakeSegmentDrawer(snakeSegment, canvas);
                    }

                    // display player's name and score
                    Vector2D headPosition = snake.Position[snake.Position.Count - 1];
                    float stringOffset = 25;
                    canvas.FontColor = Colors.Black;
                    canvas.DrawString(snake.Name + ": " + snake.Score, (float)headPosition.X, (float)headPosition.Y - stringOffset, HorizontalAlignment.Center);
                }
            }
        }
    }



}