using System;
using System.Collections.Generic;
using System.Text;

namespace eBRestarter.Core.Domain.Models
{
    public class CountdownTimer
    {
        public int SecondsRemaining { get; private set; }

        public CountdownTimer(int startSeconds)
        {
            if (startSeconds < 0) throw new ArgumentException("Zeit darf nicht negativ sein.");
            SecondsRemaining = startSeconds;
        }

        public void Tick()
        {
            if (SecondsRemaining > 0)
            {
                SecondsRemaining--;
            }
        }

        public bool IsFinished => SecondsRemaining == 0;
    }
}
