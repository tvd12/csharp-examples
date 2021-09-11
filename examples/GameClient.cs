using System;

namespace examples
{
    public class GameClientEventLoop : EventLoop
    {
        protected override void DoUpdate()
        {
            Console.WriteLine("Render Graphic");
        }
    }

    public class GameClientProgram
    {
        public static void Main(String[] args)
        {
            EventLoop eventLoop = new GameClientEventLoop();

            static void inputTask()
            {
                Console.WriteLine("Please input something:");
                var line = Console.ReadLine();
                Console.WriteLine("You input: {0}", line);
            }

            eventLoop.OnUpdated(() =>
            {
                eventLoop.Async(inputTask);
            });
            eventLoop.Start(1000);
        }
    }
}
