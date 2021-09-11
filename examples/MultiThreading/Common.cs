using System;
using System.Threading;
using System.Collections.Generic;

namespace examples
{
    public delegate void Runnable();

    public class NonBlockingQueue
    {
        private readonly Queue<Runnable> Tasks = new Queue<Runnable>();
        private static readonly NonBlockingQueue INSTANCE = new NonBlockingQueue();

        private NonBlockingQueue()
        {
        }

        public static NonBlockingQueue GetInstance()
        {
            return INSTANCE;
        }

        public void AddTask(Runnable task)
        {
            lock (Tasks)
            {
                Tasks.Enqueue(task);
            }
        }

        public void TakeTask(List<Runnable> buffer)
        {
            lock (Tasks)
            {
                while (Tasks.Count > 0)
                {
                    buffer.Add(Tasks.Dequeue());
                }
            }
        }
    }

    public class EventLoop
    {
        private readonly string ThreadName;
        private Runnable UpdateCallback;
        private const int DAFAUT_SLEEP_TIME = 16;
        public const string MAIN_THREAD_NAME = "main";

        public EventLoop(string threadName = MAIN_THREAD_NAME)
        {
            this.ThreadName = threadName;
        }

        public void Start(int defaultSleepTime = DAFAUT_SLEEP_TIME)
        {
            var buffer = new List<Runnable>();
            while (true)
            {
                if(string.IsNullOrEmpty(Thread.CurrentThread.Name))
                {
                    Thread.CurrentThread.Name = ThreadName;
                }
                var startTime = DateTime.Now;
                buffer.Clear();
                NonBlockingQueue.GetInstance().TakeTask(buffer);
                foreach (Runnable task in buffer)
                {
                    try
                    {
                        task();
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("process task error: {0}", e.ToString());
                    }
                }
                Update();
                var endTime = DateTime.Now;
                var elapsedTime = (endTime - startTime).TotalMilliseconds;
                var sleeptime = defaultSleepTime - (long)elapsedTime;
                if (sleeptime > 0)
                {
                    Thread.Sleep((int)sleeptime);
                }
            }
        }

        public void Async(Runnable task)
        {
            NonBlockingQueue.GetInstance().AddTask(task);
        }

        private void Update()
        {
            DoUpdate();
            UpdateCallback?.Invoke();
        }

        protected virtual void DoUpdate()
        {
        }

        public void OnUpdated(Runnable callback)
        {
            UpdateCallback = callback;
        }
    }
}
