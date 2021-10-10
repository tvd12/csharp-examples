using System;
using System.Threading;
using examples.Concurrent;

namespace examples.MultiThreading
{
    public class ExecutorService
    {
        private readonly string ThreadName;
        private readonly int MinThreads;
        private readonly int MaxThreads;
        private readonly EzySynchronizedQueue<Thread> Threads;
        private readonly EzyBlockingQueue<object> Tasks;
        private readonly AtomicLong ThreadCount;
        private readonly AtomicInteger RunningThreadCount;
        private readonly FutureManager FutureManager;
        public static readonly object POISON = new object();

        public ExecutorService(string threadName, int minThreads, int maxThreads)
        {
            this.ThreadName = threadName;
            this.MinThreads = minThreads;
            this.MaxThreads = maxThreads;
            this.ThreadCount = new AtomicLong();
            this.FutureManager = new FutureManager();
            this.RunningThreadCount = new AtomicInteger();
            this.Tasks = new EzyBlockingQueue<object>();
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
                    if(currentTask is Runnable)
                    {
                        ((Runnable)currentTask)();
                    }
                    else
                    {
                        var taskT = ((Runnable<object>)currentTask);
                        var future = FutureManager.RemoveFuture(taskT);
                        try
                        {
                            var result = taskT();
                            if (future != null)
                            {
                                future.Finish(result);
                            }
                        }
                        catch (Exception e)
                        {
                            if(future != null)
                            {
                                future.Finish(e);
                            }
                        }

                    }
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

        public Future<T> Submit<T>(Runnable<object> task)
        {
            Future<T> future = FutureManager.AddNewFuture<T>(task);
            Tasks.offer(task);
            return future;
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

    public class ExecutorServiceExample
    {
        public static void MainX(string[] args)
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
