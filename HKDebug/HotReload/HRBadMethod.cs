using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HKDebug.HotReload
{
    public class HRBadMethod : Exception
    {
        public HRBadMethod(): base("调用了损坏的方法")
        {

        }
    }
}
