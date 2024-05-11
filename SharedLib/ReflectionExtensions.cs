using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace SharedLib
{
    public static class ReflectionExtensions
    {
        /// <summary>
        /// Get most specific public instance property
        /// </summary>
        /// <param name="type">Type to analyze</param>
        /// <param name="propName">Name of property</param>
        /// <returns>First or default PropertyInfo match in inheritance tree</returns>
        public static PropertyInfo GetMostSpecificProperty(this Type type, string propName)
        {
            return type.GetMostSpecificProperty(propName, BindingFlags.Instance | BindingFlags.Public);
        }
        /// <summary>
        /// Get most specific property of provided bindnings
        /// </summary>
        /// <param name="type">Type to analyze</param>
        /// <param name="propName">Name of property</param>
        /// <param name="bindingFlags">Bindning to match</param>
        /// <returns>First or default PropertyInfo match in inheritance tree</returns>
        public static PropertyInfo GetMostSpecificProperty(this Type type, string propName, BindingFlags bindingFlags)
        {
            if ((object)type == null)
                return null;
            PropertyInfo info = type.GetProperties(bindingFlags).FirstOrDefault(propInfo => propInfo.Name == propName);
            if (info != null) return info;
            return type.GetInterfaces().SelectFirstOrDefault<Type, PropertyInfo>(qType => qType.GetProperty(propName, bindingFlags | BindingFlags.DeclaredOnly), propInfo => propInfo != null);
        }
    }
}
