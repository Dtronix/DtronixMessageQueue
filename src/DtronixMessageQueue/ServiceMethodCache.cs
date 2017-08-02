using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading;

namespace DtronixMessageQueue
{
    public class ServiceMethodCache
    {

        /// <summary>
        /// Internal delegate used for calling of the compile method calls.
        /// </summary>
        /// <param name="target">Instance this invoker is invoking on.</param>
        /// <param name="paramters">PArameters passed to method.</param>
        /// <returns>Return object if there is one.</returns>
        private delegate object InvokeHandler(object target, object[] paramters);

        /// <summary>
        /// Dictionary for all services and associated cached methods.
        /// </summary>
        private readonly Dictionary<string, Dictionary<string, CachedMethodInfo>> _services = 
            new Dictionary<string, Dictionary<string, CachedMethodInfo>>();

        /// <summary>
        /// Add a service and associated public methods to the cache.
        /// </summary>
        /// <param name="serviceName">Name of the server</param>
        /// <param name="instance">Instance containing all public methods to cache.</param>
        public void AddService(string serviceName, object instance)
        {
            // If this service has already been registered, do nothing.
            if (_services.ContainsKey(serviceName))
                return;

            // Create a new dictionary for all available services in this instance.
            var serviceMethods = new Dictionary<string, CachedMethodInfo>();

            // Get all the public methods for this 
            var methods = instance.GetType().GetMethods();

            // All public methods to cache.
            foreach (var methodInfo in methods)
            {
                var smi = new CachedMethodInfo(methodInfo);

                // See if the last parameter is a cancellation token.  If so, cache the info.
                if (smi.ParameterTypes.Length > 0)
                {
                    var lastParam = smi.ParameterTypes[smi.ParameterTypes.Length - 1];
                    smi.HasCancellation = lastParam == typeof(CancellationToken);
                }

                // Add the cached info the service cached dictionary.
                serviceMethods.Add(smi.MethodName, smi);
            }

            // Add all the caches to the main dictionary.
            _services.Add(serviceName, serviceMethods);
        }

        /// <summary>
        /// Gets the cached information about the specified service method.
        /// </summary>
        /// <param name="service">Service name to lookup.</param>
        /// <param name="method">Method name to lookup</param>
        /// <returns></returns>
        public CachedMethodInfo GetMethodInfo(string service, string method)
        {
            if (!_services.ContainsKey(service) || !_services[service].ContainsKey(method))
                return null;

            return _services[service][method];
        }

        /// <summary>
        /// Cache class to contain all important information about the cached method.
        /// </summary>
        public class CachedMethodInfo
        {
            /// <summary>
            /// Method info for this method.
            /// </summary>
            public MethodInfo MethodInfo;

            /// <summary>
            /// Name of this method
            /// </summary>
            public string MethodName;

            /// <summary>
            /// Name of the service this method belongs to.
            /// </summary>
            public string ServiceName;

            /// <summary>
            /// Created method invoker for this method.
            /// </summary>
            private InvokeHandler Invoker;

            /// <summary>
            /// Array of all the parameter types.
            /// </summary>
            public Type[] ParameterTypes;

            /// <summary>
            /// True of this is a cancel-able method.
            /// </summary>
            public bool HasCancellation;

            /// <summary>
            /// Type that this method returns.
            /// </summary>
            public Type ReturnType;

            /// <summary>
            /// Invokes the cached method.
            /// </summary>
            /// <param name="target">Target instance.</param>
            /// <param name="parameters">Method parameters.</param>
            /// <returns>Return value if there is one.</returns>
            public object Invoke(object target, object[] parameters)
            {
                return Invoker(target, parameters);
            }

            /// <summary>
            /// Creates a cached method info for 
            /// </summary>
            /// <param name="methodInfo"></param>
            public CachedMethodInfo(MethodInfo methodInfo)
            {
                MethodName = methodInfo.Name;
                MethodInfo = methodInfo;
                ReturnType = methodInfo.ReturnType;
                Invoker = GetMethodInvoker(methodInfo);
                ParameterTypes = methodInfo.GetParameters().Select(p => p.ParameterType).ToArray();
            }
        }

        /// <summary>
        /// Black magic box to create IL code invoker.
        /// </summary>
        /// <param name="methodInfo">Method to create an invoker for.</param>
        /// <returns>Invoker delegate crafted for the specified method.</returns>
        /// <remarks>
        /// https://www.codeproject.com/Articles/14593/A-General-Fast-Method-Invoker
        /// http://archive.is/JSZAY
        /// </remarks>
        private static InvokeHandler GetMethodInvoker(MethodInfo methodInfo)
        {
            if (methodInfo == null)
                throw new ArgumentNullException(nameof(methodInfo), "Passed method must not be null.");

            var dynamicMethod = new DynamicMethod(string.Empty, typeof(object),
                new[] { typeof(object), typeof(object[]) }, methodInfo.DeclaringType.Module);

            var il = dynamicMethod.GetILGenerator();
            var ps = methodInfo.GetParameters();
            var paramTypes = new Type[ps.Length];

            for (var i = 0; i < paramTypes.Length; i++)
            {
                if (ps[i].ParameterType.IsByRef)
                    paramTypes[i] = ps[i].ParameterType.GetElementType();
                else
                    paramTypes[i] = ps[i].ParameterType;
            }

            var locals = new LocalBuilder[paramTypes.Length];

            for (var i = 0; i < paramTypes.Length; i++)
                locals[i] = il.DeclareLocal(paramTypes[i], true);

            for (var i = 0; i < paramTypes.Length; i++)
            {
                il.Emit(OpCodes.Ldarg_1);
                EmitFastInt(il, i);
                il.Emit(OpCodes.Ldelem_Ref);
                EmitCastToReference(il, paramTypes[i]);
                il.Emit(OpCodes.Stloc, locals[i]);
            }

            if (!methodInfo.IsStatic)
                il.Emit(OpCodes.Ldarg_0);

            for (var i = 0; i < paramTypes.Length; i++)
                il.Emit(ps[i].ParameterType.IsByRef ? OpCodes.Ldloca_S : OpCodes.Ldloc, locals[i]);

            il.EmitCall(methodInfo.IsStatic ? OpCodes.Call : OpCodes.Callvirt, methodInfo, null);


            if (methodInfo.ReturnType == typeof(void))
                il.Emit(OpCodes.Ldnull);
            else
                EmitBoxIfNeeded(il, methodInfo.ReturnType);

            for (var i = 0; i < paramTypes.Length; i++)
            {
                if (!ps[i].ParameterType.IsByRef)
                    continue;

                il.Emit(OpCodes.Ldarg_1);
                EmitFastInt(il, i);
                il.Emit(OpCodes.Ldloc, locals[i]);

                if (locals[i].LocalType.IsValueType)
                    il.Emit(OpCodes.Box, locals[i].LocalType);

                il.Emit(OpCodes.Stelem_Ref);
            }

            il.Emit(OpCodes.Ret);
            var invoker = (InvokeHandler)dynamicMethod.CreateDelegate(typeof(InvokeHandler));
            return invoker;
        }

        /// <summary>
        /// Helper method.
        /// </summary>
        private static void EmitCastToReference(ILGenerator il, Type type)
        {
            il.Emit(type.IsValueType ? OpCodes.Unbox_Any : OpCodes.Castclass, type);
        }

        /// <summary>
        /// Helper method.
        /// </summary>
        private static void EmitBoxIfNeeded(ILGenerator il, Type type)
        {
            if (type.IsValueType)
                il.Emit(OpCodes.Box, type);
        }

        /// <summary>
        /// Helper method.
        /// </summary>
        private static void EmitFastInt(ILGenerator il, int value)
        {
            switch (value)
            {
                case -1:
                    il.Emit(OpCodes.Ldc_I4_M1);
                    return;
                case 0:
                    il.Emit(OpCodes.Ldc_I4_0);
                    return;
                case 1:
                    il.Emit(OpCodes.Ldc_I4_1);
                    return;
                case 2:
                    il.Emit(OpCodes.Ldc_I4_2);
                    return;
                case 3:
                    il.Emit(OpCodes.Ldc_I4_3);
                    return;
                case 4:
                    il.Emit(OpCodes.Ldc_I4_4);
                    return;
                case 5:
                    il.Emit(OpCodes.Ldc_I4_5);
                    return;
                case 6:
                    il.Emit(OpCodes.Ldc_I4_6);
                    return;
                case 7:
                    il.Emit(OpCodes.Ldc_I4_7);
                    return;
                case 8:
                    il.Emit(OpCodes.Ldc_I4_8);
                    return;
            }

            if (value > -129 && value < 128)
            {
                il.Emit(OpCodes.Ldc_I4_S, (SByte)value);
            }
            else
            {
                il.Emit(OpCodes.Ldc_I4, value);
            }
        }


    }



 


}
