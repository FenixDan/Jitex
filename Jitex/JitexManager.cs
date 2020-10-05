﻿using System;
using System.Collections.Generic;
using Jitex.JIT;
using Jitex.Utils.Comparer;

namespace Jitex
{
    /// <summary>
    /// Jitex manager.
    /// </summary>
    public static class JitexManager
    {
        private static ManagedJit _jit;

        private static ManagedJit Jit => _jit ??= ManagedJit.GetInstance();

        /// <summary>
        /// All modules load on Jitex.
        /// </summary>
        public static IDictionary<Type, JitexModule> ModulesLoaded { get; } = new Dictionary<Type, JitexModule>(TypeComparer.Instance);

        /// <summary>
        /// Returns if Jitex is loaded on application. 
        /// </summary>
        public static bool IsLoaded => ManagedJit.IsLoaded;

        /// <summary>
        /// Load module on Jitex.
        /// </summary>
        /// <typeparam name="TModule">Module to load.</typeparam>
        public static void LoadModule<TModule>() where TModule : JitexModule, new()
        {
            if (!ModuleIsLoaded<TModule>())
            {
                JitexModule module = new TModule();
                module.LoadResolvers();
            }
        }

        /// <summary>
        /// Load module on Jitex.
        /// </summary>
        /// <param name="typeModule">Module to load.</param>
        public static void LoadModule(Type typeModule)
        {
            if (!ModuleIsLoaded(typeModule))
            {
                JitexModule module = (JitexModule)Activator.CreateInstance(typeModule);
                module.LoadResolvers();
            }
        }

        /// <summary>
        /// Load module on Jitex.
        /// </summary>
        /// <param name="module">Module to load.</param>
        internal static void LoadModule(JitexModule module)
        {
            ModulesLoaded.Add(module.GetType(), module);
            module.LoadResolvers();
        }

        /// <summary>
        /// Remove module from Jitex.
        /// </summary>
        /// <typeparam name="TModule">Module to remove.</typeparam>
        public static void RemoveModule<TModule>() where TModule : JitexModule
        {
            RemoveModule(typeof(TModule));
        }

        /// <summary>
        /// Remove module from Jitex.
        /// </summary>
        /// <param name="typeModule">Module to remove.</param>
        public static void RemoveModule(Type typeModule)
        {
            if (ModulesLoaded.TryGetValue(typeModule, out JitexModule module))
            {
                ModulesLoaded.Remove(typeModule);
                module.Dispose();
            }
        }

        /// <summary>
        /// Returns if module is loaded on Jitex.
        /// </summary>
        /// <typeparam name="TModule">Module to check.</typeparam>
        public static bool ModuleIsLoaded<TModule>() where TModule : JitexModule
        {
            return ModuleIsLoaded(typeof(TModule));
        }

        /// <summary>
        /// Returns if module is loaded on Jitex.
        /// </summary>
        /// <param name="typeModule">Module to check.</param>
        /// <returns></returns>
        public static bool ModuleIsLoaded(Type typeModule)
        {
            if (ModulesLoaded.TryGetValue(typeModule, out JitexModule module))
            {
                return module.IsLoaded;
            }

            return false;
        }

        /// <summary>
        /// Add a method resolver.
        /// </summary>
        /// <param name="methodResolver">Method resolver to add.</param>
        public static void AddMethodResolver(JitexHandler.MethodResolverHandler methodResolver)
        {
            Jit.AddMethodResolver(methodResolver);
        }

        /// <summary>
        /// Add a token resolver.
        /// </summary>
        /// <param name="tokenResolver">Token resolver to add.</param>
        public static void AddTokenResolver(JitexHandler.TokenResolverHandler tokenResolver)
        {
            Jit.AddTokenResolver(tokenResolver);
        }

        /// <summary>
        /// Remove a method resolver.
        /// </summary>
        /// <param name="methodResolver">Method resolver to remove.</param>
        public static void RemoveMethodResolver(JitexHandler.MethodResolverHandler methodResolver)
        {
            Jit.RemoveMethodResolver(methodResolver);
        }

        /// <summary>
        /// Remove a token resolver.
        /// </summary>
        /// <param name="tokenResolver">Token resolver to remove.</param>
        public static void RemoveTokenResolver(JitexHandler.TokenResolverHandler tokenResolver)
        {
            Jit.RemoveTokenResolver(tokenResolver);
        }

        /// <summary>
        /// If a method resolver is already loaded.
        /// </summary>
        /// <param name="methodResolver">Method resolver.</param>
        /// <returns>True to already loaded. False to not loaded.</returns>
        public static bool HasMethodResolver(JitexHandler.MethodResolverHandler methodResolver) => _jit.HasMethodResolver(methodResolver);

        /// <summary>
        /// If a token resolver is already loaded.
        /// </summary>
        /// <param name="tokenResolver">Token resolver.</param>
        /// <returns>True to already loaded. False to not loaded.</returns>
        public static bool HasTokenResolver(JitexHandler.TokenResolverHandler tokenResolver) => _jit.HasTokenResolver(tokenResolver);

        /// <summary>
        /// Unload Jitex and modules from application.
        /// </summary>
        public static void Remove()
        {
            _jit.Dispose();
            _jit = null;
        }
    }
}