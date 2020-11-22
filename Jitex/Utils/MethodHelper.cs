﻿using System;
using System.Reflection;

namespace Jitex.Utils
{
    internal static class MethodHelper
    {
        private static readonly ConstructorInfo? CtorHandle;
        private static readonly MethodInfo? GetMethodBase;

        static MethodHelper()
        {
            Type? runtimeMethodHandleInternalType = Type.GetType("System.RuntimeMethodHandleInternal");

            if (runtimeMethodHandleInternalType == null)
                throw new TypeLoadException("Type System.RuntimeMethodHandleInternal was not found!");

            Type? runtimeType = Type.GetType("System.RuntimeType");

            if (runtimeType == null)
                throw new TypeLoadException("Type System.RuntimeType was not found!");

            CtorHandle = runtimeMethodHandleInternalType.GetConstructor(BindingFlags.NonPublic | BindingFlags.Instance, null, new[] { typeof(IntPtr) }, null);

            if (CtorHandle == null)
                throw new MethodAccessException("Constructor from RuntimeMethodHandleInternal was not found!");

            GetMethodBase = runtimeType
                .GetMethod("GetMethodBase", BindingFlags.NonPublic | BindingFlags.Static, null, CallingConventions.Any, new[] { runtimeType, runtimeMethodHandleInternalType }, null);

            if (GetMethodBase == null)
                throw new MethodAccessException("Method GetMethodBase from RuntimeType was not found!");
        }

        public static MethodBase GetMethodFromHandle(IntPtr methodHandle)
        {
            object? handle = CtorHandle!.Invoke(new object?[] { methodHandle });
            return (MethodBase)GetMethodBase!.Invoke(null, new[] { null, handle });
        }
    }
}
