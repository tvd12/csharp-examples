using System;
using System.Threading;
using examples.Concurrent;

namespace examples.MultiThreading
{
    public class ThreadWrapper
    {
        private readonly Thread RealThread;
        private readonly EzyBlockingQueue<Runnable> Tasks;
        public static readonly Runnable POISON = () => { };

        public ThreadWrapper(string threadName)
        {
            RealThread = new Thread(Loop);
            RealThread.Name = threadName;
        }

        public void Start()
        {
            RealThread.Start();
        }

        private void Loop()
        {
            while (true)
            {
                var currentTask = Tasks.take();
                if (currentTask == POISON)
                {
                    break;
                }
                try
                {
                    currentTask();
                }
                catch (Exception e)
                {
                    Console.WriteLine("handle task failed", e);
                }
            }
        }

        public void Execute(Runnable task)
        {
            Tasks.offer(task);
        }

        public void Stop()
        {
            Tasks.offer(POISON);
        }

        public void StopNow()
        {
            RealThread.Abort();
        }
    }

    public class ThreadPool
    {
        private readonly string ThreadName;
        private readonly int MinThreads;
        private readonly EzySynchronizedQueue<ThreadWrapper> Threads;
        private readonly AtomicLong ThreadCount;

        public ThreadPool(string threadName, int minThreads)
        {
            this.ThreadName = threadName;
            this.MinThreads = minThreads;
            this.ThreadCount = new AtomicLong();
            this.Threads = new EzySynchronizedQueue<ThreadWrapper>();
            this.AddMinThreads();
        }

        private void AddMinThreads()
        {
            for(int i = 0; i < MinThreads; ++i)
            {
                Threads.offer(NewThread());
            }
        }

        private ThreadWrapper NewThread()
        {
            var threadName = ThreadName + "-" + ThreadCount.incrementAndGet();
            var newThread = new ThreadWrapper(threadName);
            newThread.Start();
            return newThread;
        }

        public void Execute(Runnable task)
        {
            lock(Threads)
            {
                var currentThread = Threads.poll();
                Threads.offer(currentThread);
                currentThread.Execute(task);
            }
        }

        public void Shutdown()
        {
            while (!Threads.isEmpty())
            {
                var thread = Threads.poll();
                thread.StopNow();
            }
        }

        public void ShutdownNow()
        {
            while(!Threads.isEmpty())
            {
                var thread = Threads.poll();
                thread.StopNow();
            }
        }
    }

    public class ThreadPoolExample
    {
        public static void Main(string[] args)
        {
            var executorService = new ExecutorService("test", 3, 10);
            var numberOfTasks = 12;
            var tasks = new Runnable[numberOfTasks];
            for (int i = 0; i < numberOfTasks; ++i)
            {
                var tmp = i;
                tasks[tmp] = () =>
                {
                    Console.WriteLine(Thread.CurrentThread.Name + ": Task-" + (tmp + 1) + " Run");
                    Thread.Sleep(1000);
                };
            }
            for (int i = 0; i < numberOfTasks; ++i)
            {
                executorService.Execute(tasks[i]);
            }
        }
    }
}
