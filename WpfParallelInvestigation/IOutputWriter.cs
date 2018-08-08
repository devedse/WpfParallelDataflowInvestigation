using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WpfParallelInvestigation
{
    public interface IOutputWriter
    {
        void WriteOutput(string msg);
    }
}
