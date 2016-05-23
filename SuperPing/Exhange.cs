using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SuperPing
{
    class ExhangeNode
    {
        public object Tag;
        public Task Action;
        public bool HasRun;

        public ExhangeNode(object tag, Task TaskToRun)
        {
            Tag = tag;
            Action = TaskToRun;
            HasRun = false;
        }
    }

    class Exhange
    {
        private static Exhange _ex;

        public static Exhange GetExhange()
        {
            if (_ex == null)
            {
                _ex = new Exhange();
            }
            return _ex;
        }

        private List<ExhangeNode> _queue;

        public Exhange()
        {
            _queue = new List<ExhangeNode>();
        }

        public void AddExhangeNode(ExhangeNode node)
        {
            node.HasRun = false;
            node.Action.Start();
        }
    }
}
