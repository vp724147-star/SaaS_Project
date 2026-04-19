
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.Contracts
{
    public class KafkaMessage<T>
    {
        public T Data { get; set; }
        public int RetryCount { get; set; } = 0;
    }
}
