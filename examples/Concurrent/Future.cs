using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace examples.Concurrent
{
    public interface IFuture
    {
        public void Finish(object result);
    }

    public class Future<T> : IFuture
    {
        protected T Result;
        protected Exception Exception;
        protected volatile bool Done;
        protected volatile bool Cancelled;
        protected readonly long Id = ID_GENTOR.incrementAndGet();
        protected readonly TimeSpan Timeout = TimeSpan.Zero;
        protected readonly DateTime CreationDate = DateTime.Now;
        protected readonly Object Lock = new Object();
        protected readonly AutoResetEvent State = new AutoResetEvent(false);

        private static readonly AtomicLong ID_GENTOR = new AtomicLong(0);

        public Future()
        {
        }

        public Future(TimeSpan timeout)
        {
            this.Timeout = timeout;
        }

        public T GetResult()
        {
            while (!Done && !Cancelled)
            {
                if (Timeout.Equals(TimeSpan.Zero))
                {
                    bool success = State.WaitOne();
                    if (!success)
                        throw new ThreadInterruptedException("Task: " + Id + " has interrupted");
                }
                else
                {
                    bool success = State.WaitOne(Timeout);
                    if (!success)
                    {
                        throw new TimeoutException(GetTimeoutMessage());
                    }
                }
            }
            if (Cancelled)
            {
                throw new TaskCanceledException("Task: " + Id + " has canceled");
            }
            if (Exception != null)
            {
                throw Exception;
            }
            return Result;
        }

        public void Finish(object result)
        {
            if (result is Exception)
            {
                Finish((Exception)result);
            }
            else
            {
                Finish((T)result);
            }
        }

        public void Finish(T result)
        {
            lock (Lock)
            {
                if (!Done && !Cancelled)
                {
                    Result = result;
                    Done = true;
                    State.Set();
                }
            }
        }

        public void Finish(Exception exception)
        {
            lock (Lock)
            {
                if (!Done && !Cancelled)
                {
                    Exception = exception;
                    Done = true;
                    State.Set();
                }
            }
        }

        public void Cancel()
        {
            lock (Lock)
            {
                if (!Done)
                {
                    Cancelled = true;
                    State.Set();
                }
            }
        }

        public bool IsDone()
        {
            return Done;
        }

        public bool IsCancelled()
        {
            return Cancelled;
        }

        private String GetTimeoutMessage()
        {
            return new StringBuilder()
                .Append("Task: ").Append(Id)
                .Append(" Create At: ").Append(CreationDate)
                .Append(" Timeout: ").Append(Timeout)
                .ToString();
        }

        public void Dispose()
        {
            Cancel();
        }

        public override string ToString()
        {
            return new StringBuilder()
                .Append("Task[Id: ").Append(Id)
                .Append(", Create At: ").Append(CreationDate)
                .Append("]")
                .ToString();
        }
    }

    public class FutureManager
    {
        protected readonly Object Lock = new object();
        protected readonly Dictionary<Object, IFuture> FutureByTaskId = new Dictionary<Object, IFuture>();

        public Future<T> AddNewFuture<T>(Object taskId)
        {
            return AddNewFuture<T>(taskId, TimeSpan.Zero);
        }

        public Future<T> AddNewFuture<T>(Object taskId, TimeSpan timeout)
        {
            var task = new Future<T>(timeout);
            AddFuture(taskId, task);
            return task;
        }

        public void AddFuture<T>(Object taskId, Future<T> future)
        {
            lock (Lock)
            {
                if (!future.IsDone() && !future.IsCancelled())
                    FutureByTaskId[taskId] = future;
            }
        }

        public IFuture RemoveFuture(Object taskId)
        {
            var future = default(IFuture);
            lock (Lock)
            {
                if (FutureByTaskId.ContainsKey(taskId))
                {
                    future = FutureByTaskId[taskId];
                    FutureByTaskId.Remove(taskId);
                }
            }
            return future;
        }

        public void Dispose()
        {
            lock (Lock)
            {
                foreach (var task in FutureByTaskId.Values)
                {
                    ((IDisposable)task).Dispose();
                }
                FutureByTaskId.Clear();
            }
        }
    }
}
