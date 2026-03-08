// See the LICENSE file in the project root for more information.

using System;

namespace Tye2.Core
{
    public class CommandException : Exception
    {
        public CommandException(string message)
            : base(message)
        { }

        public CommandException(string message, Exception inner)
        : base(message, inner)
        { }
    }
}
