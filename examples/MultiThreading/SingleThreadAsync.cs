using System;
using System.Threading;

namespace examples
{
    public class SingleThreadAsync
    {
        public static void Main2(String[] args)
        {
            var eventLoop = new EventLoop();

            void task1()
            {
                Console.WriteLine("Task 1 Run");
                eventLoop.Async(() =>
                {
                    Thread.Sleep(1000);
                    Console.WriteLine("Task 2 Run");
                });
            };
            void task3()
            {
                Console.WriteLine("Task 3 Run");
                eventLoop.Async(() =>
                {
                    Thread.Sleep(1000);
                    Console.WriteLine("Task 4 Run");
                });
            }

            eventLoop.Async(task1);
            eventLoop.Async(task3);

            void inputTask()
            {
                Console.WriteLine("Please input something:");
                var line = Console.ReadLine();
                Console.WriteLine("You input: {0}", line);
            }

            eventLoop.OnUpdated(() =>
            {
                eventLoop.Async(inputTask);
                Console.WriteLine("Hello World");
            });
            eventLoop.Start(1000);

            // this fragment will no be executed
            eventLoop.Async(() =>
            {
                Console.WriteLine("Finished");
            });
        }
    }
}
