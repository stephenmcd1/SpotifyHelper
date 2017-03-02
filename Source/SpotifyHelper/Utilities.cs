using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace SpotifyHelper
{
    public static class Utilities
    {
        public static IEnumerable<Type> GetAvailableTypes(this Assembly assembly)
        {
            try
            {
                return assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException ex)
            {
                return ex.Types.Where(x => x != null);
            }
        }
    }
}
