using System;
using System.Linq;

namespace Belzont.Utils
{
    public static class BridgeUtils
    {
        public static object[] GetAllLoadableClassesInAppDomain(Type t)
        {
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            return assemblies.SelectMany(s =>
            {
                try
                {
                    return s.GetExportedTypes();
                }
                catch
                {
                    return new Type[0];
                }
            }).Where(p =>
            {
                try
                {
                    var result = t.IsAssignableFrom(p) && p.IsClass && !p.IsAbstract;
                    return result;
                }
                catch { return false; }
            }).Select(x =>
            {
                try
                {
                    LogUtils.DoLog("Trying to instantiate '{0}'", x.AssemblyQualifiedName);
                    return x.GetConstructor(new Type[0]).Invoke(new object[0]);
                }
                catch (Exception e)
                {
                    LogUtils.DoLog("Failed instantiate '{0}': {1}", x.AssemblyQualifiedName, e);
                    return null;
                }
            }).Where(x => x != null).ToArray();
        }
        public static T[] GetAllLoadableClassesInAppDomain<T>() where T : class
        {
            return GetAllLoadableClassesInAppDomain(typeof(T)).Cast<T>().ToArray();
        }

        public static T[] GetAllLoadableClassesByTypeName<T, U>(Func<U> destinationGenerator) where T : class where U : T
        {
            var classNameBase = typeof(T).FullName;
            var allTypes = GetAllInterfacesWithTypeName(classNameBase);
            LogUtils.DoLog("Classes with same name of '{0}' found: {1}", classNameBase, allTypes.Length);
            return allTypes
                   .SelectMany(x =>
                   {
                       var res = GetAllLoadableClassesInAppDomain(x);
                       LogUtils.DoLog("Objects loaded: {0}", res.Length);
                       return res;
                   })
                   .Select(x => TryConvertClass<T, U>(x, destinationGenerator))
                   .Where(x => x != null).ToArray();
        }

        public static Type[] GetAllInterfacesWithTypeName(string typeName)
        {
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            return assemblies.SelectMany(s =>
            {
                try
                {
                    return s.GetExportedTypes();
                }
                catch
                {
                    return new Type[0];
                }
            }).Where(p =>
            {
                try
                {
                    return p.FullName == typeName && p.IsInterface;
                }
                catch { return false; }
            }).ToArray();
        }

        public static T TryConvertClass<T, U>(object instance, Func<U> destinationGenerator) where U : T
        {
            LogUtils.DoLog("Trying to convert {0} to class {1}", instance.GetType().FullName, typeof(T).FullName);
            if (instance.GetType().IsAssignableFrom(typeof(T)))
            {
                return (T)instance;
            }
            var newInstanceOfT = destinationGenerator();
            foreach (var fieldOnSrc in typeof(T).GetProperties(RedirectorUtils.allFlags))
            {
                var property = newInstanceOfT.GetType().GetProperty(fieldOnSrc.Name, RedirectorUtils.allFlags);
                if (!(fieldOnSrc is null) && fieldOnSrc.PropertyType.IsAssignableFrom(property.PropertyType))
                {
                    property.SetValue(newInstanceOfT, fieldOnSrc.GetValue(instance));
                }
            }
            return newInstanceOfT;
        }
    }
}
