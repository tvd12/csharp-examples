using System;
using System.Threading;

namespace examples.MultiThreading
{
    public delegate T Execution<T>();

    public class Task<T>
    {
        private readonly Execution<T> Action;
        private static readonly ExecutorService ExecutorService =
            new ExecutorService("task", 3, 10);

        public Task(Action action) : this(ActionExecution(action))
        {
        }

        public Task(Execution<T> action)
        {
            this.Action = action;
        }

        public static Execution<T> ActionExecution(Action action)
        {
            return () =>
            {
                action();
                return default(T);
            };
        }

        public void Run()
        {
            Action();
        }

        public void Start()
        {
            ExecutorService.Execute(() => Action());
        }

        public T Get()
        {
            return ExecutorService.Submit<T>(() =>
            {
                return Action();
            }).GetResult();
        }
    }

    public class Task : Task<object>
    {
        public Task(Action action) : base(action)
        {
        }
    }

    // https://docs.microsoft.com/en-us/dotnet/api/system.threading.tasks.task?view=net-5.0
    public class TaskExample
    {
        public static void Main(string[] args)
        {
            Action action1 = () =>
            {
                Thread.Sleep(100);
                Console.WriteLine("Thread={0}", Thread.CurrentThread.ManagedThreadId);
            };

            // Create a task but do not start it.
            Task t1 = new Task(action1);
            t1.Start();

            Execution<int> action2 = () =>
            {
                Thread.Sleep(100);
                return 1 + 1;
            };

            // Create a task but do not start it.
            Task<int> t2 = new Task<int>(action2);
            var result = t2.Get();
            Console.WriteLine("Must be run after t2, result = " + result);
            Thread.Sleep(10000);
        }
    }
}
