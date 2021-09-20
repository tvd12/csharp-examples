using System.Threading;

namespace examples.Concurrent
{
	public class AtomicInteger
	{
		protected int value = 0;

		public int get()
		{
			return this.value;
		}

		public void set(int val)
		{
			Interlocked.Exchange(ref value, val);
		}

		public int incrementAndGet()
		{
			int answer = Interlocked.Increment(ref value);
			return answer;
		}

		public int decrementAndGet()
		{
			int answer = Interlocked.Decrement(ref value);
			return answer;
		}
	}
}
