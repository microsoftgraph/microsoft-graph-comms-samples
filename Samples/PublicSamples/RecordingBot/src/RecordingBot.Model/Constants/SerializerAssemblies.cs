using Microsoft.Graph.Communications.Client;
using Microsoft.Graph.Communications.Common;
using Microsoft.Graph.Models;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace RecordingBot.Model.Constants
{
    public static class SerializerAssemblies
    {
        private static readonly IEnumerable<Assembly> _assemblies =
            [
                typeof(Entity).Assembly,
                typeof(Error).Assembly,
                typeof(CommunicationsClientBuilder).Assembly
            ];

        private static Assembly[] _distinctAssemblies = null;

        public static Assembly[] Assemblies
        {
            get
            {
                if (_distinctAssemblies == null)
                {
                    _distinctAssemblies = _assemblies
                        .Where(assembly => assembly != null)
                        .Distinct()
                        .ToArray();
                }

                return _distinctAssemblies;
            }
        }
    }
}
