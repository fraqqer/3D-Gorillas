using OpenTK;

namespace Main
{
    public class Program
    {
        public static GameWindow gameWindow;
        static void Main(string[] args)
        {
            gameWindow = new Game.Window();

            if (gameWindow != null)
            {
                gameWindow.Run();
            }
        }
    }
}
