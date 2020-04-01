﻿using Jitex.Utils.Comparer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Jitex.JIT
{
    internal static class AppModules
    {
        private static readonly IDictionary<IntPtr, Module> MapScopeToHandle = new Dictionary<IntPtr, Module>(IntPtrEqualityComparer.Instance);

        private static readonly FieldInfo m_pData;

        static AppModules()
        {
            m_pData = Type.GetType("System.Reflection.RuntimeModule").GetField("m_pData", BindingFlags.NonPublic | BindingFlags.Instance);

            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                AddAssembly(assembly);
            }

            AppDomain.CurrentDomain.AssemblyLoad += CurrentDomainOnAssemblyLoad;
        }

        private static void CurrentDomainOnAssemblyLoad(object? sender, AssemblyLoadEventArgs args)
        {
            AddAssembly(args.LoadedAssembly);
        }

        private static void AddAssembly(Assembly assembly)
        {
            Module module = assembly.Modules.First();
            IntPtr scope = GetPointerFromModule(module);
            MapScopeToHandle.Add(scope, module);
        }

        public static Module GetModuleByPointer(IntPtr scope)
        {
            return MapScopeToHandle.TryGetValue(scope, out Module module) ? module : null;
        }

        public static IntPtr GetPointerFromModule(Module module)
        {
            return (IntPtr)m_pData.GetValue(module);
        }
    }
}