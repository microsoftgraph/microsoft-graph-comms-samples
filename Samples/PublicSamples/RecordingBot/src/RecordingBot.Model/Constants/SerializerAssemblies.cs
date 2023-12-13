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
        private static IEnumerable<Assembly> _assemblies = new List<Assembly>
            {
                typeof(Entity).Assembly,
                typeof(Error).Assembly,
                typeof(CommunicationsClientBuilder).Assembly
            };

        private static Assembly[] _distinctAssemblies = null;

        public static Assembly[] Assemblies
        {
            get
            {
                if (_distinctAssemblies == null)
                {
                    HashSet<Assembly> hashSet = [];
                    List<Assembly> list = [];
                    _assemblies = _assemblies.Where(assembly => assembly != null);
                    foreach (Assembly item in _assemblies)
                    {
                        if (!hashSet.Contains(item))
                        {
                            list.Add(item);
                            hashSet.Add(item);
                        }
                    }
                    _distinctAssemblies = [.. list];
                }
                return _distinctAssemblies;
            }
        }
    }
}
