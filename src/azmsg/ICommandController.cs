using System;
using System.Collections.Generic;
using System.CommandLine;
using System.Text;

namespace azmsg
{
    interface ICommandController
    {
        Command CreateCommand();
    }
}
