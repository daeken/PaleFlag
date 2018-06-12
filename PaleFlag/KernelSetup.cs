using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using HypervisorSharp;
using static System.Console;

namespace PaleFlag {
	class KernelAttribute : Attribute {
		public readonly int Number;

		public KernelAttribute(int number) => Number = number;
	}

	public interface IKernel {
	}
	
	public static class KernelSetup {
		public static readonly Dictionary<int, Action<CpuCore>> Functions = new Dictionary<int, Action<CpuCore>>();
		
		public static void Setup() {
			var asm = typeof(IKernel).Assembly;
			foreach(var type in asm.GetTypes()) {
				if(!type.GetInterfaces().Contains(typeof(IKernel)))
					continue;
				foreach(var method in type.GetMethods(BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public)) {
					var attr = method.GetCustomAttribute<KernelAttribute>();
					if(attr == null)
						continue;
					Functions[attr.Number] = MakeWrapper(method);
				}
			}
		}
	
		static Action<CpuCore> MakeWrapper(MethodInfo method) {
			var toStructure = typeof(Marshal).GetMethod("PtrToStructure", new[] { typeof(IntPtr) });
			var paramBuilders = method.GetParameters().Select<ParameterInfo, Func<CpuCore, object>>(param => {
				var spec = toStructure.MakeGenericMethod(param.ParameterType);
				Func<IntPtr, object> caller = ptr => spec.Invoke(null, new object[] { ptr });
				if(param.ParameterType.IsGenericType &&
				   param.ParameterType.GetGenericTypeDefinition() == typeof(GuestMemory<>))
					caller = ptr => 
						Activator.CreateInstance(typeof(GuestMemory<>).MakeGenericType(param.ParameterType.GenericTypeArguments[0]), 
							Marshal.PtrToStructure<uint>(ptr));

				var size = Extensions.SizeOf(param.ParameterType);
				return cpu => {
					var sp = cpu[HvReg.RSP];
					var data = PageManager.Instance.Read(sp, size);
					var handle = GCHandle.Alloc(data, GCHandleType.Pinned);
					var ret = caller(handle.AddrOfPinnedObject());
					handle.Free();
					cpu[HvReg.RSP] = sp + (uint) size;
					return ret;
				};
			}).ToArray();
			var numParams = paramBuilders.Length;
			var iparams = new object[numParams];
			if(method.ReturnType == typeof(void))
				return cpu => {
					var sp = cpu[HvReg.RSP];
					cpu[HvReg.RSP] = sp + 4;
					var retAddr = new GuestMemory<uint>(sp).Value;
					for(var i = 0; i < numParams; ++i)
						iparams[i] = paramBuilders[i](cpu);
					method.Invoke(null, iparams);
					cpu[HvReg.RIP] = retAddr;
				};
			else {
				var retType = method.ReturnType;
				var size = Extensions.SizeOf(retType);
				Debug.Assert(size == 4);
				var ptr = Marshal.AllocHGlobal(size);
				var arr = new byte[size];
				return cpu => {
					var sp = cpu[HvReg.RSP];
					cpu[HvReg.RSP] = sp + 4;
					var retAddr = new GuestMemory<uint>(sp).Value;
					for(var i = 0; i < numParams; ++i)
						iparams[i] = paramBuilders[i](cpu);
					var ret = method.Invoke(null, iparams);
					Marshal.StructureToPtr(ret.GetType().IsEnum ? Convert.ChangeType(ret, Enum.GetUnderlyingType(ret.GetType())) : ret, ptr, true);
					Marshal.Copy(ptr, arr, 0, size);
					cpu[HvReg.RAX] = BitConverter.ToUInt32(arr, 0);
					cpu[HvReg.RIP] = retAddr;
				};
			}
		}
	}
}