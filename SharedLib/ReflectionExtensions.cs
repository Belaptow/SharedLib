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
        public static PropertyInfo GetMostSpecificProperty(this Type type, string propName)
        {
            return type.GetMostSpecificProperty(propName, BindingFlags.Instance | BindingFlags.Public);
        }
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
