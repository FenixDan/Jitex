﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using Jitex.Builder.IL;
using Jitex.Builder.PE;
using Jitex.Builder.Utils.Extensions;

namespace Jitex.Builder.Method
{
    /// <summary>
    /// Provides create a body of a method.
    /// </summary>
    public class MethodBody
    {
        private byte[] _il;

        /// <summary>
        /// Module from body.
        /// </summary>
        public Module Module { get; }

        /// <summary>
        /// Generic class arguments used in body.
        /// </summary>
        public Type[] GenericTypeArguments { get; set; }

        /// <summary>
        /// Generic 
        /// </summary>
        public Type[] GenericMethodArguments { get; set; }

        /// <summary>
        /// IL from body.
        /// </summary>
        public byte[] IL
        {
            get => _il;
            set
            {
                _il = value;
                CalculateMaxStack();
            }
        }

        /// <summary>
        ///     Local variables from method.
        /// </summary>
        public IList<LocalVariableInfo> LocalVariables { get; set; }

        /// <summary>
        /// If body contains some local variable.
        /// </summary>
        public bool HasLocalVariable => LocalVariables?.Count > 0;

        /// <summary>
        /// Stack size from body.
        /// </summary>
        public uint MaxStackSize { get; set; }

        /// <summary>
        /// Create a body from method.
        /// </summary>
        /// <param name="methodBase">Method to read.</param>
        public MethodBody(MethodBase methodBase)
        {
            Module = methodBase.Module;
            LocalVariables = methodBase.GetMethodBody().LocalVariables.Select(s => new LocalVariableInfo(s.LocalType)).ToList();

            if (methodBase.IsGenericMethod)
                GenericMethodArguments = methodBase.GetGenericArguments();

            if (methodBase.DeclaringType.IsGenericType)
                GenericTypeArguments = methodBase.DeclaringType.GetGenericArguments();

            IL = methodBase.GetILBytes();
        }
        
        /// <summary>
        /// Create a body from IL.
        /// </summary>
        /// <param name="il">IL instructions.</param>
        /// <param name="module">Module from IL.</param>
        /// <param name="genericTypeArguments">Generic class arguments used in body.</param>
        /// <param name="genericMethodArguments">Generic method arguments used in body.</param>
        /// <param name="variables">Local variables.</param>
        public MethodBody(IEnumerable<byte> il, Module module, Type[] genericTypeArguments = null, Type[] genericMethodArguments = null, params Type[] variables)
        {
            Module = module;
            LocalVariables = variables.Select(s => new LocalVariableInfo(s)).ToList();
            GenericTypeArguments = genericTypeArguments;
            GenericMethodArguments = genericMethodArguments;

            IL = il.ToArray();
        }


        /// <summary>
        /// Create a body from IL.
        /// </summary>
        /// <param name="il">IL instructions.</param>
        /// <param name="module">Module from IL.</param>
        /// <param name="genericTypeArguments">Generic class arguments used in body.</param>
        /// <param name="genericMethodArguments">Generic method arguments used in body.</param>
        public MethodBody(byte[] il, Module module, Type[] genericTypeArguments = null, Type[] genericMethodArguments = null)
        {
            Module = module;
            GenericTypeArguments = genericTypeArguments;
            GenericMethodArguments = genericMethodArguments;

            IL = il;
        }

        /// <summary>
        /// Create a body from IL.
        /// </summary>
        /// <param name="il">IL instructions.</param>
        /// <param name="module">Module from IL.</param>
        /// <param name="variables">Local variables.</param>
        public MethodBody(IEnumerable<byte> il, Module module, params Type[] variables)
        {
            Module = module;
            LocalVariables = variables.Select(s => new LocalVariableInfo(s)).ToList();

            IL = il.ToArray();
        }

        /// <summary>
        /// Create a body from IL.
        /// </summary>
        /// <param name="il">IL instructions.</param>
        /// <param name="variables">Local variables.</param>
        public MethodBody(IEnumerable<byte> il, params Type[] variables)
        {
            LocalVariables = variables.Select(s => new LocalVariableInfo(s)).ToList();
            _il = il.ToArray();
        }

        /// <summary>
        /// Create a body from IL.
        /// </summary>
        /// <param name="il">IL instructions.</param>
        /// <param name="maxStack">Stack size to body.</param>
        public MethodBody(IEnumerable<byte> il, uint maxStack = 8)
        {
            _il = il.ToArray();
            MaxStackSize = maxStack;
        }

        /// <summary>
        /// Read IL instructions from body.
        /// </summary>
        /// <returns>Operations from body.</returns>
        public IEnumerable<Operation> ReadIL()
        {
            return new ILReader(IL, Module, GenericTypeArguments, GenericMethodArguments);
        }

        /// <summary>
        ///     Calculate .maxstack from body.
        /// </summary>
        private void CalculateMaxStack()
        {
            int maxStackSize = 0;
            foreach (Operation operation in ReadIL())
            {
                switch (operation.OpCode.StackBehaviourPush)
                {
                    case StackBehaviour.Push0:
                        break;

                    case StackBehaviour.Push1:
                    case StackBehaviour.Pushi:
                    case StackBehaviour.Pushi8:
                    case StackBehaviour.Pushr4:
                    case StackBehaviour.Pushr8:
                    case StackBehaviour.Pushref:
                    case StackBehaviour.Varpush:
                        maxStackSize++;
                        break;

                    case StackBehaviour.Push1_push1:
                        maxStackSize += 2;
                        break;

                    default:
                        throw new NotImplementedException($"Stack operation not implemented: {operation.OpCode.Name}");
                }

                switch (operation.OpCode.StackBehaviourPop)
                {
                    case StackBehaviour.Pop0:
                    case StackBehaviour.Varpop:
                        break;

                    case StackBehaviour.Popref:
                    case StackBehaviour.Popi:
                    case StackBehaviour.Pop1:
                        maxStackSize--;
                        break;

                    case StackBehaviour.Popi_popr4:
                    case StackBehaviour.Popi_popr8:
                    case StackBehaviour.Popref_pop1:
                    case StackBehaviour.Popref_popi:
                    case StackBehaviour.Pop1_pop1:
                    case StackBehaviour.Popi_popi:
                    case StackBehaviour.Popi_pop1:
                    case StackBehaviour.Popi_popi8:
                        maxStackSize -= 2;
                        break;

                    case StackBehaviour.Popi_popi_popi:
                    case StackBehaviour.Popref_popi_pop1:
                    case StackBehaviour.Popref_popi_popi:
                    case StackBehaviour.Popref_popi_popi8:
                    case StackBehaviour.Popref_popi_popr4:
                    case StackBehaviour.Popref_popi_popr8:
                    case StackBehaviour.Popref_popi_popref:
                        maxStackSize -= 3;
                        break;

                    default:
                        throw new NotImplementedException($"Stack operation not implemented: {operation.OpCode.Name}");
                }

                if (maxStackSize > MaxStackSize)
                {
                    MaxStackSize = (uint)maxStackSize;
                }
            }
        }

        /// <summary>
        ///     Get compressed signature from local variables.
        /// </summary>
        /// <returns>Byte array - compressed signature.</returns>
        public byte[] GetSignatureVariables()
        {
            BlobBuilder blob = new BlobBuilder();

            blob.WriteByte(0x07);
            blob.WriteCompressedInteger(LocalVariables.Count);

            MetadataInfo metadataInfo = null;

            foreach (LocalVariableInfo variable in LocalVariables)
            {
                CorElementType elementType = variable.ElementType;

                if (elementType == CorElementType.ELEMENT_TYPE_SZARRAY)
                {
                    blob.WriteByte((byte)elementType);
                    elementType = LocalVariableInfo.DetectCorElementType(variable.Type.GetElementType());
                }

                if (elementType == CorElementType.ELEMENT_TYPE_CLASS || elementType == CorElementType.ELEMENT_TYPE_VALUETYPE)
                {
                    //TODO
                    //Pinned variables

                    if (Module != null)
                    {
                        metadataInfo ??= new MetadataInfo(Module.Assembly);
                    }
                    else
                    {
                        metadataInfo = new MetadataInfo(variable.Type.Assembly);
                    }

                    EntityHandle typeHandle = metadataInfo.GetTypeHandle(variable.Type);

                    //Check if type is referenced on assembly,
                    //If not, we should get reference in assembly of type.
                    //Ex.: String is not referenced directly on metadata assembly.
                    if (typeHandle == default && metadataInfo.Assembly != variable.Type.Assembly)
                    {
                        MetadataInfo metadataAssembly = new MetadataInfo(variable.Type.Assembly);
                        typeHandle = metadataAssembly.GetTypeHandle(variable.Type);
                    }

                    int typeInfo = CodedIndex.TypeDefOrRefOrSpec(typeHandle);

                    blob.WriteByte((byte)elementType);
                    blob.WriteCompressedInteger(typeInfo);
                }
                else
                {
                    blob.WriteByte((byte)elementType);
                }
            }

            BlobBuilder blobSize = new BlobBuilder();
            blobSize.WriteCompressedInteger(blob.Count);
            blob.LinkPrefix(blobSize);

            return blob.ToArray();
        }
    }
}