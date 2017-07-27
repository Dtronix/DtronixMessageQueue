using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;

namespace DtronixMessageQueue.Rpc
{
    public class ServiceMethodCache<TSession, TConfig>
        where TSession : RpcSession<TSession, TConfig>, new()
        where TConfig : RpcConfig
    {

        private delegate object InvokeHandler(object target, object[] paramters);


        private Dictionary<string, Dictionary<string, ServiceMethodInfo>> _serviceMethods = 
            new Dictionary<string, Dictionary<string, ServiceMethodInfo>>();

        public ServiceMethodCache()
        {

        }

        public void AddService<T>(T instance) where T : IRemoteService<TSession, TConfig>
        {
            // If this service has already been registered, do nothing.
            if (_serviceMethods.ContainsKey(instance.Name))
                return;

            var methods = new 

            // Get all the public methods for this 
            var methods = instance.GetType().GetMethods();

            foreach (var methodInfo in methods)
            {
                methodInfo.Name
            }
        }

        public void Invoke(string service, string method, object serviceTarget, object[] parameters)
        {
            
        }

        private class ServiceMethodInfo
        {
            public MethodInfo MethodInfo;
            public Type[] ParameterTypes;
            public bool hasCancellation;
        }

        /// <summary>
        /// Black magic box to create IL code invoker.
        /// </summary>
        /// <param name="methodInfo">Method to create an invoker for.</param>
        /// <returns>Invoker delegate crafted for the specified method.</returns>
        /// <remarks>
        /// https://www.codeproject.com/Articles/14593/A-General-Fast-Method-Invoker?msg=1555655#xx1555655xx
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

        private static void EmitCastToReference(ILGenerator il, System.Type type)
        {
            il.Emit(type.IsValueType ? OpCodes.Unbox_Any : OpCodes.Castclass, type);
        }

        private static void EmitBoxIfNeeded(ILGenerator il, System.Type type)
        {
            if (type.IsValueType)
            {
                il.Emit(OpCodes.Box, type);
            }
        }

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
