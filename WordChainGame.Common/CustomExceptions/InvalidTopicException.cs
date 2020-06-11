using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WordChainGame.Common.CustomExceptions
{
    using System;
    public class InvalidTopicException : Exception
    {
        public InvalidTopicException()
        {
        }

        public InvalidTopicException(string message) : base(message)
        {
        }

        public InvalidTopicException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
