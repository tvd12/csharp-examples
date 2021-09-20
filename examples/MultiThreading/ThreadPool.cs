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
        private readonly int MaxThreads;
        private readonly EzySynchronizedQueue<ThreadWrapper> Threads;
        private readonly EzyBlockingQueue<Runnable> Tasks;
        private readonly AtomicLong ThreadCount;
        private readonly AtomicInteger RunningThreadCount;
        public static readonly Runnable POISON = () => { };

        public ThreadPool(string threadName, int minThreads, int maxThreads)
        {
            this.ThreadName = threadName;
            this.MinThreads = minThreads;
            this.MaxThreads = maxThreads;
            this.ThreadCount = new AtomicLong();
            this.RunningThreadCount = new AtomicInteger();
            this.Tasks = new EzyBlockingQueue<Runnable>();
            this.Threads = new EzySynchronizedQueue<Thread>();
            this.AddMinThreads();
        }

        private void AddMinThreads()
        {
            for(int i = 0; i < MinThreads; ++i)
            {
                Threads.offer(NewThread());
            }
        }

        private Thread NewThread()
        {
            var threadName = ThreadName + "-" + ThreadCount.incrementAndGet();
            var newThread = new Thread(Loop);
            newThread.Name = threadName;
            newThread.Start();
            return newThread;
        }

        private void Loop()
        {
            while(true)
            {
                var currentTask = Tasks.take();
                int currentRunningThreadCount = RunningThreadCount.incrementAndGet();
                if(!Tasks.isEmpty() &&
                    currentRunningThreadCount >= Threads.size() &&
                    currentRunningThreadCount < MaxThreads)
                {
                    Threads.offer(NewThread());
                }
                if(currentTask == POISON)
                {
                    break;
                }
                try
                {
                    currentTask();
                }
                catch(Exception e)
                {
                    Console.WriteLine("handle task failed", e);
                }
                finally
                {
                    RunningThreadCount.decrementAndGet();
                }
            }
        }

        public void Execute(Runnable task)
        {
            Tasks.offer(task);
        }

        public void Shutdown()
        {
            for(int i = 0; i < Threads.size(); ++i)
            {
                Tasks.add(POISON);
            }
        }

        public void ShutdownNow()
        {
            while(!Threads.isEmpty())
            {
                var thread = Threads.poll();
                thread.Abort();
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
